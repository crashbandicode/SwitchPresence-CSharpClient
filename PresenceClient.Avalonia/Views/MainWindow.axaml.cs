using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PresenceClient.Avalonia.ViewModels;
using System;
using System.ComponentModel;
using PresenceClient.Avalonia.Models;

namespace PresenceClient.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        private TrayIcon? _trayIcon;

        public MainWindow()
        {
            InitializeComponent();

            // Retrieve the TrayIcon defined in App.axaml (assuming it's the only one).
            var icons = TrayIcon.GetIcons(Application.Current);
            if (icons != null && icons.Count > 0)
            {
                _trayIcon = icons[0];
                if (_trayIcon != null)
                {
                    // Set the command for the tray icon itself (e.g., left-click).
                    _trayIcon.Command = new ActionCommand(ShowMainWindow);
                    
                    // Set commands for the menu flyout items.
                    if (_trayIcon.Menu?.Items[0] is NativeMenuItem showItem)
                    {
                        showItem.Command = new ActionCommand(ShowMainWindow);
                    }
                    if (_trayIcon.Menu?.Items[1] is NativeMenuItem exitItem)
                    {
                        exitItem.Command = new ActionCommand(ExitApplication);
                    }
                }
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                if (vm.MinimizeToTray)
                {
                    e.Cancel = true;
                    this.Hide();
                    if (_trayIcon != null)
                    {
                        _trayIcon.IsVisible = true;
                    }
                }
                else
                {
                    vm.StopClient();
                }
            }
            base.OnClosing(e);
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            if (_trayIcon != null)
            {
                _trayIcon.IsVisible = false;
            }
        }

        private void ExitApplication()
        {
            if (DataContext is MainViewModel vm)
            {
                vm.StopClient();
            }
            (Application.Current?.ApplicationLifetime as IControlledApplicationLifetime)?.Shutdown();
        }
    }
}