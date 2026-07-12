using AHelper.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AHelper.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial PerformanceMode SelectedMode { get; set; } = PerformanceMode.Balanced;

    [RelayCommand]
    private void SelectMode(PerformanceMode mode)
    {
        SelectedMode = mode;

        // TODO: call into a Service/Performance service to apply mode
    }
}