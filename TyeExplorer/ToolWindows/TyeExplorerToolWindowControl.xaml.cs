using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TyeExplorer.Tye.Models;

namespace TyeExplorer.ToolWindows
{
	public partial class TyeExplorerToolWindowControl : UserControl
	{
		public TyeExplorerToolWindowControl()
		{
			this.InitializeComponent();
		}

		public event EventHandler<AttachToReplicaEventArgs> AttachToReplica;
		
		public void SetWaiting()
		{
			Cursor = Cursors.Wait;
		}
		
		public void UnsetWaiting()
		{
			Cursor = Cursors.Arrow;
		}
		
		public void ClearServices()
		{
			TreeView.Items.Clear();
		}

		public void SetServices(V1Service[] services)
		{
			ClearServices();

			foreach (var service in services)
			{
				var serviceItem = new TreeViewItem()
				{
					Header = service.Description.Name,
					Tag = service
				};

				foreach (var replica in service.Replicas)
				{
					var replicaItem = new TreeViewItem()
					{
						Header = replica.Value.Name,
						Tag = replica.Value
					};
					
					replicaItem.MouseRightButtonUp += ReplicaItemOnMouseRightButtonUp;

					serviceItem.Items.Add(replicaItem);
				}

				TreeView.Items.Add(serviceItem);
			}
		}

		private void ReplicaItemOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			var contextMenu = FindResource("ContextMenuReplica") as ContextMenu;
			contextMenu.PlacementTarget = sender as TreeViewItem;
			contextMenu.IsOpen = true;
		}
		
		private void OnClickAttachDebuggerToProcess(object sender, RoutedEventArgs e)
		{
			var menuItem = sender as MenuItem;
			var contextMenu = menuItem?.Parent as ContextMenu;
			var treeViewItem = contextMenu?.PlacementTarget as TreeViewItem;

			if (treeViewItem?.Tag is V1ReplicaStatus replica)
				OnAttachToReplica(new AttachToReplicaEventArgs
				{
					Replica = replica
				});
		}

		protected virtual void OnAttachToReplica(AttachToReplicaEventArgs e)
		{
			AttachToReplica?.Invoke(this, e);
		}
	}
}