using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;
using Ecosim.SceneData.Action;

namespace Ecosim.SceneEditor
{
	public class ModulePanel : Panel
	{
		
		private Scene scene;
		private EditorCtrl ctrl;
		private string sceneName = "Scene";
		private Vector2 scrollPos;
		
		private enum State
		{
			MAIN,
			NEW,
			RESIZE,
		};
		private State state = State.MAIN;
		/**
		 * Called when scene is set or changed in Editor
		 */
		public void Setup (EditorCtrl ctrl, Scene scene)
		{
			this.ctrl = ctrl;
			this.scene = scene;
			
			List<int> sizesList = new List<int> ();
			List<string> sizeStringsList = new List<string> ();
			for (int size = 128; size <= 2048; size += 128) {
				sizesList.Add (size);
				sizeStringsList.Add (size.ToString ());
			}
			sizes = sizesList.ToArray ();
			sizeStrings = sizeStringsList.ToArray ();
			sceneName = PlayerPrefs.GetString ("EditorSceneName", "NewScene");
			if (scene != null) {
				yearStr = scene.progression.startYear.ToString ();
				budgetStr = scene.progression.budget.ToString ();
			}
		}
		
		string yearStr;
		string budgetStr;
		
//		delegate IEnumerable<bool> DCoroutine();
		IEnumerator<bool> coRoutine;
		
//		DCoroutine activeCoroutine = null;
		
		IEnumerable<bool> COLoadScene ()
		{
			// we do two yield as we're lazy and ask for moveNext, thus already
			// advancing on first iteration. The return value actually doesn't
			// matter as we don't look at it.
			yield return true;
			yield return true;
			scene = Scene.LoadForEditing (sceneName, delegate(string obj) {
				EditorCtrl.self.StartOkDialog (obj, null);
			});
			Log.LogWarning ("Scene is loaded: " + scene.sceneName);
			yield return true;
			TerrainMgr.self.SetupTerrain (scene);
			CameraControl.SetupCamera (scene);
			PlayerPrefs.SetString ("EditorSceneName", sceneName);
			PlayerPrefs.Save ();
			ctrl.SceneIsLoaded (scene);
		}

		IEnumerable<bool> COSaveScene ()
		{
			// we do two yield as we're lazy and ask for moveNext, thus already
			// advancing on first iteration. The return value actually doesn't
			// matter as we don't look at it.
			yield return true;
			yield return true;
			scene.ForceLoadAllData ();
//			scene.sceneName = sceneName;
			scene.Save (sceneName);
			PlayerPrefs.SetString ("EditorSceneName", sceneName);
			PlayerPrefs.Save ();
		}
				
		bool RenderMain (int mx, int my)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Scene name", GUILayout.Width (100));
			sceneName = GUILayout.TextField (sceneName, GUILayout.Width (200));
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.Space (4);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("", GUILayout.Width (100));
			if (GUILayout.Button ("New...")) {
				state = State.NEW;
			}
			if (GUILayout.Button ("Load")) {
				coRoutine = COLoadScene ().GetEnumerator ();
				return false;				
			}
			if (scene != null) {
				if (GUILayout.Button ("Save")) {
					coRoutine = COSaveScene ().GetEnumerator ();
				}
				if (GUILayout.Button ("Resize...")) {
					state = State.RESIZE;
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			if (scene != null) {
				GUILayout.Space (16);
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Redraw terrain", GUILayout.Width (100));
				if (GUILayout.Button ("Refresh")) {
					TerrainMgr.self.ForceRedraw ();
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Generate overview", GUILayout.Width (100));
				if (GUILayout.Button ("Generate")) {
					RenderOverviewTiles.StartRenderingOverviewTiles (ctrl);
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Calculate water", GUILayout.Width (100));
				if (GUILayout.Button ("Calculate")) {
					scene.progression.CalculateWater ();
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				GUILayout.Space (16);
				
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Description", GUILayout.Width (100));
				scene.description = GUILayout.TextArea (scene.description, GUILayout.Width (250), GUILayout.Height (100));
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Start year", GUILayout.Width (100));
				yearStr = GUILayout.TextField (yearStr, GUILayout.Width (80));
				long outNr;
				if (long.TryParse (yearStr, out outNr)) {
					scene.progression.startYear = Mathf.Clamp ((int)outNr, 1900, 2500);
					yearStr = scene.progression.startYear.ToString ();
				}
				GUILayout.EndHorizontal ();

				RenderBudget ();
				
				GUILayout.Space (8);
				RenderUIGroup ("Research", scene.actions.uiGroups [UserInteractionGroup.CATEGORY_RESEARCH], mx, my);
				RenderUIGroup ("Measures", scene.actions.uiGroups [UserInteractionGroup.CATEGORY_MEASURES], mx, my);
				
//				RenderTest ();
			}
			
			return false;
		}

		bool variableBudgetsOpened = true;

		void RenderBudget ()
		{
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				GUILayout.Space (2);

				int budget = (int)scene.progression.budget;
				EcoGUI.IntField ("Start budget", ref budget, 120, 80);
				scene.progression.budget = budget;
				EcoGUI.IntField ("Extra budget per year", ref scene.progression.yearBudget, 120, 80);

				if (EcoGUI.Foldout ("Variable year budgets", ref variableBudgetsOpened))
				{
					GUILayout.Space (5);

					foreach (Progression.VariableYearBudget yb in scene.progression.variableYearBudgets)
					{
						GUILayout.BeginHorizontal ();
						{
							EcoGUI.skipHorizontal = true;
							EcoGUI.IntField ("\tYear:", ref yb.year, 50, 50);
							EcoGUI.IntField ("Extra budget:", ref yb.budget, 80, 80);
							EcoGUI.skipHorizontal = false;

							GUILayout.Space (10);
							if (GUILayout.Button ("-", GUILayout.Width (20))) {
								scene.progression.variableYearBudgets.Remove (yb);
								GUILayout.EndHorizontal ();
								break;
							}
						}
						GUILayout.EndHorizontal ();
					}

					GUILayout.Space (3);
					GUILayout.BeginHorizontal ();
					{
						GUILayout.Space (16);
						if (GUILayout.Button ("Add year", GUILayout.Width (100))) 
						{
							Progression.VariableYearBudget yb = new Progression.VariableYearBudget ();
							yb.year = scene.progression.startYear;
							yb.budget = scene.progression.yearBudget;
							if (scene.progression.variableYearBudgets.Count > 0) {
								Progression.VariableYearBudget last = scene.progression.variableYearBudgets [scene.progression.variableYearBudgets.Count - 1];
								yb.year = last.year + 1;
								yb.budget = last.budget;
							}
							scene.progression.variableYearBudgets.Add (yb);
						}
					}
					GUILayout.EndHorizontal ();
				}
			}
			GUILayout.EndHorizontal ();
		}
		
		private Dictionary<string, UserInteractionGroup.GroupData> selectedGrpDict = new Dictionary<string, UserInteractionGroup.GroupData> ();
		
		void RenderUIGroup (string name, UserInteractionGroup category, int mx, int my)
		{
			GUILayout.BeginVertical (ctrl.skin.box);
			GUILayout.Label (name);
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Add group")) {
				UserInteractionGroup.GroupData grp = new UserInteractionGroup.GroupData ();
				grp.iconId = 0;
				grp.icon = scene.assets.GetIcon (0);
				grp.activeIcon = scene.assets.GetHighlightedIcon (0);
				grp.uiList = new UserInteraction[0];
				if (selectedGrpDict.ContainsKey (name)) {
					selectedGrpDict [name] = grp;
				} else {
					selectedGrpDict.Add (name, grp);
				}
				List<UserInteractionGroup.GroupData> grpList = new List<UserInteractionGroup.GroupData> (category.groups);
				grpList.Add (grp);
				category.groups = grpList.ToArray ();
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			UserInteractionGroup.GroupData selectedGroup;
			selectedGrpDict.TryGetValue (name, out selectedGroup);
			foreach (UserInteractionGroup.GroupData grp in category.groups) {
				if (GUILayout.Button ((grp == selectedGroup) ? (grp.activeIcon) : (grp.icon))) {
					selectedGroup = grp;
					if (selectedGrpDict.ContainsKey (name)) {
						selectedGrpDict [name] = grp;
					} else {
						selectedGrpDict.Add (name, grp);
					}
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			if (selectedGroup != null) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Group icon", GUILayout.Width (100));
				if (GUILayout.Button (selectedGroup.icon)) {
					ctrl.StartIconSelection (selectedGroup.iconId, newIndex => {
						selectedGroup.iconId = newIndex;
						selectedGroup.icon = scene.assets.GetIcon (newIndex);
						selectedGroup.activeIcon = scene.assets.GetHighlightedIcon (newIndex);
					});
				}
				if (GUILayout.Button ("Delete group")) {
					List<UserInteractionGroup.GroupData> grp = new List<UserInteractionGroup.GroupData>(category.groups);
					grp.Remove (selectedGroup);
					category.groups = grp.ToArray ();
					selectedGrpDict.Remove (name);
					selectedGroup = null;
					scene.actions.UpdateReferences ();
					return;
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				foreach (UserInteraction ui in selectedGroup.uiList) {
					GUILayout.BeginHorizontal ();
					GUILayout.Label (ui.name, GUILayout.Width (100));
					GUILayout.Label (ui.icon);
					if (GUILayout.Button ("X", GUILayout.Width (20))) {
						List<UserInteraction> uiList = new List<UserInteraction> (selectedGroup.uiList);
						uiList.Remove (ui);
						selectedGroup.uiList = uiList.ToArray ();
						scene.actions.UpdateReferences ();
						break;
					}
					GUILayout.EndHorizontal ();
				}
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Add entry")) {
					List<string> names = new List<string> ();
					foreach (UserInteraction ui2 in scene.actions.EnumerateUI()) {
						names.Add (ui2.name);
					}
					if (names.Count == 0) {
						ctrl.StartOkDialog ("There are currently no actions with user buttons defined!", null);
						return;
					}
					ctrl.StartSelection (names.ToArray (), -1, newIndex => {
						List<UserInteraction> uiList = new List<UserInteraction> (selectedGroup.uiList);
						uiList.Add (scene.actions.GetUIByName (names[newIndex]));
						selectedGroup.uiList = uiList.ToArray ();
						scene.actions.UpdateReferences ();
					});
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
			}
			GUILayout.EndVertical ();
		}

		IEnumerable<bool> CONewScene (int width, int height, SuccessionType[] successionTypes, PlantType[] plantTypes, AnimalType[] animalTypes, ActionObjectsGroup[] actionObjectGroups, CalculatedData.Calculation[] calculations, ActionMgr actionMgr, ManagedDictionary<string, object> variables)
		{
			// we do two yield as we're lazy and ask for moveNext, thus already
			// advancing on first iteration. The return value actually doesn't
			// matter as we don't look at it.
			yield return true;
			yield return true;
			scene = Scene.CreateNewScene (sceneName, width, height, successionTypes, plantTypes, animalTypes, actionObjectGroups, calculations, actionMgr, variables);
			yield return true;
			TerrainMgr.self.SetupTerrain (scene);
			CameraControl.SetupCamera (scene);
			state = State.MAIN;				
			PlayerPrefs.SetString ("EditorSceneName", sceneName);
			PlayerPrefs.Save ();
			ctrl.SceneIsLoaded (scene);
		}
		
		private int newWidthIndex = 1;
		private int newHeightIndex = 1;
		private int[] sizes;
		private string[] sizeStrings;

		private bool reuseVegetation = true;
		private bool reusePlants = true;
		private bool reuseAnimals = true;
		private bool reuseActions = true;
		private bool reuseVariables = true;
		private bool reuseActionObjectGroups = true;
		private bool reuseResearchPoints = true;
		private bool reuseCalculations = false;
		
		bool RenderNew (int mx, int my)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Scene name", GUILayout.Width (100));
			sceneName = GUILayout.TextField (sceneName, GUILayout.Width (200));
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Size (width x height)", GUILayout.Width (100));
			
			if (GUILayout.Button (sizeStrings [newWidthIndex], GUILayout.Width (60))) {
				ctrl.StartSelection (sizeStrings, newWidthIndex,
					newIndex => {
					newWidthIndex = newIndex; });
			}
			if (GUILayout.Button (sizeStrings [newHeightIndex], GUILayout.Width (60))) {
				ctrl.StartSelection (sizeStrings, newHeightIndex,
					newIndex => {
					newHeightIndex = newIndex; });
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.Space (4);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("", GUILayout.Width (100));
			if (GUILayout.Button ("Cancel")) {
				state = State.MAIN;
			}
			if (GUILayout.Button ("Create")) {
				if (System.IO.Directory.Exists (GameSettings.GetPathForScene (sceneName))) {
					ctrl.StartOkDialog ("Scene with name '" + sceneName + "' already exists, delete it first through finder/explorer", null);
				} else {
					SuccessionType[] successionTypes = null;
					if ((scene != null) && (reuseVegetation)) {
						successionTypes = scene.successionTypes;
					}
					PlantType[] plantTypes = null;
					if ((scene != null) && reusePlants) {
						plantTypes = scene.plantTypes;
					}
					AnimalType[] animalTypes = null;
					if ((scene != null) && reuseAnimals) {
						animalTypes = scene.animalTypes;
					}
					ActionMgr actionMgr = null;
					if ((scene != null) && reuseActions) {
						actionMgr = scene.actions;
					}
					ManagedDictionary<string, object> variables = null;
					if ((scene != null) && reuseVariables) {
						variables = scene.progression.variables;
					}
					ActionObjectsGroup[] actionObjectGroups = null;
					if ((scene != null) && reuseActionObjectGroups) {
						actionObjectGroups = scene.actionObjectGroups;
					}
					CalculatedData.Calculation[] calculations = null;
					if ((scene != null) && reuseCalculations) {
						calculations = scene.calculations;
					}

					coRoutine = CONewScene (sizes [newWidthIndex], sizes [newHeightIndex], successionTypes, plantTypes, animalTypes, actionObjectGroups, calculations, actionMgr, variables).GetEnumerator ();
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			if (scene != null) {
				reuseVegetation = GUILayout.Toggle (reuseVegetation, "Use current vegetation");
				reusePlants = GUILayout.Toggle (reusePlants, "Use current plants");
				reuseAnimals = GUILayout.Toggle (reuseAnimals, "Use current animals");
				reuseActions = GUILayout.Toggle (reuseActions, "Use current actions");
				reuseVariables = GUILayout.Toggle (reuseVariables, "Use current variables");
				// FIXME: Something goes wrong if we reuse the calculations? Check this some time.
				//reuseCalculations = GUILayout.Toggle (reuseCalculations, "Use current Calculated data calculations");
			}

			return false;
		}
		
		IEnumerable<bool> COResizeScene (int offsetX, int offsetY, int width, int height)
		{
			// we do two yield as we're lazy and ask for moveNext, thus already
			// advancing on first iteration. The return value actually doesn't
			// matter as we don't look at it.
			yield return true;
			yield return true;
			scene = scene.ResizeTo (sceneName, offsetX, offsetY, width, height);
			yield return true;
			TerrainMgr.self.SetupTerrain (scene);
			CameraControl.SetupCamera (scene);
			state = State.MAIN;				
			PlayerPrefs.SetString ("EditorSceneName", sceneName);
			PlayerPrefs.Save ();
			ctrl.SceneIsLoaded (scene);
		}

		string offsetXstr = "0";
		string offsetYstr = "0";
		
		bool RenderResize (int mx, int my)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Scene name", GUILayout.Width (100));
			sceneName = GUILayout.TextField (sceneName, GUILayout.Width (200));
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Offsets", GUILayout.Width (100));
			offsetXstr = GUILayout.TextField (offsetXstr, GUILayout.Width (60));
			offsetYstr = GUILayout.TextField (offsetYstr, GUILayout.Width (60));
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Current size (width x height)", GUILayout.Width (100));
			GUILayout.Label (scene.width.ToString (), GUILayout.Width (60));
			GUILayout.Label (scene.height.ToString (), GUILayout.Width (60));
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Size (width x height)", GUILayout.Width (100));
			
			if (GUILayout.Button (sizeStrings [newWidthIndex], GUILayout.Width (60))) {
				ctrl.StartSelection (sizeStrings, newWidthIndex,
					newIndex => {
					newWidthIndex = newIndex; });
			}
			if (GUILayout.Button (sizeStrings [newHeightIndex], GUILayout.Width (60))) {
				ctrl.StartSelection (sizeStrings, newHeightIndex,
					newIndex => {
					newHeightIndex = newIndex; });
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.Space (4);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("", GUILayout.Width (100));
			if (GUILayout.Button ("Cancel")) {
				state = State.MAIN;
			}
			if (GUILayout.Button ("Create")) {
				if (System.IO.Directory.Exists (GameSettings.GetPathForScene (sceneName))) {
					ctrl.StartOkDialog ("Scene with name '" + sceneName + "' already exists, use unique name for resized scene.", null);
				} else {
					int offsetX = int.Parse (offsetXstr);
					int offsetY = int.Parse (offsetYstr);
					coRoutine = COResizeScene (offsetX, offsetY, sizes [newWidthIndex], sizes [newHeightIndex]).GetEnumerator ();
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			return false;
		}
		
		/**
		 * Called every frame when this panel is active
		 * It is guaranteed that setup has been called with
		 * a valid scene before the first call of Render.
		 */
		public bool Render (int mx, int my)
		{
			if (coRoutine != null) {
				// we have a coroutine, basically we use this
				// to delay operations over more than one frame
				// we just keep calling MoveNext on every frame
				// until we finished the coroutine
				if (!coRoutine.MoveNext ()) {
					coRoutine = null;
				}
				GUILayout.Label ("Please wait....");
				GUILayout.FlexibleSpace ();
				return false;
			}
			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			GUILayout.BeginVertical ();
			switch (state) {
			case State.MAIN :
				RenderMain (mx, my);
				break;
			case State.NEW :
				RenderNew (mx, my);
				break;
			case State.RESIZE :
				RenderResize (mx, my);
				break;
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndVertical ();
			GUILayout.EndScrollView ();
			return false;
		}
		
		/* Called for extra edit sub-panel, will be called after Render */
		public void RenderExtra (int mx, int my)
		{
		}

		/* Called for extra side edit sub-panel, will be called after RenderExtra */
		public void RenderSide (int mx, int my)
		{
		}
		
		/* Returns true if a side panel is needed. Won't be called before RenderExtra has been called */
		public bool NeedSidePanel ()
		{
			return false;
		}
		
		public bool IsAvailable ()
		{
			return true;
		}

		public void Activate ()
		{
		}
		
		public void Deactivate ()
		{
		}

		public void Update ()
		{
		}
	}
}