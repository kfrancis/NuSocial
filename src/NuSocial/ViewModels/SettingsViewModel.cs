using CommunityToolkit.Mvvm.Collections;
using NuSocial.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private ObservableCollection<Relay> Relays { get; } = new ObservableCollection<Relay>();

        [RelayCommand]
        private Task SaveAsync()
        {
            return Task.CompletedTask;
        }
    }
}
