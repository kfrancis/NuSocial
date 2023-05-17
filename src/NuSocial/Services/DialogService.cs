using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using NuSocial.Core.Threading;
using NuSocial.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NuSocial.Services
{
    public interface IDialogService
    {
        Task AlertAsync(string? message, string title, string buttonText);

        Task AlertAsync(string message, string title);

        void CloseAllDialogs();

        Task<bool> ConfirmAsync(string message, string title, string ok, string cancel);

        Task<string> DisplayActionSheet(string title, string cancel, string destruction, params string[] buttons);
        Task HideLoading();

        Task ShowError(string title, string message, string buttonText, Action? callback = null);

        Task ShowError(string title, Exception exception, string buttonText, Action? callback = null);

        Task ShowMessage(string title, string message);

        Task ShowMessage(string title, string message, string buttonText, Action? callback = null);

        Task<bool> ShowMessage(string title, string message, string buttonConfirmText, string buttonCancelText, Action<bool> callback);

        Task ShowProgress(string message, Action? callback = null);
    }

    public class DialogService : IDialogService, ITransientDependency
    {
        private static IDialogService? s_currentInstance;
        private LoadingIndicatorPopup? _loadingDialog;
        private LocalizationResourceManager? _localizationResourceManager;

        public DialogService(ICustomDispatcher dispatcher)
        {
            Dispatcher = new List<ICustomDispatcher> { dispatcher };
        }

        public DialogService() : this(DependencyService.Get<ICustomDispatcher>())
        {
        }

        public static IDialogService Instance
        {
            get
            {
                if (s_currentInstance == null)
                {
                    s_currentInstance = Ioc.Default.GetRequiredService<IDialogService>();

                    if (s_currentInstance == null)
                        throw new Exception("Need to register a dialog service");
                }

                return s_currentInstance;
            }
            set => s_currentInstance = value;
        }

        private List<ICustomDispatcher> Dispatcher { get; }

        public LocalizationResourceManager L
        {
            get
            {
                _localizationResourceManager ??= Ioc.Default.GetRequiredService<LocalizationResourceManager>();
                return _localizationResourceManager;
            }
        }

        public async Task AlertAsync(string? message, string title, string buttonText)
        {
            await Dispatcher.RunOnUIThreadAsync(async () =>
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.DisplayAlert(title: title, message: message ?? "", cancel: buttonText ?? L.Localize("OK"));
                }
                else
                {
                    if (Application.Current != null && Application.Current.MainPage != null)
                        await Application.Current.MainPage.DisplayAlert(title: title, message: message ?? "", cancel: buttonText ?? L.Localize("OK"));
                }
            });
        }

        public Task AlertAsync(string message, string title)
        {
            return AlertAsync(message, title, L.Localize("OK"));
        }

        public async void CloseAllDialogs()
        {
            if (_loadingDialog != null)
            {
                await HideLoading();
            }
        }

        public Task<bool> ConfirmAsync(string message, string title, string ok, string cancel)
        {
            if (Shell.Current != null)
            {
                return Shell.Current.DisplayAlert(title: title, message: message ?? "", ok ?? L.Localize("OK"), cancel ?? L.Localize("Cancel"));
            }
            else
            {
                if (Application.Current != null && Application.Current.MainPage != null)
                    return Application.Current.MainPage.DisplayAlert(title: title, message: message ?? "", ok ?? L.Localize("OK"), cancel ?? L.Localize("Cancel"));
            }

            return Task.FromResult(false);
        }

        //This could be broken, I coded it based on DisplayAlert and I might have missed something.
        public async Task<string> DisplayActionSheet(string? title, string cancel, string? destruction, params string[] buttons)
        {
            var selectedOption = string.Empty;
            destruction = null;
            await Dispatcher.RunOnUIThreadAsync(async () =>
            {
                if (Shell.Current != null)
                {
                    selectedOption = await Shell.Current.DisplayActionSheet(title: title, cancel: cancel ?? L.Localize("OK"), destruction: destruction, buttons: buttons);
                }
                else
                {
                    if (Application.Current != null && Application.Current.MainPage != null)
                    {
                        selectedOption = await Application.Current.MainPage.DisplayActionSheet(title: title, cancel: cancel ?? L.Localize("OK"), destruction: destruction, buttons: buttons);
                    }
                }
                return selectedOption;
            });
            return selectedOption;
        }

        public Task DisplayActionSheet(string title, string cancel, params string[] buttons)
        {
            return DisplayActionSheet(title, cancel, null, buttons);
        }
        public Task HideLoading()
        {
            if (_loadingDialog == null)
            {
                return Task.CompletedTask;
            }

            try
            {
                _loadingDialog.Close();
                _loadingDialog = null;
            }
            catch (IndexOutOfRangeException)
            {
                // catch and swallow out of range exceptions when dismissing dialogs.
            }

            return Task.CompletedTask;
        }

        public async Task ShowError(string title, string message, string buttonText, Action? callback = null)
        {
            await Dispatcher.RunOnUIThreadAsync(async () =>
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.DisplayAlert(title: title, message: message ?? "", cancel: buttonText ?? L.Localize("OK"));
                }
                else
                {
                    if (Application.Current != null && Application.Current.MainPage != null)
                        await Application.Current.MainPage.DisplayAlert(title: title, message: message ?? "", cancel: buttonText ?? L.Localize("OK"));
                }
            });
        }

        public async Task ShowError(string title, Exception exception, string buttonText, Action? callback = null)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlert(title: title, message: exception.Message, cancel: buttonText ?? L.Localize("OK"));
            }
            else
            {
                if (Application.Current != null && Application.Current.MainPage != null)
                    await Application.Current.MainPage.DisplayAlert(title: title, message: exception.Message, cancel: buttonText ?? L.Localize("OK"));
            }
        }

        public async Task ShowMessage(string title, string message)
        {
            await Dispatcher.RunOnUIThreadAsync(async () =>
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.DisplayAlert(title: title, message: message ?? "", cancel: L.Localize("OK"));
                }
                else
                {
                    if (Application.Current != null && Application.Current.MainPage != null)
                        await Application.Current.MainPage.DisplayAlert(title: title, message: message ?? "", cancel: L.Localize("OK"));
                }
            });
        }

        public async Task ShowMessage(string title, string message, string buttonText, Action? callback = null)
        {
            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlert(title: title, message: message, cancel: buttonText ?? L.Localize("OK"));
            }
            else
            {
                if (Application.Current != null && Application.Current.MainPage != null)
                    await Application.Current.MainPage.DisplayAlert(title: title, message: message, cancel: buttonText ?? L.Localize("OK"));
            }
        }

        public async Task<bool> ShowMessage(string title, string message, string buttonConfirmText, string buttonCancelText, Action<bool> callback)
        {
            if (Shell.Current != null)
            {
                return await Shell.Current.DisplayAlert(title: title, message: message, accept: buttonConfirmText ?? L.Localize("OK"), cancel: buttonCancelText ?? L.Localize("Cancel"));
            }
            else
            {
                if (Application.Current != null && Application.Current.MainPage != null)
                    return await Application.Current.MainPage.DisplayAlert(title: title, message: message, accept: buttonConfirmText ?? L.Localize("OK"), cancel: buttonCancelText ?? L.Localize("Cancel"));
            }

            return false;
        }

        public async Task ShowProgress(string loadingMessage)
        {
            if (_loadingDialog != null)
            {
                await HideLoading();
            }

            _loadingDialog = new(loadingMessage);
            await Shell.Current.ShowPopupAsync(_loadingDialog);
        }

        public async Task ShowProgress(string message, Action? callback = null)
        {
            if (_loadingDialog != null)
            {
                await HideLoading();
            }

            _loadingDialog = new(message);
            if (Shell.Current != null)
            {
                await Shell.Current.ShowPopupAsync(_loadingDialog);
            }
            else
            {
                if (Application.Current != null && Application.Current.MainPage != null)
                    await Application.Current.MainPage.ShowPopupAsync(_loadingDialog);
            }
            callback?.InvokeAsync();
        }
    }
}
