using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Marionet.Core.Windows
{
    internal class InputSender : IDisposable
    {
        private readonly BlockingCollection<Native.INPUT> nextInputs = new BlockingCollection<Native.INPUT>();
        private readonly Thread process;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public InputSender()
        {
            process = new Thread(() =>
            {
                try
                {
                    DateTime previousCheck = DateTime.Now;
                    while (!nextInputs.IsCompleted)
                    {
                        var next = nextInputs.Take(cancellationTokenSource.Token);
                        next.SendSingleInput();
                    }
                }
                catch (OperationCanceledException) { }
            });
            process.Start();
        }

        public void AddInput(Native.INPUT input)
        {
            nextInputs.Add(input);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                    nextInputs.Dispose();
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
