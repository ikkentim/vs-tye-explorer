using System;
using TyeExplorer.Tye.Models;

namespace TyeExplorer.ToolWindows
{
	public class ServiceEventArgs : EventArgs
	{
		public V1Service Service { get; set; }
	}
}