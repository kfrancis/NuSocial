using Microsoft.Extensions.Localization;
using NuSocial.Core.ViewModel;
using NuSocial.Localization;

namespace NuSocial.Helpers
{
    /// <summary>
    /// The base localization manager.
    /// </summary>
    [ContentProperty(nameof(Text))]
    public class TranslateExtension : IMarkupExtension<BindingBase>
    {
        public object? BindingContext { get; set; }
        public IValueConverter? Converter { get; set; }
        public object? ConverterParameter { get; set; }
        public string? StringFormat { get; set; }

        /// <summary>
        /// The text for localization.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        public virtual string GetResourceString(IStringLocalizer<NuSocialResource> loc)
        {
            if (loc is null)
            {
                throw new ArgumentNullException(nameof(loc));
            }

            return loc[Text];
        }

        /// <summary>
        /// Gets the localization text.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            var label = new Label { BindingContext = BindingContext };
            label.SetBinding(Label.TextProperty, new Binding(Text));
            var value = label.GetValue(Label.TextProperty) ?? Text;

            var localizationResourceManager = serviceProvider.GetRequiredService<IRootObjectProvider>()
                .RootObject.As<BindableObject>()
                .BindingContext.As<ViewModelBase>()
                .L;

            return new Binding
            {
                Mode = BindingMode.OneWay,
                Path = $"[{value}]",
                Source = localizationResourceManager,
                StringFormat = StringFormat,
                Converter = Converter,
                ConverterParameter = ConverterParameter
            };
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return (this as IMarkupExtension<BindingBase>).ProvideValue(serviceProvider);
        }
    }
}