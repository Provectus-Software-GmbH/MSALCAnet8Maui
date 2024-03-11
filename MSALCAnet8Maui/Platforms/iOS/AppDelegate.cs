using Foundation;
using Microsoft.Identity.Client;
using UIKit;

namespace MSALCAnet8Maui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

    public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
    {
        Logger.Debug($"OpenUrl invoked {url}");

        if (AuthenticationContinuationHelper.IsBrokerResponse(null))
        {
            // Done on different thread to allow return in no time.
            _ = Task.Factory.StartNew(() => AuthenticationContinuationHelper.SetBrokerContinuationEventArgs(url));            
            return true;
        }
        else if (!AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(url))
        {            
            return false;
        }        
        return true;
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

