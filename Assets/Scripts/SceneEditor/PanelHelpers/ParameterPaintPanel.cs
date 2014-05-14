using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor.Helpers
{
	public class ParameterPaintPanel : PanelHelper
	{
		protected readonly MapsPanel parent;

		protected Scene scene;
		protected EditorCtrl ctrl;
		
		protected int brushWidth;
		protected enum EBrushMode
		{
			Area,
			Circle
		};
		protected EBrushMode brushMode;
		protected GridTextureSettings gridSettings255;
		
		protected EditData edit;
		protected Data data;
		protected Data backupCopy;
		protected Data clipboardCopy;
		
		protected GUIStyle tabNormal;
		protected GUIStyle tabSelected;
		
		protected int maxParamValue = 255;
		protected int paramStrength = 255;
		protected string paramStrengthStr = "";
		
		public ParameterPaintPanel (EditorCtrl ctrl, MapsPanel parent, Scene scene)
		{
			this.parent = parent;
			this.ctrl = ctrl;
			
			this.scene = parent.scene;
			tabNormal = ctrl.listItem;
			tabSelected = ctrl.listItemSelected;
		}

		protected virtual void SetupBackupCopy()
		{
			backupCopy = new BitMap8 (scene);
		}
		
		protected virtual void Setup (string editDataParamName)
		{
			paramStrength = maxParamValue;
			gridSettings255 = new GridTextureSettings (false, 0, 16, "MapGrid255", true, "ActiveMapGrid255");
			
			edit = EditData.CreateEditData (editDataParamName, data, delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
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

			SetupBackupCopy ();
		}
		
		public virtual bool Render (int mx, int my)
		{
			return false;
		}

		protected void RenderSaveRestoreApply ()
		{
			GUILayout.BeginHorizontal (); // Save, restore etc
			{
				if (backupCopy != null)
				{
					if (GUILayout.Button ("Save to clipboard")) { //, GUILayout.Width (100))) {
						edit.CopyData (backupCopy);
					}
					if (GUILayout.Button ("Restore from clipb.")) { //, GUILayout.Width (100))) {
						backupCopy.CopyTo (data);
						edit.SetData (data);
					}
				}
				if (GUILayout.Button ("Apply")) { //, GUILayout.Width (60))) {
					edit.CopyData (data);
				}
				if (GUILayout.Button ("Reset")) { //, GUILayout.Width (60))) {
					edit.SetData (data);
				}
			}
			GUILayout.EndHorizontal (); //~Save, restore etc
		}

		protected void RenderBrushMode ()
		{
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				GUILayout.Label ("Paint Brush");
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
					if (maxParamValue > 1)
					{
						GUILayout.Label ("Brush value", GUILayout.Width (100));
						
						if (paramStrength > maxParamValue)
							paramStrength = maxParamValue;
						
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
			}
			GUILayout.EndVertical ();
		}

		protected void RenderFromImage (string paramName)
		{
			if (parent.texture != null) 
			{
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("Set from image", GUILayout.Width (100));
					if (GUILayout.Button ("Set " + paramName)) 
					{
						int maxValue = maxParamValue;
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
					//.GUILayout.FlexibleSpace ();
				}
				GUILayout.EndHorizontal ();
			}
			parent.RenderLoadTexture ();
		}
		
		public virtual void Disable ()
		{
			if (edit != null)
				edit.Delete ();
			edit = null;
		}
		
		public virtual void Update ()
		{
			
		}
	}
}

