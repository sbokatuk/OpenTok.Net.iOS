namespace OpenTok.Net.iOS.PackageTests;

/// <summary>
/// Asserts the shape of the hand-written binding's public API, read out of the packed assembly.
/// </summary>
/// <remarks>
/// <para>
/// These are regression guards, not coverage. Every check here corresponds to something that was
/// actually wrong in a shipped version of this package and that the compiler cannot notice: a
/// binding with unusable enum member names, missing enum members, a constructor that traps, or a
/// delegate typed over the wrong class all build cleanly and pack cleanly. The failure only shows
/// up in a consuming app, at runtime.
/// </para>
/// <para>
/// They read the packed <c>.nupkg</c> rather than the build output, for the same reason the layout
/// tests do — what ships is what matters.
/// </para>
/// </remarks>
public class BindingApiTests
{
    /// <summary>
    /// The public API is identical across target frameworks (one source, three pack passes), so
    /// these read one rather than all three. That the passes agree is the layout suite's job.
    /// </summary>
    private const string ApiTargetFramework = "net8.0-ios18.0";

    private static AssemblyApi OpenBinding()
    {
        using var package = Packages.OpenPackage();
        var entry = package.GetEntry($"lib/{ApiTargetFramework}/{Packages.Id}.dll");
        Assert.True(entry is not null, $"package has no binding assembly for {ApiTargetFramework}.");

        var buffer = new MemoryStream();
        using (var stream = entry!.Open())
        {
            stream.CopyTo(buffer);
        }

        buffer.Position = 0;
        return new AssemblyApi(buffer);
    }

    [Fact]
    public void Scale_behaviour_members_are_named_after_the_header()
    {
        using var api = OpenBinding();

        // OTVideoKit.h declares OTVideoViewScaleBehaviorFit then OTVideoViewScaleBehaviorFill.
        // Objective Sharpie's prefix stripping once reduced those to "t" and "ll" — the values were
        // right and the names were unusable, which no compiler and no layout check would catch.
        Assert.Equal(
            [("Fit", 0L), ("Fill", 1L)],
            api.EnumMembersOf("OpenTok.Net.iOS.OTVideoViewScaleBehavior"));
    }

    [Fact]
    public void Video_event_reasons_cover_every_value_the_sdk_sends()
    {
        using var api = OpenBinding();

        // A missing member is silent: the delegate callback still fires and the parameter still
        // arrives, as an enum value with no name. These sets are transcribed from
        // OTVideoEventReason.h — note the gaps (no 2 or 4 on the publisher, no 5-7 on the
        // subscriber), which are the SDK's, not an omission here.
        Assert.Equal(
            [
                ("None", 0L),
                ("PublisherPropertyChanged", 1L),
                ("QualityChanged", 3L),
                ("QualityDegradationBandwidth", 5L),
                ("QualityDegradationCPU", 6L),
                ("QualityDegradationOther", 7L),
                ("CodecChange", 8L),
                ("ResolutionChange", 9L),
            ],
            api.EnumMembersOf("OpenTok.Net.iOS.OTPublisherVideoEventReason"));

        Assert.Equal(
            [
                ("None", 0L),
                ("PublisherPropertyChanged", 1L),
                ("SubscriberPropertyChanged", 2L),
                ("QualityChanged", 3L),
                ("CodecNotSupported", 4L),
                ("CodecChange", 8L),
                ("ResolutionChange", 9L),
                ("VideoInterruption", 10L),
            ],
            api.EnumMembersOf("OpenTok.Net.iOS.OTSubscriberVideoEventReason"));
    }

    [Fact]
    public void Types_whose_native_init_is_unavailable_have_no_default_constructor()
    {
        using var api = OpenBinding();

        // OTSession, OTPublisherKit and OTSubscriberKit all declare `- init NS_UNAVAILABLE`. Without
        // [DisableDefaultCtor] the generator emits a parameterless constructor anyway: `new
        // OTSession()` compiles, and then traps or hands back an unusable object at runtime.
        foreach (var type in new[]
        {
            "OpenTok.Net.iOS.OTSession",
            "OpenTok.Net.iOS.OTPublisherKit",
            "OpenTok.Net.iOS.OTSubscriberKit",
            "OpenTok.Net.iOS.OTPublisher",
            "OpenTok.Net.iOS.OTSubscriber",
        })
        {
            Assert.False(
                api.HasPublicDefaultConstructor(type),
                $"{type} exposes a parameterless constructor, but its native init is unavailable.");
        }
    }

    [Fact]
    public void Kit_base_types_are_bound_separately_from_their_view_backed_subclasses()
    {
        using var api = OpenBinding();

        // OTSubscriberKit was once folded into OTSubscriber, which made OTSession.Subscribe demand
        // the view-backed subclass — so a subscriber driving a custom OTVideoRender, which the SDK
        // constructs as a bare OTSubscriberKit, could not be passed to it at all.
        var types = api.PublicTypes;

        Assert.Contains("OpenTok.Net.iOS.OTSubscriberKit", types);
        Assert.Contains("OpenTok.Net.iOS.OTSubscriber", types);
        Assert.Contains("OpenTok.Net.iOS.OTPublisherKit", types);
        Assert.Contains("OpenTok.Net.iOS.OTPublisher", types);
    }

    [Fact]
    public void Types_added_by_the_2_34_sdk_are_bound()
    {
        using var api = OpenBinding();

        // Guards against an SDK bump that re-pins the native artifact without extending the
        // hand-written ApiDefinition to match — which is exactly how this binding came to describe
        // a 2.27-era API under a later version number.
        var types = api.PublicTypes;

        foreach (var type in new[]
        {
            "OpenTok.Net.iOS.OTVideoCodecPreference",
            "OpenTok.Net.iOS.OTTransportStats",
            "OpenTok.Net.iOS.OTPublisherKitMediaLinkStats",
            "OpenTok.Net.iOS.OTSubscriberKitMediaLinkStats",
            "OpenTok.Net.iOS.OTPublisherKitVideoLayerStats",
        })
        {
            Assert.Contains(type, types);
        }
    }

    [Fact]
    public void Publisher_error_codes_include_the_2_35_scalability_mode_value()
    {
        using var api = OpenBinding();

        // 2.35.1 added OTPublisherInvalidScalabilityMode = 6006 to OTError.h alongside the new
        // setTargetScalabilityMode:error:. Same failure shape as the video-event reasons: the SDK
        // reports the code and the enum has no name for it. Values and names follow the header
        // (minus the OT prefix, as with every member of this enum); the order is the binding's.
        Assert.Equal(
            [
                ("PublisherSuccess", 0L),
                ("SessionDisconnected", 1010L),
                ("PublisherWebRTCError", 1610L),
                ("PublisherInternalError", 2000L),
                ("PublisherEncryptionInternalError", 6001L),
                ("EncryptionUnsupportedCodec", 6005L),
                ("PublisherInvalidScalabilityMode", 6006L),
            ],
            api.EnumMembersOf("OpenTok.Net.iOS.OTPublisherErrorCode"));
    }
}
