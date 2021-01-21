using System.Collections.Generic;

namespace TyeExplorer
{
	public class SessionConfiguration
	{
		// Services and replicas which are excluded from "attach to all"
		private readonly List<string> _attachExclusion = new List<string>();

		public bool IsAttachToAllEnabled(string name)
		{
			return !_attachExclusion.Contains(name);
		}

		public void SetAttachToAllEnabled(string name, bool value)
		{
			if (value)
				_attachExclusion.Remove(name);
			else if (name != null && IsAttachToAllEnabled(name))
				_attachExclusion.Add(name);
		}
	}
}
