using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace TyeExplorer.Commands
{
	internal abstract class TyeCommand
	{
		private readonly Guid _commandSet;
		private readonly int _commandId;

		protected TyeCommand(int commandId) : this(PackageGuids.guidTyeExplorerCommandsAndMenus, commandId)
		{
		}

		protected TyeCommand(Guid commandSet, int commandId)
		{
			_commandSet = commandSet;
			_commandId = commandId;
		}
		
		protected AsyncPackage Package { get; private set; }
		
		protected IAsyncServiceProvider ServiceProvider => Package;

		protected MenuCommand MenuCommand { get; private set; }
		
		public virtual async Task Initialize(AsyncPackage package)
		{
			Package = package ?? throw new ArgumentNullException(nameof(package));

			var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
			
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			var menuCommandId = new CommandID(_commandSet, _commandId);
			MenuCommand = new MenuCommand(Execute, menuCommandId);
			
			commandService.AddCommand(MenuCommand);
		}

		protected virtual Task ExecuteAsync(object sender, EventArgs e)
		{
			return Task.CompletedTask;
		}
		
		protected virtual void Execute(object sender, EventArgs e)
		{
			Package.JoinableTaskFactory.RunAsync(async delegate
			{
				await ExecuteAsync(sender, e);
			});
		}
	}
}