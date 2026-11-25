using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elementwright.Core.Automation;
using Elementwright.Core.Discovery;
using Elementwright.Core.Models;

namespace Elementwright.Studio.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IApplicationDiscovery _applicationDiscovery;
    private readonly UiaTreeReader _treeReader;

    [ObservableProperty]
    private ObservableCollection<ApplicationInfo> applications = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectToApplicationCommand))]
    private ApplicationInfo? selectedApplication;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private int discoveredCount;

    // --- Inspector state ---

    [ObservableProperty]
    private bool isInspecting;

    [ObservableProperty]
    private ApplicationInfo? connectedApplication;

    [ObservableProperty]
    private ObservableCollection<ElementNode> rootElements = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopySelectorCommand))]
    private ElementNode? selectedElement;

    public IEnumerable<ApplicationInfo> FilteredApplications =>
        string.IsNullOrWhiteSpace(SearchText)
            ? Applications
            : Applications.Where(x =>
                x.WindowTitle.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                x.ProcessName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));

    public MainViewModel(IApplicationDiscovery applicationDiscovery, UiaTreeReader treeReader)
    {
        _applicationDiscovery = applicationDiscovery;
        _treeReader = treeReader;
    }

    partial void OnSearchTextChanged(string value) =>
        OnPropertyChanged(nameof(FilteredApplications));

    [RelayCommand]
    private async Task RefreshApplicationsAsync()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Scanning desktop applications...";

            var result = await _applicationDiscovery.GetApplicationsAsync();

            Applications = new ObservableCollection<ApplicationInfo>(result);
            DiscoveredCount = result.Count;
            OnPropertyChanged(nameof(FilteredApplications));

            StatusMessage = $"Discovery complete · {DiscoveredCount} windows found";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Discovery failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanConnect() => SelectedApplication is not null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectToApplicationAsync()
    {
        var target = SelectedApplication;
        if (target is null)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = $"Reading UI tree of '{target.WindowTitle}'...";

            var root = await _treeReader.ReadAsync(target.MainWindowHandle);

            ConnectedApplication = target;
            RootElements = [root];
            SelectedElement = root;
            IsInspecting = true;

            StatusMessage = $"Connected: {target.WindowTitle} (PID {target.ProcessId}) · {Count(root)} elements";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connect failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void BackToApplications()
    {
        IsInspecting = false;
        RootElements = [];
        SelectedElement = null;
        ConnectedApplication = null;
        StatusMessage = "Ready";
    }

    private bool CanCopySelector() => SelectedElement is not null;

    [RelayCommand(CanExecute = nameof(CanCopySelector))]
    private void CopySelector()
    {
        if (SelectedElement is null)
            return;

        try
        {
            Clipboard.SetText(SelectedElement.Selector);
            StatusMessage = "Selector copied to clipboard";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Clipboard error: {ex.Message}";
        }
    }

    partial void OnIsLoadingChanged(bool value) =>
        ConnectToApplicationCommand.NotifyCanExecuteChanged();

    private static int Count(ElementNode node) => 1 + node.Children.Sum(Count);
}
