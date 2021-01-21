using System;
using System.Collections.Generic;
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
		public const string DashboardUrl = "http://localhost:8000";

		private readonly HttpClient _client = new HttpClient();
		private readonly JsonSerializer _serializer;

		public TyeServicesProvider(SessionConfiguration sessionConfiguration)
		{
			_sessionConfiguration = sessionConfiguration;
			_client.BaseAddress = new Uri($"{DashboardUrl}/api/v1/");
			_client.Timeout = TimeSpan.FromSeconds(5);
			_serializer = JsonSerializer.CreateDefault();
		}

		private V1Service[] _services;
		private bool _runAllAvailable;
		public event EventHandler ServicesRequestStarted;
		
		public event EventHandler<ServicesReceivedEventArgs> ServicesReceived;
		public event EventHandler<ServiceRequestFailureEventArgs> ServiceRequestFailure;
		public event EventHandler<RunAllAvailabilityChangedEventArgs> RunAllAvailabilityChanged;

		public IEnumerable<V1Service> Services => _services;

		public IEnumerable<V1ReplicaStatus> AllAttachableReplicas =>
			_services?.Where(IsAttachToAllService)
				.SelectMany(s => s.Replicas)
				.Select(kv => kv.Value)
				.Where(IsAttachToAllReplica);
		
		/// <remarks>Can be service or replica</remarks>
		public object SelectedService { get; set; }

		private void CalcRunAllAvailable()
		{
			var newValue = _services?.Any(IsAttachToAllService) ?? false;

			if (_runAllAvailable != newValue)
			{
				_runAllAvailable = newValue;
				OnRunAllAvailabilityChanged(new RunAllAvailabilityChangedEventArgs {IsAvailable = newValue});
			}
		}

		public bool IsAttachableService(V1Service service)
		{
			return service != null &&
			       (service.ServiceType == ServiceType.Project ||
			        service.ServiceType == ServiceType.Function ||
			        service.ServiceType == ServiceType.Executable);
		}
		
		public bool IsAttachToAllService(V1Service service)
		{
			return IsAttachableService(service) && _sessionConfiguration.IsAttachToAllEnabled(service.Description.Name);
		}

		public bool IsAttachToAllReplica(V1ReplicaStatus replica)
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
				var response = await _client.GetAsync("services");
				response.EnsureSuccessStatusCode();

				var services = await DeserializeResponse<V1Service[]>(response);
				OnServicesReceived(new ServicesReceivedEventArgs
				{
					Services = services
				});

				_services = services ?? throw new Exception("Failed to deserialize services response from API.");

				CalcRunAllAvailable();
			}
			catch (Exception e)
			{
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
		
		private void OnRunAllAvailabilityChanged(RunAllAvailabilityChangedEventArgs e)
		{
			RunAllAvailabilityChanged?.Invoke(this, e);
		}
	}
}