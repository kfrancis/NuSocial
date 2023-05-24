using BindableProps;

namespace NuSocial.Controls
{
    public partial class MessagePreviewItem : ContentView
    {
        public MessagePreviewItem()
        {
            InitializeComponent();
        }

        private void ProfileImage_Tapped(object sender, TappedEventArgs e)
        {

        }

        [BindableProp]
        private Message _data;
    }
}
