using System;
using Microsoft.VisualStudio.Shell;
using TyeExplorer.Services;
using Task = System.Threading.Tasks.Task;

namespace TyeExplorer.Commands
{
	internal sealed class AttachToAllCommand : TyeCommand
	{
		private readonly TyeServicesProvider _tyeServicesProvider;
		private readonly DebuggerAttacher _debuggerAttacher;

		public AttachToAllCommand(TyeServicesProvider tyeServicesProvider, DebuggerAttacher debuggerAttacher) :
			base(PackageIds.TyeExplorer_AttachAll)
		{
			_tyeServicesProvider = tyeServicesProvider;
			_debuggerAttacher = debuggerAttacher;
		}

		protected override async Task ExecuteAsync(object sender, EventArgs e)
		{
			await _tyeServicesProvider.RefreshAsync();
			
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(Package.DisposalToken);
            _debuggerAttacher.Attach(_tyeServicesProvider.AllAttachableReplicas);
		}
	}
}