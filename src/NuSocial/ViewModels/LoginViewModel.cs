namespace NuSocial.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _key = string.Empty;

        [RelayCommand]
        private Task LoginAsync()
        {
            return SetBusyAsync(() =>
            {
                return Task.CompletedTask;
            });
        }
    }
}
