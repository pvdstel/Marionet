using Marionet.App;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;

namespace Marionet.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private bool isSupervisorRunning;
        private bool isRunningAllowed;
        private bool preventClose = true;

        public MainWindowViewModel()
        {
            StartSupervisorCommand = ReactiveCommand.Create(StartSupervisor, this.WhenAnyValue(x => x.IsSupervisorRunning, x => x.PreventClose, (v, c) => !v && c));
            StopSupervisorCommand = ReactiveCommand.Create(StopSupervisor, this.WhenAnyValue(x => x.IsSupervisorRunning, x => x.PreventClose, (v, c) => v && c));
            OpenSettingsFileCommand = ReactiveCommand.Create(OpenSettingsFile);
            OpenSettingsDirectoryCommand = ReactiveCommand.Create(OpenSettingsDirectory);
            ExitApplicationCommand = ReactiveCommand.Create(ExitApplication, this.WhenAnyValue(x => x.PreventClose));

            Supervisor.Started += OnSupervisorStarted;
            Supervisor.Stopped += OnSupervisorStopped;
            Supervisor.RunningAllowedUpdated += OnSupervisorRunningAllowedUpdated;

            IsSupervisorRunning = Supervisor.Running;
            IsRunningAllowed = Supervisor.RunningAllowed;
        }

        public event EventHandler? ExitTriggered;

        public bool IsSupervisorRunning
        {
            get => isSupervisorRunning;
            private set
            {
                this.RaiseAndSetIfChanged(ref isSupervisorRunning, value);
            }
        }

        public bool IsRunningAllowed
        {
            get => isRunningAllowed;
            private set
            {
                this.RaiseAndSetIfChanged(ref isRunningAllowed, value);
            }
        }

        public bool PreventClose
        {
            get => preventClose;
            private set
            {
                this.RaiseAndSetIfChanged(ref preventClose, value);
            }
        }

        public ReactiveCommand<Unit, Unit> StartSupervisorCommand { get; }

        public ReactiveCommand<Unit, Unit> StopSupervisorCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenSettingsFileCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenSettingsDirectoryCommand { get; }

        public ReactiveCommand<Unit, Unit> ExitApplicationCommand { get; }

        private void StartSupervisor()
        {
            _ = Supervisor.StartAsync();
        }

        private static void StopSupervisor()
        {
            Supervisor.Stop();
        }

        private void OpenSettingsFile()
        {
            using Process process = new Process();
            process.StartInfo.FileName = App.Configuration.Config.ConfigurationFile;
            process.StartInfo.UseShellExecute = true;
            process.Start();
        }

        private void OpenSettingsDirectory()
        {
            using Process process = new Process();
            process.StartInfo.FileName = App.Configuration.Config.ConfigurationDirectory;
            process.StartInfo.UseShellExecute = true;
            process.Start();
        }

        private async void ExitApplication()
        {
            bool wasRunning = IsSupervisorRunning;
            StopSupervisor();
            PreventClose = false;
            if (wasRunning)
            {
                await Task.Delay(500);
            }
            ExitTriggered?.Invoke(this, new EventArgs());
            Program.ShutdownApplication();
        }

        private void OnSupervisorStarted(object? sender, EventArgs e)
        {
            IsSupervisorRunning = true;
        }

        private void OnSupervisorStopped(object? sender, EventArgs e)
        {
            IsSupervisorRunning = false;
        }

        private void OnSupervisorRunningAllowedUpdated(object? sender, EventArgs e)
        {
            IsRunningAllowed = Supervisor.RunningAllowed;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Supervisor.Started -= OnSupervisorStarted;
                    Supervisor.Stopped -= OnSupervisorStopped;
                    Supervisor.RunningAllowedUpdated -= OnSupervisorRunningAllowedUpdated;
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
