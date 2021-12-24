using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using TyeExplorer.Services;
using TyeExplorer.Tye.Models;

namespace TyeExplorer.Commands
{
	internal sealed class OpenSelectedServiceLoggingCommand : TyeCommand
	{
		private readonly TyeServicesProvider _tyeServicesProvider;
		private readonly TyeServiceOutputManager _tyeServiceOutputManager;

		public OpenSelectedServiceLoggingCommand(TyeServicesProvider tyeServicesProvider, TyeServiceOutputManager tyeServiceOutputManager) :
			base(PackageIds.TyeExplorer_OpenLoggingSelected)
		{
			_tyeServicesProvider = tyeServicesProvider;
			_tyeServiceOutputManager = tyeServiceOutputManager;
		}

        protected override void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            switch (_tyeServicesProvider.SelectedService)
            {
                case V1Service service:
                    _tyeServiceOutputManager.Attach(service);
                    break;
                case V1ReplicaStatus replica:
                    var replicaService = _tyeServicesProvider.Services.FirstOrDefault(s => s.Replicas.ContainsKey(replica.Name));
                    if (replicaService != null)
                        _tyeServiceOutputManager.Attach(replicaService);
                    break;
            }
        }
	}
}