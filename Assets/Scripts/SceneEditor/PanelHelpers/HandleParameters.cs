using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandleParameters : PanelHelper
	{
		private readonly MapsPanel parent;
		private string[] parameters;
		private string[] bitSizeStings = new string[] { "1 bit (0-1)", "2 bit (0-3)", "4 bit (0-15)", "8 bit (0-255)", "Sparse 1-bit", "Sparse 8-bit", "TextMarkers", "Calculated" };
		private int bitSizeIndex = 3;
		private string newParamName = "Parameter";
		private int activeParameter = 0;
		private int maxParamValue;
		private Scene scene;
		private EditorCtrl ctrl;
		private GridTextureSettings gridSettings255 = new GridTextureSettings (false, 0, 16, "MapGrid255", true, "ActiveMapGrid255");
		private int brushWidth;
//		private float brushStrength = 1.0f;
		private enum EBrushMode
		{
			Area,
			Circle
		};
		private EBrushMode brushMode;
		private int paramStrength = 255;
		private string paramStrengthStr = "255";
		private EditData edit;
		private Data backupCopy;
		private GUIStyle tabNormal;
		private GUIStyle tabSelected;
		
		// when onlyEditThisData is set, the user isn't given a choice to select
		// which data to edit, but can only edit this data.
		private Data onlyEditThisData = null;
		
		public HandleParameters (EditorCtrl ctrl, MapsPanel parent, Scene scene, Data data)
		{
			this.parent = parent;
			this.ctrl = ctrl;
			onlyEditThisData = data;
			
			this.scene = parent.scene;
			tabNormal = ctrl.listItem;
			tabSelected = ctrl.listItemSelected;
			
			if (scene != null) {
				List<string> pList = new List<string> ();
				foreach (string p in scene.progression.GetAllDataNames()) {
					Data dataFindNames = scene.progression.GetData (p);
					if ((dataFindNames.GetMax() < 256) && (!p.StartsWith ("_")))
						pList.Add (p);
				}
				parameters = pList.ToArray ();
				activeParameter = 0;
			}
			Setup ();
		}
		
		void Setup ()
		{
			Data data = (onlyEditThisData != null) ? onlyEditThisData :
				(scene.progression.GetData (parameters [activeParameter]));
			maxParamValue = data.GetMax ();
			paramStrength = maxParamValue;
			paramStrengthStr = paramStrength.ToString ();
			edit = EditData.CreateEditData ("parameter", data, delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
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
			data.CopyTo (backupCopy);
		}
				
		public bool Render (int mx, int my)
		{
			Data currentData = (onlyEditThisData != null)?onlyEditThisData:
				(scene.progression.GetData (parameters [activeParameter]));
			if (onlyEditThisData == null) {
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button (parameters [activeParameter], GUILayout.Width (140))) {
					ctrl.StartSelection (parameters, activeParameter,
					newIndex => {
						if (newIndex != activeParameter) {
							activeParameter = newIndex;
							Data data = scene.progression.GetData (parameters [activeParameter]);
							edit.SetData (data);
							maxParamValue = data.GetMax ();
							paramStrength = maxParamValue;
							paramStrengthStr = paramStrength.ToString ();
						}
					});
				}
				string typeName = currentData.GetType().ToString();
				typeName = typeName.Substring(typeName.LastIndexOf('.') + 1);
				GUILayout.Label ("range 0-" + maxParamValue + " " + typeName);
				GUILayout.FlexibleSpace ();
				if ((parameters.Length > 1) && GUILayout.Button ("Delete")) {
					ctrl.StartDialog ("Delete '" + parameters [activeParameter] + "'?",
				actionResult => {
						scene.progression.DeleteData (parameters [activeParameter]);
						List<string> parametersList = new List<string> (parameters);
						parametersList.RemoveAt (activeParameter);
						parameters = parametersList.ToArray ();
						activeParameter = 0;
						Data data = scene.progression.GetData (parameters [activeParameter]);
						edit.SetData (data);
					}, null);
				}
				GUILayout.EndHorizontal ();
			}
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("brush mode", GUILayout.Width (100));
			if (GUILayout.Button ("Area select", (brushMode == EBrushMode.Area) ? tabSelected : tabNormal, GUILayout.Width (100))) {
				brushMode = EBrushMode.Area;
				edit.SetModeAreaSelect ();
			}
			if (GUILayout.Button ("Circle brush", (brushMode == EBrushMode.Circle) ? tabSelected : tabNormal, GUILayout.Width (100))) {
				brushMode = EBrushMode.Circle;
				edit.SetModeBrush (brushWidth);
			}
			
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Brush value", GUILayout.Width (100));
			if (maxParamValue > 1) {
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
			} else {
				GUILayout.Label (paramStrengthStr);
			}

			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			if (brushMode == EBrushMode.Circle) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Brush width", GUILayout.Width (100));
				int newBrushWidth = (int)GUILayout.HorizontalSlider (brushWidth, 0f, 10f, GUILayout.Width (160f));
				GUILayout.Label (brushWidth.ToString ());
				if (newBrushWidth != brushWidth) {
					brushWidth = newBrushWidth;
					edit.SetModeBrush (brushWidth);
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
			}
			GUILayout.Space (16);
			if (onlyEditThisData == null) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Create parameter", GUILayout.Width (100));
				if (GUILayout.Button (bitSizeStings [bitSizeIndex], tabNormal, GUILayout.Width (80))) {
					ctrl.StartSelection (bitSizeStings, bitSizeIndex,
					newIndex => {
						bitSizeIndex = newIndex;
					});
				}
				newParamName = GUILayout.TextField (newParamName, GUILayout.Width (80));
				if (GUILayout.Button ("Create")) {
					newParamName = newParamName.Trim ();
					if (!StringUtil.IsValidID (newParamName)) {
						ctrl.StartOkDialog ("Not a valid parameter name ([a-zA-Z][a-zA-Z0-9_]*)", null);
					} else if (scene.progression.HasData (newParamName)) {
						ctrl.StartOkDialog ("Name already exists", null);
					} else {
						Data data = null;
						switch (bitSizeIndex) {
						case 0 :
							data = new BitMap1 (scene);
							break;
						case 1 :
							data = new BitMap2 (scene);
							break;
						case 2 :
							data = new BitMap4 (scene);
							break;
						case 3 :
							data = new BitMap8 (scene);
							break;
						case 4 :
							data = new SparseBitMap1 (scene);
							break;
						case 5 :
							data = new SparseBitMap8 (scene);
							break;
						case 6 :
							data = new TextBitMap (scene);
							break;
						case 7 :
							data = new CalculatedData (scene, newParamName);
							break;
						}
						scene.progression.AddData (newParamName, data);
						List<string> paramNameList = new List<string> (parameters);
						paramNameList.Add (newParamName);
						parameters = paramNameList.ToArray ();
						activeParameter = parameters.Length - 1;
						edit.SetData (data);
						maxParamValue = data.GetMax ();
						paramStrength = maxParamValue;
						paramStrengthStr = paramStrength.ToString ();
					}
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				GUILayout.Space (16);
			}
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Save to clipboard", GUILayout.Width (100))) {
				edit.CopyData (backupCopy);
			}
			if (GUILayout.Button ("Restore from clipb.", GUILayout.Width (100))) {
				backupCopy.CopyTo (currentData);
				edit.SetData (currentData);
			}
			if (GUILayout.Button ("Apply", GUILayout.Width (60))) {
				// set values
				edit.CopyData (currentData);
			}
			if (GUILayout.Button ("Reset", GUILayout.Width (60))) {
				edit.SetData (currentData);
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.FlexibleSpace ();
			GUILayout.Space (8);
			if (parent.texture != null) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("set from image", GUILayout.Width (100));
				if (GUILayout.Button ("set " + parameters [activeParameter])) {
					int maxValue = currentData.GetMax ();
					for (int y = 0; y < scene.height; y++) {
						for (int x = 0; x < scene.width; x++) {
							int v = (int)(maxValue * parent.GetFromImage (x, y));
							currentData.Set (x, y, v);
						}
					}
					edit.SetData (currentData);
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
			}
			parent.RenderLoadTexture ();
			return false;
		}

		delegate int ProcessParamFnXY (int x,int y,int val,int changeVal);

		void ApplyParamFnXY (ProcessParamFnXY fn, Data param)
		{
			BitMap8 data = new BitMap8 (scene);
			edit.CopyData (data);
			byte[] changeVals = data.data;
			
			int minX = scene.width;
			int maxX = 0;
			int minY = scene.height;
			int maxY = 0;
			
			int p = 0;
			int minVal = param.GetMin ();
			int maxVal = param.GetMax ();
			
			for (int y = 0; y < scene.height; y++) {
				for (int x = 0; x < scene.height; x++) {
					int changeVal = changeVals [p];
					int val = param.Get (x, y);
					int newVal = fn (x, y, val, changeVal);
					if (newVal != val) {
						param.Set (x, y, Mathf.Clamp (newVal, minVal, maxVal));
						minX = Mathf.Min (x, minX);
						minY = Mathf.Min (y, minY);
						maxX = Mathf.Max (x, maxX);
						maxY = Mathf.Max (y, maxY);
					}
					p++;
				}
			}
			if (minX < maxX) {
				edit.SetData (data);
			}
		}
		
		public void Disable ()
		{
			edit.Delete ();
			edit = null;
		}

		public void Update ()
		{
		}
	}
}
