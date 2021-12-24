using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using TyeExplorer.Commands;

namespace TyeExplorer
{
	public class TyeCommandManager
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly Dictionary<Type, object> _commands = new Dictionary<Type, object>();

		public TyeCommandManager(TyeExplorerServices serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		public T Get<T>()
		{
			return _commands.TryGetValue(typeof(T), out var value)
				? (T) value
				: default;
		}

		public void Initialize(AsyncPackage package)
		{
			// Initialize all commands which implement TyeCommand in this assembly
			foreach (var type in GetType()
				.Assembly
				.GetTypes()
				.Where(t => t != typeof(TyeCommand) && typeof(TyeCommand).IsAssignableFrom(t)))
			{
				var args = type.GetConstructors()
					.First()
					.GetParameters()
					.Select(p => _serviceProvider.GetService(p.ParameterType))
					.ToArray();
				
				var command = (TyeCommand) Activator.CreateInstance(type, args);
				
				command.Initialize(package);
				
				_commands[type] = command;
			}
		}
	}
}