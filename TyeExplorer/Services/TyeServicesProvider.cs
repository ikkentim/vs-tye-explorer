using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using TyeExplorer.Tye.Models;
using Task = System.Threading.Tasks.Task;

namespace TyeExplorer.Services
{
	public class TyeServicesProvider
	{
		public const string DashboardUrl = "http://127.0.0.1:8000";
		private static readonly TimeSpan ApiTimeout = TimeSpan.FromSeconds(5);
		private static readonly TimeSpan BackgroundRefreshInterval = TimeSpan.FromSeconds(15);
		public static readonly Uri ApiUri = new Uri($"{DashboardUrl}/api/v1/");

		private readonly HttpClient _client = new HttpClient();
		private readonly TyeExplorerLogger _logger;
		private readonly AsyncPackage _package;
		private readonly JsonSerializer _serializer;
		private readonly SessionConfiguration _sessionConfiguration;
		private bool _isBackgroundRefreshRunning;
		private bool _runAllAvailable;
		private V1Service[] _services;

		public TyeServicesProvider(SessionConfiguration sessionConfiguration, TyeExplorerLogger logger, AsyncPackage package)
		{
			_sessionConfiguration = sessionConfiguration;
			_logger = logger;
			_package = package;
			_client.BaseAddress = ApiUri;
			_client.Timeout = ApiTimeout;
			_serializer = JsonSerializer.CreateDefault();
		}

		public IEnumerable<V1Service> Services => _services;

		public IEnumerable<V1ReplicaStatus> AllAttachableReplicas =>
			_services?.Where(IsAttachToAllAvailable)
				.SelectMany(s => s.Replicas)
				.Select(kv => kv.Value)
				.Where(IsAttachToAllAvailable);

		/// <remarks>Can be service or replica</remarks>
		public object SelectedService { get; set; }

		public event EventHandler<ServiceRequestStartedEventArgs> ServicesRequestStarted;
		public event EventHandler<ServicesReceivedEventArgs> ServicesReceived;
		public event EventHandler<ServiceRequestFailureEventArgs> ServiceRequestFailure;
		public event EventHandler<AvailabilityChangedEventArgs> RunAllAvailabilityChanged;

		private async Task CalcRunAllAvailable()
		{
			var newValue = _services?.Any(IsAttachToAllAvailable) ?? false;

			if (_runAllAvailable != newValue)
			{
				_runAllAvailable = newValue;
				
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
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

		public async void EnableBackgroundRefresh()
		{
			await TaskScheduler.Default;
			await Refresh();
		}

		public async Task Refresh()
		{
			// Spawn background refresh
			if (!_isBackgroundRefreshRunning)
				BackgroundRefresh();
			
			await InnerRefresh(false);
		}

		private async void BackgroundRefresh()
		{
			if (_isBackgroundRefreshRunning)
				return;

			lock (this)
			{
				if (_isBackgroundRefreshRunning)
					return;

				_isBackgroundRefreshRunning = true;
			}

			try
			{
				await TaskScheduler.Default;

				var token = _package.DisposalToken;
				while (!token.IsCancellationRequested)
				{
					try
					{
						await Task.Delay(BackgroundRefreshInterval, token);
					}
					catch (TaskCanceledException)
					{
						return;
					}

					if (!await InnerRefresh(true))
						return;
				}
			}
			finally
			{
				_isBackgroundRefreshRunning = false;
			}
		}
		
		private async Task<bool> InnerRefresh(bool isBackground)
		{
			await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
				OnServicesRequestStarted(new ServiceRequestStartedEventArgs
				{
					IsBackground = isBackground
				});
			});
			
			try
			{
				var sw = new Stopwatch();
				sw.Start();
				var response = await _client.GetAsync("services");
				response.EnsureSuccessStatusCode();
				sw.Stop();

				var services = await DeserializeResponse<V1Service[]>(response);
				
				await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
					OnServicesReceived(new ServicesReceivedEventArgs
					{
						Services = services
					});
				});
				
				_services = services ?? throw new Exception("Failed to deserialize services response from API.");

				if (!isBackground)
					_logger.Log($"Reloaded services, found {services.Length} services in {sw.Elapsed.TotalMilliseconds:0}ms.");

				await CalcRunAllAvailable();
				return true;
			}
			catch (Exception e)
			{
				_logger.Log($"Failed to reload services: {e.Message}");

				await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
					OnServiceRequestFailure(new ServiceRequestFailureEventArgs
					{
						Reason = e.Message,
						IsBackground = isBackground
					});
				});
				
				return false;
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

		private void OnServicesRequestStarted(ServiceRequestStartedEventArgs e)
		{
			ServicesRequestStarted?.Invoke(this, e);
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