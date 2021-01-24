using System;
using TyeExplorer.Tye.Models;

namespace TyeExplorer.Services
{
	public class AvailabilityChangedEventArgs : EventArgs
	{
		public bool IsAvailable { get; set; }
	}
}