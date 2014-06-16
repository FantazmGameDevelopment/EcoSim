using System.Collections.Generic;
using System.IO;
using System.Xml;

using Ecosim.SceneData.Rules;
using Ecosim.SceneData.AnimalPopulationModel;

namespace Ecosim.SceneData
{
	public class LargeAnimalType : AnimalType
	{
		public LargeAnimalType ()
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
			AnimalStartPopulationModel sm = new AnimalStartPopulationModel ();
			sm.nests.show = true;
			this.models.Add (sm);

			// Growth population model
			AnimalPopulationGrowthModel gm = new AnimalPopulationGrowthModel ();
			gm.fixedNumber.show = true;
			this.models.Add (gm);

			// Decrease population model
			AnimalPopulationDecreaseModel dm = new AnimalPopulationDecreaseModel ();
			dm.fixedNumber.show = true;
			dm.specifiedNumber.show = true;
			dm.specifiedNumber.naturalDeathRate.show = true;
			dm.specifiedNumber.starvation.show = true;
			dm.specifiedNumber.artificialDeath.show = true;
			this.models.Add (dm);

			// Land use population model
			AnimalPopulationLandUseModel lu = new AnimalPopulationLandUseModel ();
			lu.food.show = true;
			lu.movement.show = true;
			this.models.Add (lu);
		}

		public static LargeAnimalType Load (XmlTextReader reader, Scene scene)
		{
			LargeAnimalType animal = new LargeAnimalType ();
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

		public override void UpdateReferences (Scene scene)
		{
			base.UpdateReferences (scene);

			foreach (IAnimalPopulationModel m in models) {
				m.UpdateReferences (scene);
			}
		}
	}
}
