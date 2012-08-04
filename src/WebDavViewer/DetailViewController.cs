using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using Cadenza.Net;

namespace WebDavViewer
{
	public partial class DetailViewController : UIViewController
	{
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		UIPopoverController masterPopoverController;
		string detailItem;
		WebDavClient client;
		UIImageView image;

		public DetailViewController (WebDavClient client)
			: base (UserInterfaceIdiomIsPhone ? "DetailViewController_iPhone" : "DetailViewController_iPad", null)
		{
			this.client = client;
		}
		
		public void SetDetailItem (string newDetailItem)
		{
			if (detailItem != newDetailItem) {
				detailItem = newDetailItem;
				
				// Update the view
				ConfigureView ();
			}
			
			if (this.masterPopoverController != null)
				this.masterPopoverController.Dismiss (true);
		}
		
		void ConfigureView ()
		{
			// Update the user interface for the detail item
			if (detailItem == null)
				return;
			var f = Path.GetTempFileName ();
			client.Download (detailItem, f).ContinueWith (t => {
					if (t.IsFaulted)
						Console.WriteLine ("Downloading image faulted! {0}", t.Exception);
					image.Image = UIImage.FromFile (f);
					DetailContents.ContentSize = image.Image.Size;
					// File.Delete (f);
			}, TaskScheduler.FromCurrentSynchronizationContext ());
		}
		
		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			// Perform any additional setup after loading the view, typically from a nib.
			DetailContents.BackgroundColor  = UIColor.UnderPageBackgroundColor;
			DetailContents.MaximumZoomScale = 3f;
			DetailContents.MinimumZoomScale = .1f;
			DetailContents.PagingEnabled    = true;
			DetailContents.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			DetailContents.ContentMode      = UIViewContentMode.Center;
			DetailContents.ViewForZoomingInScrollView = v => image;
			DetailContents.DecelerationEnded += (object sender, EventArgs e) => {
				Console.WriteLine ("DecelarationEnded; change the image!");
			};
			float imageHeight = this.View.Frame.Height - this.NavigationController.NavigationBar.Frame.Height;
			image = new UIImageView (new RectangleF (0, 0, this.View.Frame.Width, imageHeight)) {
				AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth,
				ContentMode = UIViewContentMode.ScaleAspectFit,
			};
			DetailContents.AddSubview (image);

			ConfigureView ();
		}
		
		public override void ViewDidUnload ()
		{
			base.ViewDidUnload ();
			
			// Clear any references to subviews of the main view in order to
			// allow the Garbage Collector to collect them sooner.
			//
			// e.g. myOutlet.Dispose (); myOutlet = null;
			
			ReleaseDesignerOutlets ();
		}
		
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			if (UserInterfaceIdiomIsPhone) {
				return (toInterfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
			} else {
				return true;
			}
		}
		
		[Export("splitViewController:willHideViewController:withBarButtonItem:forPopoverController:")]
		public void WillHideViewController (UISplitViewController splitController, UIViewController viewController, UIBarButtonItem barButtonItem, UIPopoverController popoverController)
		{
			barButtonItem.Title = "Master";
			NavigationItem.SetLeftBarButtonItem (barButtonItem, true);
			masterPopoverController = popoverController;
		}
		
		[Export("splitViewController:willShowViewController:invalidatingBarButtonItem:")]
		public void WillShowViewController (UISplitViewController svc, UIViewController vc, UIBarButtonItem button)
		{
			// Called when the view is shown again in the split view, invalidating the button and popover controller.
			NavigationItem.SetLeftBarButtonItem (null, true);
			masterPopoverController = null;
		}
	}
}

