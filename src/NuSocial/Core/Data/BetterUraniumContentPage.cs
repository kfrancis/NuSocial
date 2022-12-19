using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UraniumUI.Pages;

namespace NuSocial
{
    public abstract class BetterUraniumContentPage : UraniumContentPage
    {

    }

    public abstract class BetterUraniumContentPage<T> : BetterUraniumContentPage where T: BaseViewModel
    {
        private bool _initialView = true;
        private bool _isInitialized;
        T? _viewModel;

        protected BetterUraniumContentPage()
        {
            PageInstanceId = Guid.NewGuid();
        }

        public T ViewModel
        {
            get
            {
                return _viewModel;
            }
            set
            {
                BindingContext = _viewModel = value;
            }
        }

        async void InitializeAsync()
        {
            if (ViewModel == null) return;

            await ViewModel.InitializeAsync();
        }

        protected virtual Guid PageInstanceId { get; set; }
        protected virtual string StyleSheet { get; set; } = string.Empty;

        protected override async void OnAppearing()
        {
            try
            {
                base.OnAppearing();

                if (BindingContext is BaseViewModel bindingViewContext)
                {
                    bindingViewContext.OnAppearing();
                }

                LoadStyles();

                if (_isInitialized)
                    return;

                InitializeAsync();

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Demystify());
                throw;
            }
        }

        protected override void OnDisappearing()
        {
            try
            {
                if (BindingContext is BaseViewModel bindingViewContext)
                {
                    bindingViewContext.OnDisappearing();
                }

                base.OnDisappearing();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Demystify());
                throw;
            }
        }

        private void LoadStyles()
        {
            if (!_initialView)
                return;

            _initialView = false;

            if (!string.IsNullOrEmpty(this.StyleSheet))
            {
                try
                {
                    if (Activator.CreateInstance(Type.GetType(this.StyleSheet)) is VisualElement styleSheet)
                    {
                        foreach (var resource in styleSheet.Resources)
                            this.Resources.Add(resource.Key, resource.Value);
                    }
                }
                catch
                {
                    // Failed to add stylesheet
                }
            }
        }
    }
}
