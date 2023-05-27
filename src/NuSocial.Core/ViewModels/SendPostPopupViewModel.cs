using CommunityToolkit.Mvvm.DependencyInjection;
using Mopups.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels
{
    public partial class SendPostPopupViewModel : BasePopupModel, ITransientDependency
    {
        [ObservableProperty]
        private string _message = string.Empty;

        public SendPostPopupViewModel() : this(Ioc.Default.GetRequiredService<IDialogService>())
        {
        }

        public SendPostPopupViewModel(IDialogService dialogService) : base(dialogService)
        {
        }

        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        private Task Send()
        {
            return SetBusyAsync(() =>
            {
                return Task.CompletedTask;
            });
        }
    }
}
