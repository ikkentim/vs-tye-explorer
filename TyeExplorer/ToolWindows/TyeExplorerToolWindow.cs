﻿using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using TyeExplorer.Services;
using TyeExplorer.ToolWindows;
using Task = System.Threading.Tasks.Task;

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
			ToolBar = new CommandID(PackageGuids.guidTyeExplorerCommandsAndMenus, PackageIds.TyeExplorer_Toolbar);
			
			_control.AttachToReplica += OnAttachToReplica;
			_control.AttachToService += OnAttachToService;
			_control.SelectedItemChanged += OnSelectedItemChanged;
			_control.ShowServiceLogs += OnShowServiceLogs;
			
			var servicesProvider = TyeExplorerServices.Get<TyeServicesProvider>();
			servicesProvider.ServicesRequestStarted += OnServicesRequestStarted;
			servicesProvider.ServicesReceived += OnServicesReceived;
			servicesProvider.ServiceRequestFailure += ServicesProviderOnServiceRequestFailure;

			var services = servicesProvider.Services;
			if (services != null)
				_control.SetServices(services.ToArray());
		}
		
		private void ServicesProviderOnServiceRequestFailure(object sender, ServiceRequestFailureEventArgs e)
		{
			_control.UnsetWaiting();
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
		
		private void OnAttachToReplica(object sender, ReplicaEventArgs e)
		{
			if (e.Replica.Pid == null)
				return;
			
            ThreadHelper.ThrowIfNotOnUIThread();

			TyeExplorerServices.Get<DebuggerAttacher>().Attach(e.Replica);
		}
		
		private void OnAttachToService(object sender, ServiceEventArgs e)
		{
            ThreadHelper.ThrowIfNotOnUIThread();
			TyeExplorerServices.Get<DebuggerAttacher>().Attach(e.Service.Replicas.Values);
		}
		
		private void OnShowServiceLogs(object sender, ServiceEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			TyeExplorerServices.Get<TyeServiceOutputManager>().Attach(e.Service);
		}

		private void OnSelectedItemChanged(object sender, ItemEventArgs e)
		{
			TyeExplorerServices.Get<TyeServicesProvider>().SelectedService = e.Item;
		}
	}
}
