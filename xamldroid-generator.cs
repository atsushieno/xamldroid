using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Mono.Android.Xaml.Generator
{
	public class Driver
	{
		public static void Main (string [] args)
		{
			new Driver ().Run (args);
		}

		internal static Assembly android_ass;
		internal static Assembly [] asses;
		internal static List<Type> targets = new List<Type> ();
		internal static List<Type> views = new List<Type> ();
		internal static List<Type> anims = new List<Type> ();

		public void Run (string [] args)
		{
			asses = (from arg in args select Assembly.ReflectionOnlyLoadFrom (arg)).ToArray ();
			android_ass = asses.First (a => a.GetName ().Name == "Mono.Android");

			CollectViews ();
		}
		
		void CollectViews ()
		{
			var view = android_ass.GetType ("Android.Views.View");
			var animation = android_ass.GetType ("Android.Views.Animations.Animation");
			targets.Add (view);
			var d = new Dictionary<Type,bool> ();
			foreach (var ass in asses)
				foreach (var t in ass.GetTypes ())
					CheckIfTypeIsTarget (view, d, t);
			views.AddRange (from e in d where e.Value select e.Key);
			targets.AddRange (views);
			d.Clear ();

			targets.Add (animation);
			foreach (var ass in asses)
				foreach (var t in ass.GetTypes ())
					CheckIfTypeIsTarget (animation, d, t);
			anims.AddRange (from e in d where e.Value select e.Key);

			targets.AddRange (anims);

			GenerateCode ();
		}
		
		bool CheckIfTypeIsTarget (Type target, Dictionary<Type,bool> d, Type t)
		{
			bool v;
			if (!t.IsPublic)
				return false;
			if (d.TryGetValue (t, out v))
				return v;
			if (t.BaseType == target)
				v = true;
			else if (t.BaseType == null)
				v = false;
			else
				v = CheckIfTypeIsTarget (target, d, t.BaseType);
			d [t] = v;
			return v;
		}
		
		StringWriter output;

		void GenerateCode ()
		{
			output = new StringWriter () { NewLine = "\n" };
			foreach (var t in targets)
				GenerateCode (t);

			string header = @"// This file is generated by xamldroid skeleton generator.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Android.Views;
using Android.Widget;

/*
{1}
*/

namespace Android.Views.Xaml
{
";
			using (var fs = File.CreateText ("xamldroid.generated.cs")) {
				fs.NewLine = "\n";
				fs.WriteLine (header.Replace ("{1}", String.Join ("\n", (from t in targets select String.Format ("using Impl{0} = {1};", t.CSName (), t.CSFullName ())).ToArray ())));
				fs.WriteLine (output);

				fs.WriteLine (@"
	internal static class Extensions
	{
		public static T GetWrappedView<T> (object impl) where T : class
		{
			if (impl == null)
				return null;
			object w = XamlView.GetRegisteredItem (impl);
			if (w == null)
				w = CreateWrappedView (impl);
			return (T) w;
		}
		
		static object CreateWrappedView (object impl)
		{");

				foreach (var view in targets)
					if (!view.IsAbstract)
						fs.WriteLine (@"
			if (impl is {1})
				return new {0} (({1}) impl);", view.CSName (), view.CSFullName ());
				fs.WriteLine (@"
			throw new NotSupportedException (""not supported conversion"");
		}");
				fs.WriteLine (@"
	}
} // end of namespace");

			}
		}

		void GenerateCode (Type type)
		{
			if (type.IsGenericType && !type.IsGenericTypeDefinition)
				return; // generate nothing.

			var implprops = new List<PropertyInfo> ();
			var miscprops = new List<PropertyInfo> ();
			foreach (var p in type.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
				if (targets.Contains (p.PropertyType) && !p.PropertyType.IsEnum)
					implprops.Add (p);
				else
					miscprops.Add (p);
			}

			output.WriteLine ("// generic ordinal type for " + type);
			string template = @"
	public {10}partial class {0} : {1} {2}
	{{
		{7} impl;

		// constructor, if not abstract.
		{9}

		// impl-to-wrapper constructor
		internal protected {8} ({7} impl) {6}
		{{
			this.impl = impl;
			{11}
			Initialize ();
		}}

		// initializer
		void Initialize ()
		{{
			// repeat for all auto (new-Android-type) properties
			{3}
		}}

		// explicit conversion operator
		public static explicit operator {7} ({0} source)
		{{
			return source.impl;
		}}

		// For a property whose type is wrapper for Android type, just make it auto property (use private set for get-only ones)
		{4}

		// Non-Android properties follow.
		{5}
	}}
";

			var actx = android_ass.GetType ("Android.Content.Context");
			var aaset = android_ass.GetType ("Android.Util.IAttributeSet");
			string args = type.GetConstructor (new Type [] {actx}) != null ? "XamlView.CurrentContext" : type.GetConstructor (new Type [] {actx, aaset}) != null ? "XamlView.CurrentContext, null" : null;
			string publicConstructor = type.IsAbstract ? String.Empty : String.Format (@"
		public {0} ()
			: this (new {1} ({2}))
		{{
			
		}}", type.NonGenericName (), type.CSFullName (), args);

			string templateInit = @"
			if (impl.{0} != null)
				{0} = Extensions.GetWrappedView<{1}> (impl.{0});";
			var isw = new StringWriter () { NewLine = "\n" };
			foreach (var p in implprops)
				if (!p.IsAbstract ())
					isw.WriteLine (templateInit, p.Name, p.PropertyType.CSName ());

			string templateImplProp = @"
		public {3}{0} {1} {{ get; {2}set; }}";
			var dpsw = new StringWriter () { NewLine = "\n" };
			foreach (var p in implprops)
				dpsw.WriteLine (templateImplProp, p.PropertyType.CSSwitchName (), p.Name, p.IsSetterPublic () ? null : "internal ", GetModifier (p));

			string templateOrdProp1 = @"
		public {3}{0} {1} {{
			get {{ return {4}; }}
			{2}
		}}";
			string templateOrdProp2 = @"
		public {3}{0} {1} {{ get; {5} }}";
			var nsw = new StringWriter () { NewLine = "\n" };
			foreach (var p in miscprops) {
				var setter = String.Format ("set {{ impl.{0} = value; }}", p.Name);
				nsw.WriteLine (p.IsAbstract () ? templateOrdProp2 : templateOrdProp1, p.PropertyType.CSSwitchName (), p.Name, p.IsSetterPublic () ? setter : null, GetModifier (p), GetValueExpression (p), p.IsSetterPublic () ? "set;" : null);
			}

			string gconsts = null;
			foreach (var arg in type.GetGenericArguments ()) {
				var gca = String.Join (",", (from t in arg.GetGenericParameterConstraints () select t.CSSwitchName ()).ToArray ());
				gconsts += String.IsNullOrEmpty (gca) ? null : "where " + arg.Name + " : " + gca;
			}

			// FIXME: write custom attributes
			bool callBase = targets.Contains (type.BaseType);
			output.WriteLine (template, type.CSName (), type.BaseType.CSSwitchName (), gconsts, isw, dpsw, nsw, callBase ? " : base (impl)" : null, type.CSFullName (), type.NonGenericName (), publicConstructor, type.IsAbstract ? "abstract " : null, callBase ? null : "XamlView.Register (impl, this);");
		}

		string GetValueExpression (PropertyInfo p)
		{
			var type = p.PropertyType;
			if (type.IsGenericType && type.GetGenericArguments ().Any (t => targets.Contains (t)) || targets.Contains (type) && !type.IsEnum) {
				if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (IList<>))
					return String.Format ("(from x in {0} select ({1}) x).ToList ()", p.Name, type.GetGenericArguments () [0].CSName ());
				else if (type.IsArray)
					return String.Format ("(from x in {0} select ({1}) x).ToArray ()", p.Name, type.CSName ().Substring (0, type.CSName ().LastIndexOf ('[')));
				else
					return "(" + type.CSFullName () + ") " + p.Name;
			}
			else
				return "impl." + p.Name;
		}

		string GetModifier (PropertyInfo p)
		{
			if (p.IsAbstract ())
				return "abstract ";
			if (p.IsOverride ())
				return "override ";
			if (p.GetGetMethod ().IsVirtual)
				return "virtual ";
			return null;
		}

		string FormatParameter (ParameterInfo p)
		{
			return String.Format ("{0}{1} {2}", /*p.GetCustomAttribute<ParamArrayAttribute> () != null ? "params " :*/ null, p.ParameterType.CSSwitchName (), p.Name);
		}
	}

	static class Extensions
	{
		public static T GetCustomAttribute<T> (this ParameterInfo p)
		{
			foreach (var a in p.GetCustomAttributes (true))
				if (a is T)
					return (T) a;
			return default (T);
		}

		public static bool IsSetterPublic (this PropertyInfo p)
		{
			var mi = p.GetSetMethod ();
			return mi != null && mi.IsPublic;
		}

		public static string CSSwitchName (this Type type)
		{
			if (type.IsGenericParameter)
				return type.Name;
			if (type.IsGenericType)
				return (Driver.targets.Contains (type) ? "" : type.Namespace + ".") + (type.DeclaringType != null ? type.DeclaringType.Name + "." : null) + type.Name.Substring (0, type.Name.IndexOf ('`')) + "<" + String.Join (",", (from t in type.GetGenericArguments () select t.CSSwitchName ()).ToArray ()) + ">";
			else
				return ((Driver.targets.Contains (type) ? "" : type.Namespace + ".") + (type.DeclaringType != null ? type.DeclaringType.Name + "." : null) + type.Name).Replace ('+', '.');
		}

		public static string CSFullName (this Type type)
		{
			if (type.IsGenericParameter)
				return type.Name;
			if (type.IsGenericType)
				return type.Namespace + "." + (type.DeclaringType != null ? type.DeclaringType.Name + "." : null) + type.Name.Substring (0, type.Name.IndexOf ('`')) + "<" + String.Join (",", (from t in type.GetGenericArguments () select t.CSFullName ()).ToArray ()) + ">";
			else
				return type.FullName.Replace ('+', '.');
		}

		// FIXME: incomplete. generic args should be also conditionalized whether Android or non-Android types.
		public static string CSName (this Type type)
		{
			if (type.IsGenericParameter)
				return type.Name;
			if (type.IsGenericType)
				return type.NonGenericName () + "<" + String.Join (",", (from t in type.GetGenericArguments () select t.CSName ()).ToArray ()) + ">";
			else
				return type.Name;
		}

		public static string NonGenericName (this Type type)
		{
			return type.IsGenericType ? type.Name.Substring (0, type.Name.IndexOf ('`')) : type.Name;
		}

		// FIXME: use it
		public static string GetGenericConstraintString (this Type t)
		{
			if (t == null)
				return null;
			switch (t.GenericParameterAttributes) {
			case GenericParameterAttributes.ReferenceTypeConstraint:
				return "class";
			case GenericParameterAttributes.DefaultConstructorConstraint:
				return "new()";
			}
			return t.CSSwitchName ();
		}
		
		public static bool IsAbstract (this PropertyInfo p)
		{
			return p.GetGetMethod ().IsAbstract;
		}

		public static bool IsOverride (this PropertyInfo p)
		{
			var m = p.GetGetMethod ();
			return m != null && m.GetBaseDefinition () != m;
		}
	}
}
