using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ecosim.SceneData
{
	public class ReportBase
	{
		private Scene scene;

		public int id;
		public string name;
		public bool enabled;
		public bool useIntroduction;
		public string introduction;
		public bool useConclusion;
		public string conclusion;

		public bool opened;
		public bool introOpened;
		public bool conclusionOpened;
		public bool introShown;
		public bool conclusionShown;

		public ReportBase ()
		{
			this.introduction = "Intro";
			this.conclusion = "Conclusion";
		}

		public void LoadBase (XmlTextReader reader, Scene scene)
		{
			id = int.Parse (reader.GetAttribute ("id"));
			name = reader.GetAttribute ("name");
			enabled = bool.Parse (reader.GetAttribute ("enabled"));
			useIntroduction = bool.Parse (reader.GetAttribute ("useintro"));
			introduction = reader.GetAttribute ("intro");
			useConclusion = bool.Parse (reader.GetAttribute ("useconcl"));
			conclusion = reader.GetAttribute ("concl");
		}

		public void SaveBase (XmlTextWriter writer, Scene scene)
		{
			writer.WriteAttributeString ("id", id.ToString());
			writer.WriteAttributeString ("name", name);
			writer.WriteAttributeString ("enabled", enabled.ToString().ToLower());
			writer.WriteAttributeString ("useintro", useIntroduction.ToString().ToLower());
			writer.WriteAttributeString ("intro", introduction);
			writer.WriteAttributeString ("useconcl", useConclusion.ToString().ToLower());
			writer.WriteAttributeString ("concl", conclusion);
		}
		
		public virtual void UpdateReferences (Scene scene)
		{
		}
	}
}
