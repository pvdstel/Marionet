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
        private bool isWaiting;
        private string selfName = default!;
        private List<string> knownHosts = default!;
        private string? selectedHost = default;
        private bool preventClose = true;

        public MainWindowViewModel()
        {
            StartSupervisorCommand = ReactiveCommand.Create(StartSupervisor, this.WhenAnyValue(x => x.IsSupervisorRunning, x => x.PreventClose, x => x.IsWaiting, (v, c, w) => !w && !v && c));
            StopSupervisorCommand = ReactiveCommand.Create(StopSupervisor, this.WhenAnyValue(x => x.IsSupervisorRunning, x => x.PreventClose, x => x.IsWaiting, (v, c, w) => !w && v && c));
            MoveSelectedHostUpCommand = ReactiveCommand.Create<string>(MoveSelectedHostUp, this.WhenAnyValue(x => x.KnownHosts, x => x.SelectedHost, x => x.IsWaiting, (k, s, w) => !w && !string.IsNullOrEmpty(s) && k.IndexOf(s) > 0));
            MoveSelectedHostDownCommand = ReactiveCommand.Create<string>(MoveSelectedHostDown, this.WhenAnyValue(x => x.KnownHosts, x => x.SelectedHost, x => x.IsWaiting, (k, s, w) =>
            {
                if (!w || string.IsNullOrEmpty(s)) { return false; }
                int index = k.IndexOf(s);
                return index >= 0 && index < k.Count - 1;
            }));
            OpenSettingsFileCommand = ReactiveCommand.Create(OpenSettingsFile);
            OpenSettingsDirectoryCommand = ReactiveCommand.Create(OpenSettingsDirectory);
            ExitApplicationCommand = ReactiveCommand.Create(ExitApplication, this.WhenAnyValue(x => x.PreventClose, x => x.IsWaiting, (c, w) => !w && c));

            Supervisor.Started += OnSupervisorStarted;
            Supervisor.Stopped += OnSupervisorStopped;
            Supervisor.RunningAllowedUpdated += OnSupervisorRunningAllowedUpdated;
            Supervisor.HostRunningUpdated += OnHostRunningUpdated;
            Config.SettingsReloaded += OnSettingsReloaded;

            IsSupervisorRunning = Supervisor.Running;
            IsRunningAllowed = Supervisor.RunningAllowed;
            IsHostRunning = Supervisor.HostRunning;
            SelfName = Config.Instance.Self;
            KnownHosts = Config.Instance.Desktops.ToList();

            var systemPermissions = PlatformSelector.GetSystemPersmissions();
            IsAdmin = systemPermissions.IsAdmin().Result;
            HasUiAccess = systemPermissions.HasUiAccess().Result;
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

        public string SelfName
        {
            get => selfName;
            private set
            {
                this.RaiseAndSetIfChanged(ref selfName, value);
            }
        }

        public bool IsWaiting
        {
            get => isWaiting;
            private set
            {
                this.RaiseAndSetIfChanged(ref isWaiting, value);
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

        public bool IsAdmin { get; }

        public bool HasUiAccess { get; }

        public ReactiveCommand<Unit, Unit> StartSupervisorCommand { get; }

        public ReactiveCommand<Unit, Unit> StopSupervisorCommand { get; }

        public ReactiveCommand<string, Unit> MoveSelectedHostUpCommand { get; }

        public ReactiveCommand<string, Unit> MoveSelectedHostDownCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenSettingsFileCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenSettingsDirectoryCommand { get; }

        public ReactiveCommand<Unit, Unit> ExitApplicationCommand { get; }

        private void StartSupervisor()
        {
            IsWaiting = true;
            _ = Supervisor.StartAsync();
        }

        private void StopSupervisor()
        {
            IsWaiting = true;
            _ = Supervisor.Stop();
        }

        private void MoveSelectedHostUp(string which)
        {
            IsWaiting = false;
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
            IsWaiting = false;
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
            IsWaiting = true;
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
                IsWaiting = false;
            });
        }

        private void OnSupervisorStopped(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsSupervisorRunning = false;
                IsWaiting = false;
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
                SelfName = Config.Instance.Self;
                KnownHosts = Config.Instance.Desktops.ToList();
                IsWaiting = false;
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
