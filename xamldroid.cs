using System;
using Android.Content;
using Android.Util;
using System.Xaml;

namespace Android.Views.Xaml
{
	public class XamlView : Android.Views.ViewGroup
	{
		public static IntPtr CurrentHandle { get; set; }
		public static Context CurrentContext { get; set; }

		public XamlView (IntPtr handle)
			: base (handle)
		{
			CurrentHandle = handle;
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

		public XamlView (Context context, IAttributeSet attrs, Int32 defStyle)
			: base (context, attrs, defStyle)
		{
			CurrentContext = context;
		}

		public XamlView (Android.Views.View view)
			: base (view.Context)
		{
			CurrentContext = view.Context;
			this.view = view;
		}

		Android.Views.View view;

		public void LoadXaml (XamlReader reader)
		{
			RemoveAllViews ();
			AddView ((Android.Views.View) (Android.Views.Xaml.View) XamlServices.Load (reader));
		}

        public void AddView (View view)
        {
            AddView ((Android.Views.View)view);
        }

		protected override void OnLayout (bool changed, int l, int t, int r, int b)
		{
			view.Layout (l, t, r, b);
		}

        protected override void OnDraw (Graphics.Canvas canvas)
        {
            view.Draw (canvas);
        }
	}
}
