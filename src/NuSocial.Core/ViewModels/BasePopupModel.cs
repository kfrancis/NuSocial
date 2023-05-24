using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NuSocial.Core.ViewModel
{
    public partial class BasePopupModel : ViewModelBase
    {
        private IDialogService _dialogService;

        public BasePopupModel()
            : this(Ioc.Default.GetRequiredService<IDialogService>())
        {
        }

        public BasePopupModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public virtual async Task InitializeAsync() => await Task.CompletedTask;

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
                {
                    await func();
                }
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
