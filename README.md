# Introduction 
.net8 Maui iOS - MSAL Conditional Access require AppProtectionPolicy POC App

# Requirements
- Apple Developer Account, AppID, Provisioning Profile & Signing Certificates
- Azure/Entra AppRegistration / EnterpriseApp with given consent
- Azure/Entra UserAccount with an assigned License including Intune usage
- Intune configured Conditional Access and AppProtectionPolicy
- iPhone having MS Authenticator and MS Companyportal installed
- iPhone should be MAM registered via Companyportal

# Getting Started
Update settings as per your app registration and bundleID in MainPage.xaml.cs 
AND ALSO update corresponding entries in

Platforms/iOS/Entitlements.plist:
- Keychain Access Groups -> $(AppIdentifierPrefix){bundleID}

Platforms/iOS/Info.plist:
- IntuneMAMSettings -> ADALClientID -> {clientId}
- IntuneMAMSettings -> ADALRedirectUri -> msauth.{bundleID}://auth
- URL types -> URL identifier -> {bundleID}
- URL types -> URL Schemes -> msauth.{bundleID}-intunemam
- URL types -> URL Schemes -> msauth.{bundleID}

# Helpful Resources:
Get started with the Microsoft Intune App SDK:
https://learn.microsoft.com/en-us/mem/intune/developer/app-sdk-get-started

MSAL prerequisite and setup:
https://learn.microsoft.com/en-us/mem/intune/developer/app-sdk-ios-phase2

Intune SDK integration into your iOS app:
https://learn.microsoft.com/en-us/mem/intune/developer/app-sdk-ios-phase3

App Protection CA support:
https://learn.microsoft.com/en-us/mem/intune/developer/app-sdk-ios-phase6
