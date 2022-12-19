namespace NuSocial.ViewModels;

public partial class LocalizationViewModel : BaseViewModel
{
    public string LocalizedText => NuSocial.Resources.Strings.AppResources.HelloMessage;
}
