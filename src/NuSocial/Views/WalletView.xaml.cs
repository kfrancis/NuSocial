using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class WalletView
{
    public WalletView(WalletViewModel vm) : base(vm)
    {
        InitializeComponent();
    }

    public WalletView() : this(Ioc.Default.GetRequiredService<WalletViewModel>())
    {
    }
}