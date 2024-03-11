using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog;

namespace MSALCAnet8Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
        LogManager.Setup().RegisterMauiLog()
               .LoadConfigurationFromAssemblyResource(typeof(App).Assembly);

        var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()            
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif
		// use Nlog
		builder.Logging.ClearProviders();
		builder.Logging.AddNLog();

        return builder.Build();
	}
}

