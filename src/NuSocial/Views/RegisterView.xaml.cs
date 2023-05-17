using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class RegisterView
{
    public RegisterView(RegisterViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
    public RegisterView() : this(Ioc.Default.GetRequiredService<RegisterViewModel>())
    {
    }
}