using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

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

		WebDavClient    client;
		string          path;
		Task<IEnumerable<WebDavEntry>> entriesTask;

		public RootViewController (WebDavClient client, string path = "/")
			: base (UserInterfaceIdiomIsPhone ? "RootViewController_iPhone" : "RootViewController_iPad", null)
		{
			if (!UserInterfaceIdiomIsPhone) {
				this.ClearsSelectionOnViewWillAppear = false;
				this.ContentSizeForViewInPopover = new SizeF (320f, 600f);
			}
			
			// Custom initialization
			this.client = client;
			this.path = path;
			entriesTask = client.List (path);
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
			List<WebDavEntry>   entries;

			public DataSource (RootViewController controller)
			{
				this.controller = controller;
			}

			List<WebDavEntry> Entries {
				get {
					if (entries != null)
						return entries;
					entries = controller.entriesTask.Result.OrderBy (e => e.Name.ToLowerInvariant ()).ToList ();
					controller.entriesTask = null;
					return entries;
				}
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
					cell = new UITableViewCell (UITableViewCellStyle.Default, cellIdentifier);
					if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone) {
						cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
					}
				}

				var e = Entries [indexPath.Row];
				
				// Configure the cell.
				// cell.TextLabel.Text = NSBundle.MainBundle.LocalizedString ("Detail", "Filename");
				cell.TextLabel.Text = e.Name;
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
				if (e.Type == WebDavEntryType.Directory) {
					var c = new RootViewController (controller.client, e.Path);
					controller.NavigationController.PushViewController (c, true);
					return;
				}
				if (UserInterfaceIdiomIsPhone) {
					var DetailViewController = new DetailViewController (controller.client);
					DetailViewController.SetDetailItem (e.Path);
					// Pass the selected object to the new view controller.
					controller.NavigationController.PushViewController (DetailViewController, true);
				} else {
					AppDelegate.DetailViewController.SetDetailItem (e.Path);
					// Navigation logic may go here -- for example, create and push another view controller.
				}
			}
		}
	}
}

