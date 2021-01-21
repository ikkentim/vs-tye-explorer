using System;

namespace TyeExplorer.Services
{
	public class ServiceRequestFailureEventArgs : EventArgs
	{
		public string Reason { get; set; }
	}
}