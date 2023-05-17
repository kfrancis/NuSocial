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

    private async void ScrollView_Scrolled(object sender, ScrolledEventArgs e)
    {
        await ViewModel.EulaScrolledAsync(sender, e);
    }
}