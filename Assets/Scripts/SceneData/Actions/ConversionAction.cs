using System;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using Ecosim;
using Ecosim.SceneData;
using UnityEngine;

namespace Ecosim.SceneData.Action
{
	public class ConversionAction : BasicAction
	{
		public const string XML_ELEMENT = "conversion";
		private MethodInfo convertToFloatMI;
		private MethodInfo convertToStringMI;
		
		public delegate int GetFn (int x,int y);
		
		public ConversionAction (Scene scene, int id) : base(scene, id)
		{
		}
		
		public ConversionAction (Scene scene) : base(scene, scene.actions.lastId)
		{
			// as scripts is basically the purpose of this action, it starts with
			// the default template already created.
			CreateDefaultScript ();
		}

		public override string GetDescription ()
		{
			return "Parameter Conversion";
		}
	
		protected override void LinkEcoBase ()
		{
			base.LinkEcoBase ();
			if (ecoBase != null) {
				convertToFloatMI = ecoBase.GetType ().GetMethod ("ConvertToFloat",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {typeof(string), typeof(int)}, null);
				convertToStringMI = ecoBase.GetType ().GetMethod ("ConvertToString",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {typeof(string), typeof(int)}, null);
				
				scene.progression.conversionHandler = this;
			}
		}

		protected override void UnlinkEcoBase ()
		{
			base.UnlinkEcoBase ();
			convertToFloatMI = null;
			convertToStringMI = null;
		}
		
		
		/**
		 * Returns a delegate for (int x, int y) parameters and returning int. This can used to create
		 * dynamic Data functions. Parameter name is the name of the function in the script that will
		 * be used as the delegate function. On success function is returned, on failure null.
		 */
		public GetFn GetDataDelegate (string name)
		{
			if (ecoBase != null) {
				MethodInfo mi = ecoBase.GetType ().GetMethod (name,
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {typeof(int), typeof(int)}, null);
				if (mi != null) {
					Delegate d = 
            			Delegate.CreateDelegate (typeof(GetFn), ecoBase, mi, false);
					if (d != null) {
						return (GetFn)d;
					}
				}
			}
			return null;
		}
		
		public float ConvertToFloat (string dataName, int val)
		{
			if (convertToFloatMI != null) {
				return (float)convertToFloatMI.Invoke (ecoBase, new object[] { dataName, val });
			} else {
				return (float)val;
			}
		}

		public string ConvertToString (string dataName, int val)
		{
			if (convertToStringMI != null) {
				return (string)convertToStringMI.Invoke (ecoBase, new object[] { dataName, val });
			} else {
				return ConvertToFloat (dataName, val).ToString ("0.00");
			}
		}
		
		public static ConversionAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			ConversionAction action = new ConversionAction (scene, id);

			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == UserInteraction.XML_ELEMENT)) {
						action.uiList.Add (UserInteraction.Load (action, reader));
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}
			return action;
		}
		
		public override void Save (XmlTextWriter writer)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("id", id.ToString ());
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			writer.WriteEndElement ();
		}
	}
}