using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Reflection;

namespace Ecosim.SceneData
{
	public class ExportMgr
	{
		public enum SelectionTypes
		{
			All,
			Selection
		}
		
		public enum DataTypes
		{
			Always,
			OnlyWhenSurveyed
		}

		private readonly Scene scene;

		public bool exportEnabled;
		public SelectionTypes selectionType;
		public DataTypes dataType;

		public string[] parameters;
		public string[] animals;
		public string[] plants;

		public ExportMgr (Scene scene)
		{
			this.scene = scene;
			this.parameters = new string[] { };
			this.animals = new string[] { };
			this.plants = new string[] { };
		}

		public void AddParameter (string param)
		{
			List<string> list = new List<string> (parameters);
			if (list.Contains (param)) return;
			list.Add (param);
			list.Sort ();
			this.parameters = list.ToArray ();
		}

		public void RemoveParameter (string param)
		{
			List<string> list = new List<string> (this.parameters);
			if (!list.Contains (param)) return;
			list.Remove (param);
			list.Sort ();
			this.parameters = list.ToArray ();
		}

		public void AddAnimal (string name)
		{
			List<string> list = new List<string> (this.animals);
			if (list.Contains (name)) return;
			list.Add (name);
			list.Sort ();
			this.animals = list.ToArray ();
		}

		public void RemoveAnimal (string name)
		{
			List<string> list = new List<string> (this.animals);
			if (!list.Contains (name)) return;
			list.Remove (name);
			list.Sort ();
			this.animals = list.ToArray ();
		}

		public void AddPlant (string name)
		{
			List<string> list = new List<string> (this.plants);
			if (list.Contains (name)) return;
			list.Add (name);
			list.Sort ();
			this.plants = list.ToArray ();
		}
		
		public void RemovePlant (string name)
		{
			List<string> list = new List<string> (this.plants);
			if (!list.Contains (name)) return;
			list.Remove (name);
			list.Sort ();
			this.plants = list.ToArray ();
		}

		public static ExportMgr Load (string path, Scene scene)
		{
			ExportMgr mgr = new ExportMgr (scene);
			if (File.Exists (path + "exportsettings.xml")) {
				XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (path + "exportsettings.xml"));
				try {
					while (reader.Read()) {
						XmlNodeType nType = reader.NodeType;
						if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "export")) {
							mgr.Load (reader);
						}
					}
				} finally {
					reader.Close ();
				}
			}
			return mgr;
		}

		private void Load (XmlTextReader reader)
		{
			this.exportEnabled = bool.Parse (reader.GetAttribute ("enabled"));
			this.selectionType = (SelectionTypes)System.Enum.Parse (typeof(SelectionTypes), reader.GetAttribute ("selectiontype"));
			this.dataType = (DataTypes)System.Enum.Parse (typeof(DataTypes), reader.GetAttribute ("datatype"));

			List<string> paramList = new List<string>();
			List<string> animalList = new List<string> ();
			List<string> plantList = new List<string>();

			while (reader.Read()) 
			{
				XmlNodeType nType = reader.NodeType;
				
				if (nType == XmlNodeType.Element)
				{
					switch (reader.Name.ToLower ())
					{
					case "param" :
						paramList.Add (reader.GetAttribute ("name"));
						break;
					case "animal" :
						animalList.Add (reader.GetAttribute ("name"));
						break;
					case "plant" :
						plantList.Add (reader.GetAttribute ("name"));
						break;
					}
				} 
				else if ((nType == XmlNodeType.EndElement) && 
				         (reader.Name.ToLower () == "export")) {
					break;
				}
			}

			this.parameters = paramList.ToArray ();
			this.animals = animalList.ToArray ();
			this.plants = plantList.ToArray ();
		}

		public void Save (string path)
		{
			XmlTextWriter writer = new XmlTextWriter (path + "exportsettings.xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement ("export");
			writer.WriteAttributeString ("enabled", this.exportEnabled.ToString().ToLower());
			writer.WriteAttributeString ("selectiontype", this.selectionType.ToString());
			writer.WriteAttributeString ("datatype", this.dataType.ToString());
			foreach (string s in this.parameters) {
				writer.WriteStartElement ("param");
				writer.WriteAttributeString ("name", s);
				writer.WriteEndElement ();
			}
			foreach (string s in this.animals) {
				writer.WriteStartElement ("animal");
				writer.WriteAttributeString ("name", s);
				writer.WriteEndElement ();
			}
			foreach (string s in this.plants) {
				writer.WriteStartElement ("plant");
				writer.WriteAttributeString ("name", s);
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();		
		}
		
		public void UpdateReferences ()
		{
			List<string> list = new List<string> ();
			foreach (string s in this.parameters) {
				if (scene.progression.HasData (s))
					list.Add (s);
			}
			this.parameters = list.ToArray ();

			list = new List<string> ();
			foreach (string s in this.animals) {
				foreach (AnimalType a in scene.animalTypes) {
					if (a.name.ToLower () == s.ToLower ()) {
						list.Add (a.name);
						break;
					}
				}
			}
			this.animals = list.ToArray ();

			list = new List<string> ();
			foreach (string s in this.animals) {
				foreach (PlantType p in scene.plantTypes) {
					if (p.name.ToLower () == s.ToLower ()) {
						list.Add (p.name);
						break;
					}
				}
			}
			this.plants = list.ToArray ();
		}
	}
}
