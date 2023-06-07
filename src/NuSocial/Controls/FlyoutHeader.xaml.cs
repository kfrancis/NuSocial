using BindableProps;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using NuSocial.Messages;

namespace NuSocial.Controls;

/// <summary>
/// Flyout header control
/// </summary>
public partial class FlyoutHeader : ContentView
{

    [BindableProp]
    private int _followerCount;

    [BindableProp]
    private int _followingCount;

    [BindableProp(PropertyChangedDelegate = nameof(ProfileChanged))]
    private Profile? _profile;

    /// <summary>
    /// Constructor
    /// </summary>
    public FlyoutHeader()
    {
        InitializeComponent();


        WeakReferenceMessenger.Default.Register<NostrReadyMessage>(this, async (r, m) =>
        {
            var nostrService = Ioc.Default.GetRequiredService<INostrService>();
            Profile = await nostrService.GetProfileAsync(GlobalSetting.Instance.CurrentUser.PublicKeyString, true);
        });
        WeakReferenceMessenger.Default.Register<NostrUserChangedMessage>(this, (s, m) =>
        {
        });
    }

    private static void ProfileChanged(BindableObject bindable, object? oldValue = null, object? newValue = null)
    {
        if (bindable is FlyoutHeader flyoutHeader && newValue is Profile profile)
        {
            flyoutHeader.FollowerCount = profile.Follows.Count;
            flyoutHeader.FollowingCount = profile.Follows.Count;
        }
    }
}