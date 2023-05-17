using CommunityToolkit.Mvvm.Messaging;
using NuSocial.Messages;
using Volo.Abp.DependencyInjection;

namespace NuSocial;

public partial class AppShell : Shell, ISingletonDependency
{
    private ShellNavigationState? _navState;

    public AppShell(ShellViewModel vm)
    {
        BindingContext = vm;
        InitializeComponent();
        Uri = new Stack<ShellNavigationState>();

        WeakReferenceMessenger.Default.Register<LogoutMessage>(this, (r, m) =>
        {
            // When we log out, we clear the nav history
            Uri.Clear();
        });

        WeakReferenceMessenger.Default.Register<ResetNavMessage>(this, (r, m) =>
        {
            if (!string.IsNullOrEmpty(m.Value))
            {
                Uri.Clear();
                Uri.Push(m.Value);
            }
            else
            {
                Uri.Clear();
            }
        });

        RegisterRoutes();
    }

    private Stack<ShellNavigationState> Uri { get; set; } // Navigation stack.

    protected override bool OnBackButtonPressed()
    {
        if (Uri.Count > 0)
        {
            Shell.Current.GoToAsync(Uri.Pop());
            return true;
        }
        else
        {
            return false;
        }
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        base.OnNavigated(args);
        if (Uri != null && args.Previous != null)
        {
            if (_navState == null || _navState != args.Previous)
            {
                Uri.Push(args.Previous);
                _navState = args.Current;
            }
        }
    }

    private void RegisterRoutes()
    {
        Routing.RegisterRoute(nameof(LoginViewModel), typeof(LoginView));
        Routing.RegisterRoute(nameof(RegisterViewModel), typeof(RegisterView));
        Routing.RegisterRoute(nameof(AgreeViewModel), typeof(AgreeView));
        Routing.RegisterRoute(nameof(RegisterViewModel), typeof(RegisterView));
        Routing.RegisterRoute(nameof(ProfileViewModel), typeof(ProfileView));
        Routing.RegisterRoute(nameof(RelaysViewModel), typeof(RelaysView));
        Routing.RegisterRoute(nameof(SettingsViewModel), typeof(SettingsView));
        Routing.RegisterRoute(nameof(WalletViewModel), typeof(WalletView));
    }
}
