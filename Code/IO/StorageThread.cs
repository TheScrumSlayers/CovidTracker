using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CovidTracker.Code.IO
{
    /// <summary>
    /// StorageThread encapsulates and manages a specialized thread which manages various storage functions.
    /// </summary>
    public class StorageThread
    {
        private Thread thread;
        private StorageThreadState threadState;
        private CurrentTask currentTask;
        private Exception threadException;
        private uint secondsPastSinceTrace;

        private object storageLock;

        /// <summary>Returns true if the thread should run a maintenance task.</summary>
        private bool PendingMaintenance => FileIO.CurrentEntries >= FileIO.MaxEntries;
        
        /// <summary>Returns true if the thread should write any pending traces from the buffer.</summary>
        private bool PendingTrace => secondsPastSinceTrace > 60 * 10;

        /// <summary>
        /// Create a new storage thread.
        /// </summary>
        /// <param name="storageLock">Shared storage lock.</param>
        public StorageThread(object storageLock)
        {
            this.storageLock = storageLock;
            threadState = StorageThreadState.Idle;
            currentTask = CurrentTask.None;
            thread = new Thread(Loop) {
                Name = "Storage management thread",
                IsBackground = true
            };
        }

        private void Loop()
        {
            while (threadState != StorageThreadState.Shutdown) {
                currentTask = CurrentTask.None;
                threadState = StorageThreadState.Running;

                // First, check if the system should run a storage maintenance task.
                if (PendingMaintenance) {
                    currentTask = CurrentTask.Maintenance;
                    lock (storageLock) {
                        try {
                            MaintenanceTask();
                        } catch (Exception e) {
                            threadException = e;
                            threadState = StorageThreadState.Error;
                        } finally {
                            currentTask = CurrentTask.None;
                        }
                    }
                }

                // Next, write the trace buffer to the disk and/or network if needed.
                if (PendingTrace) {
                    currentTask = CurrentTask.Trace;
                    lock (storageLock) {
                        try {
                            TraceTask();
                        } catch (Exception e) {
                            threadException = e;
                            threadState = StorageThreadState.Error;
                        } finally {
                            currentTask = CurrentTask.None;
                            secondsPastSinceTrace = 0;
                        }
                    }
                }

                if (threadState == StorageThreadState.Error) 
                    continue; // TODO: Handle error.

                // Idle - thread sleep for 1 second.
                threadState = StorageThreadState.Idle;
                currentTask = CurrentTask.None;
                secondsPastSinceTrace += 1;
                Thread.Sleep(1000);
            }
        }

        private void MaintenanceTask()
        {
            // TODO: Maintenance task (remove really old entries).
        }

        private void TraceTask()
        {
            IOReturn ret = TraceIO.WritePendingBuffer();
            if (ret.Status == IOReturnStatus.Fail) {
                throw ret.Exception;
            }
        }

        private enum CurrentTask
        {
            None,
            Maintenance,
            Trace
        }

        private enum StorageThreadState
        {
            Idle,
            Running,
            Error,
            Shutdown
        }
    }
}
