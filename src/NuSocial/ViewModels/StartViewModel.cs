using NuSocial.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels
{
    public partial class StartViewModel : BaseViewModel, ITransientDependency
    {
        public StartViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
        {
        }

        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        private Task CreateAccountAsync()
        {
            return SetBusyAsync(() =>
            {
                return Task.CompletedTask;
            });
        }

        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        private Task GoToLoginAsync()
        {
            return SetBusyAsync(() =>
            {
                return Navigation.NavigateTo(nameof(LoginViewModel));
            });
        }
    }
}
