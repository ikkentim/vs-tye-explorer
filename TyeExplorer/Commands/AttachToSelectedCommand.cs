using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using TyeExplorer.Services;
using TyeExplorer.Tye.Models;

namespace TyeExplorer.Commands
{
	internal sealed class AttachToSelectedCommand : TyeCommand
	{
		private readonly TyeServicesProvider _tyeServicesProvider;
		private readonly DebuggerAttacher _debuggerAttacher;

		public AttachToSelectedCommand(TyeServicesProvider tyeServicesProvider, DebuggerAttacher debuggerAttacher) :
			base(PackageIds.TyeExplorer_AttachSelected)
		{
			_tyeServicesProvider = tyeServicesProvider;
			_debuggerAttacher = debuggerAttacher;
		}

		protected override void Execute(object sender, EventArgs e)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

			switch (_tyeServicesProvider.SelectedService)
			{
				case V1Service service:
					if (_tyeServicesProvider.IsAttachable(service))
						_debuggerAttacher.Attach(service.Replicas.Values);
					break;
				case V1ReplicaStatus replica:
					if (_tyeServicesProvider.IsAttachable(replica))
						_debuggerAttacher.Attach(replica);
					break;
			}
		}
	}
}