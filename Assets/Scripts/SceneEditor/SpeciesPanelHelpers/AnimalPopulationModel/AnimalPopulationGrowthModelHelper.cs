using UnityEngine;
using System.IO;
using System.Xml;
using Ecosim.SceneData;
using Ecosim.SceneData.AnimalPopulationModel;
using Ecosim.SceneEditor;
using Ecosim.SceneEditor.Helpers;

namespace Ecosim.SceneEditor.Helpers.AnimalPopulationModel
{
	public class AnimalPopulationGrowthModelHelper : IAnimalPopulationModelHelper
	{
		private AnimalPopulationGrowthModel model;

		public override void Setup (SpeciesPanel panel, SpeciesPanel.AnimalState animalState, IAnimalPopulationModel model, CreateExtraPanelDelegate onCreateExtraPanel)
		{
			base.Setup (panel, animalState, model, onCreateExtraPanel);
			this.model = (AnimalPopulationGrowthModel)model;
		}
		
		public override void Render (int mx, int my)
		{
			GUILayout.BeginVertical ();//EditorCtrl.self.skin.box);
			{
				GUILayout.Label (" Population Growth");

				if (this.RenderHeaderStart ("Fixed", this.model.fixedNumber, true))
				{
					// Type
					EcoGUI.EnumButton<AnimalPopulationGrowthModel.FixedNumber.Type>("Type", model.fixedNumber.type, 
					                                                                delegate(AnimalPopulationGrowthModel.FixedNumber.Type newType) {
						model.fixedNumber.type = newType;
					}, GUILayout.Width (50), GUILayout.Width (150)); 
					
					// Litter size
					GUILayout.BeginHorizontal ();
					{
						GUILayout.Space (2);
						GUILayout.Label ("Litter size", GUILayout.Width (50));
						EcoGUI.skipHorizontal = true;
						GUILayout.BeginHorizontal ();
						{
							EcoGUI.IntField ("Min", ref model.fixedNumber.minLitterSize, GUILayout.Width (20), GUILayout.Width (50));
							EcoGUI.IntField ("Max", ref model.fixedNumber.maxLitterSize, GUILayout.Width (20), GUILayout.Width (50));
						}
						GUILayout.EndHorizontal ();
						EcoGUI.skipHorizontal = false;
					}
					GUILayout.EndHorizontal ();
				}
				this.RenderHeaderEnd (this.model.fixedNumber);
			}
			GUILayout.EndVertical ();
		}
	}
}
