using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class StartView
{
    public StartView(StartViewModel vm) : base(vm)
    {
        InitializeComponent();
    }
    public StartView() : this(Ioc.Default.GetRequiredService<StartViewModel>())
    {
    }
}