using Microsoft.Extensions.Localization;
using Volo.Abp.DependencyInjection;

namespace NuSocial.Localization;

public partial class LocalizationResourceManager : ObservableObject, ISingletonDependency
{
    [ObservableProperty]
    private CultureInfo _currentCulture;

    private readonly IStringLocalizer _localizer;

    public LocalizationResourceManager(IServiceProvider serviceProvider)
    {
        _localizer = serviceProvider.GetRequiredService<IStringLocalizerFactory>().Create(typeof(NuSocialResource));
        _currentCulture = CultureInfo.CurrentCulture;
    }

    public LocalizedString this[string resourceKey] => GetValue(resourceKey);

    public string Localize(string resourceKey, params object[] args)
    {
        return string.Format(CurrentCulture, this[resourceKey], args) ?? $"[{resourceKey}]";
    }

    public LocalizedString GetValue(string resourceKey)
    {
        CultureInfo.CurrentCulture = CurrentCulture;
        CultureInfo.CurrentUICulture = CurrentCulture;

        var loc = _localizer[resourceKey];
        if (loc.ResourceNotFound)
        {
            Debug.WriteLine($"L10N: Resource not found '{resourceKey}'");
            loc = new LocalizedString(loc.Name, $"[{resourceKey}]", loc.ResourceNotFound);
        }
        return loc;
    }

    internal static object GetInstance(IServiceProvider serviceProvider)
    {
        _instance ??= new LocalizationResourceManager(serviceProvider);
        return _instance;
    }

    private static LocalizationResourceManager? _instance;
}