using CommunityToolkit.Maui.Views;
using NuSocial.Resources.Strings;

namespace NuSocial.Views;

public partial class LoadingIndicatorPopup : Popup
{
    public LoadingIndicatorPopup() : this(AppResources.Loading)
    {
    }

    public LoadingIndicatorPopup(string text)
    {
        InitializeComponent();

        loadingIndicatorLabel.Text = text;
    }
}