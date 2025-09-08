using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace KitchenWise.Desktop
{
    public partial class App : Application
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Allocate a console window to see output
            AllocConsole();
            Console.Title = "KitchenWise Debug Console";

            Console.WriteLine("===========================================");
            Console.WriteLine("🍽️ KitchenWise Application Starting...");
            Console.WriteLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("===========================================");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Console.WriteLine("KitchenWise Application Exiting...");
            base.OnExit(e);
        }
    }
}