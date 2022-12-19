namespace NuSocial.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private ObservableCollection<Relay> Relays { get; } = new ObservableCollection<Relay>();

        [RelayCommand]
        private Task SaveAsync()
        {
            return Task.CompletedTask;
        }
    }
}