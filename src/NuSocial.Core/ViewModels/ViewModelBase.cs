using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Localization;
using NuSocial.Core.Threading;
using NuSocial.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NuSocial.Core.ViewModel;

public abstract partial class ViewModelBase : ObservableValidator, IQueryAttributable
{
    private const string _parameterKey = "nsParameter";

    [ObservableProperty]
    private bool _canLoadMore = true;

    private ICustomDispatcher? _dispatcher;

    [ObservableProperty]
    private string? _footer = string.Empty;

    private bool _hasAppeared;

    [ObservableProperty]
    private string? _header = string.Empty;

    [ObservableProperty]
    private string? _icon = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotInitialized))]
    private bool _isInitialized;

    [ObservableProperty]
    private string? _subtitle = string.Empty;

    [ObservableProperty]
    private string? _title = string.Empty;

    protected ViewModelBase()
    {
        var application = ApplicationResolver.Current;

        if (application != null)
        {
            application.ApplicationResume += (sender, args) =>
            {
                OnApplicationResume();
            };

            application.ApplicationSleep += (sender, args) =>
            {
                OnApplicationSleep();
            };
        }
    }

    /// <summary>
    /// To make testing easier, use this dispatcher
    /// </summary>
    public ICustomDispatcher Dispatcher
    {
        get
        {
            if (_dispatcher == null)
            {
                var serviceProvider = Ioc.Default.GetRequiredService<ICachedServiceProvider>();
                _dispatcher = serviceProvider.GetRequiredService<ICustomDispatcher>();
            }
            return _dispatcher;
        }
    }

    public bool IsNotBusy => !IsBusy;

    public bool IsNotInitialized
    {
        get
        {
            return !IsInitialized;
        }
    }

    public ITransientCachedServiceProvider LazyServiceProvider { get; set; }

    public LocalizationResourceManager L => LazyServiceProvider.GetRequiredService<LocalizationResourceManager>();

    /// <inheritdoc />
    public object? NavigationParameter { get; set; }

    /// <inheritdoc />
    public IDictionary<string, object>? QueryParameters { get; set; }

    /// <summary>
    /// Creates a toast message and displays it immediately with the current settings
    /// </summary>
    /// <param name="msg">The message to display</param>
    /// <param name="duration">(optional) The duration of which to display the message on-screen</param>
    /// <param name="fontSize">(optional) The font size</param>
    /// <returns>The toast task</returns>
    public static Task SendToast(string msg, ToastDuration duration = ToastDuration.Short, double fontSize = 14)
    {
        if (string.IsNullOrEmpty(msg))
        {
            return Task.CompletedTask;
        }

        using var cts = new CancellationTokenSource();

        var toast = Toast.Make(msg, duration, fontSize);

        return toast.Show(cts.Token);
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query == null || query.Count == 0)
        {
            return;
        }

        if (query.ContainsKey(_parameterKey))
        {
            NavigationParameter = query[_parameterKey];

            if (query.Count > 1)
            {
                var queryParameters = new Dictionary<string, object>();

                foreach (var item in query)
                {
                    if (item.Key == _parameterKey)
                    {
                        continue;
                    }

                    queryParameters.Add(item.Key, item.Value);
                }

                QueryParameters = queryParameters;
            }
        }
        else
        {
            QueryParameters = query;
        }

        await Dispatcher.RunAsync(async () =>
        {
            await OnParameterSet();
        });
    }

    public virtual Task OnAppearing()
    {
        if (!_hasAppeared)
        {
            Dispatcher.RunAsync(async () => await OnFirstAppear());
        }

        _hasAppeared = true;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task OnApplicationResume() => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnApplicationSleep() => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnDisappearing() => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnFirstAppear() => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnParameterSet() => Task.CompletedTask;
}
