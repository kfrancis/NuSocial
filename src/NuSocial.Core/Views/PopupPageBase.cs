﻿using Mopups.Animations;
using Mopups.Pages;
using NuSocial.Core.ViewModel;
using System.Diagnostics;
using Volo.Abp.DependencyInjection;

namespace NuSocial.Core.View
{
    public abstract class PopupPageBase : PopupPage
    {
    }

    public abstract class PopupPageBase<T> : PopupPageBase, IPopupFor<T> where T : BasePopupModel, ITransientDependency
    {
        private bool _initialView = true;
        private T? _viewModel;

        protected PopupPageBase(T vm)
        {
            PageInstanceId = Guid.NewGuid();
            ViewModel = vm;
            Animation = new MoveAnimation(Mopups.Enums.MoveAnimationOptions.Top, Mopups.Enums.MoveAnimationOptions.Top);
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

        object IViewForBase.ViewModel
        {
            get => _viewModel;
            set => ViewModel = (T)value;
        }

        internal SemaphoreSlim InternalLock { get; private set; } = new SemaphoreSlim(1, 1);
        protected virtual Guid PageInstanceId { get; set; }

        protected virtual string StyleSheet { get; set; } = string.Empty;

        protected override async void OnAppearing()
        {
            try
            {
                base.OnAppearing();

                if (BindingContext is BasePopupModel bindingViewContext)
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

                LoadStyles();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Demystify());
                throw;
            }
        }

        protected override async void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (BindingContext is BasePopupModel viewModel)
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

        protected override void OnDisappearing()
        {
            try
            {
                base.OnDisappearing();

                if (BindingContext is BasePopupModel bindingViewContext)
                {
                    Dispatcher.DispatchAsync(async () =>
                    {
                        await InternalLock.WaitAsync();

                        await bindingViewContext.OnDisappearing();

                        InternalLock.Release();
                    });
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