using System;
using TyeExplorer.Tye.Models;

namespace TyeExplorer.Services
{
	public class RunAllAvailabilityChangedEventArgs : EventArgs
	{
		public bool IsAvailable { get; set; }
	}
}