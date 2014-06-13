using System.Collections.Generic;
using System.IO;
using System.Xml;

using Ecosim.SceneData.Rules;
using Ecosim.SceneData.AnimalPopulationModel;

namespace Ecosim.SceneData
{
	public class AnimalType
	{
		public const string XML_ELEMENT = "animal";
		
		public string name;
		public int index;
		
		/*public string foodParamName;
		public string foodOverruleParamName;
		public string dangerParamName;

		public int moveDistanceMale;
		public int moveDistanceFemale;

		public float wanderMale;
		public float wanderFemale;*/

		public List<IAnimalPopulationModel> models;

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

			/*foodParamName = scene.progression.GetAllDataNames (false)[0];
			foodOverruleParamName = foodParamName;
			dangerParamName = foodParamName;

			moveDistanceMale = 50;
			moveDistanceFemale = 50;

			wanderMale = 0.1f;
			wanderFemale = 0.1f;*/

			this.models = new List<IAnimalPopulationModel>();

			// Add to scene
			List<AnimalType> tmpList = new List<AnimalType>(scene.animalTypes);
			tmpList.Add (this);
			scene.animalTypes = tmpList.ToArray();
		}
		
		public static void Load (AnimalType animal, XmlTextReader reader, Scene scene)
		{
			animal.name = reader.GetAttribute ("name");
			/*animal.foodParamName = reader.GetAttribute ("foodparam");
			animal.foodOverruleParamName = reader.GetAttribute ("foodoverruleparam");
			animal.dangerParamName = reader.GetAttribute ("dangerparam");
			animal.moveDistanceMale = int.Parse(reader.GetAttribute ("movedistm"));
			animal.moveDistanceFemale = int.Parse(reader.GetAttribute ("movedistf"));
			animal.wanderMale = float.Parse(reader.GetAttribute ("wanderm"));
			animal.wanderFemale = float.Parse(reader.GetAttribute ("wanderf"));*/
			animal.dataName = reader.GetAttribute ("dataname");

			if (string.IsNullOrEmpty(animal.dataName)) 
				animal.dataName = string.Format("_animal{0}", StringUtil.MakeValidID(animal.name));

			// TODO: ADD TYPE (S,M,L)
		}
		
		public virtual void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			SaveBase (writer, scene);
			writer.WriteEndElement ();

			// TODO: ADD TYPE (S,M,L)
		}
		protected void SaveBase (XmlTextWriter writer, Scene scene)
		{
			writer.WriteAttributeString ("name", name);
			/*writer.WriteAttributeString ("foodparam", foodParamName);
			writer.WriteAttributeString ("foodoverruleparam", foodOverruleParamName);
			writer.WriteAttributeString ("dangerparam", dangerParamName);
			writer.WriteAttributeString ("movedistm", moveDistanceMale.ToString());
			writer.WriteAttributeString ("movedistf", moveDistanceFemale.ToString());
			writer.WriteAttributeString ("wanderm", wanderMale.ToString());
			writer.WriteAttributeString ("wanderf", wanderFemale.ToString());*/
			writer.WriteAttributeString ("dataname", dataName);
		}
		
		public virtual void UpdateReferences (Scene scene)
		{

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
