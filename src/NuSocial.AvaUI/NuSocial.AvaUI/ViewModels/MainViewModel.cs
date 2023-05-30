using ReactiveUI.Fody.Helpers;

namespace NuSocial.AvaUI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public string Greeting => "Welcome to Avalonia 123!";

        public string TestString => "Test!";

        [Reactive] public bool ShowLatencyWarning { get; set; } = false;
    }
}