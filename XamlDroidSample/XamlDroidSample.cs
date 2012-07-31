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
				var button = new Button() { Text = "XamlDroid sample" };
                v.AddView(button);
                SetContentView(v);
                var xaml = XamlServices.Save(button);
                Android.Util.Log.Debug ("XamlDroid", xaml);
                v.LoadXaml (new XamlXmlReader(new StringReader(xaml.Replace ("<x:Reference>__ReferenceID0</x:Reference>", "")))); // ZAPZAPZAP!
            }
            catch (Exception ex)
            {
                Android.Util.Log.Debug ("XamlDroid", ex.ToString());
            }
        }
    }
}

