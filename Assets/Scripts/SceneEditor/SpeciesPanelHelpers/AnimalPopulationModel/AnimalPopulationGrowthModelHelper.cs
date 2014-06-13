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
			// TODO: AnimalPopulationGrowthModelHelper.Render
			GUILayout.Label ("TODO: AnimalPopulationGrowthModelHelper");
		}
	}
}
