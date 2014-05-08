using System.Collections.Generic;
using UnityEngine;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;

namespace Ecosim.SceneEditor
{
	public class TileExtraPanel : ExtraPanel
	{
		EditorCtrl ctrl;
		GUISkin skin;
		TileType tile;
		Vector2 scrollPos;
		string[] treeNames;
		string[] objNames;
		string[] decalNames;
		string[] detailNames;
		int totalDetails = 0;
		Dictionary<int, string> detailCountStrings;
		bool isChanged = true;
		float lastUpdated = 0f;
		private static TileType copyBufferTile;
		VegetationPanel panel;
		Texture2D image;
		RenderTileIcons.RenderSettings renderSettings = new RenderTileIcons.RenderSettings (10f, 30f, 25f, 2f);
		
		public TileExtraPanel (EditorCtrl ctrl, TileType tile, VegetationPanel vegetationPanel)
		{
			this.ctrl = ctrl;
			this.tile = tile;
			this.panel = vegetationPanel;
			skin = ctrl.skin;
			
			treeNames = EcoTerrainElements.GetTreeNames ();
			objNames = EcoTerrainElements.GetObjectNames ();
			decalNames = EcoTerrainElements.GetDecalNames ();
			detailNames = EcoTerrainElements.GetDetailNames ();
			detailCountStrings = new Dictionary<int, string> ();
			for (int i = 0; i < tile.detailCounts.Length; i++) {
				int count = tile.detailCounts [i];
				if (count > 0) {
					totalDetails++;
					detailCountStrings.Add (i, count.ToString ());
				}
			}
		}
		
		bool HandleTree (int mx, int my, int index, TileType.TreeData tree)
		{
			GUILayout.BeginHorizontal (skin.box); // tree
			if (GUILayout.Button ("-", GUILayout.Width (20))) {
				isChanged = true;
				return true;
			}
			GUILayout.BeginVertical (); // 2 rows for trees
			GUILayout.BeginHorizontal (); // row 1
			
			if (GUILayout.Button (treeNames [tree.prototypeIndex], GUILayout.Width (120))) {
				ctrl.StartSelection (treeNames, tree.prototypeIndex,
					newIndex => {
					tree.prototypeIndex = newIndex;
					isChanged = true;
				});
			}
			
			GUILayout.Label ("X");
			float newX = GUILayout.HorizontalSlider (tree.x, 0f, 1f, GUILayout.Width (50));
			GUILayout.Label ("Y");
			float newY = GUILayout.HorizontalSlider (tree.y, 0f, 1f, GUILayout.Width (50));
			GUILayout.Label ("Var R");
			float newR = GUILayout.HorizontalSlider (tree.r, 0f, 0.5f, GUILayout.Width (25));
			// GUILayout.Space (8);
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal (); // ~row 1
			GUILayout.BeginHorizontal (); // row 2
			float oldHeight = .5f * (tree.minHeight + tree.maxHeight);
			float oldHeightVar = tree.maxHeight - tree.minHeight;
			float oldWidth = .5f * (tree.maxWidthVariance + tree.minWidthVariance);
			float oldWidthVar = tree.maxWidthVariance - tree.minWidthVariance;
			
			GUILayout.Label ("H");
			float newHeight = GUILayout.HorizontalSlider (oldHeight, 0.5f, 2f, GUILayout.Width (40));
			GUILayout.Label ("Var");
			float newHeightVar = GUILayout.HorizontalSlider (oldHeightVar, 0f, 1f, GUILayout.Width (25));
			GUILayout.Space (8);
			GUILayout.Label ("W");
			float newWidth = GUILayout.HorizontalSlider (oldWidth, 0.25f, 2f, GUILayout.Width (40));
			GUILayout.Label ("Var");
			float newWidthVar = GUILayout.HorizontalSlider (oldWidthVar, 0f, 1f, GUILayout.Width (25));
			GUILayout.Space (8);
			GUILayout.Label ("Col");
			float oldColour = tree.colorTo.r;
			float newColour = GUILayout.HorizontalSlider (oldColour, 0.25f, 1f, GUILayout.Width (30));
			if (GUILayout.Button ("<size=8>Reset</size>"))
				newColour = 0.75f;
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal (); // ~row 2
			GUILayout.EndVertical (); // ~2 rows for trees
			GUILayout.EndHorizontal (); // ~tree
		
			if ((newX != tree.x) || (newY != tree.y) || (newR != tree.r)) {
				tree.x = newX;
				tree.y = newY;
				tree.r = newR;
				isChanged = true;
			}
			if ((newHeight != oldHeight) || (newHeightVar != oldHeightVar)) {
				tree.minHeight = Mathf.Max (0f, newHeight - 0.5f * newHeightVar);
				tree.maxHeight = newHeight + 0.5f * newHeightVar;
				isChanged = true;
			}
			if ((newWidth != oldWidth) || (newWidthVar != oldWidthVar)) {
				tree.minWidthVariance = Mathf.Max (0f, newWidth - 0.5f * newWidthVar);
				tree.maxWidthVariance = newWidth + 0.5f * newWidthVar;
				isChanged = true;
			}
			if (newColour != oldColour) {
				Color toColour = new Color (newColour, newColour, newColour, 1f);
				tree.colorTo = toColour;
				tree.colorFrom = 0.75f * toColour;
				isChanged = true;
			}
			return false;
		}

		bool HandleObject (int mx, int my, int index, TileType.ObjectData obj)
		{
			GUILayout.BeginHorizontal (skin.box); // obj
			if (GUILayout.Button ("-", GUILayout.Width (20))) {
				isChanged = true;
				return true;
			}
			GUILayout.BeginVertical (); // 2 rows for objects
			GUILayout.BeginHorizontal (); // row 1
			
			if (GUILayout.Button (objNames [obj.index], GUILayout.Width (120))) {
				ctrl.StartSelection (objNames, obj.index,
					newIndex => {
					obj.index = newIndex;
					isChanged = true;
				});
			}
			
			GUILayout.Label ("X");
			float newX = GUILayout.HorizontalSlider (obj.x, 0f, 1f, GUILayout.Width (50));
			GUILayout.Label ("Y");
			float newY = GUILayout.HorizontalSlider (obj.y, 0f, 1f, GUILayout.Width (50));
			GUILayout.Label ("Var R");
			float newR = GUILayout.HorizontalSlider (obj.r, 0f, 0.5f, GUILayout.Width (25));
			// GUILayout.Space (8);
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal (); // ~row 1
			GUILayout.BeginHorizontal (); // row 2
			float oldHeight = .5f * (obj.minHeight + obj.maxHeight);
			float oldHeightVar = obj.maxHeight - obj.minHeight;
			float oldWidth = .5f * (obj.maxWidthVariance + obj.minWidthVariance);
			float oldWidthVar = obj.maxWidthVariance - obj.minWidthVariance;
			
			GUILayout.Label ("H");
			float newHeight = GUILayout.HorizontalSlider (oldHeight, 0.5f, 2f, GUILayout.Width (50));
			GUILayout.Label ("Var");
			float newHeightVar = GUILayout.HorizontalSlider (oldHeightVar, 0f, 1f, GUILayout.Width (25));
			GUILayout.Space (8);
			GUILayout.Label ("W");
			float newWidth = GUILayout.HorizontalSlider (oldWidth, 0.25f, 2f, GUILayout.Width (50));
			GUILayout.Label ("Var");
			float newWidthVar = GUILayout.HorizontalSlider (oldWidthVar, 0f, 1f, GUILayout.Width (25));
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal (); // ~row 2
			GUILayout.EndVertical (); // ~2 rows for objects
			GUILayout.EndHorizontal (); // ~obj
		
			if ((newX != obj.x) || (newY != obj.y) || (newR != obj.r)) {
				obj.x = newX;
				obj.y = newY;
				obj.r = newR;
				isChanged = true;
			}
			if ((newHeight != oldHeight) || (newHeightVar != oldHeightVar)) {
				obj.minHeight = Mathf.Max (0f, newHeight - 0.5f * newHeightVar);
				obj.maxHeight = newHeight + 0.5f * newHeightVar;
				isChanged = true;
			}
			if ((newWidth != oldWidth) || (newWidthVar != oldWidthVar)) {
				obj.minWidthVariance = Mathf.Max (0f, newWidth - 0.5f * newWidthVar);
				obj.maxWidthVariance = newWidth + 0.5f * newWidthVar;
				isChanged = true;
			}
			return false;
		}

		bool HandleDecal (int mx, int my, int index, int decalId)
		{
			GUILayout.BeginHorizontal (skin.box); // obj
			if (GUILayout.Button ("-", GUILayout.Width (20))) {
				return true;
			}
			
			if (GUILayout.Button (decalNames [decalId], GUILayout.Width (120))) {
				List<string> names = new List<string> (decalNames);
				names.RemoveAll (x => {
					foreach (int i in tile.decals) {
						if ((i != decalId) && (EcoTerrainElements.GetDecalNameForIndex (i) == x))
							return true;
					}
					return false;
				});
				ctrl.StartSelection (names.ToArray (), names.IndexOf (EcoTerrainElements.GetDecalNameForIndex (decalId)),
					newIndex => {
					tile.decals [index] = EcoTerrainElements.GetIndexOfDecal (names [newIndex]);
					isChanged = true;
				});
			}
			
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal (); // ~obj	
			return false;
		}

		bool HandleDetail (int mx, int my, int detailIndex)
		{
			GUILayout.BeginHorizontal (skin.box); // obj
			if (GUILayout.Button ("-", GUILayout.Width (20))) {
				return true;
			}
			
			if (GUILayout.Button (detailNames [detailIndex], GUILayout.Width (120))) {
				List<string> names = new List<string> (detailNames);
				for (int i = tile.detailCounts.Length - 1; i >= 0; i--) {
					if ((i != detailIndex) && (tile.detailCounts [i] > 0)) {
						names.RemoveAt (i);
					}
				}
				ctrl.StartSelection (names.ToArray (), names.IndexOf (EcoTerrainElements.GetDetailNameForIndex (detailIndex)),
					newIndex => {
					int newDetailIndex = EcoTerrainElements.GetIndexOfDetailPrototype (names [newIndex]);
					if (newDetailIndex != detailIndex) {
						if (newDetailIndex >= tile.detailCounts.Length) {
							int[] newCounts = new int[newDetailIndex + 1];
							System.Array.Copy (tile.detailCounts, newCounts, tile.detailCounts.Length);
							tile.detailCounts = newCounts;
						}
						tile.detailCounts [newDetailIndex] = tile.detailCounts [detailIndex];
						detailCountStrings.Add (newDetailIndex, tile.detailCounts [detailIndex].ToString ());
						detailCountStrings.Remove (detailIndex);
						tile.detailCounts [detailIndex] = 0;
						isChanged = true;
					}
				});
			}
			string newString = GUILayout.TextField (detailCountStrings [detailIndex], GUILayout.Width (30));
			int result;
			if (int.TryParse (newString, out result) && (result > 0) && (result < 100)) {
				if (result != tile.detailCounts [detailIndex]) {
					tile.detailCounts [detailIndex] = result;
					isChanged = true;
				}
			} else {
				GUILayout.Label (ctrl.warning); 
			}
			detailCountStrings [detailIndex] = newString;
			
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal (); // ~obj	
			return false;
		}
		
		public bool Render (int mx, int my)
		{
			if (tile == null)
				return false;
			bool keepOpen = true;
			GUILayout.BeginHorizontal (); // title
			if (GUILayout.Button (ctrl.foldedOpen, ctrl.icon)) {
				keepOpen = false;
			}
			GUILayout.Label ("Tile", GUILayout.Width (100));
			GUILayout.Label (tile.vegetationType.successionType.name + "\n" + tile.vegetationType.name + " tegel " + tile.index);
			GUILayout.EndHorizontal (); // ~title
			
			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			
			GUILayout.BeginHorizontal (); // ondergrond
			GUILayout.Label ("Ground color", GUILayout.Width (65));
			float newS0 = GUILayout.HorizontalSlider (tile.splat0, 0f, 1f, GUILayout.Width (45));
			float newS1 = GUILayout.HorizontalSlider (tile.splat1, 0f, 1f, GUILayout.Width (45));
			float newS2 = GUILayout.HorizontalSlider (tile.splat2, 0f, 1f, GUILayout.Width (45));
			float oldS3 = 1f - tile.splat0 - tile.splat1 - tile.splat2;
			float newS3 = GUILayout.HorizontalSlider (oldS3, 0f, 1f, GUILayout.Width (45));
			if ((tile.splat0 != newS0) || (tile.splat1 != newS1) || (tile.splat2 != newS2) || (oldS3 != newS3)) {
				float total = newS0 + newS1 + newS2 + newS3;
				float factor = 1f;
				if (total > 1f)
					factor = 1f / total;
				tile.splat0 = newS0 * factor;
				tile.splat1 = newS1 * factor;
				tile.splat2 = newS2 * factor;
				isChanged = true;
			}
			if (tile.vegetationType.index > 0) {
				if (GUILayout.Button ("Delete", GUILayout.Width (35))) {
					ctrl.StartDialog ("Delete tile?", result => {
						panel.DeleteTile (tile);
						tile = null;
					}, null);
				}
			}
			if (GUILayout.Button ("Copy", GUILayout.Width (35))) {
				if (copyBufferTile == null) {
					copyBufferTile = new TileType ();
				}
				tile.CopyTo (copyBufferTile);
			}
			if ((copyBufferTile != null) && GUILayout.Button ("Paste", GUILayout.Width (35))) {
				copyBufferTile.CopyTo (tile, true);
				detailCountStrings.Clear ();
				for (int i = 0; i < tile.detailCounts.Length; i++) {
					int count = tile.detailCounts [i];
					if (count > 0) {
						totalDetails++;
						detailCountStrings.Add (i, count.ToString ());
					}
				}
				isChanged = true;
			}
			GUILayout.EndHorizontal (); // ~ondergrond

			// Trees
			GUILayout.BeginVertical (skin.box);
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("+", GUILayout.Width (20))) {
				List<TileType.TreeData> trees = new List<TileType.TreeData> (tile.trees);
				TileType.TreeData tree = new TileType.TreeData ();
				trees.Add (tree);
				tile.trees = trees.ToArray ();
				isChanged = true;
			}
			GUILayout.Label ("Trees");
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			int index = 0;
			TileType.TreeData removeTree = null;
			foreach (TileType.TreeData tree in tile.trees) {
				if (HandleTree (mx, my, index++, tree)) {
					removeTree = tree;
				}
			}
			if (removeTree != null) {
				List<TileType.TreeData> trees = new List<TileType.TreeData> (tile.trees);
				trees.Remove (removeTree);
				tile.trees = trees.ToArray ();
				isChanged = true;
			}
		
			GUILayout.EndVertical ();
			// ~Trees
			
			// Objects
			GUILayout.BeginVertical (skin.box);
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("+", GUILayout.Width (20))) {
				List<TileType.ObjectData> objects = new List<TileType.ObjectData> (tile.objects);
				TileType.ObjectData obj = new TileType.ObjectData ();
				objects.Add (obj);
				tile.objects = objects.ToArray ();
				isChanged = true;
			}
			GUILayout.Label ("Objects");
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			index = 0;
			TileType.ObjectData removeObject = null;
			foreach (TileType.ObjectData obj in tile.objects) {
				if (HandleObject (mx, my, index++, obj)) {
					removeObject = obj;
				}
			}
			if (removeObject != null) {
				List<TileType.ObjectData> objects = new List<TileType.ObjectData> (tile.objects);
				objects.Remove (removeObject);
				tile.objects = objects.ToArray ();
				isChanged = true;
			}
		
			GUILayout.EndVertical ();
			// ~Objects

			// Decals
			GUILayout.BeginVertical (skin.box);
			GUILayout.BeginHorizontal ();
			if (tile.decals.Length < decalNames.Length) {
				if (GUILayout.Button ("+", GUILayout.Width (20))) {
					List<string> names = new List<string> (decalNames);
					names.RemoveAll (x => {
						foreach (int i in tile.decals) {
							if (EcoTerrainElements.GetDecalNameForIndex (i) == x)
								return true;
						}
						return false;
					});
					ctrl.StartSelection (names.ToArray (), -1,
					newIndex => {
						List<int> decals = new List<int> (tile.decals);
						decals.Add (EcoTerrainElements.GetIndexOfDecal (names [newIndex]));
						tile.decals = decals.ToArray ();
						isChanged = true;
					});
				}
			}
			GUILayout.Label ("Decals");
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			index = 0;
			int removeDecal = -1;
			foreach (int decalId in tile.decals) {
				if (HandleDecal (mx, my, index++, decalId)) {
					removeDecal = decalId;
				}
			}
			if (removeDecal >= 0) {
				List<int> decals = new List<int> (tile.decals);
				decals.Remove (removeDecal);
				tile.decals = decals.ToArray ();
				isChanged = true;
			}
		
			GUILayout.EndVertical ();
			// ~Decals
			
			// Detail Objects
			GUILayout.BeginVertical (skin.box);
			GUILayout.BeginHorizontal ();
			if (totalDetails < detailNames.Length) {
				if (GUILayout.Button ("+", GUILayout.Width (20))) {
					List<string> names = new List<string> (detailNames);
					for (int i = tile.detailCounts.Length - 1; i >= 0; i--) {
						if (tile.detailCounts [i] > 0)
							names.RemoveAt (i);
					}
					ctrl.StartSelection (names.ToArray (), -1,
					newIndex => {
						int ix = EcoTerrainElements.GetIndexOfDetailPrototype (names [newIndex]);
						if (ix >= tile.detailCounts.Length) {
							int[] newCounts = new int[ix + 1];
							System.Array.Copy (tile.detailCounts, newCounts, tile.detailCounts.Length);
							tile.detailCounts = newCounts;
						}
						tile.detailCounts [ix] = 1;
						detailCountStrings.Add (ix, "1");
						isChanged = true;
					});
				}
			}
			GUILayout.Label ("Detail");
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			int deleteDetail = -1;
			int resultingDetailCount = 0;
			for (int i = 0; i < tile.detailCounts.Length; i++) {
				if (tile.detailCounts [i] > 0) {
					if (HandleDetail (mx, my, i)) {
						deleteDetail = i;
					} else {
						resultingDetailCount = i + 1;
					}
				}
			}
			if (deleteDetail >= 0) {
				tile.detailCounts [deleteDetail] = 0;
				detailCountStrings.Remove (deleteDetail);
				if (resultingDetailCount < tile.detailCounts.Length) {
					int[] newDetail = new int[resultingDetailCount];
					System.Array.Copy (tile.detailCounts, newDetail, resultingDetailCount);
					tile.detailCounts = newDetail;
				}
			}
		
			GUILayout.EndVertical ();
			// ~Detail Objects
			
			
			GUILayout.EndScrollView ();
			return keepOpen;
		}
		
		private long renderId = 0L;

		public bool RenderSide (int mx, int my)
		{
			if (isChanged && lastUpdated < Time.timeSinceLevelLoad - 1.0f) {
				isChanged = false;
				lastUpdated = Time.timeSinceLevelLoad;
				
				if (image == null) {
					image = new Texture2D (384, 256, TextureFormat.RGB24, false, false);
				}
				renderId = RenderTileIcons.self.Render (renderSettings, ref image, tile);
				RenderTileIcons.RenderTile (tile); // rerender icon
			}
			GUILayout.BeginVertical ();
			GUILayout.Label (image, GUILayout.Width (384), GUILayout.Height (256));
			GUILayout.BeginHorizontal ();
			float dist = renderSettings.distance;
			float newDist = GUILayout.HorizontalSlider (dist, 5f, 50f, GUILayout.Width (60));
			float angleH = renderSettings.angleH;
			float newAngleH = GUILayout.HorizontalSlider (angleH, 0f, 360f, GUILayout.Width (60));
			float angleV = renderSettings.angleV;
			float newAngleV = GUILayout.HorizontalSlider (angleV, 5f, 90f, GUILayout.Width (60));
			if ((newDist != dist) || (angleH != newAngleH) || (angleV != newAngleV)) {
				renderSettings.distance = newDist;
				renderSettings.angleH = newAngleH;
				renderSettings.angleV = newAngleV;
				renderId = RenderTileIcons.self.ReRender (renderSettings, renderId, ref image, tile);
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.FlexibleSpace ();
			GUILayout.EndVertical ();
			return true;
		}
		
	}
}
