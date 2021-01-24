using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TyeExplorer.Tye.Models;

namespace TyeExplorer.Services
{
	public class TyeServicesProvider
	{
		private readonly SessionConfiguration _sessionConfiguration;
		private readonly TyeExplorerLogger _logger;
		public const string DashboardUrl = "http://localhost:8000";

		private readonly HttpClient _client = new HttpClient();
		private readonly JsonSerializer _serializer;

		public TyeServicesProvider(SessionConfiguration sessionConfiguration, TyeExplorerLogger logger)
		{
			_sessionConfiguration = sessionConfiguration;
			_logger = logger;
			_client.BaseAddress = new Uri($"{DashboardUrl}/api/v1/");
			_client.Timeout = TimeSpan.FromSeconds(5);
			_serializer = JsonSerializer.CreateDefault();
		}

		private V1Service[] _services;
		private bool _runAllAvailable;
		public event EventHandler ServicesRequestStarted;
		
		public event EventHandler<ServicesReceivedEventArgs> ServicesReceived;
		public event EventHandler<ServiceRequestFailureEventArgs> ServiceRequestFailure;
		public event EventHandler<AvailabilityChangedEventArgs> RunAllAvailabilityChanged;

		public IEnumerable<V1Service> Services => _services;

		public IEnumerable<V1ReplicaStatus> AllAttachableReplicas =>
			_services?.Where(IsAttachToAllAvailable)
				.SelectMany(s => s.Replicas)
				.Select(kv => kv.Value)
				.Where(IsAttachToAllAvailable);
		
		/// <remarks>Can be service or replica</remarks>
		public object SelectedService { get; set; }

		private void CalcRunAllAvailable()
		{
			var newValue = _services?.Any(IsAttachToAllAvailable) ?? false;

			if (_runAllAvailable != newValue)
			{
				_runAllAvailable = newValue;
				OnRunAllAvailabilityChanged(new AvailabilityChangedEventArgs {IsAvailable = newValue});
			}
		}

		public bool IsAttachable(V1Service service)
		{
			return service != null &&
			       (service.ServiceType == ServiceType.Project ||
			        service.ServiceType == ServiceType.Function ||
			        service.ServiceType == ServiceType.Executable);
		}
		
		public bool IsAttachable(V1ReplicaStatus replica)
		{
			var service = _services.FirstOrDefault(s => s.Replicas.ContainsKey(replica.Name));
			
			return service != null &&
			       (service.ServiceType == ServiceType.Project ||
			        service.ServiceType == ServiceType.Function ||
			        service.ServiceType == ServiceType.Executable);
		}
		
		public bool IsAttachToAllAvailable(V1Service service)
		{
			return IsAttachable(service) && _sessionConfiguration.IsAttachToAllEnabled(service.Description.Name);
		}

		public bool IsAttachToAllAvailable(V1ReplicaStatus replica)
		{
			return _sessionConfiguration.IsAttachToAllEnabled(replica.Name);
		}
		
		public void SetExcludedFromAttachToAll(V1Service service, bool value)
		{
			_sessionConfiguration.SetAttachToAllEnabled(service.Description.Name, !value);
		}

		public void SetExcludedFromAttachToAll(V1ReplicaStatus replica, bool value)
		{
			_sessionConfiguration.SetAttachToAllEnabled(replica.Name, !value);
		}

		public async Task Refresh()
		{
			OnServicesRequestStarted();
				
			try
			{
				var sw = new Stopwatch();
				sw.Start();
				var response = await _client.GetAsync("services");
				response.EnsureSuccessStatusCode();
				sw.Stop();

				var services = await DeserializeResponse<V1Service[]>(response);
				OnServicesReceived(new ServicesReceivedEventArgs
				{
					Services = services
				});

				_services = services ?? throw new Exception("Failed to deserialize services response from API.");
				
				_logger.Log($"Reloaded services, found {services.Length} services in {sw.Elapsed.TotalMilliseconds:0}ms.");
				
				CalcRunAllAvailable();
			}
			catch (Exception e)
			{
				_logger.Log($"Failed to reload services: {e.Message}");
				OnServiceRequestFailure(new ServiceRequestFailureEventArgs
				{
					Reason = e.Message
				});
			}
		}

		private async Task<T> DeserializeResponse<T>(HttpResponseMessage response)
		{
			using (var stream = await response.Content.ReadAsStreamAsync())
			{
				using (var textReader = new StreamReader(stream))
				using (var jReader = new JsonTextReader(textReader))
					return _serializer.Deserialize<T>(jReader);
			}
		}
		
		private void OnServicesReceived(ServicesReceivedEventArgs e)
		{
			ServicesReceived?.Invoke(this, e);
		}

		private void OnServicesRequestStarted()
		{
			ServicesRequestStarted?.Invoke(this, EventArgs.Empty);
		}

		private void OnServiceRequestFailure(ServiceRequestFailureEventArgs e)
		{
			ServiceRequestFailure?.Invoke(this, e);
		}
		
		private void OnRunAllAvailabilityChanged(AvailabilityChangedEventArgs e)
		{
			RunAllAvailabilityChanged?.Invoke(this, e);
		}
	}
}