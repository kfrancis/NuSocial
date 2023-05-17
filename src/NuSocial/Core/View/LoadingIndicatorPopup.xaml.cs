using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Localization;
using NuSocial.Localization;

namespace NuSocial.Views;

public partial class LoadingIndicatorPopup : Popup
{
    public LoadingIndicatorPopup(IStringLocalizer<NuSocialResource> stringLocalizer, string? text = null)
    {
        if (stringLocalizer is null)
        {
            throw new ArgumentNullException(nameof(stringLocalizer));
        }

        InitializeComponent();

        loadingIndicatorLabel.Text = text ?? stringLocalizer["Loading"];
    }

    public LoadingIndicatorPopup(string? text) : this(Ioc.Default.GetRequiredService<IStringLocalizer<NuSocialResource>>(), text)
    {
    }
}