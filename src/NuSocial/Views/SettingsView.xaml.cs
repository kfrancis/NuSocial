using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class SettingsView 
{
	public SettingsView(SettingsViewModel vm) : base(vm)
	{
		InitializeComponent();
	}

	public SettingsView() : this(Ioc.Default.GetRequiredService<SettingsViewModel>())
	{
    }
}