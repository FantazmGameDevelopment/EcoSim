using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandleBuildings : PanelHelper
	{
		private Scene scene;
		private EditorCtrl ctrl;
		private readonly MapsPanel parent;
		private GUIStyle tabNormal;
		private GUIStyle tabSelected;
		private PanelHelper helper;
		private bool isDragging;
		private string[] buildingCategories;
		private EcoTerrainElements.BuildingPrototype[] buildingsInCategory;
		private int activeCategory = 0;
		private int activeBuildingInC = 0;
		private float angleX = 0f;
		private float angleY = 0f;
		private float angleZ = 0f;
		private HandleParameters handleParams = null;
		private Buildings.Building selectedBuilding = null;
		private Texture2D renderTex;
		
		public HandleBuildings (EditorCtrl ctrl, MapsPanel parent, Scene scene)
		{
			this.ctrl = ctrl;
			this.parent = parent;
			
			this.scene = parent.scene;
			tabNormal = ctrl.listItem;
			tabSelected = ctrl.listItemSelected;
			renderTex = new Texture2D (380, 300, TextureFormat.RGB24, false);
			
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
		
		void Setup ()
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
		
		void RenderPreviewTexture () {
			RenderTileIcons.RenderSettings rs = new RenderTileIcons.RenderSettings (60f, 30f, 120f, 24f);
			EcoTerrainElements.BuildingPrototype bp = buildingsInCategory[activeBuildingInC];
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
				
		public bool Render (int mx, int my)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Category", GUILayout.Width (100));
			if (GUILayout.Button (buildingCategories [activeCategory], GUILayout.Width (200))) {
				ctrl.StartSelection (buildingCategories, activeCategory, newIndex => {
					if (newIndex != activeCategory) {
						activeCategory = newIndex;
						SetupBuildingsInCategory ();
						activeBuildingInC = (buildingsInCategory.Length > 0) ? 0 : -1;
						RenderPreviewTexture ();
					}
				});
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Building", GUILayout.Width (100));
			if (activeBuildingInC >= 0) {
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
				GUILayout.Label ("Category is empty");
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			// GUILayout.FlexibleSpace ();
			GUILayout.Label (renderTex, GUIStyle.none);
			if (selectedBuilding != null) {
				bool hasChanged = false;
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Rotation", GUILayout.Width (100));
				float newY = GUILayout.HorizontalSlider (angleY, 0f, 360f, GUILayout.Width (60));
				if (newY != angleY) {
					angleY = Mathf.Round (newY / 5) * 5;
					hasChanged = true;
				}
				GUILayout.Label (Mathf.Round (angleY).ToString (), GUILayout.Width (20));
				float newX = GUILayout.HorizontalSlider (angleX, -45f, 45f, GUILayout.Width (60));
				if (newX != angleX) {
					angleX = Mathf.Round (newX);
					hasChanged = true;
				}
				GUILayout.Label (Mathf.Round (angleX).ToString (), GUILayout.Width (20));
				float newZ = GUILayout.HorizontalSlider (angleZ, -45, 45f, GUILayout.Width (60));
				if (newZ != angleZ) {
					angleZ = Mathf.Round (newZ);
					hasChanged = true;
				}
				GUILayout.Label (Mathf.Round (angleZ).ToString (), GUILayout.Width (20));
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				float level;
				if (TerrainMgr.TryGetTerrainHeight (selectedBuilding.position, out level)) {
					GUILayout.BeginHorizontal ();
					GUILayout.Label ("Vertical pos", GUILayout.Width (100));
					float pos = selectedBuilding.position.y;
					float newPos = GUILayout.HorizontalSlider (pos, level - 10f, level + 20f, GUILayout.Width (200));
					if (pos != newPos) {
						selectedBuilding.position.y = newPos;
						hasChanged = true;
					}
					GUILayout.FlexibleSpace ();
					GUILayout.EndHorizontal ();
				}
				GUILayout.BeginHorizontal ();
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
				
				GUILayout.EndHorizontal ();
								
				Data buildingMap = null;
				string mapName = Buildings.GetMapNameForBuildingId(selectedBuilding.id);
				if (scene.progression.HasData (mapName)) {
					buildingMap = scene.progression.GetData (mapName);
				}
				if (buildingMap != null) {
					GUILayout.BeginHorizontal ();
					GUILayout.Label ("map " + mapName, GUILayout.Width (100));
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
					GUILayout.EndHorizontal ();
				} else {
					GUILayout.BeginHorizontal ();
					GUILayout.Label ("map " + mapName, GUILayout.Width (100));
					if (GUILayout.Button ("Create as 1 bit (0..1)")) {
						scene.progression.AddData (mapName, new SparseBitMap1 (scene));
					}
					if (GUILayout.Button ("Create as 8 bit (0..255)")) {
						scene.progression.AddData (mapName, new SparseBitMap8 (scene));
					}
					GUILayout.FlexibleSpace ();
					GUILayout.EndHorizontal ();
				}
				
				if (handleParams != null) {
					handleParams.Render (mx, my);
				}
				
				if (hasChanged) {
					selectedBuilding.rotation = Quaternion.Euler (angleX, angleY, angleZ);
					EditBuildings.self.BuildingChanged (selectedBuilding);
				}
				
			}
			
			return false;
		}
		
		private void SelectBuilding ()
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

		public void Update ()
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
			if (Input.GetKeyDown (KeyCode.N) && (activeBuildingInC >= 0)) {
				Vector3 hitPoint;
				if (TerrainMgr.TryScreenToTerrainCoord (mousePos, out hitPoint)) {
					Buildings.Building newBuilding = new Buildings.Building (scene.buildings.GetNewBuildingID ());
					newBuilding.name = buildingsInCategory [activeBuildingInC].name;
					newBuilding.prefab = buildingsInCategory [activeBuildingInC].prefabContainer;
					newBuilding.position = hitPoint;
					newBuilding.scale = Vector3.one;
					newBuilding.rotation = Quaternion.identity;
					EditBuildings.self.AddBuilding (newBuilding);
					EditBuildings.self.MarkBuildingSelected (newBuilding);
					SelectBuilding ();
					angleX = 0;
					angleY = 0;
					angleZ = 0;
				}
			}
			if (Input.GetMouseButtonDown (0) && !ctrlKey) {
				Ray screenRay = Camera.main.ScreenPointToRay (mousePos);
				RaycastHit hit;
				if (Physics.Raycast (screenRay, out hit, 10000f, Layers.M_EDIT1)) {
					GameObject go = hit.collider.gameObject;
					Buildings.Building building = EditBuildings.self.GetBuildingForGO (go);
					if (building != null) {
						if (building == selectedBuilding) {
							// building was already selected, we start to drag....
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
			} else if (Input.GetMouseButton (0) && isDragging) {
				Vector3 hitPoint;
				if (TerrainMgr.TryScreenToTerrainCoord (mousePos, out hitPoint)) {
					selectedBuilding.position = hitPoint;
					EditBuildings.self.BuildingChanged (selectedBuilding);
				}
			} else if (isDragging) {
				isDragging = false;
			} else if (Input.GetKeyDown (KeyCode.Delete) || Input.GetKeyDown (KeyCode.Backspace)) {
				Buildings.Building selectedBuilding = EditBuildings.self.GetSelection ();
				if (selectedBuilding != null) {
					EditBuildings.self.DestroyBuilding (selectedBuilding);
					SelectBuilding ();
				}
			}
		}
	}
}
