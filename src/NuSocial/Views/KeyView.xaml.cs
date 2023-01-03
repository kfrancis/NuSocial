using CommunityToolkit.Maui.Views;
using NostrLib.Models;

namespace NuSocial.Views;

public partial class KeyView : Popup
{
    public KeyView(NostrKeyPair nostrKeyPair)
    {
        InitializeComponent();
        BindingContext = nostrKeyPair;
    }
}
