using Avalonia;
using Avalonia.ReactiveUI;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;

namespace FactoryPlanner
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            XmlConfigurator.Configure(new FileInfo(".\\log4net.config"));

            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UseReactiveUI()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
