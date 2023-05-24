using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Distribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using NuSocial.Core;
using NuSocial.Localization;

namespace NuSocial;

public partial class App : AppBase
{
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<NuSocialResource> _localizer;

    public App(IServiceProvider serviceProvider, AppShell shell, IConfiguration configuration, IStringLocalizer<NuSocialResource> localizer) : base(serviceProvider)
    {
		InitializeComponent();

        MauiExceptions.UnhandledException += MauiExceptions_UnhandledException; ;

        MainPage = shell;
        _configuration = configuration;
        _localizer = localizer;
    }

    private void MauiExceptions_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e != null && e.ExceptionObject != null && e.ExceptionObject is Exception ex)
            Debug.WriteLine($"********************************** UNHANDLED EXCEPTION! Details: {ex.ToStringDemystified()}");
    }

    protected override async void OnStart()
    {
        base.OnStart();

        StartAppCenter();

        var nav = Ioc.Default.GetRequiredService<INavigationService>();
        await nav.Initialize();

        OnResume();
    }

    public const string LogTag = "NuSocial";

    bool ShouldProcess(ErrorReport report)
    {
        AppCenterLog.Info(LogTag, "Determining whether to process error report");
        return true;

    }

    bool ConfirmationHandler()
    {

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            string answer;
            if (DeviceInfo.Platform == DevicePlatform.macOS)
            {
                answer = await Current.MainPage.DisplayActionSheet("Crash detected. Send anonymous crash report?", _localizer["Send"], _localizer["AlwaysSend"]);
            }
            else
            {
                answer = await Current.MainPage.DisplayActionSheet("Crash detected. Send anonymous crash report?", null, null, _localizer["Send"], _localizer["AlwaysSend"], _localizer["DontSend"]);
            }

            UserConfirmation userConfirmationSelection;
            if (answer == _localizer["Send"])
            {
                userConfirmationSelection = UserConfirmation.Send;
            }
            else if (answer == _localizer["AlwaysSend"])
            {
                userConfirmationSelection = UserConfirmation.AlwaysSend;
            }
            else
            {
                userConfirmationSelection = UserConfirmation.DontSend;
            }
            AppCenterLog.Debug(LogTag, "User selected confirmation option: \"" + answer + "\"");
            Crashes.NotifyUserConfirmation(userConfirmationSelection);
        });
        return true;
    }

    static IEnumerable<ErrorAttachmentLog> GetErrorAttachmentsCallback(ErrorReport report)
    {
        return GetErrorAttachments();
    }

    public static IEnumerable<ErrorAttachmentLog> GetErrorAttachments()
    {
        var attachments = new List<ErrorAttachmentLog>();
        if (Preferences.ContainsKey("TEXT_ATTACHMENT"))
        {
            var attachment = ErrorAttachmentLog.AttachmentWithText(Preferences.Get("TEXT_ATTACHMENT", string.Empty), "hello.txt");
            attachments.Add(attachment);
        }
        if (Preferences.ContainsKey("FILE_ATTACHMENT"))
        {
            var filePicker = DependencyService.Get<IFilePicker>();
            if (filePicker != null)
            {
                try
                {
                    var filePath = Preferences.Get("FILE_ATTACHMENT", string.Empty);
                    var fileBytes = File.ReadAllBytes(filePath);

                    //var result = filePicker.ReadFile(Preferences.Get(CrashesContentPage.FileAttachmentKey, string.Empty);
                    if (fileBytes != null)
                    {
                        var attachment = ErrorAttachmentLog.AttachmentWithBinary(fileBytes, Path.GetFileName(filePath), null);
                        attachments.Add(attachment);
                    }
                }
                catch (Exception e)
                {
                    AppCenterLog.Warn(LogTag, "Couldn't read file attachment", e);
                    Preferences.Remove("FILE_ATTACHMENT");
                }
            }
        }
        return attachments;
    }

    private void StartAppCenter()
    {
        if (!AppCenter.Configured)
        {
            AppCenterLog.Assert(LogTag, "AppCenter.LogLevel=" + AppCenter.LogLevel);
            AppCenter.LogLevel = LogLevel.Verbose;
            AppCenterLog.Info(LogTag, "AppCenter.LogLevel=" + AppCenter.LogLevel);
            AppCenterLog.Info(LogTag, "AppCenter.Configured=" + AppCenter.Configured);

            // Set callbacks
            Crashes.ShouldProcessErrorReport = ShouldProcess;
            Crashes.ShouldAwaitUserConfirmation = ConfirmationHandler;
            Crashes.GetErrorAttachments = GetErrorAttachmentsCallback;
            Distribute.ReleaseAvailable = OnReleaseAvailable;
            Distribute.WillExitApp = OnWillExitApp;
            Distribute.NoReleaseAvailable = OnNoReleaseAvailable;

            // Event handlers
            Crashes.SendingErrorReport += SendingErrorReportHandler;
            Crashes.SentErrorReport += SentErrorReportHandler;
            Crashes.FailedToSendErrorReport += FailedToSendErrorReportHandler;

            // Country code.
            if (Preferences.ContainsKey("country-code"))
            {
                AppCenter.SetCountryCode(Preferences.Get("country-code", string.Empty));
            }

            // Manual session tracker.
            if (Preferences.ContainsKey("enable-manual-session-tracker")
                && Preferences.Get("enable-manual-session-tracker", false))
            {
                Analytics.EnableManualSessionTracker();
            }

            AppCenterLog.Assert(LogTag, "AppCenter.Configured=" + AppCenter.Configured);

            if (!Preferences.Get("automatic-update-check", true))
            {
                Distribute.DisableAutomaticCheckForUpdate();
            }
            if (Preferences.ContainsKey("storage-max-size"))
            {
                AppCenter.SetMaxStorageSizeAsync(Preferences.Get("storage-max-size", 0));
            }

            var appSecret = GetTokensString();
            AppCenter.Start(appSecret, typeof(Analytics), typeof(Crashes), typeof(Distribute));

            if (Preferences.ContainsKey("userId"))
            {
                AppCenter.SetUserId(Preferences.Get("userId", string.Empty));
            }
            AppCenter.IsEnabledAsync().ContinueWith(enabled =>
            {
                AppCenterLog.Info(LogTag, "AppCenter.Enabled=" + enabled.Result);
            });
            AppCenter.GetInstallIdAsync().ContinueWith(installId =>
            {
                AppCenterLog.Info(LogTag, "AppCenter.InstallId=" + installId.Result);
            });
            AppCenterLog.Info(LogTag, "AppCenter.SdkVersion=" + AppCenter.SdkVersion);
            Crashes.HasCrashedInLastSessionAsync().ContinueWith(hasCrashed =>
            {
                AppCenterLog.Info(LogTag, "Crashes.HasCrashedInLastSession=" + hasCrashed.Result);
            });
            Crashes.GetLastSessionCrashReportAsync().ContinueWith(task =>
            {
                AppCenterLog.Info(LogTag, "Crashes.LastSessionCrashReport.StackTrace=" + task.Result?.StackTrace);
            });
        }
    }

    static void SendingErrorReportHandler(object sender, SendingErrorReportEventArgs e)
    {
        AppCenterLog.Info(LogTag, "Sending error report");
    }

    static void SentErrorReportHandler(object sender, SentErrorReportEventArgs e)
    {
        AppCenterLog.Info(LogTag, "Sent error report");
    }

    static void FailedToSendErrorReportHandler(object sender, FailedToSendErrorReportEventArgs e)
    {
        AppCenterLog.Info(LogTag, "Failed to send error report");
    }

    void OnNoReleaseAvailable()
    {
        AppCenterLog.Info(LogTag, "No release available callback invoked.");
    }

    void OnWillExitApp()
    {
        AppCenterLog.Info(LogTag, "App will close callback invoked.");
    }

    bool OnReleaseAvailable(ReleaseDetails releaseDetails)
    {
        AppCenterLog.Info(LogTag, "OnReleaseAvailable id=" + releaseDetails.Id
                                        + " version=" + releaseDetails.Version
                                        + " releaseNotesUrl=" + releaseDetails.ReleaseNotesUrl);
        var custom = releaseDetails.ReleaseNotes?.ToLowerInvariant().Contains("custom") ?? false;
        if (custom)
        {
            var title = "Version " + releaseDetails.ShortVersion + " available!";
            Task answer;
            if (releaseDetails.MandatoryUpdate)
            {
                answer = Current.MainPage.DisplayAlert(title, releaseDetails.ReleaseNotes, "Update now!");
            }
            else
            {
                answer = Current.MainPage.DisplayAlert(title, releaseDetails.ReleaseNotes, "Update now!", "Maybe tomorrow...");
            }
            answer.ContinueWith((task) =>
            {
                if (releaseDetails.MandatoryUpdate || ((Task<bool>)task).Result)
                {
                    Distribute.NotifyUpdateAction(UpdateAction.Update);
                }
                else
                {
                    Distribute.NotifyUpdateAction(UpdateAction.Postpone);
                }
            });
        }
        return custom;
    }

    private string GetTokensString()
    {
        return _configuration["Analytics:AppCenterKey"] ?? string.Empty;
    }
}
