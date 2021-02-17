using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using TyeExplorer.Services;

namespace TyeExplorer.Commands
{
	internal sealed class OpenTyeDashboardCommand :TyeCommand
	{
		public OpenTyeDashboardCommand() : 
			base(PackageIds.TyeExplorer_OpenDashboard)
		{
		}
		
		protected override void Execute(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			Process.Start(TyeServicesProvider.DashboardUrl);
		}
	}
}
