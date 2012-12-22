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
	public partial class PagingDetailViewController : UIViewController
	{
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		HashSet<DetailViewController>   VisiblePages    = new HashSet<DetailViewController> ();
		HashSet<DetailViewController>   RecycledPages   = new HashSet<DetailViewController> ();
		UIScrollView                    PagingView;
		CollectionViewController              collectionView;
		int?                            startIndex;

		public PagingDetailViewController (CollectionViewController collectionView, int? startIndex = null)
			: base (UserInterfaceIdiomIsPhone ? "PagingDetailViewController_iPhone" : "PagingDetailViewController_iPad", null)
		{
			this.collectionView = collectionView;
			this.startIndex     = startIndex;
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
			PagingView = new UIScrollView (View.Bounds) {
				PagingEnabled 					= true,
				ShowsHorizontalScrollIndicator	= false,
			};
			PagingView.Scrolled += (sender, e) => {
				TilePages ();
			};
			PagingView.DecelerationEnded += (sender, e) => {
				OnPagingDecelerationEnded (e);
			};
			View.AddSubview (PagingView);

			if (startIndex.HasValue) {
				SetCurrentPageIndex (collectionView, startIndex.Value);
				startIndex = null;
			} else
				TilePages ();
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

		public int CurrentPageIndex {
			get {
				return (int)((PagingView.ContentOffset.X /
				              PagingView.Bounds.Size.Width));
			}
		}
		
		public void SetCurrentPageIndex (CollectionViewController collectionView, int index)
		{
			if (!object.ReferenceEquals (this.collectionView, collectionView)) {
				this.collectionView = collectionView;
				foreach (var vc in VisiblePages) {
					RecycledPages.Add (vc);
					vc.View.RemoveFromSuperview ();
				}
				VisiblePages.ExceptWith (RecycledPages);
			}
			var visibleBounds = PagingView.Bounds;
			PagingView.Bounds = new RectangleF (visibleBounds.Width * index, visibleBounds.Y, visibleBounds.Width, visibleBounds.Height);
			PagingView.SetContentOffset (new PointF (PagingView.Bounds.Size.Width * index, 0), true);
			TilePages ();
		}

		DetailViewController CreatePageController ()
		{
			return new DetailViewController (collectionView.Builder);
		}
		
		bool IsValidIndex (int index)
		{
			if (index >= collectionView.Entries.Count)
				return false;
			var e = collectionView.Entries [index];
			if (e.ResourceType == WebDavResourceType.Collection)
				return false;
			return true;
		}
		
		int? MaximumPageIndex {
			get {return collectionView.Entries.Count-1;}
		}
		
		int GetIndexForController (DetailViewController page)
		{
			for (int i = 0; i < collectionView.Entries.Count; ++i)
				if (collectionView.Entries [i].Href == page.Filename)
					return i;
			return -1;
		}
		
		void SetIndexForController (DetailViewController page, int index)
		{
			if (index > MaximumPageIndex)
				return;
			var e = collectionView.Entries [index];
			if (e.ResourceType == WebDavResourceType.Collection)
				return;
			page.Filename = e.Href;
		}
		
		void OnPagingDecelerationEnded (EventArgs e)
		{
			if (!IsValidIndex (CurrentPageIndex))
				return;
			collectionView.TableView.SelectRow (NSIndexPath.FromRowSection (CurrentPageIndex, 0), true, UITableViewScrollPosition.Middle);
		}

		bool IsDisplayingPageForIndex (int index)
		{
			return VisiblePages.Any (p => GetIndexForController (p) == index);
		}
		
		DetailViewController DequeueRecycledPage ()
		{
			DetailViewController viewController = RecycledPages.FirstOrDefault ();
			if (viewController != null) {
				RecycledPages.Remove (viewController);
			}
			return viewController;
		}
		
		RectangleF FrameForPageAtIndex (int index)
		{
			SizeF pageSize = PagingView.Bounds.Size;
			return new RectangleF (index * pageSize.Width, 0, pageSize.Width, pageSize.Height);
		}
		
		void AddPageWithIndex (int index)
		{
			DetailViewController viewController = DequeueRecycledPage ();
			if (viewController == null)
				viewController = CreatePageController ();
			viewController.View.Frame = FrameForPageAtIndex (index);
			SetIndexForController (viewController, index);
			PagingView.AddSubview  (viewController.View);
			VisiblePages.Add (viewController);
		}
		
		void ResizePagingViewContentSize ()
		{
			PagingView.ContentSize = new SizeF (
				PagingView.Bounds.Size.Width * (CurrentPageIndex + 2),
				PagingView.Bounds.Size.Height);
		}
		
		void TilePages ()
		{
			var visibleBounds           = PagingView.Bounds;
			int firstNeededPageIndex    = (int) Math.Floor (visibleBounds.X / visibleBounds.Width);
			int lastNeededPageIndex     = (int) Math.Floor ((GetMaxX (visibleBounds)-1) / visibleBounds.Width);
			int historyCount            = 5;
			firstNeededPageIndex        = Math.Max (firstNeededPageIndex-historyCount, 0);
			lastNeededPageIndex         = Math.Max (lastNeededPageIndex+1, 1);
			
			if (MaximumPageIndex.HasValue)
				lastNeededPageIndex = Math.Min (lastNeededPageIndex, MaximumPageIndex.Value);
			
			// Recycle unneeded controllers
			foreach (var vc in VisiblePages) {
				int index = GetIndexForController (vc);
				if (index < firstNeededPageIndex || index > lastNeededPageIndex) {
					RecycledPages.Add (vc);
					vc.View.RemoveFromSuperview ();
				}
			}
			VisiblePages.ExceptWith (RecycledPages);
			
			// Add missing pages
			for (int i = firstNeededPageIndex; i <= lastNeededPageIndex; ++i) {
				if (!IsDisplayingPageForIndex (i) && IsValidIndex (i))
					AddPageWithIndex (i);
			}
			
			ResizePagingViewContentSize ();
		}
		
		static float GetMaxX (RectangleF self)
		{
			return self.X + self.Width;
		}
	}
}

