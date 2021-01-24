using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using TyeExplorer.Tye.Models;

namespace TyeExplorer.Services
{
	public class TyeServiceOutputConnector
	{
		private readonly CancellationToken _cancellationToken;
		private readonly HttpClient _client;
		private readonly IVsOutputWindowPane _pane;
		private readonly JsonSerializer _serializer;

		private CancellationTokenSource _cancellationTokenSource;
		private V1Service _service;

		public TyeServiceOutputConnector(IVsOutputWindowPane pane, V1Service service, Guid paneId, CancellationToken cancellationToken)
		{
			PaneId = paneId;
			_pane = pane;
			_cancellationToken = cancellationToken;
			_service = service;
			ServiceName = service.Description.Name;
			_client = new HttpClient
			{
				BaseAddress = new Uri($"{TyeServicesProvider.DashboardUrl}/api/v1/"),
				Timeout = TimeSpan.FromSeconds(1)
			};
			_serializer = JsonSerializer.CreateDefault();
		}
		
		public Guid PaneId { get; }
		
		public V1Service Service
		{
			get => _service;
			set
			{
				if (_service == value) return;
				var oldValue = _service;
				_service = value;
				OnServiceChanged(oldValue, value);
			}
		}

		public bool IsRunning { get; private set; }

		public string ServiceName { get; }

		private void Log(string message)
		{
#pragma warning disable VSTHRD010
			_pane.OutputStringThreadSafe(message);
			_pane.OutputStringThreadSafe(Environment.NewLine);
#pragma warning restore VSTHRD010
		}

		private void OnServiceChanged(V1Service oldService, V1Service newService)
		{
			if (oldService == null)
			{
				Log("Service reappeared.");

				IsRunning = true;
				StartPoller();
			}

			if (newService == null)
			{
				Log("Service disappeared.");
				StopPoller();
			}
		}

		private string _firstLine;
		private int _lineCount;
		
		private async void StartPoller()
		{
			if (_cancellationTokenSource != null)
				return;

			_cancellationTokenSource = new CancellationTokenSource();

			var comboSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, _cancellationTokenSource.Token);
			var token = comboSource.Token;

			while (!token.IsCancellationRequested)
			{
				try
				{
					var response = await _client.GetAsync($"logs/{ServiceName}", token);
					response.EnsureSuccessStatusCode();

					var lines = await DeserializeResponse<string[]>(response);

					if (lines.Length == 0 || lines[0] != _firstLine)
					{
						_lineCount = 0;
					}

					if (lines.Length > 0)
					{
						_firstLine = lines[0];
					}

					foreach (var line in lines.Skip(_lineCount))
					{
						Log(line);
					}

					_lineCount = lines.Length;
					
					token.ThrowIfCancellationRequested();
				}
				catch(Exception e)
				{
					Log($"Polling failed: {e.Message}");
					_cancellationTokenSource = null;
					return;
				}

				try
				{
					await Task.Delay(TimeSpan.FromSeconds(2), token);
				}
				catch (TaskCanceledException)
				{
				}
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


		private void StopPoller()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource = null;
		}

		public void Start()
		{
			if (Service == null || IsRunning)
				return;

			IsRunning = true;
			
			StartPoller();
		}

		public void Resume()
		{
			if (Service == null || IsRunning)
				return;

			IsRunning = true;
			
			StartPoller();
		}

		public void Stop()
		{
			if (!IsRunning)
				return;

			IsRunning = false;

			StopPoller();
		}
	}
}