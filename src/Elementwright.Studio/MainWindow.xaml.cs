using System.Windows;
using Elementwright.Core.Automation;
using Elementwright.Studio.ViewModels;

namespace Elementwright.Studio;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // TreeView.SelectedItem не биндится в MVVM напрямую.
    private void OnElementSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel vm && e.NewValue is ElementNode node)
            vm.SelectedElement = node;
    }
}
