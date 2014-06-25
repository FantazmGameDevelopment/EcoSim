using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Ecosim.SceneData;
using Ecosim.SceneData.AnimalPopulationModel;
using Ecosim.SceneEditor;
using Ecosim.SceneEditor.Helpers;

namespace Ecosim.SceneEditor.Helpers.AnimalPopulationModel
{
	public class AnimalPopulationLandUseModelHelper : IAnimalPopulationModelHelper
	{
		private AnimalPopulationLandUseModel model;
		
		public override void Setup (SpeciesPanel panel, SpeciesPanel.AnimalState animalState, IAnimalPopulationModel model, CreateExtraPanelDelegate onCreateExtraPanel)
		{
			base.Setup (panel, animalState, model, onCreateExtraPanel);
			this.model = (AnimalPopulationLandUseModel)model;
		}
		
		public override void Render (int mx, int my)
		{
			GUILayout.BeginVertical ();//EditorCtrl.self.skin.box);
			{
				GUILayout.Label (" Land use");

				if (this.RenderHeaderStart ("Food", this.model.food, true))
				{
					GUILayout.BeginHorizontal ();
					{
						GUILayout.Space (2);
						GUILayout.Label ("Fouraging area", GUILayout.Width (100));
						if (GUILayout.Button (this.model.food.parameterName))
						{
							List<string> items = panel.scene.progression.GetAllDataNames ();
							if (items.Count > 0)
							{
								int selected = Mathf.Max (0, items.IndexOf (this.model.food.parameterName));
								EditorCtrl.self.StartSelection (items.ToArray(), selected, delegate(int index) {
									this.model.food.parameterName = items [index];
								});
							}
						}
					}
					GUILayout.EndHorizontal ();

					EcoGUI.IntField ("Carrying capacity", ref this.model.food.foodCarryCapacity, 100, 50);
				}
				this.RenderHeaderEnd (this.model.food);

				if (this.RenderHeaderStart ("Movement", this.model.movement, true))
				{
					GUILayout.BeginHorizontal ();
					{
						GUILayout.Space (2);
						GUILayout.Label ("Move preference area", GUILayout.Width (120));
						if (GUILayout.Button (this.model.movement.movePreferenceAreaParamName))
						{
							List<string> items = panel.scene.progression.GetAllDataNames ();
							if (items.Count > 0)
							{
								int selected = Mathf.Max (0, items.IndexOf (this.model.movement.movePreferenceAreaParamName));
								EditorCtrl.self.StartSelection (items.ToArray(), selected, delegate(int index) {
									this.model.movement.movePreferenceAreaParamName = items [index];
								});
							}
						}
					}
					GUILayout.EndHorizontal ();

					
					EcoGUI.skipHorizontal = true;
					GUILayout.BeginHorizontal ();
					{
						GUILayout.Label (" Walk distance", GUILayout.Width (100));
						EcoGUI.IntField ("Min", ref this.model.movement.minWalkDistance, GUILayout.Width (20), GUILayout.Width (50));
						EcoGUI.IntField ("Max", ref this.model.movement.maxWalkDistance, GUILayout.Width (20), GUILayout.Width (50));
					}
					GUILayout.EndHorizontal ();
					EcoGUI.skipHorizontal = false;
				}
				this.RenderHeaderEnd (this.model.movement);
			}
			GUILayout.EndVertical ();
		}
	}
}
