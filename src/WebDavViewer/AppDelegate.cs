using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

		internal static WebDavMethodBuilder  Builder;

		internal static PagingDetailViewController  PagingDetailViewController;

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
			Builder = new WebDavMethodBuilder {
				Server              = new Uri ((string) server.Attribute ("Uri")),
				NetworkCredential   = new NetworkCredential ((string) server.Attribute ("User"), (string) server.Attribute ("Password")),
			};

			// load the appropriate UI, depending on whether the app is running on an iPhone or iPad
			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone) {
				var controller = new RootViewController (Builder);
				navigationController = new UINavigationController (controller);
				window.RootViewController = navigationController;
			} else {
				var masterViewController = new RootViewController (Builder);
				var masterNavigationController = new UINavigationController (masterViewController);
				PagingDetailViewController     = new PagingDetailViewController (masterViewController);
				var detailNavigationController = new UINavigationController (PagingDetailViewController);
				
				splitViewController = new UISplitViewController ();
				splitViewController.WeakDelegate = PagingDetailViewController;
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

