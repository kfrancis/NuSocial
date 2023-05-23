using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Localization;
using NuSocial.Localization;

namespace NuSocial.Views;

public partial class RelaysView
{
    public RelaysView(RelaysViewModel vm, IStringLocalizer<NuSocialResource> loc) : base(vm)
	{
        if (loc is null)
        {
            throw new ArgumentNullException(nameof(loc));
        }

        Title = loc["Relays"];

		InitializeComponent();
	}

	public RelaysView() :
        this(Ioc.Default.GetRequiredService<RelaysViewModel>(),
             Ioc.Default.GetRequiredService<IStringLocalizer<NuSocialResource>>())
	{
    }
}