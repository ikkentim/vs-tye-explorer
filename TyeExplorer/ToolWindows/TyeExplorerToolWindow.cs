using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using TyeExplorer.ToolWindows;
using TyeExplorer.Tye;

namespace TyeExplorer
{
	/// <summary>
	/// This class implements the tool window exposed by this package and hosts a user control.
	/// </summary>
	/// <remarks>
	/// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
	/// usually implemented by the package implementer.
	/// <para>
	/// This class derives from the ToolWindowPane class provided from the MPF in order to use its
	/// implementation of the IVsUIElementPane interface.
	/// </para>
	/// </remarks>
	[Guid("f7d617d9-adfa-44fc-8a9a-227ac5721df0")]
	public class TyeExplorerToolWindow : ToolWindowPane
	{
		private TyeExplorerToolWindowControl _control;
		/// <summary>
		/// Initializes a new instance of the <see cref="TyeExplorerToolWindow"/> class.
		/// </summary>
		public TyeExplorerToolWindow() : base(null)
		{
			Caption = "Tye Explorer";

			// This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
			// we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
			// the object returned by the Content property.
			Content = _control = new TyeExplorerToolWindowControl();
			
			ToolBar = new CommandID(ReloadTyeExplorerCommand.CommandSet, TyeExplorerGuids.TyeExplorerToolbar);
			
			// toolbar guide: https://docs.microsoft.com/en-us/visualstudio/extensibility/adding-a-toolbar-to-a-tool-window?view=vs-2019
			
			_control.AttachToReplica += OnAttachToReplica;
			
			// TODO: Remove handler at some point? When?
			TyeApiConnector.Instance.ServicesRequestStarted += OnServicesRequestStarted;
			TyeApiConnector.Instance.ServicesReceived += OnServicesReceived;
		}

		private async void OnAttachToReplica(object sender, AttachToReplicaEventArgs e)
		{
			if (e.Replica.Pid == null)
				return;
			
			// Access DTE on main thread
			var package = Package as TyeExplorerPackage;
			
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			try
			{
				var dte = (DTE) GetService(typeof(DTE));
				
				foreach (Process localProcess in dte.Debugger.LocalProcesses)
				{
					if (e.Replica.Pid == localProcess.ProcessID)
					{
						localProcess.Attach();
						break;
					}
				}
			}
			catch (Exception ex)
			{
				// Show a message box to prove we were here  
				VsShellUtilities.ShowMessageBox(  
					package,  
					$"Failed to attach debugger to process.{Environment.NewLine}{Environment.NewLine}{ex}",  
					"Tye Explorer",  
					OLEMSGICON.OLEMSGICON_CRITICAL,  
					OLEMSGBUTTON.OLEMSGBUTTON_OK,  
					OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);  
			}
		}

		private async void OnServicesReceived(object sender, TyeApiServicesResponse e)
		{
			_control.UnsetWaiting();
			
			if (e.FailureReason != null)
			{
				var package = Package as TyeExplorerPackage;
			
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
				
				VsShellUtilities.ShowMessageBox(  
					package,  
					$"Failed to load services (Is Tye running?).{Environment.NewLine}{Environment.NewLine}{e.FailureReason}",  
					"Tye Explorer",  
					OLEMSGICON.OLEMSGICON_CRITICAL,  
					OLEMSGBUTTON.OLEMSGBUTTON_OK,  
					OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);  
			}
			
			if (e.Services != null)
				_control.SetServices(e.Services);
			
		}

		private void OnServicesRequestStarted(object sender, EventArgs e)
		{
			_control.ClearServices();
			_control.SetWaiting();
		}
	}
}
