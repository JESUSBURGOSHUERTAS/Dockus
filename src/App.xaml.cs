using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace Dockus;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
        {
            try { File.WriteAllText(Path.Combine(baseDir, "crash_domain.log"), ev.ExceptionObject.ToString()); } catch { }
        };

        DispatcherUnhandledException += (s, ev) =>
        {
            try { File.WriteAllText(Path.Combine(baseDir, "crash_dispatcher.log"), ev.Exception.ToString()); } catch { }
            ev.Handled = true;
        };

        base.OnStartup(e);
    }
}
