using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.AnimalPopulationModel;
using Ecosim.SceneEditor;
using Ecosim.SceneEditor.Helpers.AnimalPopulationModel;

namespace Ecosim.SceneEditor.Helpers
{
	public class SpeciesPanelAnimalsHelper
	{
		private static string newAnimalName = "New animal";
		private static string newAnimalType = "";

		private static Dictionary<string, System.Type> _animalTypes;
		private static Dictionary<string, System.Type>  animalTypes {
			get {
				if (_animalTypes == null) 
				{
					_animalTypes = new Dictionary<string, System.Type>();
					_animalTypes.Add ("Large", typeof (LargeAnimalType));
					_animalTypes.Add ("Normal", null);
					_animalTypes.Add ("Small", null);
				}
				return _animalTypes;
			}
		}

		private static Dictionary<System.Type, System.Type> _modelHelpers;
		private static Dictionary<System.Type, System.Type> modelHelpers {
			get {
				if (_modelHelpers == null) 
				{
					_modelHelpers = new Dictionary<System.Type, System.Type>();
					_modelHelpers.Add (typeof(AnimalStartPopulationModel), typeof(AnimalStartPopulationModelHelper));
					_modelHelpers.Add (typeof(AnimalPopulationGrowthModel), typeof(AnimalPopulationGrowthModelHelper));
				}
				return _modelHelpers;
			}
		}

		private static SpeciesPanel panel;
		private static EditorCtrl ctrl;
		private static Scene scene;

		public static void Setup (SpeciesPanel pnl)
		{
			panel = pnl;
			ctrl = panel.ctrl;
			scene = panel.scene;

			foreach (SpeciesPanel.AnimalState ast in panel.animals) {
				SetupAnimalState (ast);
			}
		}

		private static void SetupAnimalState (SpeciesPanel.AnimalState ast)
		{
			ast.modelHelpers = new List<IAnimalPopulationModelHelper>();

			foreach (IAnimalPopulationModel m in ast.animal.models) 
			{
				SpeciesPanel.AnimalState tmpAst = ast;
				System.Type mhType = modelHelpers [m.GetType()];
				IAnimalPopulationModelHelper mh = (IAnimalPopulationModelHelper)System.Activator.CreateInstance(mhType);

				mh.Setup(panel, tmpAst, m, delegate (System.Type type, out ExtraPanel outPanel) 
				{
					outPanel = (ExtraPanel)System.Activator.CreateInstance (type, ctrl, tmpAst.animal);
					if (panel.extraPanel != null)
						panel.extraPanel.Dispose ();
					panel.extraPanel = outPanel;
				});
				ast.modelHelpers.Add (mh);
			}
		}

		public static void Render (int mx, int my)
		{
			int index = 0;
			foreach (SpeciesPanel.AnimalState ast in panel.animals)
			{
				GUILayout.BeginVertical (ctrl.skin.box);
				{
					// Header
					GUILayout.BeginHorizontal ();
					{
						if (GUILayout.Button (ast.isFoldedOpen ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) 
						{
							ast.isFoldedOpen = !ast.isFoldedOpen;
						}
						
						GUILayout.Label (index.ToString(), GUILayout.Width (40));
						ast.animal.name = GUILayout.TextField (ast.animal.name);
						
						if (GUILayout.Button ("-", GUILayout.Width (20)))
						{
							SpeciesPanel.AnimalState tmp = ast;
							ctrl.StartDialog (string.Format("Delete animal '{0}'?", tmp.animal.name), newVal => { 
								panel.DeleteAnimal (ast);
							}, null);
						}
					}
					GUILayout.EndHorizontal(); // ~Header
					
					// Animal body
					if (ast.isFoldedOpen) 
					{
						GUILayout.Space (8f);
						RenderAnimal (ast, mx, my);
					}
				}
				GUILayout.EndVertical(); // ~Animal body
				index++;
			} // ~AnimalState foreach

			// New animal type
			GUILayout.BeginHorizontal ();
			{
				if (string.IsNullOrEmpty (newAnimalType)) {
					foreach (KeyValuePair<string, System.Type> p in animalTypes) {
						newAnimalType = p.Key;
						break;
					}
				}
				GUILayout.Label ("New type:", GUILayout.Width (60));
				if (GUILayout.Button (newAnimalType, GUILayout.Width (200)))
				{
					List<string> types = new List<string>();
					foreach (KeyValuePair<string, System.Type> p in animalTypes) {
						types.Add (p.Key);
					}
					ctrl.StartSelection (types.ToArray(), types.IndexOf (newAnimalType), delegate (int newIdx) {
						newAnimalType = types [newIdx];
					});
				}
			}
			GUILayout.EndHorizontal ();

			// New animal name
			if (panel.RenderAddButton ("New animal:", ref newAnimalName))
			{
				// Add new animal
				System.Type type = animalTypes [newAnimalType];
				if (type == null) {

					ctrl.StartOkDialog (string.Format("Animal type of '{0}' is still under construction, please select another and try again.", newAnimalType), null);
					return;
				}

				AnimalType t = (AnimalType)System.Activator.CreateInstance (type, scene, newAnimalName);
				SpeciesPanel.AnimalState state = new SpeciesPanel.AnimalState (t);
				panel.animals.Add (state);
				SetupAnimalState (state);
			}
		}

		private static void RenderAnimal (SpeciesPanel.AnimalState ast, int mx, int my)
		{
			// Render each population model helpers
			foreach (IAnimalPopulationModelHelper mh in ast.modelHelpers) {
				mh.Render (mx, my);
				GUILayout.Space (5f);
			}
		}
	}
}