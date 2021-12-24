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
	public class TyeServiceOutputAdapter
	{
		private static readonly TimeSpan LogPollingTimeout = TimeSpan.FromSeconds(1);
		private static readonly TimeSpan LogPollingInterval = TimeSpan.FromSeconds(2);

		private readonly CancellationToken _cancellationToken;
		private readonly HttpClient _client;
		private readonly IVsOutputWindowPane _pane;
		private readonly JsonSerializer _serializer;

		private CancellationTokenSource _cancellationTokenSource;

		private string _firstLine;
		private int _lineCount;
		private V1Service _service;

		public TyeServiceOutputAdapter(IVsOutputWindowPane pane, V1Service service, Guid paneId, CancellationToken cancellationToken)
		{
			PaneId = paneId;
			_pane = pane;
			_cancellationToken = cancellationToken;
			_service = service;
			ServiceName = service.Description.Name;
			_client = new HttpClient
			{
				BaseAddress = TyeServicesProvider.ApiUri,
				Timeout = LogPollingTimeout
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
                Start();
			}

			if (newService == null)
			{
				Log("Service disappeared.");
				StopPoller();
			}
		}

		private async Task PollerRunnerAsync()
		{
			if (_cancellationTokenSource != null || IsRunning)
				return;

			_cancellationTokenSource = new CancellationTokenSource();

			var comboSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, _cancellationTokenSource.Token);
			var token = comboSource.Token;
			
            try
            {
                IsRunning = true;

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
                    catch (Exception e)
                    {
                        Log($"Polling failed: {e.Message}");
                        return;
                    }

                    try
                    {
                        await Task.Delay(LogPollingInterval, token);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                }
            }
            finally
            {
                IsRunning = false;
                _cancellationTokenSource = null;
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
			
			_ = Task.Run(PollerRunnerAsync, _cancellationToken);
		}
	}
}