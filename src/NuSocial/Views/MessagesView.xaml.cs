using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class MessagesView
{
    public MessagesView(MessagesViewModel vm) : base(vm)
    {
        InitializeComponent();
    }

    public MessagesView() : this(Ioc.Default.GetRequiredService<MessagesViewModel>())
    {
    }
}