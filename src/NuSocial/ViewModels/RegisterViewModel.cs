using CommunityToolkit.Mvvm.Messaging;
using NuSocial.Core.ViewModel;
using NuSocial.Messages;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class RegisterViewModel : BaseFormModel, ITransientDependency
{
    public RegisterViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
        Title = L["CreateAccount"];
    }

    [Required]
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string? _displayName;

    [ObservableProperty]
    private string? _about;

    [ObservableProperty]
    private string _accountId = "123";

    public override Task OnFirstAppear()
    {
        WeakReferenceMessenger.Default.Send<ResetNavMessage>(new("//start"));
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task CreateAsync()
    {
        return SetBusyAsync(() =>
        {
            // create account
            return ValidateAsync((isValid) =>
            {
                return Task.CompletedTask;
            }, async () => await ShowErrorsCommand.ExecuteAsync(null));
        });
    }
}