﻿using System;

namespace TyeExplorer.Services
{
	public class ServiceRequestFailureEventArgs : EventArgs
	{
		public bool IsBackground { get; set; }
		public string Reason { get; set; }
	}
}