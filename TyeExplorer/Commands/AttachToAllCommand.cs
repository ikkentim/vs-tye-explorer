using System;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using TyeExplorer.Services;
using TyeExplorer.Tye;
using Task = System.Threading.Tasks.Task;

namespace TyeExplorer.Commands
{
	internal sealed class AttachToAllCommand : TyeCommand
	{
		private readonly TyeServicesProvider _tyeServicesProvider;
		private readonly DebuggerAttacher _debuggerAttacher;

		public AttachToAllCommand(TyeServicesProvider tyeServicesProvider, DebuggerAttacher debuggerAttacher) :
			base(new Guid(TyeExplorerGuids.TyeExplorerToolbarCmdSet),
				TyeExplorerGuids.AttachToAllCommandId)
		{
			_tyeServicesProvider = tyeServicesProvider;
			_debuggerAttacher = debuggerAttacher;
		}

		protected override async Task ExecuteAsync(object sender, EventArgs e)
		{
			await _tyeServicesProvider.Refresh();
			await _debuggerAttacher.Attach(_tyeServicesProvider.AllAttachableReplicas);
		}
	}
}