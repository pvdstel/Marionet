using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Marionet.UI.Native.Windows;
using Marionet.UI.ViewModels;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace Marionet.UI.Views
{
    public class MainWindow : Window, IDisposable
    {
        private readonly MainWindowViewModel vm = new MainWindowViewModel();
        private BasicNotifyIcon? notifyIcon;
        private Icon? trayIconImage;

        public MainWindow()
        {
            DataContext = vm;
            InitializeComponent();

            vm.ExitTriggered += OnViewModelExitTriggered;

            Closing += (sender, e) =>
            {
                if (vm.PreventClose)
                {
                    e.Cancel = true;
                    Hide();
                }
            };

            SetUpTrayIcon();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SetUpTrayIcon()
        {
            if (BasicNotifyIcon.IsSupported())
            {
                Assembly current = Assembly.GetExecutingAssembly();
                using Stream? iconStream = current.GetManifestResourceStream("Marionet.UI.Assets.tray-icon.ico");
                if (iconStream == null)
                {
                    throw new FileNotFoundException("The icon resource could not be loaded.");
                }
                trayIconImage = new Icon(iconStream);
                notifyIcon = new BasicNotifyIcon(trayIconImage)
                {
                    Text = "Marionet",
                    Visible = vm.ConfigurationService.Configuration.ShowTrayIcon
                };
                notifyIcon.Clicked += OnNotifyIconClicked;
            }

            vm.ConfigurationService.ConfigurationChanged += OnConfigurationChanged;
        }

        private void OnViewModelExitTriggered(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnNotifyIconClicked(object? sender, EventArgs e)
        {
            Show();
        }

        private void OnConfigurationChanged(object? sender, EventArgs e)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = vm.ConfigurationService.Configuration.ShowTrayIcon;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Dispose(true);
            base.OnClosed(e);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    vm.ExitTriggered -= OnViewModelExitTriggered;
                    vm.ConfigurationService.ConfigurationChanged -= OnConfigurationChanged;

                    if (notifyIcon != null)
                    {
                        notifyIcon.Clicked -= OnNotifyIconClicked;
                        notifyIcon.Dispose();
                    }
                    trayIconImage?.Dispose();
                    vm.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
