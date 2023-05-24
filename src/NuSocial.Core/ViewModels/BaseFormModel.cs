using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Volo.Abp.DependencyInjection;
using NuSocial.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NuSocial.Core.ViewModel
{
    /// <summary>
    /// Base class for ViewModels that are forms, ie have fields that need to be validated
    /// </summary>
    public partial class BaseFormModel : ViewModelBase
    {
        private IDialogService? _dialogService;

        [ObservableProperty]
        private List<string> _errors = new();

        [ObservableProperty]
        private bool _isValid = true;

        private INavigationService? _navigationService;

        public BaseFormModel()
            : this(Ioc.Default.GetRequiredService<IDialogService>(),
                   Ioc.Default.GetRequiredService<INavigationService>())
        {
        }

        public BaseFormModel(IDialogService dialogService, INavigationService navigationService)
        {
            _dialogService = dialogService;
            _navigationService = navigationService;

            ErrorsChanged += BaseFormModel_ErrorsChanged;
        }

        /// <summary>
        /// To make testing easier, use this dispatcher
        /// </summary>
        public IDialogService Dialog
        {
            get
            {
                if (_dialogService == null)
                {
                    var serviceProvider = Ioc.Default.GetRequiredService<ICachedServiceProvider>();
                    _dialogService = serviceProvider.GetRequiredService<IDialogService>();
                }

                return _dialogService;
            }
        }

        public INavigationService Navigation
        {
            get
            {
                if (_navigationService == null)
                {
                    var serviceProvider = Ioc.Default.GetRequiredService<ICachedServiceProvider>();
                    _navigationService = serviceProvider.GetRequiredService<INavigationService>();
                }

                return _navigationService;
            }
        }

        /// <summary>
        /// Called when the ViewModel is created
        /// </summary>
        /// <returns></returns>
        public virtual async Task InitializeAsync() => await Task.CompletedTask;

        /// <summary>
        /// Perform work, making sure to mark IsBusy, while showing a loading message (if set)
        /// </summary>
        /// <param name="func"></param>
        /// <param name="loadingMessage"></param>
        /// <param name="showException"></param>
        /// <returns></returns>
        public async Task SetBusyAsync(Func<Task>? func, string? loadingMessage = null, bool showException = true)
        {
            IsBusy = true;
            try
            {
                if (loadingMessage != null && Dialog != null)
                {
                    await Dialog.ShowProgress(loadingMessage);
                }
                if (func != null)
                    await func();
            }
            catch (Exception ex) when (LogException(ex, true, showException))
            {
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<T?> SetBusyAsync<T>(Func<Task<T>> func, string? loadingMessage = null, bool showException = true)
        {
            IsBusy = true;
            try
            {
                if (loadingMessage != null)
                {
                    await Dialog.ShowProgress(loadingMessage);
                }
                if (func != null)
                    return await func();
                else
                    return default;
            }
            catch (Exception ex) when (LogException(ex, true, showException))
            {
                return default;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Show the validation errors
        /// </summary>
        /// <returns></returns>
        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        public Task ShowErrors()
        {
            string message = string.Join(Environment.NewLine, GetErrors().Select(e => e.ErrorMessage));

            if (!string.IsNullOrEmpty(message))
            {
                return Dialog.ShowMessage(L["Validation"], message);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        public virtual void Validate(Action<bool>? onCompleted = null, Action? onError = null)
        {
            ClearErrors();
            Errors.Clear();
            ValidateAllProperties();
            Errors = GetErrors().Where(e => !string.IsNullOrEmpty(e.ErrorMessage)).Select(e => e.ErrorMessage ?? string.Empty).ToList();
            IsValid = !HasErrors;

            if (HasErrors)
            {
                onError?.Invoke();
            }
            else
            {
                onCompleted?.Invoke(IsValid);
            }
        }

        public virtual async Task ValidateAsync(Func<bool, Task>? completedTask = null, Func<Task>? errorTask = null)
        {
            ClearErrors();
            Errors.Clear();
            ValidateAllProperties();
            Errors = GetErrors().Where(e => !string.IsNullOrEmpty(e.ErrorMessage)).Select(e => e.ErrorMessage ?? string.Empty).ToList();
            IsValid = !HasErrors;

            if (HasErrors && errorTask != null)
            {
                await errorTask.Invoke();
            }
            else
            {
                if (completedTask != null)
                    await completedTask.Invoke(IsValid);
            }
        }

        private void BaseFormModel_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
        {
            IsValid = !HasErrors;
        }

        private bool LogException(Exception ex, bool shouldCatch, bool shouldDisplay)
        {
            if (ex == null) return shouldCatch;

            //_logger?.LogException(ex.Demystify());
            if (shouldDisplay)
            {
                Dialog.ShowError(L["Error"], ex.Message, L["OK"], () => { });
            }

            return shouldCatch;
        }
    }
}
