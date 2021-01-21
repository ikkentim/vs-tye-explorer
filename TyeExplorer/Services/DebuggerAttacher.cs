using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using TyeExplorer.Tye.Models;
using Task = System.Threading.Tasks.Task;

namespace TyeExplorer.Services
{
	public class DebuggerAttacher
	{
		private readonly AsyncPackage _package;

		public DebuggerAttacher(AsyncPackage package)
		{
			_package = package;
		}

		public Task Attach(params V1ReplicaStatus[] replicas)
		{
			return Attach((IEnumerable<V1ReplicaStatus>) replicas);
		}
		
		public async Task Attach(IEnumerable<V1ReplicaStatus> replicas)
		{
			var replicaList = replicas.ToList();
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
			
			var dte = (DTE) await _package.GetServiceAsync(typeof(DTE));
			
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);

			try
			{
				foreach (var localProcess in
					dte.Debugger.LocalProcesses.Cast<Process>()
						.Where(localProcess => replicaList.Any(r => r.Pid == localProcess.ProcessID)))
				{
					localProcess.Attach();
				}
			}
			catch (Exception ex)
			{
				// Show a message box to prove we were here  
				VsShellUtilities.ShowMessageBox(  
					_package,  
					$"Failed to attach debugger to process.{Environment.NewLine}{Environment.NewLine}{ex}",  
					"Tye Explorer",  
					OLEMSGICON.OLEMSGICON_CRITICAL,  
					OLEMSGBUTTON.OLEMSGBUTTON_OK,  
					OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);  
			}
		}
	}
}
