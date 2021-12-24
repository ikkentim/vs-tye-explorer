using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using TyeExplorer.Tye.Models;
using Task = System.Threading.Tasks.Task;

namespace TyeExplorer.Services
{
	public class DebuggerAttacher
	{
		private readonly AsyncPackage _package;
		private readonly TyeExplorerLogger _logger;

		public DebuggerAttacher(AsyncPackage package, TyeExplorerLogger logger)
		{
			_package = package;
			_logger = logger;
		}

		public void Attach(params V1ReplicaStatus[] replicas)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

			Attach((IEnumerable<V1ReplicaStatus>) replicas);
		}
		
		public void Attach(IEnumerable<V1ReplicaStatus> replicas)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

			var replicaList = replicas.ToList();

			_logger.Log($"Attaching to {replicaList.Count} replicas");
			foreach (var replica in replicaList)
			{
				_logger.Log($"Attaching {replica.Name} (PID: {replica.Pid}, State: {replica.State})");
			}

            var dte = _package.GetService<SDTE, DTE2>();

			try
			{
				var attachedReplicas = new List<V1ReplicaStatus>();
                foreach (var localProcess in dte.Debugger.LocalProcesses.Cast<Process>())
                {
                    var pid = localProcess.ProcessID;
                    var replica = replicaList.FirstOrDefault(r => r.Pid == pid);

                    if (replica == null)
                    {
                        continue;
                    }

                    attachedReplicas.Add(replica);
                    localProcess.Attach();
                }

                var unattachedReplicas = replicaList.Except(attachedReplicas).ToList();
				if (unattachedReplicas.Any())
				{
					_logger.Log($"Did not attach to replicas {string.Join(", ", unattachedReplicas.Select(r => r.Name))}.");
				}
			}
			catch (Exception ex)
			{
				_logger.Log($"Failed to attach debugger to process. {ex}");
			}
		}
	}
}
