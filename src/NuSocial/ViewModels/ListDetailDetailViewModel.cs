using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

[QueryProperty(nameof(Item), "Item")]
public partial class ListDetailDetailViewModel : BaseViewModel, ITransientDependency
{
	[ObservableProperty]
	SampleItem item;
}
