using System;
using TyeExplorer.Services;
using TyeExplorer.Tye;
using Task = System.Threading.Tasks.Task;

namespace TyeExplorer.Commands
{
	internal sealed class ReloadTyeExplorerCommand : TyeCommand
	{
		private readonly TyeServicesProvider _apiConnector;

		public ReloadTyeExplorerCommand(TyeServicesProvider apiConnector) :
			base(new Guid(TyeExplorerGuids.GuidTyeExplorerCommandsAndMenus),
				TyeExplorerGuids.ReloadTyeExplorerCommandId)
		{
			_apiConnector = apiConnector;
		}

		protected override async Task ExecuteAsync(object sender, EventArgs e)
		{
			await _apiConnector.Refresh();
		}
	}
}
