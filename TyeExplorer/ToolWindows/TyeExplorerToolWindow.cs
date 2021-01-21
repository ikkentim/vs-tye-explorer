using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using TyeExplorer.Services;
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
		private readonly TyeExplorerToolWindowControl _control;
		
		public TyeExplorerToolWindow() : base(null)
		{
			Caption = "Tye Explorer";

			// This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
			// we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
			// the object returned by the Content property.
			Content = _control = new TyeExplorerToolWindowControl();
			ToolBar = new CommandID(new Guid(TyeExplorerGuids.TyeExplorerToolbarCmdSet), TyeExplorerGuids.TyeExplorerToolbar);
			
			_control.AttachToReplica += OnAttachToReplica;
			_control.SelectedItemChanged += OnSelectedItemChanged;
			
			// TODO: Remove handler at some point? When?
			var servicesProvider = TyeExplorerServices.Get<TyeServicesProvider>();
			servicesProvider.ServicesRequestStarted += OnServicesRequestStarted;
			servicesProvider.ServicesReceived += OnServicesReceived;
			servicesProvider.ServiceRequestFailure += ServicesProviderOnServiceRequestFailure;
		}

		private void ServicesProviderOnServiceRequestFailure(object sender, ServiceRequestFailureEventArgs e)
		{
			_control.UnsetWaiting();
		}

		private async void OnAttachToReplica(object sender, AttachToReplicaEventArgs e)
		{
			if (e.Replica.Pid == null)
				return;

			await TyeExplorerServices.Get<DebuggerAttacher>().Attach(e.Replica);
		}

		private void OnSelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
		{
			TyeExplorerServices.Get<TyeServicesProvider>().SelectedService = e.Item;
		}

		private void OnServicesReceived(object sender, ServicesReceivedEventArgs e)
		{
			_control.UnsetWaiting();
			
			if (e.Services != null)
				_control.SetServices(e.Services);
		}

		private void OnServicesRequestStarted(object sender, EventArgs e)
		{
			_control.SetWaiting();
		}
	}
}
