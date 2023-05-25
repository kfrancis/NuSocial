using BindableProps;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Controls;

/// <summary>
/// Flyout header control
/// </summary>
public partial class FlyoutHeader : ContentView
{
	/// <summary>
	/// Constructor
	/// </summary>
	public FlyoutHeader()
	{
		InitializeComponent();
	}

	[BindableProp]
	private int _followerCount;

	[BindableProp]
	private int _followingCount;
}