using System;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace WebDavViewer
{
	public class Application
	{
		// This is the main entry point of the application.
		static void Main (string[] args)
		{
			// DISABLES ALL SSL VALIDATION!
			System.Net.ServicePointManager.ServerCertificateValidationCallback = (server, certificate, chain, sslPolicyErrors) => true;

			// if you want to use a different Application Delegate class from "AppDelegate"
			// you can specify it here.
			UIApplication.Main (args, null, "AppDelegate");
		}
	}
}
