using System;

namespace TyeExplorer.Services
{
	public class ServiceRequestStartedEventArgs : EventArgs
	{
		public bool IsBackground { get; set; }
	}
}