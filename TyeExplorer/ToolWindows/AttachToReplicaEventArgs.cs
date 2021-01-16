using System;
using TyeExplorer.Tye.Models;

namespace TyeExplorer.ToolWindows
{
	public class AttachToReplicaEventArgs : EventArgs
	{
		public V1ReplicaStatus Replica { get; set; }
	}
}