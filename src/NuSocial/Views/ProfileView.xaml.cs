using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class ProfileView 
{
	public ProfileView(ProfileViewModel vm) : base(vm)
	{
		InitializeComponent();
	}

	public ProfileView() : this(Ioc.Default.GetRequiredService<ProfileViewModel>())
	{
    }
}