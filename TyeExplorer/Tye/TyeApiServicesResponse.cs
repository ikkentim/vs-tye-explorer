using TyeExplorer.Tye.Models;

namespace TyeExplorer.Tye
{
	public class TyeApiServicesResponse
	{
		public V1Service[] Services { get; set; }
		public string FailureReason { get; set; }
	}
}