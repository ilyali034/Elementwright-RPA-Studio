using System.Windows;
using Elementwright.Core.Automation;
using Elementwright.Core.Discovery;
using Elementwright.Studio.ViewModels;

namespace Elementwright.Studio;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var viewModel = new MainViewModel(new WindowsApplicationDiscovery(), new UiaTreeReader());
        var window = new MainWindow { DataContext = viewModel };
        MainWindow = window;
        window.Show();

        if (viewModel.RefreshApplicationsCommand.CanExecute(null))
            viewModel.RefreshApplicationsCommand.Execute(null);
    }
}
