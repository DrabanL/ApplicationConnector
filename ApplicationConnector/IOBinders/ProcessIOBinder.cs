using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Samples.Debugging.MdbgEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RabanSoft.ApplicationConnector.IOBinders
{
    public static class ProcessIOBinder
    {
        public static event Action<(string process, string message)> OnData;
        public static event Action<Exception> OnError;

        private static readonly Dictionary<int, debugProcess> _attachedProcesses;
        private static readonly MDbgEngine _debugEngine;

        class debugProcess {
            public string name;
            MDbgProcess dbgProcess;

            public debugProcess(string name, int id)
            {
                this.name = name;

                dbgProcess = _debugEngine.Attach(id);
                dbgProcess.CorProcess.OnLogMessage += logmsg;
                dbgProcess.Go().WaitOne();
                Trace.Assert((dbgProcess.StopReason?.ToString() ?? string.Empty).Equals("AttachComplete"));
                dbgProcess.Go();
                Trace.Assert(dbgProcess.StopReason == null);
            }

            public void Detach()
            {
                dbgProcess.CorProcess.OnLogMessage -= logmsg;
                dbgProcess.CorProcess.Stop(0);
                dbgProcess.CorProcess.Detach();
                dbgProcess.CorProcess.Dispose();
                dbgProcess = null;
            }

            private void logmsg(object sender, CorLogMessageEventArgs e)
            {
                try
                {
                    OnData?.Invoke((name, e.Message));
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                }
            }
        }

        static ProcessIOBinder()
        {
            _attachedProcesses = new Dictionary<int, debugProcess>();
            _debugEngine = new MDbgEngine();
        }

        public static void Attach(string processName)
        {
            var processID = Process.GetProcessesByName(processName).FirstOrDefault()?.Id ?? -1;
            if (processID == -1)
                return;

            _attachedProcesses.Add(processID, new debugProcess(processName, processID));
        }

        public static void DetachAll() => _attachedProcesses.Values.Select(v => v.name).ToList().ForEach(processName => Detach(processName));

        public static void Detach(string processName)
        {
            foreach(var process in Process.GetProcessesByName(processName))
            {
                if (!_attachedProcesses.TryGetValue(process.Id, out var obj))
                    continue;

                _attachedProcesses.Remove(process.Id);

                obj.Detach();
            }
        }
    }
}
