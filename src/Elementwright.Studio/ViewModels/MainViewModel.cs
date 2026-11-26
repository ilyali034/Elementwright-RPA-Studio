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
    private readonly UiaElementFinder _elementFinder;
    private readonly UiaElementActions _elementActions;

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

    [ObservableProperty]
    private bool isInspecting;

    [ObservableProperty]
    private ApplicationInfo? connectedApplication;

    [ObservableProperty]
    private ObservableCollection<ElementNode> rootElements = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopySelectorCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestSelectorCommand))]
    [NotifyCanExecuteChangedFor(nameof(InvokeSelectedElementCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetSelectedElementValueCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleSelectedElementCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectSelectedElementCommand))]
    private ElementNode? selectedElement;

    [ObservableProperty]
    private string valueInput = string.Empty;

    [ObservableProperty]
    private string selectorTestResult = "Select an element and validate its selector.";

    public IEnumerable<ApplicationInfo> FilteredApplications =>
        string.IsNullOrWhiteSpace(SearchText)
            ? Applications
            : Applications.Where(x =>
                x.WindowTitle.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                x.ProcessName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));

    public MainViewModel(
        IApplicationDiscovery applicationDiscovery,
        UiaTreeReader treeReader,
        UiaElementFinder? elementFinder = null,
        UiaElementActions? elementActions = null)
    {
        _applicationDiscovery = applicationDiscovery;
        _treeReader = treeReader;
        _elementFinder = elementFinder ?? new UiaElementFinder();
        _elementActions = elementActions ?? new UiaElementActions();
    }

    partial void OnSearchTextChanged(string value) =>
        OnPropertyChanged(nameof(FilteredApplications));

    partial void OnSelectedElementChanged(ElementNode? value)
    {
        ValueInput = value?.Value ?? string.Empty;
        SelectorTestResult = "Selector not validated yet.";
    }

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
        if (target is null || IsLoading)
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
            StatusMessage = $"Connected: {target.WindowTitle} · {Count(root)} elements loaded";
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
    private async Task RefreshTreeAsync()
    {
        var target = ConnectedApplication;
        if (target is null || IsLoading)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Refreshing UI Automation tree...";
            var root = await _treeReader.ReadAsync(target.MainWindowHandle);
            RootElements = [root];
            SelectedElement = root;
            StatusMessage = $"Tree refreshed · {Count(root)} elements";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Tree refresh failed: {ex.Message}";
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
        SelectorTestResult = "Select an element and validate its selector.";
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

    private bool CanTestSelector() => SelectedElement is not null && ConnectedApplication is not null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanTestSelector))]
    private async Task TestSelectorAsync()
    {
        if (SelectedElement is null || ConnectedApplication is null || IsLoading)
            return;

        try
        {
            IsLoading = true;
            var selector = ElementSelector.FromNode(SelectedElement);
            var found = await _elementFinder.FindFirstAsync(ConnectedApplication.MainWindowHandle, selector);
            SelectorTestResult = found is null
                ? "NOT FOUND · No matching element was found."
                : "FOUND · The selector resolves to an element.";
            StatusMessage = found is null ? "Selector validation failed" : "Selector validation passed";
        }
        catch (Exception ex)
        {
            SelectorTestResult = $"ERROR · {ex.Message}";
            StatusMessage = "Selector validation failed";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanInvokeSelectedElement() =>
        SelectedElement is { IsEnabled: true, HasInvokePattern: true } &&
        ConnectedApplication is not null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanInvokeSelectedElement))]
    private async Task InvokeSelectedElementAsync()
    {
        if (SelectedElement is null || ConnectedApplication is null || IsLoading)
            return;

        try
        {
            IsLoading = true;
            var element = await _elementFinder.FindFirstAsync(
                ConnectedApplication.MainWindowHandle, ElementSelector.FromNode(SelectedElement));
            if (element is null)
                throw new InvalidOperationException("The element could not be found again. Refresh the tree.");
            await Task.Run(() => _elementActions.Invoke(element));
            StatusMessage = "Invoke action completed";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Invoke failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSetSelectedElementValue() =>
        SelectedElement is { IsEnabled: true, HasValuePattern: true } &&
        ConnectedApplication is not null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanSetSelectedElementValue))]
    private async Task SetSelectedElementValueAsync()
    {
        if (SelectedElement is null || ConnectedApplication is null || IsLoading)
            return;

        try
        {
            IsLoading = true;
            var element = await _elementFinder.FindFirstAsync(
                ConnectedApplication.MainWindowHandle, ElementSelector.FromNode(SelectedElement));
            if (element is null)
                throw new InvalidOperationException("The element could not be found again. Refresh the tree.");
            await Task.Run(() => _elementActions.SetValue(element, ValueInput));
            StatusMessage = "Value updated";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Set value failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanToggleSelectedElement() =>
        SelectedElement is { IsEnabled: true, HasTogglePattern: true } &&
        ConnectedApplication is not null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanToggleSelectedElement))]
    private async Task ToggleSelectedElementAsync()
    {
        if (SelectedElement is null || ConnectedApplication is null || IsLoading)
            return;
        try
        {
            IsLoading = true;
            var element = await _elementFinder.FindFirstAsync(
                ConnectedApplication.MainWindowHandle, ElementSelector.FromNode(SelectedElement));
            if (element is null)
                throw new InvalidOperationException("The element could not be found again. Refresh the tree.");
            await Task.Run(() => _elementActions.Toggle(element));
            StatusMessage = "Toggle action completed";
        }
        catch (Exception ex) { StatusMessage = $"Toggle failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private bool CanSelectSelectedElement() =>
        SelectedElement is { IsEnabled: true, HasSelectionItemPattern: true } &&
        ConnectedApplication is not null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanSelectSelectedElement))]
    private async Task SelectSelectedElementAsync()
    {
        if (SelectedElement is null || ConnectedApplication is null || IsLoading)
            return;
        try
        {
            IsLoading = true;
            var element = await _elementFinder.FindFirstAsync(
                ConnectedApplication.MainWindowHandle, ElementSelector.FromNode(SelectedElement));
            if (element is null)
                throw new InvalidOperationException("The element could not be found again. Refresh the tree.");
            await Task.Run(() => _elementActions.Select(element));
            StatusMessage = "Select action completed";
        }
        catch (Exception ex) { StatusMessage = $"Select failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    partial void OnIsLoadingChanged(bool value)
    {
        ConnectToApplicationCommand.NotifyCanExecuteChanged();
        TestSelectorCommand.NotifyCanExecuteChanged();
        InvokeSelectedElementCommand.NotifyCanExecuteChanged();
        SetSelectedElementValueCommand.NotifyCanExecuteChanged();
        ToggleSelectedElementCommand.NotifyCanExecuteChanged();
        SelectSelectedElementCommand.NotifyCanExecuteChanged();
    }

    private static int Count(ElementNode node) => 1 + node.Children.Sum(Count);
}
