using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Maui.Controls;
using Mopups.Services;
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

    private void UnreadBtn_Clicked(object sender, EventArgs e)
    {
        // We should always scroll to the top when loading new posts because we're inserting at the top
        //collectionView.ScrollTo(0);
    }

    private void IconButton_Clicked(object sender, EventArgs e)
    {
        MopupService.Instance.PushAsync(new SendPostPopup());
    }
}