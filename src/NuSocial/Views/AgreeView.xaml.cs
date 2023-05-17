using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class AgreeView 
{
	public AgreeView(AgreeViewModel vm) : base(vm)
	{
		InitializeComponent();
	}

	public AgreeView() : this(Ioc.Default.GetRequiredService<AgreeViewModel>())
	{
    }
}