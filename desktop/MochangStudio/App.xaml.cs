using System.Windows;
using System.Windows.Threading;

namespace MochangStudio;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            StudioDialog.Alert(Current.MainWindow, "燃烧工作室", args.Exception.GetBaseException().Message);
            args.Handled = true;
        };
        base.OnStartup(e);
    }
}
