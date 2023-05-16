using NuSocial.Resources.Strings;

namespace NuSocial.MarkupExtensions;

[ContentProperty(nameof(Name))]
public class LocString : IMarkupExtension<string>
{
    public string Name { get; set; }

    public string ProvideValue(IServiceProvider serviceProvider)
    {
        return AppResources.ResourceManager.GetString(Name) ?? $"[{Name}]";
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}
