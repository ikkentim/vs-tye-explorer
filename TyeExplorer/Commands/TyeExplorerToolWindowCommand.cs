using System;
using Task = System.Threading.Tasks.Task;

namespace TyeExplorer.Commands
{
	internal sealed class TyeExplorerToolWindowCommand : TyeCommand
	{
		public TyeExplorerToolWindowCommand()
			: base(new Guid(TyeExplorerGuids.GuidTyeExplorerCommandsAndMenus),
				TyeExplorerGuids.ToolWindowCommandId)
		{
		}

		protected override async Task ExecuteAsync(object sender, EventArgs e)
		{
			var window = await Package.ShowToolWindowAsync(typeof(TyeExplorerToolWindow), 0, true, Package.DisposalToken);
			if (window?.Frame == null)
				throw new NotSupportedException("Cannot create tool window");
		}
	}
}
