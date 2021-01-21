using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell;

namespace TyeExplorer
{
	/// <summary>
	/// Tiny service container which laze loads unknown services 
	/// </summary>
	public class TyeExplorerServices : IServiceProvider
	{
		private static TyeExplorerServices _instance;

		private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
		
		public TyeExplorerServices(AsyncPackage package)
		{
			AddService(package);
			AddService(this);

			_instance = this;
		}
		
		public static T Get<T>()
		{
			return _instance.GetService<T>();
		}
		
		public T GetService<T>()
		{
			return (T) _instance.GetService(typeof(T));
		}

		public object GetService(Type serviceType)
		{
			return _services.TryGetValue(serviceType, out var value)
				? value
				: _services[serviceType] = AddService(serviceType);
		}
		
		private void AddService(object instance)
		{
			AddService(instance, instance.GetType());
		}

		private void AddService(object instance, Type type)
		{
			_services[type] = instance;

			var baseType = type.BaseType;

			if (baseType != null && baseType != typeof(object))
				AddService(instance, baseType);
		}

		public object AddService(Type type)
		{
			var constructors = type.GetConstructors();

			if (constructors.Length == 0)
			{
				throw new Exception($"Could not load service {type}; no available constructor");
			}
			
			var args = constructors
				.First()
				.GetParameters()
				.Select(p => GetService(p.ParameterType))
				.ToArray();

			var instance = Activator.CreateInstance(type, args);
			AddService(instance);
			return instance;
		}
	}
}