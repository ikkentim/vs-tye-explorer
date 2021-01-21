using System;
using TyeExplorer.Tye.Models;

namespace TyeExplorer.ToolWindows
{
	public class AttachToServiceEventArgs : EventArgs
	{
		public V1Service Service { get; set; }
	}
	public class SelectedItemChangedEventArgs : EventArgs
	{
		public object Item { get; set; }
	}
}