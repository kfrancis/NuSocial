using NuSocial.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NuSocial.Core.View
{
    public abstract class ContentPageViewBase : ContentPage
    {

    }
    public abstract class ContentPageViewBase<T> : ContentPageViewBase, IViewFor<T> where T : BaseViewModel, ITransientDependency
    {
        internal SemaphoreSlim InternalLock { get; private set; } = new SemaphoreSlim(1, 1);
        private bool _initialView = true;
        T? _viewModel;

        protected ContentPageViewBase(T vm)
        {
            PageInstanceId = Guid.NewGuid();
            ViewModel = vm;
        }

        protected override async void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (BindingContext is BaseViewModel viewModel)
            {
                if (!viewModel.IsInitialized)
                {
                    async Task InternalInitialize()
                    {
                        try
                        {
                            await InternalLock.WaitAsync();
                            await viewModel.InitializeAsync();

                            viewModel.IsInitialized = true;
                        }
                        finally
                        {
                            InternalLock.Release();
                        }
                    }

                    if (Dispatcher.IsDispatchRequired)
                    {
                        await Dispatcher.DispatchAsync(InternalInitialize);
                    }
                    else
                    {
                        await InternalInitialize();
                    }
                }
            }
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

        protected virtual Guid PageInstanceId { get; set; }
        protected virtual string StyleSheet { get; set; } = string.Empty;
        object IViewForBase.ViewModel
        {
            get => _viewModel;
            set => ViewModel = (T)value;
        }

        protected override async void OnAppearing()
        {
            try
            {
                base.OnAppearing();

                if (BindingContext is BaseViewModel bindingViewContext)
                {
                    await InternalLock.WaitAsync();

                    if (!bindingViewContext.IsInitialized)
                    {
                        await bindingViewContext.InitializeAsync();
                        bindingViewContext.IsInitialized = true;
                    }

                    await bindingViewContext.OnAppearing();

                    InternalLock.Release();
                }
                else
                {
                    if (BindingContext is BaseFormModel bindingFormContext)
                    {
                        await InternalLock.WaitAsync();

                        if (!bindingFormContext.IsInitialized)
                        {
                            await bindingFormContext.InitializeAsync();
                            bindingFormContext.IsInitialized = true;
                        }

                        await bindingFormContext.OnAppearing();

                        InternalLock.Release();
                    }
                }

                LoadStyles();
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
                base.OnDisappearing();

                if (BindingContext is BaseViewModel bindingViewContext)
                {
                    Dispatcher.DispatchAsync(async () =>
                    {
                        await InternalLock.WaitAsync();

                        await bindingViewContext.OnDisappearing();

                        InternalLock.Release();
                    });
                }
                else
                {
                    if (BindingContext is BaseFormModel bindingFormContext)
                    {
                        Dispatcher.DispatchAsync(async () =>
                        {
                            await InternalLock.WaitAsync();

                            await bindingFormContext.OnDisappearing();

                            InternalLock.Release();
                        });
                    }
                }
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
