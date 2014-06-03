using UnityEngine;
using System.Collections;
using System.Text;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;

namespace Ecosim.SceneEditor
{
	public class TerrainInfoPanel
	{
		public readonly EditorCtrl ctrl;
		private string displayHeaderStr = "";
		private string displayMainStr = "";
		
		public TerrainInfoPanel (EditorCtrl ctrl)
		{
			this.ctrl = ctrl;
		}
		
		Vector2 scroll;
		
		public bool Render (int mx, int my)
		{
			GUILayout.Label (displayHeaderStr);

			scroll = GUILayout.BeginScrollView (scroll);			
			GUILayout.Label (displayMainStr);
			GUILayout.EndScrollView ();
			return true;
		}
		
		public void Update ()
		{
			if (ctrl.scene == null) {
				displayHeaderStr = "Scene not loaded";
				displayMainStr = "";
				return;
			}
			Scene scene = ctrl.scene;
			Vector3 mousePos = Input.mousePosition;
			StringBuilder displayHdrText = new StringBuilder (2000);
			StringBuilder displayMainText = new StringBuilder (2000);
			Progression p = scene.progression;
			float realX = -1;
			float realY = -1;
			float mouseHeight = -1f;
			if (CameraControl.IsNear) {
				Vector3 hitPoint;
				if (TerrainMgr.TryScreenToTerrainCoord (mousePos, out hitPoint)) {
					realX = hitPoint.x;
					realY = hitPoint.z;
					mouseHeight = hitPoint.y;
				}
			} else {
				Ray ray = Camera.main.ScreenPointToRay (mousePos);
				realX = ray.origin.x;
				realY = ray.origin.z;
			}
			
			int tileX = (int)(realX / TerrainMgr.TERRAIN_SCALE);
			int tileY = (int)(realY / TerrainMgr.TERRAIN_SCALE);
			
			if ((realX >= 0) && (tileX >= 0) && (tileY >= 0) && (tileX < scene.width) && (tileY < scene.height)) {
				TileType tile = p.vegetation.GetTileType (tileX, tileY);
				displayHdrText.AppendLine ("[" + tileX + ", " + tileY + "]");
				displayHdrText.AppendLine (tile.vegetationType.successionType.name);
				displayHdrText.AppendLine (tile.vegetationType.name);
				displayHdrText.AppendLine ("Tile nr " + tile.index);
				HeightMap height = p.heightMap;
				HeightMap waterHeight = p.waterHeightMap;
				displayHdrText.AppendLine ("Height: " + height.GetHeight (tileX, tileY).ToString ("0.00") +
					"m " + height.GetInterpolatedHeight (realX, realY).ToString ("0.00") + "m");
				displayHdrText.Append ("Water Height: " + waterHeight.GetHeight (tileX, tileY).ToString ("0.00") + "m");
				HeightMap cWaterHeight = p.calculatedWaterHeightMap;
				if (cWaterHeight != null) {
					displayHdrText.AppendLine (" (" + cWaterHeight.GetHeight (tileX, tileY).ToString ("0.00") + "m)");
				}
				if (mouseHeight >= 0) {
					displayHdrText.AppendLine ("Point Height: " + mouseHeight.ToString ("0.00") + "m");
				}
					
				foreach (string paramName in scene.progression.GetAllDataNames()) {
					Data data = p.GetData (paramName);
					int val = data.Get (tileX, tileY);
					if ((val > 0) && (data.GetMax () < 256)) {
						displayMainText.AppendLine (paramName + " \t" + data.Get (tileX, tileY).ToString () +
							" (" + scene.progression.ConvertToString (paramName, val) + ")");
					}
				}
			}
			displayHeaderStr = displayHdrText.ToString ();
			displayMainStr = displayMainText.ToString ();
		}
	}
}
