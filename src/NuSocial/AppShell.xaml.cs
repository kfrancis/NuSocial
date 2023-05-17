using CommunityToolkit.Mvvm.Messaging;
using NuSocial.Messages;
using Volo.Abp.DependencyInjection;

namespace NuSocial;

public partial class AppShell : Shell, ISingletonDependency
{
    private ShellNavigationState? temp;

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

        WeakReferenceMessenger.Default.Register<DataLoadedMessage>(this, (r, m) =>
        {
            // When we finish loading the data, we clear the nav history because we can't go back
            Uri.Clear();
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
            if (temp == null || temp != args.Previous)
            {
                Uri.Push(args.Previous);
                temp = args.Current;
            }
        }
    }

    private void RegisterRoutes()
    {
        Routing.RegisterRoute(nameof(LoginViewModel), typeof(LoginView));
    }
}
