using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Marionet.UI.ViewModels;
using System;

namespace Marionet.UI.Views
{
    public class MainWindow : Window, IDisposable
    {
        MainWindowViewModel vm = new MainWindowViewModel();

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
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void OnViewModelExitTriggered(object? sender, EventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            vm.ExitTriggered -= OnViewModelExitTriggered;
            base.OnClosed(e);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
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
