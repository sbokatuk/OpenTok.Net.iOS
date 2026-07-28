using System.Runtime.InteropServices;
using ObjCRuntime;
using OpenTok.Net.iOS;

namespace OpenTok.Net.iOS.DeviceTests;

/// <summary>A single on-simulator check. Throws to fail.</summary>
/// <param name="Name">Human readable name, reported to stdout.</param>
/// <param name="Execute">Runs the check.</param>
public sealed record SmokeTest(string Name, Action Execute);

/// <summary>
/// End-to-end checks that only mean anything on a real device or simulator: they load the native
/// OpenTok.xcframework out of the packaged .nupkg and drive the raw binding — <see cref="OTSession"/>,
/// <see cref="OTPublisher"/> and <see cref="OTSubscriber"/> themselves, with no cross-platform
/// façade in between. This suite exists so a packaging regression in *this* repository is caught
/// where the package is produced, not in a downstream consumer.
/// </summary>
/// <remarks>
/// Nothing here touches the network. The API key and session ID are the shape of real OpenTok
/// identifiers but are not registered with Vonage — no connect is ever attempted, so there is
/// nothing to leak if CI logs are public. What this suite proves is that the packed package's
/// native payload loads, its Objective-C classes resolve, and session/publisher/subscriber
/// objects round-trip construct → drive → release.
/// <para>
/// The checks are ordered: a session has to exist before a publisher can be created against it,
/// and a failure early on cascades — which is the intent, since the first failure is the
/// informative one.
/// </para>
/// </remarks>
public static class SmokeTests
{
    // The shape of a real OpenTok API key (a numeric project ID) and session ID (base64url-ish,
    // prefixed with a version digit and an underscore), unregistered on purpose.
    private const string ApiKey = "45678123";
    private const string SessionId = "1_MX40NTY3ODEyM35-MTcwMDAwMDAwMDAwMH4wLjEzNDU2Nzg5MH4";

    /// <summary>Writes a line to stdout under the current check. Set by the app delegate.</summary>
    public static Action<string> Reporter { get; set; } = _ => { };

    private static OTSession? _session;
    private static SessionDelegate? _sessionDelegate;
    private static OTPublisher? _publisher;
    private static PublisherDelegate? _publisherDelegate;

    /// <summary>Every check, in the order they must run.</summary>
    public static SmokeTest[] All =>
    [
        new("every native framework is linked and loadable", EveryFrameworkIsLinked),
        new("OpenTok's Objective-C classes resolve", NativeClassesResolve),
        new("constructs a session with an unregistered API key", ConstructsTheSession),
        new("reads session state without connecting", ReadsSessionStateWithoutConnecting),
        new("constructs a publisher with default settings", ConstructsAPublisher),
        new("disconnect without a connect is reported, not thrown", DisconnectWithoutAConnectIsReported),
        new("releases the session and publisher", ReleasesEverything),
    ];

    private static void Report(string message) => Reporter(message);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern uint _dyld_image_count();

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern IntPtr _dyld_get_image_name(uint index);

    /// <summary>
    /// Proves OpenTok.xcframework's binary actually made it into the app and was loaded.
    /// </summary>
    /// <remarks>
    /// This is the check that catches a packaging regression the compiler cannot see: a static
    /// archive that failed to embed still compiles and links against the binding's managed
    /// surface (the ObjC selectors are only resolved at first use), and would only fail the moment
    /// a bound type is actually touched — which is exactly what the checks after this one do.
    /// </remarks>
    private static void EveryFrameworkIsLinked()
    {
        var images = new List<string>();
        var count = _dyld_image_count();
        for (uint i = 0; i < count; i++)
        {
            var name = Marshal.PtrToStringUTF8(_dyld_get_image_name(i));
            if (name is not null)
            {
                images.Add(name);
            }
        }

        // Unlike the sibling Net.Agora.iOS repository's *dynamic* frameworks, a statically linked
        // archive does not appear as its own dyld image — its code is folded into this app's own
        // executable. So the executable image itself is the thing to find here.
        var executableName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        var loaded = images.Any(image => image.Contains("/DeviceTests", StringComparison.Ordinal)
            || (executableName is not null && image.EndsWith($"/{executableName}", StringComparison.Ordinal)));

        Assert(loaded, "the app's own executable was not found among loaded dyld images.");
        Report($"{images.Count} images loaded into the process");
    }

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr object_getClass(IntPtr obj);

    /// <summary>
    /// Confirms OTSession, OTPublisher and OTSubscriber all resolve through the Objective-C
    /// runtime — the same proof the sibling Net.Agora.iOS repository's <c>getSdkVersion</c> check
    /// gives, but OpenTok's public headers expose no static version accessor to call instead.
    /// </summary>
    private static void NativeClassesResolve()
    {
        foreach (var name in new[] { "OTSession", "OTPublisher", "OTSubscriber", "OTPublisherKit", "OTSubscriberKit" })
        {
            var cls = Class.GetHandle(name);
            Assert(cls != IntPtr.Zero, $"{name} did not resolve to a native class.");
        }

        Report("OTSession, OTPublisher and OTSubscriber all resolved");
    }

    private static void ConstructsTheSession()
    {
        _sessionDelegate = new SessionDelegate();
        _session = new OTSession(ApiKey, SessionId, _sessionDelegate);

        Assert(_session is not null, "the OTSession constructor returned null.");
        Assert(_session!.Handle != NativeHandle.Zero, "OTSession has no native handle.");

        Report("session constructed (not connected — every later check drives it unconnected)");
    }

    private static OTSession Session =>
        _session ?? throw new InvalidOperationException("the session has not been constructed yet.");

    private static void ReadsSessionStateWithoutConnecting()
    {
        // Local state on an object that has never connected — no signalling call is involved.
        var status = Session.SessionConnectionStatus;
        Assert(
            status == OTSessionConnectionStatus.NotConnected,
            $"a freshly constructed session reported '{status}', not NotConnected.");

        Assert(Session.SessionId == SessionId, "SessionId did not round-trip through the constructor.");

        Report($"sessionConnectionStatus == {status}, sessionId round-tripped");
    }

    private static void ConstructsAPublisher()
    {
        // OTPublisherSettings.Name identifies the stream; audio/video tracks default to true in
        // the SDK, which is exactly what a publisher needs to be a meaningful smoke check without
        // ever calling Publish — construction alone is what proves the class's initializer and its
        // AVFoundation capture plumbing link correctly.
        var settings = new OTPublisherSettings { Name = "opentok-net-ios-device-tests" };
        _publisherDelegate = new PublisherDelegate();
        _publisher = new OTPublisher(_publisherDelegate, settings);

        Assert(_publisher is not null, "the OTPublisher constructor returned null.");
        Assert(_publisher!.Handle != NativeHandle.Zero, "OTPublisher has no native handle.");

        Report("publisher constructed (never published — no session:publish:error: call is made)");
    }

    private static void DisconnectWithoutAConnectIsReported()
    {
        // No connect ever happens in this suite, so the value is the SDK's business — what matters
        // is that the call crosses the bridge and comes back as an out OTError rather than a crash.
        Session.Disconnect(out var error);

        Report(error is null
            ? "disconnect returned no error"
            : $"disconnect returned an error: {error.Code} {error.LocalizedDescription}");
    }

    private static void ReleasesEverything()
    {
        _publisher = null;
        _publisherDelegate = null;
        _session = null;
        _sessionDelegate = null;

        Report("released");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// A subclass of the bound (all-required) session delegate protocol. Its existence at
    /// construction is the assertion; no callback fires without a connect, which this suite never
    /// attempts.
    /// </summary>
    private sealed class SessionDelegate : OTSessionDelegate
    {
        public override void DidConnect(OTSession session) => SmokeTests.Reporter("delegate: connected");
        public override void DidDisconnect(OTSession session) => SmokeTests.Reporter("delegate: disconnected");
        public override void DidFailWithError(OTSession session, OTError error) =>
            SmokeTests.Reporter($"delegate: error {error.Code}");
        public override void StreamCreated(OTSession session, OTStream stream) =>
            SmokeTests.Reporter($"delegate: stream created {stream.StreamId}");
        public override void StreamDestroyed(OTSession session, OTStream stream) =>
            SmokeTests.Reporter($"delegate: stream destroyed {stream.StreamId}");
    }

    /// <summary>
    /// A subclass of the bound publisher delegate protocol. The parameter is
    /// <see cref="OTPublisherKit"/>, matching the header — see the note on the same delegate in
    /// samples/OpenTok.Sample.iOS/MainPage.xaml.cs for why the concrete subclass would be wrong.
    /// </summary>
    private sealed class PublisherDelegate : OTPublisherKitDelegate
    {
        public override void DidFailWithError(OTPublisherKit publisher, OTError error) =>
            SmokeTests.Reporter($"delegate: publisher error {error.Code}");
    }
}
