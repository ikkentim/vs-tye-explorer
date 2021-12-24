using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using TyeExplorer.Tye.Models;
using Task = System.Threading.Tasks.Task;

namespace TyeExplorer.Services
{
	public class TyeServiceOutputManager
	{
		private readonly AsyncPackage _package;
		private readonly TyeServicesProvider _servicesProvider;

		private readonly Dictionary<string, TyeServiceOutputAdapter> _output = new Dictionary<string, TyeServiceOutputAdapter>();

		public TyeServiceOutputManager(AsyncPackage package, TyeServicesProvider servicesProvider)
		{
			_package = package;
			_servicesProvider = servicesProvider;

			servicesProvider.ServicesReceived += OnServicesReceived;
		}

		private void OnServicesReceived(object sender, ServicesReceivedEventArgs e)
		{
			foreach (var connector in _output.Values)
			{
				var service = e.Services?.FirstOrDefault(s => s.Description.Name == connector.ServiceName);
				connector.Service = service;
			}
		}

		public void Attach(V1Service service)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var outWindow = _package.GetService<SVsOutputWindow, IVsOutputWindow>();

			if(_output.TryGetValue(service.Description.Name, out var connector))
			{
				// Ensure logging poller is running
				connector.Start();

				var existingPaneId = connector.PaneId;
				outWindow.GetPane(ref existingPaneId, out var existingPane);
				existingPane.Activate();
				return;
			}
			
			var paneId = Guid.NewGuid();
			outWindow.CreatePane(ref paneId, $"Tye Explorer - {service.Description.Name}", 1, 0);

			outWindow.GetPane(ref paneId, out var customPane);
			
			connector = new TyeServiceOutputAdapter(customPane, service, paneId, _package.DisposalToken);
			connector.Start();
			customPane.Activate();
			
			_output[service.Description.Name] = connector;
		}
	}
}