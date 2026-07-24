using Microsoft.Maui.Handlers;
using UIKit;

namespace OpenTok.Sample.iOS;

/// <summary>
/// A MAUI view that hosts an OpenTok video surface. Unlike Agora's <c>SetupLocalVideo</c>/
/// <c>SetupRemoteVideo</c> (which take a plain <c>UIView</c> and add their own renderer as a
/// subview of it), OpenTok's <c>OTPublisher.View</c>/<c>OTSubscriber.View</c> are *read-only*
/// properties: the SDK creates the render view itself once the publisher/subscriber exists, and
/// the app's job is to attach that view into its own layout — see
/// <see cref="MainPage.AttachNativeView"/>. This view exists to give MAUI's layout a plain native
/// container to attach that SDK-owned view into, sized by MAUI like any other view.
/// </summary>
public class OpenTokVideoView : View
{
    /// <summary>The native container this view renders as, once the handler has been created.</summary>
    public UIView? PlatformView => Handler?.PlatformView as UIView;
}

/// <summary>Maps <see cref="OpenTokVideoView"/> to a bare black <c>UIView</c>.</summary>
public partial class OpenTokVideoViewHandler : ViewHandler<OpenTokVideoView, UIView>
{
    /// <summary>The view has no properties of its own; the mapper exists because a handler needs one.</summary>
    public static readonly IPropertyMapper<OpenTokVideoView, OpenTokVideoViewHandler> VideoMapper =
        new PropertyMapper<OpenTokVideoView, OpenTokVideoViewHandler>(ViewMapper);

    /// <summary>Creates the handler. Registered in <see cref="MauiProgram"/>.</summary>
    public OpenTokVideoViewHandler() : base(VideoMapper)
    {
    }

    /// <inheritdoc />
    protected override UIView CreatePlatformView() => new() { BackgroundColor = UIColor.Black, ClipsToBounds = true };
}
