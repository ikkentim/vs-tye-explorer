using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using TyeExplorer.Services;
using TyeExplorer.Tye;

namespace TyeExplorer.Commands
{
	internal sealed class OpenTyeDashboardCommand :TyeCommand
	{
		public OpenTyeDashboardCommand() : 
			base(new Guid(TyeExplorerGuids.GuidTyeExplorerCommandsAndMenus),
				TyeExplorerGuids.OpenDashboard)
		{
		}
		
		protected override void Execute(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			Process.Start(TyeServicesProvider.DashboardUrl);
		}
	}
}
