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
