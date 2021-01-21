using System;
using TyeExplorer.Tye.Models;

namespace TyeExplorer.Services
{
	public class ServicesReceivedEventArgs : EventArgs
	{
		public V1Service[] Services { get; set; }
	}
}
