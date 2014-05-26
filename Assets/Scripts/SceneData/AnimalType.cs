using System.Collections.Generic;
using System.IO;
using System.Xml;

using Ecosim.SceneData.Rules;

namespace Ecosim.SceneData
{
	public class AnimalType
	{
		public class Nest
		{
			public const string XML_ELEMENT = "nest";

			public int x;
			public int y;

			public int males;
			public int females;
			public int food;

			public int totalCapacity;
			public int malesCapacity;
			public int femalesCapacity;

			public Nest ()
			{
			}

			public static Nest Load (XmlTextReader reader, Scene scene)
			{
				Nest nest = new Nest ();
				nest.x = int.Parse(reader.GetAttribute ("x"));
				nest.y = int.Parse(reader.GetAttribute ("y"));
				nest.males = int.Parse(reader.GetAttribute ("males"));
				nest.females = int.Parse(reader.GetAttribute ("females"));
				nest.totalCapacity = int.Parse (reader.GetAttribute ("cap"));
				nest.malesCapacity = int.Parse (reader.GetAttribute ("malescap"));
				nest.females = int.Parse (reader.GetAttribute ("femalescap"));
				//IOUtil.ReadUntilEndElement(reader, XML_ELEMENT);
				return nest;
			}

			public void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				writer.WriteAttributeString ("x", x.ToString());
				writer.WriteAttributeString ("y", y.ToString());
				writer.WriteAttributeString ("males", males.ToString());
				writer.WriteAttributeString ("females", females.ToString());
				writer.WriteAttributeString ("cap", totalCapacity.ToString());
				writer.WriteAttributeString ("malescap", malesCapacity.ToString());
				writer.WriteAttributeString ("femalescap", femalesCapacity.ToString());
				writer.WriteEndElement ();
			}

			public void UpdateReferences (Scene scene)
			{

			}
		}

		public const string XML_ELEMENT = "animal";
		
		public string name;
		public int index;
		
		public string foodParamName;
		public string foodOverruleParamName;
		public string dangerParamName;

		public int moveDistanceMale;
		public int moveDistanceFemale;

		public float wanderMale;
		public float wanderFemale;

		//public int growthCapPerNest;
		//public float offspringPerPair;
		//public float naturalDeath;

		public Nest[] nests;

		public string dataName;
		
		public AnimalType ()
		{
		}
		
		public AnimalType (Scene scene, string name)
		{
			this.name = name;
			this.index = scene.animalTypes.Length;

			this.dataName = StringUtil.MakeValidID (name, true);
			
			// Data name
			string newDataName = this.dataName;
			int tries = 1;
			while (scene.progression.HasData (newDataName)) {
				newDataName = this.dataName + tries;
				tries++;
			}
			this.dataName = newDataName;

			scene.progression.AddData (this.dataName, new BitMap8 (scene));

			foodParamName = scene.progression.GetAllDataNames (false)[0];
			foodOverruleParamName = foodParamName;
			dangerParamName = foodParamName;

			moveDistanceMale = 50;
			moveDistanceFemale = 50;

			wanderMale = 0.1f;
			wanderFemale = 0.1f;

			nests = new Nest[] { };
			
			// Add to scene
			List<AnimalType> tmpList = new List<AnimalType>(scene.animalTypes);
			tmpList.Add (this);
			scene.animalTypes = tmpList.ToArray();
		}
		
		public static AnimalType Load (XmlTextReader reader, Scene scene)
		{
			AnimalType animal = new AnimalType ();
			animal.name = reader.GetAttribute ("name");
			animal.foodParamName = reader.GetAttribute ("foodparam");
			animal.foodOverruleParamName = reader.GetAttribute ("foodoverruleparam");
			animal.dangerParamName = reader.GetAttribute ("dangerparam");
			animal.moveDistanceMale = int.Parse(reader.GetAttribute ("movedistm"));
			animal.moveDistanceFemale = int.Parse(reader.GetAttribute ("movedistf"));
			animal.wanderMale = float.Parse(reader.GetAttribute ("wanderm"));
			animal.wanderFemale = float.Parse(reader.GetAttribute ("wanderf"));
			animal.dataName = reader.GetAttribute ("dataname");

			if (string.IsNullOrEmpty(animal.dataName)) 
				animal.dataName = string.Format("_animal{0}", StringUtil.MakeValidID(animal.name));

			List<Nest> nests = new List<Nest>();
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == Nest.XML_ELEMENT)) {
						Nest nest = Nest.Load (reader, scene);
						if (nest != null) {
							nests.Add (nest);
						}
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower() == XML_ELEMENT)) {
						break;
					}
				}
			}
			animal.nests = nests.ToArray();
			return animal;
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("name", name);
			writer.WriteAttributeString ("foodparam", foodParamName);
			writer.WriteAttributeString ("foodoverruleparam", foodOverruleParamName);
			writer.WriteAttributeString ("dangerparam", dangerParamName);
			writer.WriteAttributeString ("movedistm", moveDistanceMale.ToString());
			writer.WriteAttributeString ("movedistf", moveDistanceFemale.ToString());
			writer.WriteAttributeString ("wanderm", wanderMale.ToString());
			writer.WriteAttributeString ("wanderf", wanderFemale.ToString());
			writer.WriteAttributeString ("dataname", dataName);
			foreach (Nest n in nests) {
				n.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}
		
		public void UpdateReferences (Scene scene)
		{
			foreach (Nest n in nests) {
				n.UpdateReferences (scene);
			}
		}
		
		public static AnimalType Find (Scene scene, string name)
		{
			foreach (AnimalType t in scene.animalTypes) {
				if (t.name == name) return t;
			}
			return null;
		}
	}
}
