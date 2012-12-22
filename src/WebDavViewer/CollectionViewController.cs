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
	public partial class CollectionViewController : UITableViewController
	{
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		public readonly WebDavMethodBuilder Builder;
		string  href;
		Task<WebDavPropertyFindMethod>      entriesTask;
		List<WebDavResponse>                entries;
		string                              path;

		public CollectionViewController (WebDavMethodBuilder builder, string path = "")
			: base (UserInterfaceIdiomIsPhone ? "RootViewController_iPhone" : "RootViewController_iPad", null)
		{
			this.path = path;

			if (!UserInterfaceIdiomIsPhone) {
				this.ClearsSelectionOnViewWillAppear = false;
				this.ContentSizeForViewInPopover = new SizeF (320f, 600f);
			}

			NavigationItem.RightBarButtonItem = new UIBarButtonItem ("Hidden", UIBarButtonItemStyle.Bordered, (o, e) => {
				AppDelegate.ShowHiddenFiles = !AppDelegate.ShowHiddenFiles;

				entries     = null;
				entriesTask = CreateEntriesTask ();
				TableView.ReloadData ();
			});
			NavigationItem.Title    = GetCollectionEntryName (path);

			// Custom initialization
			this.Builder = builder;
			entriesTask = CreateEntriesTask ();
		}

		Task<WebDavPropertyFindMethod> CreateEntriesTask ()
		{
			return Task<WebDavPropertyFindMethod>.Factory.StartNew (() => {
				var c = Builder.CreateFileStatusMethodAsync (path, 0);
				c.Wait ();
				this.href = c.Result.GetResponses ().First ().Href;
				return Builder.CreateFileStatusMethodAsync (path).Result;
			});
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

		internal List<WebDavResponse> Entries {
			get {
				if (entries != null)
					return entries;
				entries = (from r in entriesTask.Result.GetResponses ()
						where r.Href != href
						let name = GetEntryName (r).ToLowerInvariant ()
						where AppDelegate.ShowHiddenFiles ? true : !name.StartsWith (".")
						orderby name
						select r)
					.ToList ();
				entriesTask = null;
				return entries;
			}
		}
		
		static string GetEntryName (WebDavResponse r)
		{
			return r.ResourceType == WebDavResourceType.Collection || r.Href.EndsWith ("/")
				? GetCollectionEntryName (r.Href)
				: GetFileEntryName (r.Href);
		}

		static string GetCollectionEntryName (string collectionName)
		{
			if (string.IsNullOrEmpty (collectionName))
				return "/";
			var name = Path.GetFileName (Path.GetDirectoryName (collectionName));
			return HttpUtility.UrlDecode (name);
		}

		static string GetFileEntryName (string fileName)
		{
			var name = Path.GetFileName (fileName);
			return HttpUtility.UrlDecode (name);
		}

		class DataSource : UITableViewSource
		{
			CollectionViewController collectionView;

			public DataSource (CollectionViewController collectionView)
			{
				this.collectionView = collectionView;
			}

			// Customize the number of sections in the table view.
			public override int NumberOfSections (UITableView tableView)
			{
				return 1;
			}
			
			public override int RowsInSection (UITableView tableview, int section)
			{
				return collectionView.Entries.Count;
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

				var e = collectionView.Entries [indexPath.Row];
				
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
				var e = collectionView.Entries [indexPath.Row];
				if (e.ResourceType == WebDavResourceType.Collection) {
					var c = new CollectionViewController (collectionView.Builder, e.Href);
					collectionView.NavigationController.PushViewController (c, true);
					c.NavigationController.NavigationBar.BackItem.Title = GetCollectionEntryName (collectionView.path);
					return;
				}
				if (UserInterfaceIdiomIsPhone) {
					var details = new PagingDetailViewController (collectionView, indexPath.Row);
					// Pass the selected object to the new view controller.
					collectionView.NavigationController.PushViewController (details, true);
				} else {
					AppDelegate.PagingDetailViewController.SetCurrentPageIndex (collectionView, indexPath.Row);
					// Navigation logic may go here -- for example, create and push another view controller.
				}
			}
		}
	}
}

