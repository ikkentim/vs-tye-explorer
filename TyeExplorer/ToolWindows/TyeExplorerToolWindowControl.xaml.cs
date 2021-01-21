using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TyeExplorer.Services;
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
		public event EventHandler<AttachToServiceEventArgs> AttachToService;
		public event EventHandler<SelectedItemChangedEventArgs> SelectedItemChanged;
		
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
			var expandedServices = TreeView.Items
				.OfType<TreeViewItem>()
				.Where(t => t.IsExpanded)
				.Select(t => ((V1Service) t.Tag).Description.Name)
				.ToList();

			var selectedItem = (TreeView.SelectedItem as TreeViewItem)?.Tag;
			
			ClearServices();
			
			foreach (var service in services)
			{
				var serviceItem = new TreeViewItem()
				{
					Header = service.Description.Name,
					Tag = service
				};

				serviceItem.MouseRightButtonUp += TreeViewItemOnMouseRightButtonUp;

				foreach (var replica in service.Replicas)
				{
					var replicaItem = new TreeViewItem()
					{
						Header = replica.Value.Name,
						Tag = new ReplicaContainer
						{
							Service = service,
							Replica = replica.Value
						}
					};

					replicaItem.MouseRightButtonUp += TreeViewItemOnMouseRightButtonUp;
					
					serviceItem.Items.Add(replicaItem);
					
					if (selectedItem is V1ReplicaStatus selectedReplica && selectedReplica.Name == replica.Value.Name)
						replicaItem.IsSelected = true;
				}

				
				if (selectedItem is V1Service selectedService && selectedService.Description.Name == service.Description.Name)
					serviceItem.IsSelected = true;

				if (expandedServices.Contains(service.Description.Name))
					serviceItem.IsExpanded = true;
				
				TreeView.Items.Add(serviceItem);
			}
		}
		
		private T FindResource<T>(string name) where T : class
		{
			if (!(FindResource(name) is T res))
				throw new Exception("Resource not found");

			return res;
		}
		
		private void TreeViewItemOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (e.Handled)
				return;
			
			e.Handled = true;

			var treeViewItem = sender as TreeViewItem;
			
			var tyeServicesProvider = TyeExplorerServices.Get<TyeServicesProvider>();
			
			var isAttachable = false;
			var isChecked = false;
			
			switch (treeViewItem?.Tag)
			{
				case V1Service service:
					isChecked = !tyeServicesProvider.IsAttachToAllService(service);
					isAttachable = tyeServicesProvider.IsAttachableService(service);
					break;
				case ReplicaContainer replica:
					isChecked = !tyeServicesProvider.IsAttachToAllReplica(replica.Replica);
					isAttachable = tyeServicesProvider.IsAttachableService(replica.Service);
					break;
			}

			var contextMenu = FindResource<ContextMenu>(isAttachable ? "ContextMenuService" : "ContextMenuUnavailable");
			contextMenu.PlacementTarget = sender as TreeViewItem;
			contextMenu.IsOpen = true;
			
			
			var menuItemToggleAttach = (MenuItem)contextMenu.Items[1]; //TODO: why does this not work: contextMenu.FindName("MenuItemToggleAttach") as MenuItem;
			menuItemToggleAttach.IsChecked = isChecked;
		}
		
		private void MenuItemOnClickAttachDebugger(object sender, RoutedEventArgs e)
		{
			var menuItem = sender as MenuItem;
			var contextMenu = menuItem?.Parent as ContextMenu;
			var treeViewItem = contextMenu?.PlacementTarget as TreeViewItem;

			switch (treeViewItem?.Tag)
			{
				case ReplicaContainer replica:
					OnAttachToReplica(new AttachToReplicaEventArgs
					{
						Replica = replica.Replica
					});
					break;
				case V1Service service:
					OnAttachToService(new AttachToServiceEventArgs
					{
						Service = service
					});
					break;
			}
				
		}

		private void MenuItemOnClickToggleAttach(object sender, RoutedEventArgs e)
		{
			var menuItem = sender as MenuItem;
			var contextMenu = menuItem?.Parent as ContextMenu;
			var treeViewItem = contextMenu?.PlacementTarget as TreeViewItem;
			
			var services = TyeExplorerServices.Get<TyeServicesProvider>();
			
			switch (treeViewItem?.Tag)
			{
				case V1Service service:
					services.SetExcludedFromAttachToAll(service, !menuItem.IsChecked);
					break;
				case ReplicaContainer replica:
					services.SetExcludedFromAttachToAll(replica.Replica, !menuItem.IsChecked);
					break;
			}
		}

		private void TreeViewOnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			var item = e.NewValue as TreeViewItem;
			var value = item?.Tag;

			if (value is ReplicaContainer replica)
			{
				value = replica.Replica;
			}

			OnSelectedItemChanged(new SelectedItemChangedEventArgs {Item = value});
		}
		
		protected virtual void OnAttachToReplica(AttachToReplicaEventArgs e)
		{
			AttachToReplica?.Invoke(this, e);
		}
		
		protected virtual void OnAttachToService(AttachToServiceEventArgs e)
		{
			AttachToService?.Invoke(this, e);
		}

		protected virtual void OnSelectedItemChanged(SelectedItemChangedEventArgs e)
		{
			SelectedItemChanged?.Invoke(this, e);
		}
		
		private class ReplicaContainer
		{
			public V1Service Service { get; set; }
			public V1ReplicaStatus Replica { get; set; }
		}
	}
}