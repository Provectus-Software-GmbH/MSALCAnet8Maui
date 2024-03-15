using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Identity.Client;
using Microsoft.Intune.MAM;
using MSALCAnet8Maui.Platforms.iOS;

namespace MSALCAnet8Maui;

public partial class MainPage : ContentPage
{
    private readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

    private static IPublicClientApplication PCA { get; set; }
    MainIntuneMAMComplianceDelegate _mamComplianceDelegate;
    ManualResetEvent _manualReset;

    // Please configure following parameters as per your app registration and bundleID
    // AND ALSO update corresponding entries in
    // Platforms/iOS/Entitlements.plist:
    // > Keychain Access Groups -> $(AppIdentifierPrefix){bundleID}
    // Platforms/iOS/Info.plist:
    // > IntuneMAMSettings -> ADALClientID -> {clientId}
    // > IntuneMAMSettings -> ADALRedirectUri -> msauth.{bundleID}://auth
    // > URL types -> URL identifier -> {bundleID}
    // > URL types -> URL Schemes -> msauth.{bundleID}
    // > URL types -> URL Schemes -> msauth.{bundleID}-intunemam
    // MSALCAnet8Maui.csproj:
    // > ApplicationId -> {bundleID}

    private const string clientId = "e9cd7eb8-86b6-4f55-8af8-eb83e3870f3f";
    private const string redirectURI = "msauth.com.company.mauiauthtestapp://auth";
    // private const string tenantID = "e112e635-31f3-43f7-b8fa-bd8e8c625076";

    // Single-Tenant-Apps should use authority -> $"https://login.microsoftonline.com/{tenantID}/"
    // Multi-Tenant-Apps should use authority -> "https://login.microsoftonline.com/organizations/"
    private string authority = "https://login.microsoftonline.com/organizations/";

    private string[] Scopes = { "https://graph.microsoft.com/.default" };    
    private string[] clientCapabilities = { "ProtApp" };
    // ClientCapabilities - must have ProtApp

    public MainPage()
    {
        InitializeComponent();

        _manualReset = new ManualResetEvent(false);
        _mamComplianceDelegate = new MainIntuneMAMComplianceDelegate(_manualReset);
        IntuneMAMComplianceManager.Instance.Delegate = _mamComplianceDelegate;


        if (PCA != null) return;

        var pcaBuilder = PublicClientApplicationBuilder.Create(clientId)
            .WithRedirectUri(redirectURI)
            .WithIosKeychainSecurityGroup("com.microsoft.adalcache")
            .WithAuthority(authority)
            .WithClientCapabilities(clientCapabilities)
            //.WithLogging(MSALLogCallback, LogLevel.Verbose)
            //.WithHttpClientFactory(new HttpSnifferClientFactory())
            .WithBroker(true);

        PCA = pcaBuilder.Build();        
    }

    // MSAL
    private async Task SignInAndEnrollAccountIntune()
    {
        Logger.Debug("SignInAndEnrollAccountIntune");
        try
        {
            // attempt silent login.
            // If this is very first time and the device is not enrolled, it will throw MsalUiRequiredException
            // If the device is enrolled, this will succeed.
            var authResult = await DoSilentAsync(Scopes).ConfigureAwait(false);
            Logger.Debug("Success Silent", authResult.AccessToken);
            ShowToast("Success Silent");
        }
        catch (MsalUiRequiredException)
        {
            Logger.Debug("MsalUiRequiredException thrown");
            // This executes UI interaction
            try
            {
                var interParamBuilder = PCA.AcquireTokenInteractive(Scopes)
                    .WithParentActivityOrWindow(Platform.GetCurrentUIViewController())
                    .WithUseEmbeddedWebView(true);
               
                var authResult = await interParamBuilder.ExecuteAsync().ConfigureAwait(false);
                Logger.Debug("Success Interactive", authResult.AccessToken);
                ShowToast("Success Interactive");
            }

            catch (IntuneAppProtectionPolicyRequiredException ex)
            {
                Logger.Debug("IntuneAppProtectionPolicyRequiredException thrown");
                // Workarround for sporadic Authenticator > MSAL Domain Error 5000 (retry > success)
                // Currently on MSAL 4.59 & Intune SDK 19.1.0                
                Thread.Sleep(300);

                // if the scope requires App Protection Policy, IntuneAppProtectionPolicyRequiredException is thrown.
                // To ensure that the policy is applied before the next call, reset the manualResetEvent
                _manualReset.Reset();

                // Using IntuneMAMComplianceManager, ensure that the device is compliant.
                // This will raise UI for compliance. After user satisfies the compliance requirements,
                // MainIntuneMAMComplianceDelegate method will be called.
                // the delegate will set the manualResetEvent
                Logger.Debug($"RemediateComplianceForIdentity for {ex.Upn}");
                IntuneMAMComplianceManager.Instance.RemediateComplianceForIdentity(ex.Upn, false);

                // wait for the delegate to set it.
                Logger.Debug("Wait for IntuneMAMComplianceDelegate");
                _manualReset.WaitOne();

                // now the device is compliant
                Logger.Debug("MAM Compliant");
                ShowToast("MAM Compliant");

                // Attempt silent acquisition again.
                // this should succeed now
                var authResult = await DoSilentAsync(Scopes).ConfigureAwait(false);
                Logger.Debug("Success Silent MAM", authResult.AccessToken);
                ShowToast("Success Silent MAM");
            }
            catch (Exception ex)
            {                
                Logger.Debug(ex.Message);
                Logger.Debug("User canceled authentication");
                ShowToast("Authentication canceled");
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"Error: {ex.Message}");
            ShowToast($"Error: {ex.Message}");
        }
    }

    private async Task SignOutAndRemoveFromCache()
    {
        var account = await FetchUserAccountFromCache();
        if (account != null)
        {
            await PCA.RemoveAsync(account);
            Logger.Debug("User account has been removed");
            ShowToast("Account removed\n" + account.Username);
        }
    }

    private async Task<IAccount> FetchUserAccountFromCache()
    {
        var accounts = await PCA.GetAccountsAsync();
        if (accounts.Count() > 1)
        {
            foreach (var account in accounts)
                await PCA.RemoveAsync(account);

            return null;
        }
        return accounts.FirstOrDefault();
    }

    private async Task<AuthenticationResult> DoSilentAsync(string[] Scopes)
    {
        var accts = await PCA.GetAccountsAsync().ConfigureAwait(false);
        var acct = accts.FirstOrDefault();
        if (acct != null)
        {
            var silentParamBuilder = PCA.AcquireTokenSilent(Scopes, acct);
            var authResult = await silentParamBuilder.ExecuteAsync().ConfigureAwait(false);
            return authResult;
        }
        else
        {
            throw new MsalUiRequiredException("ErrCode", "ErrMessage");
        }
    }

    private void MSALLogCallback(LogLevel level, string message, bool containsPii)
    {
        Logger.Debug(message);
    }

    // INTUNE
    private string GetEnrolledAccountIntune()
    {
        string UPN = IntuneMAMEnrollmentManager.Instance.EnrolledAccount;
        if (string.IsNullOrEmpty(UPN)) return "";
        return UPN;
    }

    private bool IsAccountEnrolledAndManagedIntune()
    {
        var UPN = GetEnrolledAccountIntune();
        if (string.IsNullOrEmpty(UPN)) return false;

        return IntuneMAMPolicyManager.Instance.IsIdentityManaged(UPN);
    }

    private void UnEnrollIntune()
    {
        var UPN = GetEnrolledAccountIntune();
        if (string.IsNullOrEmpty(UPN)) return;

        // IntuneMAMEnrollmentManager.Instance.DeRegisterAndUnenrollAccoun( UPN , doWipe )
        // If the app will delete the user's corporate data on its own, the doWipe flag can be set to false.
        // Otherwise, the app can have the SDK initiate a selective wipe.
        // This will result in a call to the app's selective wipe delegate.

        // Unenroll and doWipe
        IntuneMAMEnrollmentManager.Instance.DeRegisterAndUnenrollAccount(UPN, true);
    }
    
    // Button Click Handler
    private async void Button_AcquireToken(object sender, EventArgs e)
    {
        await SignInAndEnrollAccountIntune();
    }

    private async void Button_SignOutRemoveAcc(object sender, EventArgs e)
    {
        bool result = await DisplayAlert("Sign out now", "Are you sure?", "Yes", "No");
        if (!result) return;

        // clear MSAL
        await SignOutAndRemoveFromCache();

        // unenroll from Intune
        UnEnrollIntune();
    }

    private async void Button_TokenStatus(object sender, EventArgs e)
    {
        try
        {
            var authResult = await DoSilentAsync(Scopes).ConfigureAwait(false);
            if (authResult != null && authResult.AccessToken != null)
            {
                // MSAL Status:
                var sText = $"MSAL: Accesstoken OK\n{authResult.Account.Username}\n\n";

                // MAM Status:
                if (IsAccountEnrolledAndManagedIntune())                
                    sText += $"Intune MAM: managed & compliant\n{GetEnrolledAccountIntune()}";
                else
                    sText += $"Intune MAM: unmanaged";                
                
                ShowToast(sText);
            }                
        }
        catch
        {
            ShowToast("NO accesstoken");
        }
    }        

    private async void Button_ViewLogs(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LogViewPage(), false);
    }

    private void Button_IntuneDianosticConsole(object sender, EventArgs e)
    {
        IntuneMAMDiagnosticConsole.DisplayDiagnosticConsole();
    }

    private async void Button_ShareIntuneLogs(object sender, EventArgs e)
    {
        await ShareLogfiles();
    }

    // MISC    
    CancellationTokenSource ToastCancelTokenSource = new CancellationTokenSource();
    private void ShowToast(string ToastText)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var toast = Toast.Make(ToastText, ToastDuration.Long, 16);
            await toast.Show(ToastCancelTokenSource.Token);
        });
    }

    private async Task ShareLogfiles()
    {
        string BinaryType = "release";
#if DEBUG
        BinaryType = "debug";
#endif

        string FileName = string.Format("IntuneLog.{0}.txt", BinaryType);

        var IntuneLogs = GetIntuneDiagnosticLogs();
        if (string.IsNullOrEmpty(IntuneLogs)) return;


        string LogsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "../Library", FileName);
        File.WriteAllText(LogsPath, IntuneLogs);

        // add current log file
        List<ShareFile> LogfileToShare = new()
        {
            new ShareFile(LogsPath)
        };

        // share logs
        await Share.RequestAsync(new ShareMultipleFilesRequest
        {
            Title = "Intune Logs",
            Files = LogfileToShare
        });
    }

    private string GetIntuneDiagnosticLogs()
    {
        var IntuneLogs = IntuneMAMDiagnosticConsole.DiagnosticInformation;
        var sb = new StringBuilder();

        foreach (var LogItem in IntuneLogs)
        {
            Console.WriteLine($"KEY: {LogItem.Key} -> VALUE: {LogItem.Value}");

            sb.Append(LogItem.Key.ToString());
            sb.Append(LogItem.Value.ToString());
            sb.AppendLine();
        }

        return sb.ToString();
    }

}