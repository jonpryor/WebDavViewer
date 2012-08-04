using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using Cadenza.Net;

namespace WebDavViewer
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the 
	// User Interface of the application, as well as listening (and optionally responding) to 
	// application events from iOS.
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		// class-level declarations
		UINavigationController navigationController;
		UISplitViewController splitViewController;
		UIWindow window;

		internal static WebDavClient    client;

		internal static DetailViewController DetailViewController;
		
		//
		// This method is invoked when the application has loaded and is ready to run. In this 
		// method you should instantiate the window, load the UI into it and then make the window
		// visible.
		//
		// You have 17 seconds to return from this method, or iOS will terminate your application.
		//
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			// create a new window instance based on the screen size
			window = new UIWindow (UIScreen.MainScreen.Bounds);

			var servers = XDocument.Load ("Servers.xml");
			var server  = servers.Elements ("Servers").Elements ("Server").First ();
			
			// Perform any additional setup after loading the view, typically from a nib.
			client = new WebDavClient {
				Server      = (string) server.Attribute ("Uri"),
				BasePath    = (string) server.Attribute ("BasePath"),
				User        = (string) server.Attribute ("User"),
				Pass        = (string) server.Attribute ("Password"),
			};

			// load the appropriate UI, depending on whether the app is running on an iPhone or iPad
			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone) {
				var controller = new RootViewController (client);
				navigationController = new UINavigationController (controller);
				window.RootViewController = navigationController;
			} else {
				var masterViewController = new RootViewController (client);
				var masterNavigationController = new UINavigationController (masterViewController);
				DetailViewController = new DetailViewController (client);
				var detailNavigationController = new UINavigationController (DetailViewController);
				
				splitViewController = new UISplitViewController ();
				splitViewController.WeakDelegate = DetailViewController;
				splitViewController.ViewControllers = new UIViewController[] {
					masterNavigationController,
					detailNavigationController
				};
				
				window.RootViewController = splitViewController;
			}

			// make the window visible
			window.MakeKeyAndVisible ();
			
			return true;
		}
	}
}

