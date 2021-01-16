using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TyeExplorer.Tye.Models;

namespace TyeExplorer.Tye
{
	public class TyeApiConnector
	{
		public const string DashboardUrl = "http://localhost:8000";

		private readonly HttpClient _client = new HttpClient();
		private readonly JsonSerializer _serializer;
		private static TyeApiConnector _instance;

		public TyeApiConnector()
		{
			_client.BaseAddress = new Uri($"{DashboardUrl}/api/v1/");
			_client.Timeout = TimeSpan.FromSeconds(5);
			_serializer = JsonSerializer.CreateDefault();
		}

		public static TyeApiConnector Instance => _instance ?? (_instance = new TyeApiConnector());
		
		public event EventHandler ServicesRequestStarted;
		public event EventHandler<TyeApiServicesResponse> ServicesReceived;
		
		public async void LoadServices()
		{
			OnServicesRequestStarted();
				
			try
			{
				var response = await _client.GetAsync("services");

				response.EnsureSuccessStatusCode();

				var services = await DeserializeResponse<V1Service[]>(response);
				
				OnServicesReceived(new TyeApiServicesResponse
				{
					Services = services
				});
			}
			catch (Exception e)
			{
				OnServicesReceived(new TyeApiServicesResponse
				{
					FailureReason = e.Message
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
		
		private void OnServicesReceived(TyeApiServicesResponse e)
		{
			ServicesReceived?.Invoke(this, e);
		}

		private void OnServicesRequestStarted()
		{
			ServicesRequestStarted?.Invoke(this, EventArgs.Empty);
		}
	}
}
