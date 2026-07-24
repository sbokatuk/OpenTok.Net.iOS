using System.Text;
using OpenTok.Net.iOS;
using UIKit;

namespace OpenTok.Sample.iOS;

/// <summary>
/// Connects to an OpenTok session, publishing this device's camera/microphone and subscribing to
/// the first remote stream — built directly against <see cref="OTSession"/>/<see cref="OTPublisher"/>/
/// <see cref="OTSubscriber"/> rather than a cross-platform façade, since this repository binds only
/// the raw iOS SDK. Every call answers an <see cref="OTError"/> out parameter, null for success.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly StringBuilder _status = new();

    private OTSession? _session;
    private SessionDelegate? _sessionDelegate;
    private OTPublisher? _publisher;
    private PublisherDelegate? _publisherDelegate;
    private OTSubscriber? _subscriber;
    private SubscriberDelegate? _subscriberDelegate;

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnConnectClicked(object? sender, EventArgs e)
    {
        var apiKey = ApiKeyEntry.Text?.Trim();
        var sessionId = SessionIdEntry.Text?.Trim();
        var token = TokenEntry.Text?.Trim();

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(token))
        {
            Append("enter an API key, a session id and a token first");
            return;
        }

        if (!await RequestCapturePermissionsAsync())
        {
            Append("camera and microphone permission denied");
            return;
        }

        _sessionDelegate = new SessionDelegate(this);
        var session = new OTSession(apiKey, sessionId, _sessionDelegate);
        session.ConnectWithToken(token, out var error);

        if (error is not null)
        {
            Append($"connectWithToken failed: {error.Code} {error.LocalizedDescription}");
            return;
        }

        _session = session;
        ConnectButton.IsEnabled = false;
        Append("connecting…");
    }

    private void OnDisconnectClicked(object? sender, EventArgs e)
    {
        TeardownPublisher();
        TeardownSubscriber();

        if (_session is not null)
        {
            _session.Disconnect(out var error);
            if (error is not null)
            {
                Append($"disconnect returned: {error.Code} {error.LocalizedDescription}");
            }
        }

        _session = null;
        _sessionDelegate = null;
        SetConnected(false);
        Append("disconnected");
    }

    private void OnPublishClicked(object? sender, EventArgs e)
    {
        if (_session is null)
        {
            return;
        }

        var settings = new OTPublisherSettings { Name = "opentok-net-ios-sample" };
        _publisherDelegate = new PublisherDelegate(this);
        var publisher = new OTPublisher(_publisherDelegate, settings);

        // OTPublisher.View exists as soon as the publisher does — the camera preview starts
        // rendering into it immediately, before session:publish:error: is ever called. Attaching
        // it here rather than waiting for a delegate callback is what every OpenTok sample does.
        AttachNativeView(publisher.View, LocalView.PlatformView);

        _session.Publish(publisher, out var error);
        if (error is not null)
        {
            Append($"publish failed: {error.Code} {error.LocalizedDescription}");
            publisher.View?.RemoveFromSuperview();
            return;
        }

        _publisher = publisher;
        PublishButton.IsEnabled = false;
        Append("publishing");
    }

    private static async Task<bool> RequestCapturePermissionsAsync()
    {
        var camera = await Permissions.RequestAsync<Permissions.Camera>();
        var microphone = await Permissions.RequestAsync<Permissions.Microphone>();

        return camera == PermissionStatus.Granted && microphone == PermissionStatus.Granted;
    }

    private void SetConnected(bool connected)
    {
        ConnectButton.IsEnabled = !connected;
        DisconnectButton.IsEnabled = connected;
        PublishButton.IsEnabled = connected;
    }

    /// <summary>
    /// Adds an SDK-owned render view (publisher or subscriber) as a subview of one of this page's
    /// <see cref="OpenTokVideoView"/> containers, sized to fill it. Both views are SDK-created and
    /// read-only — see the type comment on <see cref="OpenTokVideoView"/> for why this is the
    /// reverse of Agora's canvas-based wiring.
    /// </summary>
    private static void AttachNativeView(UIView? nativeView, UIView? container)
    {
        if (nativeView is null || container is null)
        {
            return;
        }

        nativeView.Frame = container.Bounds;
        nativeView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        container.AddSubview(nativeView);
    }

    private void TeardownPublisher()
    {
        if (_publisher is not null && _session is not null)
        {
            _session.Unpublish(_publisher, out _);
        }

        _publisher?.View?.RemoveFromSuperview();
        _publisher = null;
        _publisherDelegate = null;
        PublishButton.IsEnabled = false;
    }

    private void TeardownSubscriber()
    {
        if (_subscriber is not null && _session is not null)
        {
            _session.Unsubscribe(_subscriber, out _);
        }

        _subscriber?.View?.RemoveFromSuperview();
        _subscriber = null;
        _subscriberDelegate = null;
    }

    private void Append(string message) => MainThread.BeginInvokeOnMainThread(() =>
    {
        _status.AppendLine($"{DateTime.Now:HH:mm:ss}  {message}");
        StatusLabel.Text = _status.ToString();
        StatusScroll.ScrollToAsync(0, StatusLabel.Height, animated: false);
    });

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        // Releases the session and its native views when the page goes away; ARC alone does not
        // tell the SDK the session is done with.
        if (Handler is null && _session is not null)
        {
            TeardownPublisher();
            TeardownSubscriber();
            _session.Disconnect(out _);
            _session = null;
            _sessionDelegate = null;
        }
    }

    /// <summary>
    /// Translates <c>OTSessionDelegate</c>'s (all-required) callbacks into UI updates and drives
    /// the subscribe side: the first remote stream this session reports gets subscribed to and
    /// rendered into <see cref="RemoteView"/>.
    /// </summary>
    private sealed class SessionDelegate(MainPage owner) : OTSessionDelegate
    {
        public override void DidConnect(OTSession session) => MainThread.BeginInvokeOnMainThread(() =>
        {
            owner.SetConnected(true);
            owner.Append("connected");
        });

        public override void DidDisconnect(OTSession session) => MainThread.BeginInvokeOnMainThread(() =>
        {
            owner.SetConnected(false);
            owner.Append("session disconnected");
        });

        public override void DidFailWithError(OTSession session, OTError error) =>
            owner.Append($"session error: {error.Code} {error.LocalizedDescription}");

        public override void StreamCreated(OTSession session, OTStream stream) =>
            MainThread.BeginInvokeOnMainThread(() =>
            {
                owner.Append($"remote stream created: {stream.StreamId}");

                // Only one remote view in this sample — the first stream gets it.
                if (owner._subscriber is not null)
                {
                    return;
                }

                owner._subscriberDelegate = new SubscriberDelegate(owner);
                var subscriber = new OTSubscriber(stream, owner._subscriberDelegate);
                session.Subscribe(subscriber, out var error);

                if (error is not null)
                {
                    owner.Append($"subscribe failed: {error.Code} {error.LocalizedDescription}");
                    return;
                }

                owner._subscriber = subscriber;
            });

        public override void StreamDestroyed(OTSession session, OTStream stream) =>
            MainThread.BeginInvokeOnMainThread(() =>
            {
                owner.Append($"remote stream destroyed: {stream.StreamId}");
                if (owner._subscriber?.Stream?.StreamId == stream.StreamId)
                {
                    owner.TeardownSubscriber();
                }
            });
    }

    /// <summary>Translates <c>OTPublisherKitDelegate</c>'s callbacks into UI updates.</summary>
    private sealed class PublisherDelegate(MainPage owner) : OTPublisherKitDelegate
    {
        public override void DidFailWithError(OTPublisher publisher, OTError error) =>
            owner.Append($"publisher error: {error.Code} {error.LocalizedDescription}");

        public override void StreamCreated(OTPublisher publisher, OTStream stream) =>
            owner.Append("local stream published");
    }

    /// <summary>
    /// Translates <c>OTSubscriberKitDelegate</c>'s callbacks into UI updates. The subscriber's
    /// render view only becomes valid once <see cref="DidConnectToStream"/> fires — attaching it
    /// any earlier would add nothing to the layout, unlike the publisher's view, which exists from
    /// construction.
    /// </summary>
    private sealed class SubscriberDelegate(MainPage owner) : OTSubscriberKitDelegate
    {
        public override void DidConnectToStream(OTSubscriber subscriber) => MainThread.BeginInvokeOnMainThread(() =>
        {
            AttachNativeView(subscriber.View, owner.RemoteView.PlatformView);
            owner.Append("subscribed — remote video attached");
        });

        public override void DidFailWithError(OTSubscriber subscriber, OTError error) =>
            owner.Append($"subscriber error: {error.Code} {error.LocalizedDescription}");
    }
}
