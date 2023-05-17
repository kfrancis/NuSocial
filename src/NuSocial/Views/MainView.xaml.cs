using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class MainView
{
    public MainView(MainViewModel vm) : base(vm)
    {
        InitializeComponent();
    }
    public MainView() : this(Ioc.Default.GetRequiredService<MainViewModel>())
    {
    }
}