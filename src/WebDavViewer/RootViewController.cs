using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using Cadenza.Net;

namespace WebDavViewer
{
	public partial class RootViewController : UITableViewController
	{
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		WebDavMethodBuilder builder;
		string  href;
		Task<WebDavPropertyFindMethod> entriesTask;

		public RootViewController (WebDavMethodBuilder builder, string path = "")
			: base (UserInterfaceIdiomIsPhone ? "RootViewController_iPhone" : "RootViewController_iPad", null)
		{
			if (!UserInterfaceIdiomIsPhone) {
				this.ClearsSelectionOnViewWillAppear = false;
				this.ContentSizeForViewInPopover = new SizeF (320f, 600f);
			}
			
			// Custom initialization
			this.builder = builder;
			entriesTask = Task<WebDavPropertyFindMethod>.Factory.StartNew (() => {
				var c = builder.CreateFileStatusMethodAsync (path, 0);
				c.Wait ();
				this.href = c.Result.GetResponses ().First ().Href;
				return builder.CreateFileStatusMethodAsync (path).Result;
			});
			entriesTask = builder.CreateFileStatusMethodAsync (path);
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			this.TableView.Source = new DataSource (this);
			
			if (!UserInterfaceIdiomIsPhone)
				this.TableView.SelectRow (NSIndexPath.FromRowSection (0, 0), false, UITableViewScrollPosition.Middle);
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
		
		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
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
		
		class DataSource : UITableViewSource
		{
			RootViewController controller;
			List<WebDavResponse>   entries;

			public DataSource (RootViewController controller)
			{
				this.controller = controller;
			}

			List<WebDavResponse> Entries {
				get {
					if (entries != null)
						return entries;
					entries = controller.entriesTask.Result.GetResponses ()
						.Where (r => r.Href != controller.href)
						.OrderBy (r => GetEntryName (r).ToLowerInvariant ())
						.ToList ();
					controller.entriesTask = null;
					return entries;
				}
			}

			static string GetEntryName (WebDavResponse r)
			{
				string name = r.Href;
				if (r.ResourceType == WebDavResourceType.Collection || name.EndsWith ("/"))
					name = Path.GetFileName (Path.GetDirectoryName (name));
				else
					name = Path.GetFileName (name);
				return HttpUtility.UrlDecode (name);
			}
			
			// Customize the number of sections in the table view.
			public override int NumberOfSections (UITableView tableView)
			{
				return 1;
			}
			
			public override int RowsInSection (UITableView tableview, int section)
			{
				return Entries.Count;
			}
			
			// Customize the appearance of table view cells.
			public override UITableViewCell GetCell (UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
			{
				const string cellIdentifier = "Cell";
				var cell = tableView.DequeueReusableCell (cellIdentifier);
				if (cell == null) {
					cell = new UITableViewCell (UITableViewCellStyle.Value1, cellIdentifier);
					if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone) {
						cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
					}
				}

				var e = Entries [indexPath.Row];
				
				// Configure the cell.
				// cell.TextLabel.Text = NSBundle.MainBundle.LocalizedString ("Detail", "Filename");
				cell.TextLabel.Text = GetEntryName (e);
				cell.DetailTextLabel.Text = e.ContentLength == null ? "\u2192" : e.ContentLength.ToString ();
				return cell;
			}

			/*
			// Override to support conditional editing of the table view.
			public override bool CanEditRow (UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
			{
				// Return false if you do not want the specified item to be editable.
				return true;
			}
			*/
			
			/*
			// Override to support editing the table view.
			public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
			{
				if (editingStyle == UITableViewCellEditingStyle.Delete) {
					// Delete the row from the data source.
					controller.TableView.DeleteRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Fade);
				} else if (editingStyle == UITableViewCellEditingStyle.Insert) {
					// Create a new instance of the appropriate class, insert it into the array, and add a new row to the table view.
				}
			}
			*/
			
			/*
			// Override to support rearranging the table view.
			public override void MoveRow (UITableView tableView, NSIndexPath sourceIndexPath, NSIndexPath destinationIndexPath)
			{
			}
			*/
			
			/*
			// Override to support conditional rearranging of the table view.
			public override bool CanMoveRow (UITableView tableView, NSIndexPath indexPath)
			{
				// Return false if you do not want the item to be re-orderable.
				return true;
			}
			*/
			
			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				var e = Entries [indexPath.Row];
				if (e.ResourceType == WebDavResourceType.Collection) {
					var c = new RootViewController (controller.builder, e.Href);
					controller.NavigationController.PushViewController (c, true);
					return;
				}
				if (UserInterfaceIdiomIsPhone) {
					var DetailViewController = new DetailViewController (controller.builder);
					DetailViewController.SetDetailItem (e.Href);
					// Pass the selected object to the new view controller.
					controller.NavigationController.PushViewController (DetailViewController, true);
				} else {
					AppDelegate.DetailViewController.SetDetailItem (e.Href);
					// Navigation logic may go here -- for example, create and push another view controller.
				}
			}
		}
	}
}

