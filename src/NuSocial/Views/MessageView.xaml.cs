using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

/// <summary>
/// View a single message
/// </summary>
public partial class MessageView 
{
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="vm"></param>
	public MessageView(MessageViewModel vm) : base(vm)
	{ 
		InitializeComponent();
	}

	/// <summary>
	/// Constructor
	/// </summary>
	public MessageView() : this(Ioc.Default.GetRequiredService<MessageViewModel>())
	{

	}
}