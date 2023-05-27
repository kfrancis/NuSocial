using CommunityToolkit.Mvvm.DependencyInjection;
using Mopups.Pages;
using Mopups.Services;

namespace NuSocial.Views;

/// <summary>
/// 
/// </summary>
public partial class SendPostPopup 
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="vm"></param>
	public SendPostPopup(SendPostPopupViewModel vm) : base(vm)
	{
		InitializeComponent();
	}

    public SendPostPopup() : this(Ioc.Default.GetRequiredService<SendPostPopupViewModel>())
    {
    }

    private void OnClose(object sender, EventArgs e)
    {
        MopupService.Instance.PopAsync();
    }

}