namespace MSALCAnet8Maui;

public partial class App : Application
{
    private readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

    public App()
	{
		InitializeComponent();

		MainPage = new AppShell();
        
#if DEBUG
        Logger.Debug("Debug Binary started");
#else
        Logger.Debug("Release Binary started");
#endif

    }
}

