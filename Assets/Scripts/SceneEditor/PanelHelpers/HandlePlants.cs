using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;
using Ecosim.SceneData.Rules;
using Ecosim.SceneData.PlantRules;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandlePlants : ParameterPaintPanel
	{
		private string[] plantNames;
		private PlantType activePlantType;

		public HandlePlants (EditorCtrl ctrl, MapsPanel parent, Scene scene)  : base(ctrl, parent, scene)
		{
			Setup ("plants");
		}
		
		protected override void Setup (string editDataParamName)
		{
			plantNames = GetPlantNames();
			if (plantNames.Length > 0) {
				activePlantType = GetPlantType (plantNames[0]);
				SetupPlantEditData ();
			}

			base.Setup (editDataParamName);
		}

		public override bool Render (int mx, int my)
		{
			if (scene.plantTypes.Length != plantNames.Length) {
				plantNames = GetPlantNames();
			}

			if (plantNames.Length == 0) {
				GUILayout.Label ("No plants found.");
				return false;
			}

			GUILayout.BeginHorizontal(); // Plant type
			{
				if (GUILayout.Button (activePlantType.name, GUILayout.ExpandWidth (true)))// GUILayout.Width(240)))
				{
					ctrl.StartSelection (plantNames, GetPlantNameIndex(plantNames, activePlantType.name),
					newIndex => {
						bool newPlant = true;
						if (activePlantType != null) {
							newPlant = (plantNames[newIndex] != activePlantType.name);
						}
						if (newPlant) {
							activePlantType = GetPlantType (plantNames[newIndex]);
							SetupPlantEditData ();
						}
					});
				}

				//GUILayout.Label ("Range: 0-" + maxParamValue, GUILayout.Width(100));
				//GUILayout.FlexibleSpace ();
			}
			GUILayout.EndHorizontal(); //~Plant type

			GUILayout.Space (5);
			this.RenderBrushMode ();
			GUILayout.Space (16);
			this.RenderSaveRestoreApply ();
			GUILayout.Space (5);
			GUILayout.FlexibleSpace ();
			this.RenderFromImage (activePlantType.name);
			return false;
		}

		void SetupPlantEditData ()
		{
			if (activePlantType == null) {
				backupCopy.Clear ();
				return;
			}
			
			if (scene.progression.GetData (activePlantType.dataName) == null)
				scene.progression.AddData (activePlantType.dataName, new BitMap8 (scene));
			data = scene.progression.GetData (activePlantType.dataName);
			
			maxParamValue = activePlantType.maxPerTile;
			paramStrength = maxParamValue;
			paramStrengthStr = paramStrength.ToString();
			
			if (edit != null) edit.SetData (data);
			if (backupCopy != null) data.CopyTo (backupCopy);
		}
		
		string[] GetPlantNames()
		{
			List<string> pList = new List<string>();
			if (scene != null) {
				foreach (PlantType p in scene.plantTypes) {
					pList.Add (p.name);
				}
			}
			return pList.ToArray();
		}
		
		PlantType GetPlantType (string name) 
		{
			if (scene != null) {
				foreach (PlantType p in scene.plantTypes) {
					if (p.name == name) return p;
				}
			}
			return null;
		}
		
		int GetPlantNameIndex (string[] names, string name)
		{
			for (int i = 0; i < names.Length; i++) {
				if (names[i] == name) return i;
			}
			return 0;
		}
	}
}

