using Microsoft.Extensions.Logging;

namespace OpenTok.Sample.iOS;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            // Registers the handler for OpenTokVideoView. MAUI does not discover handlers on its
            // own, so without this the view renders nothing. This sample shows everything a
            // consumer of the bare binding has to wire up themselves — a cross-platform façade
            // would normally hide this registration behind its own extension method.
            .ConfigureMauiHandlers(handlers =>
                handlers.AddHandler<OpenTokVideoView, OpenTokVideoViewHandler>());

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
