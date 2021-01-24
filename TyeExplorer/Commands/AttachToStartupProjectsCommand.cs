using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using TyeExplorer.Services;
using TyeExplorer.Tye.Models;
using Task = System.Threading.Tasks.Task;

namespace TyeExplorer.Commands
{
	internal sealed class AttachToStartupProjectsCommand : TyeCommand
	{
		private readonly TyeServicesProvider _tyeServicesProvider;
		private readonly DebuggerAttacher _debuggerAttacher;
		private readonly TyeExplorerLogger _logger;

		public AttachToStartupProjectsCommand(TyeServicesProvider tyeServicesProvider, DebuggerAttacher debuggerAttacher, TyeExplorerLogger _logger) :
			base(new Guid(TyeExplorerGuids.GuidTyeExplorerCommandsAndMenus),
				TyeExplorerGuids.AttachToStartupProjectsCommandId)
		{
			_tyeServicesProvider = tyeServicesProvider;
			_debuggerAttacher = debuggerAttacher;
			this._logger = _logger;
		}

		protected override async Task ExecuteAsync(object sender, EventArgs e)
		{
			await _tyeServicesProvider.Refresh();
			
			var dte = (DTE)await Package.GetServiceAsync(typeof(DTE));
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(Package.DisposalToken);

			var replicas = new List<V1ReplicaStatus>();
				
			var startupProjects = (Array) dte.Solution.SolutionBuild.StartupProjects;
			if (startupProjects == null)
			{
				_logger.Log("No startup projects available.");
				return;
			}

			foreach (string projectPath in startupProjects)
			{
				var projectName = Path.GetFileNameWithoutExtension(projectPath);
				var service = _tyeServicesProvider.Services.FirstOrDefault(s =>
					string.Equals(s.Description.Name, projectName, StringComparison.InvariantCultureIgnoreCase));

				if (service != null && _tyeServicesProvider.IsAttachable(service))
					replicas.AddRange(service.Replicas.Values);
			}

			await _debuggerAttacher.Attach(replicas);
		}
	}
}