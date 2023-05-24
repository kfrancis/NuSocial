using BindableProps;

namespace NuSocial.Controls
{
    public partial class PostItemTemplate : ContentView
    {
        [BindableProp]
        private string _imageUrl = string.Empty;

        [BindableProp]
        private DateTime _when;

        [BindableProp]
        private string _contentTitle = string.Empty;

        [BindableProp]
        private string _contentBody = string.Empty;

        public PostItemTemplate()
        {
            InitializeComponent();
        }
    }
}
