using System;
using System.IO;
using System.Xaml;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.OS;
using Android.Views.Xaml;

namespace XamlDroidSample
{
    [Activity(Label = "XamlDroidSample", MainLauncher = true)]
    public class XamlDroidSampleActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(new Android.Widget.Button(ApplicationContext) { Text = "test" });

            try
            {
                XamlView.CurrentContext = this.ApplicationContext;
                var v = new XamlView();
                Android.Util.Log.D("XamlDroid", "ZAPZAPZAPZAPZAP");
                var button = new Button () { Text = "XamlDroid sample" };
                Android.Util.Log.D("XamlDroid", "ZAPZAPZAPZAPZAP2");
                v.AddView(button);
                SetContentView(v);
                //var xr = new XamlObjectReader (button);
                //while (xr.Read ())
                //    Android.Util.Log.D("XamlDroid", String.Format ("{0} {1} {2} {3}", xr.NodeType, xr.Type, xr.Member, xr.Value));
                var xaml = XamlServices.Save(button);
                Android.Util.Log.D("XamlDroid", xaml);
                v.LoadXaml (new XamlXmlReader(new StringReader(xaml)));
            }
            catch (Exception ex)
            {
                Android.Util.Log.D("XamlDroid", ex.ToString());
            }
        }
    }
}

