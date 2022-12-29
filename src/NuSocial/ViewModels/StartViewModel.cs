namespace NuSocial.ViewModels
{
    public partial class StartViewModel : BaseViewModel
    {
        [RelayCommand(CanExecute = "IsNotBusy")]
        private Task GoToCreateAccountAsync()
        {
            return SetBusyAsync(() =>
            {
                return Shell.Current.GoToAsync("//createAccount", true);
            });
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private Task GoToLoginAsync()
        {
            return SetBusyAsync(() =>
            {
                return Shell.Current.GoToAsync("//login", true);
            });
        }
    }
}
