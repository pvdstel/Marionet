using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Marionet.App.Communication
{
    public class ClientIdentifierService : IDisposable
    {
        private readonly Dictionary<string, string> desktopToConnection = new Dictionary<string, string>();
        private readonly Dictionary<string, string> connectionToDesktop = new Dictionary<string, string>();

        private readonly SemaphoreSlim mutationLock = new SemaphoreSlim(1, 1);

        public async Task Add(string connectionId, string desktopName)
        {
            await mutationLock.WaitAsync();
            desktopToConnection[desktopName] = connectionId;
            connectionToDesktop[connectionId] = desktopName;
            mutationLock.Release();
        }

        public async Task Remove(string connectionId)
        {
            await mutationLock.WaitAsync();
            string desktopName = connectionToDesktop[connectionId];
            if (desktopToConnection.ContainsKey(desktopName))
            {
                desktopToConnection.Remove(desktopName);
            }
            if (connectionToDesktop.ContainsKey(connectionId))
            {
                connectionToDesktop.Remove(connectionId);
            }
            mutationLock.Release();
        }

        public async Task<bool> KnowsDesktop(string desktopName)
        {
            await mutationLock.WaitAsync();
            bool containsDesktopName = desktopToConnection.ContainsKey(desktopName);
            mutationLock.Release();
            return containsDesktopName;
        }

        public async Task<bool> KnowsConnection(string connectionId)
        {
            await mutationLock.WaitAsync();
            bool containsConnectionId = connectionToDesktop.ContainsKey(connectionId);
            mutationLock.Release();
            return containsConnectionId;
        }

        public async Task<string?> GetConnectionId(string desktopName)
        {
            await mutationLock.WaitAsync();
            try
            {
                if (desktopToConnection.ContainsKey(desktopName))
                {
                    return desktopToConnection[desktopName];
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                mutationLock.Release();
            }
        }

        public async Task<string?> GetDesktopName(string connectionId)
        {
            await mutationLock.WaitAsync();
            try
            {
                if (connectionToDesktop.ContainsKey(connectionId))
                {
                    return connectionToDesktop[connectionId];
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                mutationLock.Release();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    mutationLock.Dispose();
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
