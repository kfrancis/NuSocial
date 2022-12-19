using CommunityToolkit.Mvvm.DependencyInjection;
using NuSocial.Core.Threading;
using NuSocial.Resources.Strings;

namespace NuSocial.ViewModels;

public partial class ViewModelBase : ObservableValidator
{
    [ObservableProperty]
    private bool _canLoadMore = true;

    [ObservableProperty]
    private string? _footer = string.Empty;

    [ObservableProperty]
    private string? _header = string.Empty;

    [ObservableProperty]
    private string? _icon = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _subtitle = string.Empty;

    [ObservableProperty]
    private string? _title = string.Empty;

    public bool IsNotBusy => !IsBusy;
}

public partial class BaseViewModel : ViewModelBase
{
    private readonly ICustomDispatcher _customDispatcher;
    private readonly IDialogService _dialogService;

    public BaseViewModel() :
        this(Ioc.Default.GetRequiredService<IDialogService>(), Ioc.Default.GetRequiredService<ICustomDispatcher>())
    {
    }

    public BaseViewModel(IDialogService dialogService, ICustomDispatcher customDispatcher)
    {
        _dialogService = dialogService;
        _customDispatcher = customDispatcher;
    }

    public virtual async Task InitializeAsync() => await Task.CompletedTask;

    public virtual void OnAppearing()
    { }

    public virtual void OnDisappearing()
    { }

    public async Task SetBusyAsync(Func<Task>? func, string? loadingMessage = null, bool showException = true)
    {
        IsBusy = true;
        try
        {
            if (loadingMessage != null && _dialogService != null)
            {
                await _dialogService.ShowProgress(loadingMessage);
            }
            if (func != null)
                await func();
        }
        catch (Exception ex) when (LogException(ex, true, showException))
        {
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<T?> SetBusyAsync<T>(Func<Task<T>> func, string? loadingMessage = null, bool showException = true)
    {
        IsBusy = true;
        try
        {
            if (loadingMessage != null)
            {
                await _dialogService.ShowProgress(loadingMessage);
            }
            if (func != null)
                return await func();
            else
                return default;
        }
        catch (Exception ex) when (LogException(ex, true, showException))
        {
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool LogException(Exception ex, bool shouldCatch, bool shouldDisplay)
    {
        if (ex == null) return shouldCatch;

        //_logger?.LogException(ex.Demystify());
        if (shouldDisplay)
        {
            _dialogService.ShowError(AppResources.Error, ex.Message, AppResources.OK, () => { });
        }

        return shouldCatch;
    }
}