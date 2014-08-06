using System.Collections.Generic;
using System.IO;
using System.Xml;

using Ecosim.SceneData.Rules;
using Ecosim.SceneData.AnimalPopulationModel;

namespace Ecosim.SceneData
{
	public class LargeAnimalType : AnimalType
	{
		public AnimalStartPopulationModel startPopModel { get; private set; }
		public AnimalPopulationGrowthModel growthModel { get; private set; }
		public AnimalPopulationDecreaseModel decreaseModel { get; private set; }
		public AnimalPopulationLandUseModel landUseModel { get; private set; }

		public Data movementMap;
		public Data deathMap;
		public Data birthMap;
		public Data fouragingMap;

		public LargeAnimalType (Scene scene) : base (scene, "")
		{
			SetupModels ();
		}

		/************************************/

		// Default large animal action script

		/*void LoadProgress(bool initScene) 
		{
			foreach (AnimalType at in scene.animalTypes)
			{
				if (at is LargeAnimalType)
				{
					LargeAnimalType lg = at as LargeAnimalType;
					GetVariablesData (lg);
					GetFormulasData (lg);
				}
			}
		}
		
		void GetVariablesData (LargeAnimalType a)
		{
			AnimalPopulationDecreaseModel dm = a.decreaseModel;
			AnimalPopulationDecreaseModel.SpecifiedNumber dsn = dm.specifiedNumber;
			if (dsn.use) {
				// Starvation
				if (dsn.starvation.use)
				{
					string cat = a.name + " Starvation";
					AddVarData ("starvemin", "Min Starve Range", cat);  
					LinkVar (dsn.starvation, "minStarveRange", "starvemin");

					AddVarData ("starvemax", "Max Starve Range", cat);  
					LinkVar (dsn.starvation, "maxStarveRange", "starvemax");
				}
			}
		}
		
		void GetFormulasData (LargeAnimalType a)
		{
			// Get all formulas that are used for this animal
			string name = a.name;
			string cat = "Unknown";

			// Growth category, first check if the animal
			// actually uses the model(s)
			cat = "Growth";
			if (a.growthModel.fixedNumber.use) 
			{
				string body = 
@"M = current amount of males
F = current amounf of femals
L = Littersize of the animal
New population = M + F + (M / M * F * L)";
				AddFormulaRepresentation (name, cat, body);
			}

			// Decrease category, first check if the animal
			// actually uses the model(s)
			cat = "Decrease";
			if (a.decreaseModel.fixedNumber.use)
			{
				string body =
@"The absolute number is the amount of deaths across all nests.
This amount is randomly divided across all nests.";
				AddFormulaRepresentation (name, cat, body);
			}
		}

		// Copy some eco base methods to be able to auto complete them
		public void LinkVar (object obj, string field, string variable) { }

		public void AddVarData (string variable, string name, string category) { }

		public void SetupVariableLink (object obj, string field, string variable) { }

		public void AddVariableRepresentation (string variable, string name, string category) { }
		
		public void AddFormulaRepresentation (string name, string category, string body) { }*/

		/************************************/

		public LargeAnimalType (Scene scene, string name) : base (scene, name)
		{
			SetupModels ();
		}

		private void SetupModels ()
		{
			this.models = new List<IAnimalPopulationModel> ();

			// Start population model
			startPopModel = new AnimalStartPopulationModel (this);
			startPopModel.nests.show = true;
			this.models.Add (startPopModel);

			// Growth population model
			growthModel = new AnimalPopulationGrowthModel (this);
			growthModel.fixedNumber.show = true;
			this.models.Add (growthModel);

			// Decrease population model
			decreaseModel = new AnimalPopulationDecreaseModel (this);
			decreaseModel.fixedNumber.show = true;
			decreaseModel.specifiedNumber.show = true;
			decreaseModel.specifiedNumber.naturalDeathRate.show = true;
			decreaseModel.specifiedNumber.starvation.show = true;
			decreaseModel.specifiedNumber.artificialDeath.show = true;
			this.models.Add (decreaseModel);

			// Land use population model
			landUseModel = new AnimalPopulationLandUseModel (this);
			landUseModel.food.show = true;
			landUseModel.movement.show = true;
			this.models.Add (landUseModel);
		}

		public static LargeAnimalType Load (XmlTextReader reader, Scene scene)
		{
			LargeAnimalType animal = new LargeAnimalType (scene);
			AnimalType.Load (animal, reader, scene);

			if (!reader.IsEmptyElement) {
				while (reader.Read()) 
				{
					XmlNodeType nType = reader.NodeType;
					string readerName = reader.Name.ToLower();

					if (nType == XmlNodeType.Element)
					{
						foreach (IAnimalPopulationModel m in animal.models)
						{
							if (readerName == m.GetXMLElement ()) {
								m.Load (reader, scene);
							}
						}
					}
					else if ((nType == XmlNodeType.EndElement) && (readerName == XML_ELEMENT)) {
						break;
					}
				}
			}
			return animal;
		}

		public override void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("type", "Large");
			SaveBase (writer, scene);
			foreach (IAnimalPopulationModel m in models) {
				m.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}
	}
}
