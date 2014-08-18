using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using System;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.Render;
using UnityEngine;

namespace Ecosim.SceneData.Action
{
	public class CheatsAction : BasicAction
	{
		public class Cheat
		{
			public const string XML_ELEMENT = "cheat";

			public bool enabled;
			public string name;
			public string body;

			public Cheat () { }
			public Cheat (string name, string body)
			{
				this.name = name;
				this.body = body;
				this.enabled = true;
			}

			public void Save (XmlTextWriter writer)
			{
				writer.WriteStartElement (XML_ELEMENT);
				writer.WriteAttributeString ("enabled", enabled.ToString ().ToLower ());
				writer.WriteAttributeString ("name", name);
				writer.WriteAttributeString ("body", body);
				writer.WriteEndElement ();
			}

			public static Cheat Load (XmlTextReader reader)
			{
				string name = reader.GetAttribute ("name");
				string body = reader.GetAttribute ("body");
				bool enabled = bool.Parse (reader.GetAttribute ("enabled"));
				Cheat c = new Cheat (name, body);
				c.enabled = enabled;
				return c;
			}
		}

		public const string XML_ELEMENT = "cheats";

		public List<Cheat> cheats;

		public CheatsAction (Scene scene, int id) : base (scene, id)
		{
			this.cheats = new List<Cheat> ();
		}

		public CheatsAction (Scene scene) : base(scene, scene.actions.lastId)
		{
			this.cheats = new List<Cheat> ();
		}

		public override string GetDescription ()
		{
			return "Cheats";
		}

		public bool HandleCheat (string cheat)
		{
			bool cheatFound = false;
			int idx = 0;
			foreach (Cheat c in this.cheats) {
				idx++;
				if (c.body == cheat) {
					cheatFound = true;

					// Try to fire a method for the cheat in the action
					if (ecoBase != null) {
						MethodInfo cheatMI = ecoBase.GetType ().GetMethod ("ProcessCheat" + (idx).ToString (),
						BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {}, null);
						if (cheatMI != null) {
							try {
								cheatMI.Invoke (ecoBase, null);
							} catch (Exception e) {
								Log.LogException (e);
							}
						}

					}
				}
			}
			return cheatFound;
		}

		public override void Save (XmlTextWriter writer)
		{
			// TODO: Save more data if necessary
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("id", id.ToString ());
			foreach (Cheat c in cheats) {
				c.Save (writer);
			}
			writer.WriteEndElement ();
		}

		public static CheatsAction Load (Scene scene, XmlTextReader reader)
		{
			// TODO: Add more fields if necessary
			int id = int.Parse (reader.GetAttribute ("id"));
			CheatsAction a = new CheatsAction (scene, id);

			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == Cheat.XML_ELEMENT)) 
					{
						a.cheats.Add (Cheat.Load (reader));
						IOUtil.ReadUntilEndElement (reader, Cheat.XML_ELEMENT);
					}
					else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}

			return a;
		}

		public bool CheatEntered (string cheat)
		{
			foreach (Cheat c in this.cheats) {
				if (c.body == cheat) {
					return true;
				}
			}
			return false;
		}
	}
}
