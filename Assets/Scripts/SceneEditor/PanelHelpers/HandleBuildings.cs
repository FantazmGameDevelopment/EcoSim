using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandleBuildings : PanelHelper
	{
		protected Scene scene;
		protected EditorCtrl ctrl;
		protected readonly MapsPanel parent;

		protected GUIStyle tabNormal;
		protected GUIStyle tabSelected;

		private PanelHelper helper;
		private bool isDragging;
		protected string[] buildingCategories;
		protected EcoTerrainElements.BuildingPrototype[] buildingsInCategory;
		private int activeCategory = 0;
		private int activeBuildingInC = 0;

		protected float angleX = 0f;
		protected float angleY = 0f;
		protected float angleZ = 0f;

		protected HandleParameters handleParams = null;
		protected Buildings.Building selectedBuilding = null;

		protected Texture2D renderTex;
		
		public HandleBuildings (EditorCtrl ctrl, MapsPanel parent, Scene scene)
		{
			this.ctrl = ctrl;
			this.parent = parent;
			
			this.scene = parent.scene;
			tabNormal = ctrl.listItem;
			tabSelected = ctrl.listItemSelected;
			renderTex = new Texture2D (375, 300, TextureFormat.RGB24, false);
			
			Setup ();
		}
		
		void SetupBuildingsInCategory ()
		{
			EcoTerrainElements.EBuildingCategories cat = StringToEBuildingCat (buildingCategories [activeCategory]);
			List<EcoTerrainElements.BuildingPrototype> list = new List<EcoTerrainElements.BuildingPrototype> ();
			foreach (EcoTerrainElements.BuildingPrototype bp in EcoTerrainElements.self.buildings) {
				if (bp.category == cat) {
					list.Add (bp);
				}
			}
			buildingsInCategory = list.ToArray ();
		}
		
		EcoTerrainElements.EBuildingCategories StringToEBuildingCat (string str)
		{
			foreach (EcoTerrainElements.EBuildingCategories cat in System.Enum.GetValues(typeof(EcoTerrainElements.EBuildingCategories))) {
				if (cat.ToString () == str) {
					return cat;
				}
			}
			return EcoTerrainElements.EBuildingCategories.EXTRA;
		}
		
		protected virtual void Setup ()
		{
			EditBuildings.self.StartEditBuildings (scene);
			TerrainMgr.self.ForceRedraw ();
			List<string> buildingCatList = new List<string> ();
			foreach (EcoTerrainElements.EBuildingCategories cat in System.Enum.GetValues(typeof(EcoTerrainElements.EBuildingCategories))) {
				buildingCatList.Add (cat.ToString ());
			}
			buildingCategories = buildingCatList.ToArray ();
			SetupBuildingsInCategory ();
			RenderPreviewTexture ();
		}

		/*void RenderPreviewTexture () {
			(GameObject.FindObjectOfType <MonoBehaviour> () as MonoBehaviour).StartCoroutine (CORenderPreviewTexture());
		}*/

		//IEnumerator CORenderPreviewTexture () {
		protected virtual void RenderPreviewTexture () {
			RenderTileIcons.RenderSettings rs = new RenderTileIcons.RenderSettings (60f, 30f, 120f, 24f);
			EcoTerrainElements.BuildingPrototype bp = buildingsInCategory[activeBuildingInC];
			//yield return new WaitForEndOfFrame ();
			RenderTileIcons.self.Render (rs, ref renderTex, scene.successionTypes[0].vegetations[0].tiles[0], bp.prefabContainer.mesh, bp.prefabContainer.material);
		}
		
		void ResetEdit ()
		{
			if (handleParams != null) {
				handleParams.Disable ();
				handleParams = null;
			}
			EditBuildings.self.StopEditBuildings (scene);
			TerrainMgr.self.ForceRedraw ();
		}
				
		public virtual bool Render (int mx, int my)
		{
			RenderCategory ();
			RenderBuilding ();
			RenderRenderedTexture ();
			RenderTransformControls ();
			RenderActiveState ();
			RenderBuildingParameterMap (mx, my);
			return false;
		}

		protected virtual void RenderCategory ()
		{
			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label ("Category", GUILayout.Width (100));
				if (GUILayout.Button (buildingCategories [activeCategory], GUILayout.Width (200)))
				{
					ctrl.StartSelection (buildingCategories, activeCategory, newIndex => {
						if (newIndex != activeCategory) {
							activeCategory = newIndex;
							SetupBuildingsInCategory ();
							activeBuildingInC = (buildingsInCategory.Length > 0) ? 0 : -1;
							RenderPreviewTexture ();
						}
					});
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
		}

		protected virtual void RenderBuilding ()
		{
			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label ("Building", GUILayout.Width (100));
				if (activeBuildingInC >= 0) 
				{
					if (GUILayout.Button (buildingsInCategory [activeBuildingInC].ToString (), GUILayout.Width (200))) {
						ctrl.StartSelection (buildingsInCategory, activeBuildingInC, newIndex => {
							if (newIndex != activeBuildingInC) {
								activeBuildingInC = newIndex;
								RenderPreviewTexture ();
							}
						});
					}
					if (selectedBuilding != null) {
						if (GUILayout.Button ("Update")) {
							selectedBuilding.name = buildingsInCategory [activeBuildingInC].name;
							selectedBuilding.prefab = buildingsInCategory [activeBuildingInC].prefabContainer;
							EditBuildings.self.BuildingChanged (selectedBuilding);
						}
					}
				} else {
					GUILayout.Label ("Category is empty.");
				}
				GUILayout.FlexibleSpace ();
			}
			GUILayout.EndHorizontal ();
			GUILayout.Label ("Press N to place a new building. Click on a building to edit it.");
		}

		protected virtual void RenderRenderedTexture ()
		{
			GUILayout.BeginHorizontal (ctrl.skin.box);
			{
				GUILayout.Label (renderTex, GUIStyle.none);
			}
			GUILayout.EndVertical ();
		}

		protected virtual void RenderTransformControls ()
		{
			if (selectedBuilding != null) 
			{
				bool hasChanged = false;
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("Rotation:", GUILayout.Width (60));

					GUILayout.Label ("X", GUILayout.Width (10));
					float newX = GUILayout.HorizontalSlider (angleX, -45f, 45f, GUILayout.Width (45));
					EcoGUI.FloatField ("", ref newX, 0, null, GUILayout.Width (30));  
					newX = Mathf.Clamp (newX, -45f, 45f);

					GUILayout.Label ("Y", GUILayout.Width (10));
					float newY = GUILayout.HorizontalSlider (angleY, 0f, 360f, GUILayout.Width (45));
					EcoGUI.FloatField ("", ref newY, 0, null, GUILayout.Width (30));  
					newY = Mathf.Clamp (newY, 0f, 360f);

					GUILayout.Label ("Z", GUILayout.Width (10));
					float newZ = GUILayout.HorizontalSlider (angleZ, -45, 45f, GUILayout.Width (45));
					EcoGUI.FloatField ("", ref newZ, 0, null, GUILayout.Width (30));  
					newZ = Mathf.Clamp (newZ, -45f, 45f);

					if (newX != angleX) {
						angleX = Mathf.Round (newX);
						hasChanged = true;
					}

					if (newY != angleY) {
						angleY = Mathf.Round (newY / 5) * 5;
						hasChanged = true;
					}

					if (newZ != angleZ) {
						angleZ = Mathf.Round (newZ);
						hasChanged = true;
					}
					GUILayout.FlexibleSpace ();
				}
				GUILayout.EndHorizontal ();

				float level;
				if (TerrainMgr.TryGetTerrainHeight (selectedBuilding.position, out level)) 
				{
					GUILayout.BeginHorizontal ();
					{
						GUILayout.Label ("Vertical pos:", GUILayout.Width (60));
						float pos = selectedBuilding.position.y;
						float newPos = GUILayout.HorizontalSlider (pos, level - 10f, level + 20f, GUILayout.Width (270));

						if (pos != newPos) {
							selectedBuilding.position.y = newPos;
							hasChanged = true;
						}
						GUILayout.FlexibleSpace ();
					}
					GUILayout.EndHorizontal ();
				}

				if (hasChanged) {
					selectedBuilding.rotation = Quaternion.Euler (angleX, angleY, angleZ);
					EditBuildings.self.BuildingChanged (selectedBuilding);
				}
			}
		}

		protected virtual void RenderActiveState ()
		{
			if (selectedBuilding != null)
			{
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("Building ID:" + selectedBuilding.id);
					if (GUILayout.Button ("Disabled", (selectedBuilding.isActive) ? tabNormal : tabSelected, GUILayout.Width (100))) {
						selectedBuilding.isActive = false;
						selectedBuilding.startsActive = false;
					}
					if (GUILayout.Button ("Enabled", (selectedBuilding.isActive) ? tabSelected : tabNormal, GUILayout.Width (100))) {
						selectedBuilding.isActive = true;
						selectedBuilding.startsActive = true;
					}
					GUILayout.FlexibleSpace ();
				}
				GUILayout.EndHorizontal ();
			}
		}

		protected virtual void RenderBuildingParameterMap (int mx, int my)
		{
			if (selectedBuilding != null)
			{
				Data buildingMap = null;
				string mapName = Buildings.GetMapNameForBuildingId(selectedBuilding.id);
				if (scene.progression.HasData (mapName)) {
					buildingMap = scene.progression.GetData (mapName);
				}

				if (buildingMap != null) 
				{
					GUILayout.BeginHorizontal ();
					{
						GUILayout.Label ("Parameter map " + mapName, GUILayout.Width (100));
						if (handleParams != null) {
							if (GUILayout.Button ("Stop Edit")) {
								handleParams.Disable ();
								handleParams = null;
							}
						} else {
							if (GUILayout.Button ("Start Edit")) {
								handleParams = new HandleParameters (ctrl, parent, scene, buildingMap);
							}
						}
						if (GUILayout.Button ("Delete")) {
							if (handleParams != null) {
								handleParams.Disable ();
								handleParams = null;
							}
							scene.progression.DeleteData (mapName);
						}
						GUILayout.FlexibleSpace ();
					}
					GUILayout.EndHorizontal ();
				} 
				else 
				{
					GUILayout.BeginHorizontal ();
					{
						GUILayout.Label ("Parameter map " + mapName, GUILayout.Width (100));
						if (GUILayout.Button ("Create as 1 bit (0..1)")) {
							scene.progression.AddData (mapName, new SparseBitMap1 (scene));
						}
						if (GUILayout.Button ("Create as 8 bit (0..255)")) {
							scene.progression.AddData (mapName, new SparseBitMap8 (scene));
						}
						GUILayout.FlexibleSpace ();
					}
					GUILayout.EndHorizontal ();
				}

				if (handleParams != null) {
					GUILayout.BeginVertical (ctrl.skin.box);
					{
						handleParams.Render (mx, my);
						GUILayout.Space (3);
					}
					GUILayout.EndVertical ();
				}
			}
		}
		
		protected virtual void SelectBuilding ()
		{
			Buildings.Building newlySelected = EditBuildings.self.GetSelection ();
			if (newlySelected == selectedBuilding)
				return;

			if (handleParams != null) {
				handleParams.Disable ();
				handleParams = null;
			}
			selectedBuilding = newlySelected;
		}
		
		public void Disable ()
		{
			ResetEdit ();
			UnityEngine.Object.Destroy (renderTex);
		}

		public virtual void Update ()
		{
			if (handleParams != null) {
				return; // we're editing building map (parameter map for a building)
			}
			bool ctrlKey = Input.GetKey (KeyCode.LeftControl) | Input.GetKey (KeyCode.RightControl);
			Vector3 mousePos = Input.mousePosition;
			if ((CameraControl.MouseOverGUI) ||
			Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt)) {
				return;
			}

			// New building
			if (Input.GetKeyDown (KeyCode.N) && (activeBuildingInC >= 0)) 
			{
				Vector3 hitPoint;
				if (TerrainMgr.TryScreenToTerrainCoord (mousePos, out hitPoint)) {
					Buildings.Building newBuilding = new Buildings.Building (scene.buildings.GetNewBuildingID ());
					newBuilding.name = buildingsInCategory [activeBuildingInC].name;
					newBuilding.prefab = buildingsInCategory [activeBuildingInC].prefabContainer;
					newBuilding.position = hitPoint;
					newBuilding.scale = Vector3.one;
					newBuilding.rotation = Quaternion.identity;

					BuildingCreated (newBuilding);
				}
			}

			// Select building
			if (Input.GetMouseButtonDown (0) && !ctrlKey) 
			{
				Ray screenRay = Camera.main.ScreenPointToRay (mousePos);
				RaycastHit hit;
				if (Physics.Raycast (screenRay, out hit, 10000f, Layers.M_EDIT1)) 
				{
					GameObject go = hit.collider.gameObject;
					Buildings.Building building = EditBuildings.self.GetBuildingForGO (go);
					// TODO: Don't be able to click building that are Action Object buildings
					BuildingClicked (building);
				}
			} 
			// Dragging building
			else if (Input.GetMouseButton (0) && isDragging) 
			{
				Vector3 hitPoint;
				if (TerrainMgr.TryScreenToTerrainCoord (mousePos, out hitPoint)) 
				{
					selectedBuilding.position = hitPoint;
					EditBuildings.self.BuildingChanged (selectedBuilding);
				}
			} else if (isDragging) 
			{
				isDragging = false;
			} 
			// Delete building
			else if (Input.GetKeyDown (KeyCode.Delete) || Input.GetKeyDown (KeyCode.Backspace)) 
			{
				Buildings.Building selectedBuilding = EditBuildings.self.GetSelection ();
				if (selectedBuilding != null) {
					EditBuildings.self.DestroyBuilding (selectedBuilding);
					SelectBuilding ();
				}
			}
		}

		protected virtual void BuildingCreated (Buildings.Building newBuilding)
		{
			EditBuildings.self.AddBuilding (newBuilding);
			EditBuildings.self.MarkBuildingSelected (newBuilding);
			SelectBuilding ();
			angleX = 0;
			angleY = 0;
			angleZ = 0;
		}

		protected virtual void BuildingClicked (Buildings.Building building)
		{
			if (building != null) 
			{
				if (building == selectedBuilding) 
				{
					// Building was already selected, we start to drag....
					isDragging = true;
				} else {
					EditBuildings.self.MarkBuildingSelected (building);
					SelectBuilding ();
					EcoTerrainElements.BuildingPrototype proto = EcoTerrainElements.GetBuilding (building.name);
					activeCategory = (int)proto.category;
					SetupBuildingsInCategory ();
					for (int i = 0; i < buildingsInCategory.Length; i++) {
						if (buildingsInCategory [i].prefabContainer == proto.prefabContainer) {
							activeBuildingInC = i;
							RenderPreviewTexture ();
							break;
						}
					}
					Vector3 angles = building.rotation.eulerAngles;
					angleX = angles.x;
					angleY = angles.y;
					angleZ = angles.z;
				}
			} else {
				EditBuildings.self.ClearSelection ();
			}
		}
	}
}
