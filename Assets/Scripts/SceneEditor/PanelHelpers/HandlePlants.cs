using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;
using Ecosim.SceneData.Rules;
using Ecosim.SceneData.PlantRules;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandlePlants : PanelHelper
	{
		private readonly MapsPanel parent;
		private Scene scene;
		private EditorCtrl ctrl;

		private int brushWidth;
		private enum EBrushMode
		{
			Area,
			Circle
		};
		private EBrushMode brushMode;
		private GridTextureSettings gridSettings255;

		private EditData edit;
		private Data data;
		private Data backupCopy;

		private GUIStyle tabNormal;
		private GUIStyle tabSelected;

		private int maxParamValue = 0;
		private int paramStrength = 255;
		private string paramStrengthStr = "255";

		private string[] plantNames;
		private PlantType activePlantType;

		//private int activeParameter = 0;

		public HandlePlants (EditorCtrl ctrl, MapsPanel parent, Scene scene)
		{
			this.parent = parent;
			this.ctrl = ctrl;
			
			this.scene = parent.scene;
			tabNormal = ctrl.listItem;
			tabSelected = ctrl.listItemSelected;

			Setup ();
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
		
		void Setup ()
		{
			plantNames = GetPlantNames();
			if (plantNames.Length > 0) {
				activePlantType = GetPlantType (plantNames[0]);
				SetupPlantEditData ();
			}

			gridSettings255 = new GridTextureSettings (false, 0, 16, "MapGrid255", true, "ActiveMapGrid255");

			edit = EditData.CreateEditData ("plants", data, delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
				if ((!ctrl) || (maxParamValue == 1)) {
					return shift ? 0 : paramStrength;
				} else {
					return Mathf.RoundToInt ((shift ? 0 : (strength * paramStrength)) + ((1 - strength) * currentVal) + 0.49f);
				}
			}, gridSettings255);

			edit.AddRightMouseHandler (delegate(int x, int y, int v) {
				paramStrength = v;
				paramStrengthStr = paramStrength.ToString ();
			});

			edit.SetModeBrush (brushWidth);
			brushMode = EBrushMode.Circle;
			backupCopy = new BitMap8 (scene);
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

		public bool Render (int mx, int my)
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
			GUILayout.EndHorizontal(); //~Plant type*/

			GUILayout.BeginHorizontal(); // Brush mode
			{
				GUILayout.Label ("Brush mode", GUILayout.Width (100));
				if (GUILayout.Button ("Area select", (brushMode == EBrushMode.Area) ? tabSelected : tabNormal, GUILayout.Width (100))) {
					brushMode = EBrushMode.Area;
					edit.SetModeAreaSelect ();
				}
				if (GUILayout.Button ("Circle brush", (brushMode == EBrushMode.Circle) ? tabSelected : tabNormal, GUILayout.Width (100))) {
					brushMode = EBrushMode.Circle;
					edit.SetModeBrush (brushWidth);
				}
				GUILayout.FlexibleSpace ();
			}
			GUILayout.EndHorizontal(); //~Brush mode
			
			GUILayout.BeginHorizontal (); // Brush value
			{
				GUILayout.Label ("Brush value", GUILayout.Width (100));
				if (maxParamValue > 1)
				{
					if (paramStrength > activePlantType.maxPerTile)
						paramStrength = activePlantType.maxPerTile;

					int newParamStrength = Mathf.RoundToInt (GUILayout.HorizontalSlider (paramStrength, 1, maxParamValue, GUILayout.Width (160)));
					if (newParamStrength != paramStrength) {
						newParamStrength = Mathf.Clamp (newParamStrength, 1, maxParamValue);
						paramStrengthStr = newParamStrength.ToString ();
						paramStrength = newParamStrength;
					}
					string newParamStrengthStr = GUILayout.TextField (paramStrengthStr, GUILayout.Width (30));
					if (newParamStrengthStr != paramStrengthStr) {
						int intVal;
						if (int.TryParse (newParamStrengthStr, out intVal)) {
							paramStrength = Mathf.Clamp (intVal, 1, maxParamValue);
						}
						paramStrengthStr = newParamStrengthStr;
					}
					GUILayout.Label ("(0-" + maxParamValue + ")");
				} else {
					GUILayout.Label (paramStrengthStr);
				} 
			}
			GUILayout.EndHorizontal (); //~Brush value
			
			if (brushMode == EBrushMode.Circle) 
			{
				GUILayout.BeginHorizontal (); // Brush width
				{
					GUILayout.Label ("Brush width", GUILayout.Width (100));
					int newBrushWidth = (int)GUILayout.HorizontalSlider (brushWidth, 0f, 10f, GUILayout.Width (160f));
					GUILayout.Label (brushWidth.ToString ());
					if (newBrushWidth != brushWidth) {
						brushWidth = newBrushWidth;
						edit.SetModeBrush (brushWidth);
					}
					GUILayout.FlexibleSpace ();
				}
				GUILayout.EndHorizontal (); //~Brush width
			}

			GUILayout.Space (16);

			GUILayout.BeginHorizontal (); // Save, restore etc
			{
				if (GUILayout.Button ("Save to clipboard", GUILayout.Width (100))) {
					edit.CopyData (backupCopy);
				}
				if (GUILayout.Button ("Restore from clipb.", GUILayout.Width (100))) {
					backupCopy.CopyTo (data);
					edit.SetData (data);
				}
				if (GUILayout.Button ("Apply", GUILayout.Width (60))) {
					edit.CopyData (data);
				}
				if (GUILayout.Button ("Reset", GUILayout.Width (60))) {
					edit.SetData (data);
				}
			}
			GUILayout.EndHorizontal (); //~Save, restore etc

			GUILayout.FlexibleSpace ();
			GUILayout.Space (8);
			if (parent.texture != null) 
			{
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("Set from image", GUILayout.Width (100));
					if (GUILayout.Button ("Set " + activePlantType.name)) 
					{
						int maxValue = activePlantType.maxPerTile;
						for (int y = 0; y < scene.height; y++) 
						{
							for (int x = 0; x < scene.width; x++) 
							{
								int v = (int)(maxValue * parent.GetFromImage (x, y));
								data.Set (x, y, v);
							}
						}
						edit.SetData (data);
					}
					GUILayout.FlexibleSpace ();
				}
				GUILayout.EndHorizontal ();
			}
			parent.RenderLoadTexture ();
			return false;
		}

		public void Disable ()
		{
			if (edit != null)
				edit.Delete ();
			edit = null;
		}
		
		public void Update ()
		{

		}
	}
}

