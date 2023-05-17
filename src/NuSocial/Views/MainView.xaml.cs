using CommunityToolkit.Mvvm.DependencyInjection;
using NuSocial.Core.View;

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