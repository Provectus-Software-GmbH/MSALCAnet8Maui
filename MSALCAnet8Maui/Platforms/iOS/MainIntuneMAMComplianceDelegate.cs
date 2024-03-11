using Microsoft.Intune.MAM;

namespace MSALCAnet8Maui.Platforms.iOS;

public class MainIntuneMAMComplianceDelegate : IntuneMAMComplianceDelegate
{
    private readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly ManualResetEvent _manualReset;
    public MainIntuneMAMComplianceDelegate(ManualResetEvent manualReset)
    {
        _manualReset = manualReset;
        _manualReset.Reset();
    }

    public override void IdentityHasComplianceStatus(string identity, IntuneMAMComplianceStatus status, string errorMessage, string errorTitle)
    {
        Logger.Debug("IntuneMAMComplianceDelegate invoked");                        
        Logger.Debug($"MAM: {status} - {errorTitle} - {errorMessage}");
         
        if (status == IntuneMAMComplianceStatus.Compliant)
        {
            try
            {
                // Now the app is compliant, set the event. It will notify the App to take the next steps.
                _manualReset.Set();
            }
            catch (Exception ex)
            {
                Logger.Debug($"Ex = {ex.Message}");

            }
        }
    }
}
