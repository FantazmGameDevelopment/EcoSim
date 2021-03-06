using System.Collections.Generic;
using System.IO;
using System.Xml;

using Ecosim.SceneData;
using Ecosim.SceneData.Rules;
using Ecosim.SceneData.AnimalPopulationModel;

namespace Ecosim.SceneData
{
	public class AnimalType
	{
		public const string XML_ELEMENT = "animal";

		public readonly Scene scene;
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
		
		public AnimalType (Scene scene, string name)
		{
			this.scene = scene;
			this.name = name;
			this.index = scene.animalTypes.Length;

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
		}
		
		public virtual void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			SaveBase (writer, scene);
			writer.WriteEndElement ();
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
		}
		
		public virtual void UpdateReferences (Scene scene)
		{
			foreach (IAnimalPopulationModel model in this.models) {
				model.UpdateReferences (scene);
			}
		}

		public virtual void PrepareSuccession ()
		{
			if (this.models != null)
			{
				try {
					foreach (IAnimalPopulationModel m in this.models) {
						m.PrepareSuccession ();
					}
				} catch (System.Exception e) {
					Log.LogException (e);
				}
			}
		}
		
		public virtual void DoSuccession ()
		{
			if (this.models != null)
			{
				try {
					foreach (IAnimalPopulationModel m in this.models) {
						m.DoSuccession ();
					}
				} catch (System.Exception e) {
					Log.LogException (e);
				}
			}
		}
		
		public virtual void FinalizeSuccession ()
		{
			if (this.models != null)
			{
				try {
					foreach (IAnimalPopulationModel m in this.models) {
						m.FinalizeSuccession ();
					}
				} catch (System.Exception e) {
					Log.LogException (e);
				}
			}
		}

		public static AnimalType Find (Scene scene, string name)
		{
			name = name.ToLower ();
			foreach (AnimalType t in scene.animalTypes) {
				if (t.name.ToLower() == name) return t;
			}
			return null;
		}

		/// <summary>
		/// Returns the 'associated' data. Associated data has a unique name identifier
		/// that links the data named 'name' to this animal automatically.
		/// </summary>
		/// <returns>The associated data.</returns>
		/// <param name="name">Name.</param>
		public Data GetAssociatedData (string name)
		{
			string dataName = ("animal" + this.index + "_" + name);
			if (scene.progression.HasData (dataName)) {
				return scene.progression.GetData (dataName);
			}
			return null;
		}

		/// <summary>
		/// Returns the 'associated' data of the given year. Associated data has a unique name identifier
		/// that links the data named 'name' to this animal automatically.
		/// </summary>
		/// <returns>The associated data.</returns>
		/// <param name="name">Name.</param>
		public Data GetAssociatedData (string name, int year)
		{
			string dataName = ("animal" + this.index + "_" + name);
			if (scene.progression.HasData (dataName)) {
				return scene.progression.GetData (dataName, year);
			}
			return null;
		}

		/// <summary>
		/// Adds a new 'associated' data. Associated data has a unique name identifier
		/// that links the data named 'name' to this animal automatically.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="data">Data.</param>
		public void AddAssociatedData (string name, Data data)
		{
			string dataName = ("animal" + this.index + "_" + name);
			scene.progression.AddData (dataName, data);
		}
	}
}
