using System;
using System.Diagnostics;
using System.Threading;
using Savok.Server.Utils;

namespace Savok.Server.Abstractions {
    public abstract class Task {
        protected abstract int Delay { get; }
        protected abstract int Frequency { get; }
        public Server Server { get; set; }
		
        private CancellationTokenSource TaskToken { get; set; }

        public void Start() {
            TaskToken = new CancellationTokenSource();
            System.Threading.Tasks.Task.Run(() => {
                var s = new Stopwatch();
                while (s.ElapsedMilliseconds < Delay && !TaskToken.IsCancellationRequested) 
                    Thread.Sleep(100);

                if (TaskToken.IsCancellationRequested) return;

                while (!TaskToken.IsCancellationRequested) {
                    s.Restart();
					
                    try {
                        if (!DoWork()) return;
                    } catch (Exception ex) {
                        Log.E("TSK", "An error {0} occurred in {1}: {2}", ex.GetType().Name, GetType().Name,
                            ex.Message);
                    }

                    if (s.ElapsedMilliseconds < Frequency) 
                        Thread.Sleep((int) (Frequency - s.ElapsedMilliseconds));
                }

            }, TaskToken.Token);
        }

        public void Stop() {
            TaskToken.Cancel();
        }

        protected abstract bool DoWork();
    }
}