using Avalonia.Threading;
using Marionet.App;
using Marionet.App.Configuration;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace Marionet.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private bool isSupervisorRunning;
        private bool isRunningAllowed;
        private bool isHostRunning;
        private List<string> knownHosts = default!;
        private string? selectedHost = default;
        private bool preventClose = true;

        public MainWindowViewModel()
        {
            StartSupervisorCommand = ReactiveCommand.Create(StartSupervisor, this.WhenAnyValue(x => x.IsSupervisorRunning, x => x.PreventClose, (v, c) => !v && c));
            StopSupervisorCommand = ReactiveCommand.Create(StopSupervisor, this.WhenAnyValue(x => x.IsSupervisorRunning, x => x.PreventClose, (v, c) => v && c));
            MoveSelectedHostUpCommand = ReactiveCommand.Create<string>(MoveSelectedHostUp, this.WhenAnyValue(x => x.KnownHosts, x => x.SelectedHost, (k, s) => !string.IsNullOrEmpty(s) && k.IndexOf(s) > 0));
            MoveSelectedHostDownCommand = ReactiveCommand.Create<string>(MoveSelectedHostDown, this.WhenAnyValue(x => x.KnownHosts, x => x.SelectedHost, (k, s) =>
            {
                if (string.IsNullOrEmpty(s)) { return false; }
                int index = k.IndexOf(s);
                return index >= 0 && index < k.Count - 1;
            }));
            OpenSettingsFileCommand = ReactiveCommand.Create(OpenSettingsFile);
            OpenSettingsDirectoryCommand = ReactiveCommand.Create(OpenSettingsDirectory);
            ExitApplicationCommand = ReactiveCommand.Create(ExitApplication, this.WhenAnyValue(x => x.PreventClose));

            Supervisor.Started += OnSupervisorStarted;
            Supervisor.Stopped += OnSupervisorStopped;
            Supervisor.RunningAllowedUpdated += OnSupervisorRunningAllowedUpdated;
            Supervisor.HostRunningUpdated += OnHostRunningUpdated;
            Config.SettingsReloaded += OnSettingsReloaded;

            IsSupervisorRunning = Supervisor.Running;
            IsRunningAllowed = Supervisor.RunningAllowed;
            IsHostRunning = Supervisor.HostRunning;
            KnownHosts = Config.Instance.Desktops.ToList();
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

        public bool IsHostRunning
        {
            get => isHostRunning;
            private set
            {
                this.RaiseAndSetIfChanged(ref isHostRunning, value);
            }
        }

        public List<string> KnownHosts
        {
            get => knownHosts;
            private set
            {
                this.RaiseAndSetIfChanged(ref knownHosts, value);
            }
        }

        public string? SelectedHost
        {
            get => selectedHost;
            set
            {
                this.RaiseAndSetIfChanged(ref selectedHost, value);
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

        public ReactiveCommand<string, Unit> MoveSelectedHostUpCommand { get; }

        public ReactiveCommand<string, Unit> MoveSelectedHostDownCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenSettingsFileCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenSettingsDirectoryCommand { get; }

        public ReactiveCommand<Unit, Unit> ExitApplicationCommand { get; }

        private void StartSupervisor()
        {
            _ = Supervisor.StartAsync();
        }

        private static void StopSupervisor()
        {
            _ = Supervisor.Stop();
        }

        private void MoveSelectedHostUp(string which)
        {
            List<string> next = Config.Instance.Desktops.ToList();
            int index = next.IndexOf(which);
            if (index > 0)
            {
                next.RemoveAt(index);
                next.Insert(index - 1, which);
            }
            Config.Instance.Desktops = next;
            _ = Config.Save();
        }

        private void MoveSelectedHostDown(string which)
        {
            List<string> next = Config.Instance.Desktops.ToList();
            int index = next.IndexOf(which);
            if (index >= 0 && index < next.Count - 1)
            {
                next.RemoveAt(index);
                next.Insert(index + 1, which);
            }
            Config.Instance.Desktops = next;
            _ = Config.Save();
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
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsSupervisorRunning = true;
            });
        }

        private void OnSupervisorStopped(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsSupervisorRunning = false;
            });
        }

        private void OnSupervisorRunningAllowedUpdated(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsRunningAllowed = Supervisor.RunningAllowed;
            });
        }

        private void OnHostRunningUpdated(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsHostRunning = Supervisor.HostRunning;
            });
        }

        private void OnSettingsReloaded(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                KnownHosts = Config.Instance.Desktops.ToList();
            });
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
