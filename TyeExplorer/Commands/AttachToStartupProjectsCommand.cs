﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
			base(PackageIds.TyeExplorer_AttachStartupProjects)
		{
			_tyeServicesProvider = tyeServicesProvider;
			_debuggerAttacher = debuggerAttacher;
			this._logger = _logger;
		}

		protected override async Task ExecuteAsync(object sender, EventArgs e)
		{
			await _tyeServicesProvider.RefreshAsync();

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(Package.DisposalToken);

            var dte = Package.GetService<SDTE, DTE2>();
			
			var startupProjects = (Array) dte.Solution.SolutionBuild.StartupProjects;
            var solutionPath = Path.GetDirectoryName(dte.Solution.FileName);
			if (startupProjects == null || solutionPath == null)
			{
				_logger.Log("No startup projects available.");
				return;
			}
			
            var replicas = new List<V1ReplicaStatus>();
			foreach (string projectPath in startupProjects)
			{
				// Combine the relative project path and the root of the solution to derive the full path of the project
				var normalizedProjectPath = Path.GetFullPath(Path.Combine(solutionPath, projectPath));

				var service = _tyeServicesProvider.Services
					.Where(s => s.Description.RunInfo.Project != null)
					.FirstOrDefault(s => string.Equals(Path.GetFullPath(s.Description.RunInfo.Project), normalizedProjectPath, StringComparison.InvariantCultureIgnoreCase));

				if (service != null && _tyeServicesProvider.IsAttachable(service))
					replicas.AddRange(service.Replicas.Values);
			}

			_debuggerAttacher.Attach(replicas);
		}
	}
}