using System;
using System.Linq;
using System.Threading.Tasks;
using TyeExplorer.Services;
using TyeExplorer.Tye.Models;

namespace TyeExplorer.Commands
{
	internal sealed class AttachToSelectedCommand : TyeCommand
	{
		private readonly TyeServicesProvider _tyeServicesProvider;
		private readonly DebuggerAttacher _debuggerAttacher;

		public AttachToSelectedCommand(TyeServicesProvider tyeServicesProvider, DebuggerAttacher debuggerAttacher) :
			base(new Guid(TyeExplorerGuids.TyeExplorerToolbarCmdSet),
				TyeExplorerGuids.AttachToSelectedCommandId)
		{
			_tyeServicesProvider = tyeServicesProvider;
			_debuggerAttacher = debuggerAttacher;
		}

		protected override async Task ExecuteAsync(object sender, EventArgs e)
		{
			switch (_tyeServicesProvider.SelectedService)
			{
				case V1Service service:
					if (_tyeServicesProvider.IsAttachable(service))
						await _debuggerAttacher.Attach(service.Replicas.Values);
					break;
				case V1ReplicaStatus replica:
					if (_tyeServicesProvider.IsAttachable(replica))
						await _debuggerAttacher.Attach(replica);
					break;
			}
		}
	}
	internal sealed class OpenSelectedServiceLoggingCommand : TyeCommand
	{
		private readonly TyeServicesProvider _tyeServicesProvider;
		private readonly TyeServiceOutputManager _tyeServiceOutputManager;

		public OpenSelectedServiceLoggingCommand(TyeServicesProvider tyeServicesProvider, TyeServiceOutputManager tyeServiceOutputManager) :
			base(new Guid(TyeExplorerGuids.TyeExplorerToolbarCmdSet),
				TyeExplorerGuids.OpenSelectedServiceLoggingCommandId)
		{
			_tyeServicesProvider = tyeServicesProvider;
			_tyeServiceOutputManager = tyeServiceOutputManager;
		}

		protected override async Task ExecuteAsync(object sender, EventArgs e)
		{
			switch (_tyeServicesProvider.SelectedService)
			{
				case V1Service service:
					await _tyeServiceOutputManager.Attach(service);
					break;
				case V1ReplicaStatus replica:
					var replicaService = _tyeServicesProvider.Services.FirstOrDefault(s => s.Replicas.ContainsKey(replica.Name));
					if (replicaService != null)
						await _tyeServiceOutputManager.Attach(replicaService);
					break;
			}
		}
	}
}