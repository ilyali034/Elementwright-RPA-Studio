using System.Windows;
using Elementwright.Core.Automation;
using Elementwright.Studio.ViewModels;

namespace Elementwright.Studio;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        FitToAvailableWorkArea();
    }

    private void FitToAvailableWorkArea()
    {
        var workArea = SystemParameters.WorkArea;

        // Keep the first launch inside the usable desktop area, including the taskbar.
        MinWidth = Math.Min(960, workArea.Width * 0.78);
        MinHeight = Math.Min(640, workArea.Height * 0.78);
        MaxWidth = workArea.Width;
        MaxHeight = workArea.Height;

        Width = Math.Max(MinWidth, Math.Min(1240, workArea.Width * 0.92));
        Height = Math.Max(MinHeight, Math.Min(800, workArea.Height * 0.90));

        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top + (workArea.Height - Height) / 2;
        WindowState = WindowState.Normal;
    }

    // TreeView.SelectedItem is not directly bindable in WPF MVVM.
    private void OnElementSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel vm && e.NewValue is ElementNode node)
            vm.SelectedElement = node;
    }
}
