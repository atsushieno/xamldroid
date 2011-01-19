using System;
using System.Collections.Generic;
using Android.Content;
using Android.Util;
using System.Xaml;

namespace Android.Views.Xaml
{
	public partial class View : IDisposable
	{
		public override void Dispose ()
		{
			XamlView.Unregister ((Android.Views.View) this);
			base.Dispose ();
		}
	}

	public class XamlView : Android.Widget.LinearLayout
	{
		public static IntPtr CurrentHandle { get; set; }
		public static Context CurrentContext { get; set; }

		public XamlView (IntPtr handle)
			: base (handle)
		{
			CurrentHandle = handle;
		}

		static internal Dictionary<object,object> store = new Dictionary<object,object> ();

		public static void Register (object key, object value)
		{
			store.Add (key, value);
		}
		
		public static void Unregister (object key)
		{
			store.Remove (key);
		}
		
		public static object GetRegisteredItem (object key)
		{
			object ret;
			return store.TryGetValue (key, out ret) ? ret : null;
		}

		public XamlView ()
			: base (CurrentContext)
		{
		}

		public XamlView (Context context)
			: base (context)
		{
			CurrentContext = context;
		}

		public XamlView (Context context, IAttributeSet attrs)
			: base (context, attrs)
		{
			CurrentContext = context;
		}

		public XamlView (View view)
			: base (view.Context)
		{
			CurrentContext = view.Context;
			this.view = view;
		}

		View view;

		public void LoadXaml (XamlReader reader)
		{
			RemoveAllViews ();
			view = (Android.Views.Xaml.View)XamlServices.Load(reader);
			AddView ((Android.Views.View) view);
		}

		public void AddView (View view)
		{
			this.view = view;
			AddView ((Android.Views.View) view);
		}

		protected override void OnLayout (bool changed, int l, int t, int r, int b)
		{
			if (view != null)
    				((Android.Views.View) view).Layout (l, t, r, b);
		}

		protected override void OnDraw (Android.Graphics.Canvas canvas)
		{
			if (view != null)
				((Android.Views.View) view).Draw (canvas);
		}
	}
}
