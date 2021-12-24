using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace TyeExplorer.Services
{
	public class TyeExplorerLogger
	{
		private readonly AsyncPackage _package;

		private IVsOutputWindowPane _pane;

		public TyeExplorerLogger(AsyncPackage package)
		{
			_package = package;
		}

		public void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

			var outWindow = _package.GetService<SVsOutputWindow, IVsOutputWindow>();
			
			outWindow.CreatePane(ref PackageGuids.guidTyeExplorerLogWindow, "Tye Explorer", 1, 0);
			outWindow.GetPane(ref PackageGuids.guidTyeExplorerLogWindow, out _pane);
		}

		public void Log(string message)
		{
			// Using thread safe method, won't need to switch threads
#pragma warning disable VSTHRD010
			_pane.OutputStringThreadSafe(message);
			_pane.OutputStringThreadSafe(Environment.NewLine);
#pragma warning restore VSTHRD010
		}
	}
}