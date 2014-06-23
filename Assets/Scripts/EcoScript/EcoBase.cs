using System.Collections.Generic;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.AnimalPopulationModel;
using Ecosim.SceneData.Action;

namespace Ecosim.EcoScript
{
	public abstract class EcoBase
	{
		public readonly Scene scene;
		public readonly BasicAction basicAction;
		public readonly int actionId;
		public Dictionary<string, string> properties;

		public EcoBase (Scene scene, BasicAction basicAction)
		{
			this.scene = scene;
			this.basicAction = basicAction;
			this.actionId = this.basicAction.id;
			properties = new Dictionary<string, string> ();
		}
		
		public void SetProperty (string name, string v)
		{
			if (properties.ContainsKey (name)) {
				properties [name] = v;
			} else {
				properties.Add (name, v);
			}
		}

		public void SetProperty (string name, int v)
		{
			SetProperty (name, v.ToString ());
		}

		public void SetProperty (string name, long v)
		{
			SetProperty (name, v.ToString ());
		}

		public void SetProperty (string name, float v)
		{
			SetProperty (name, v.ToString ());
		}
		
		public string GetProperty (string name, string defaultVal)
		{
			string result = null;
			if (properties.TryGetValue (name, out result)) {
				return result;
			}
			return defaultVal;
		}

		public int GetIntProperty (string name, int defaultVal)
		{
			string strVal = null;
			if (properties.TryGetValue (name, out strVal)) {
				int result;
				if (int.TryParse (strVal, out result)) {
					return result;
				}
			}
			return defaultVal;
		}

		public long GetLongProperty (string name, long defaultVal)
		{
			string strVal = null;
			if (properties.TryGetValue (name, out strVal)) {
				long result;
				if (long.TryParse (strVal, out result)) {
					return result;
				}
			}
			return defaultVal;
		}
		
		public float GetFloatProperty (string name, float defaultVal)
		{
			string strVal = null;
			if (properties.TryGetValue (name, out strVal)) {
				float result;
				if (float.TryParse (strVal, out result)) {
					return result;
				}
			}
			return defaultVal;
		}
		
		public void DeleteProperty (string  name)
		{
			if (properties.ContainsKey (name)) {
				properties.Remove (name);
			}
		}
		
		public void ShowArticle (int id) {
			scene.progression.AddMessage (id);
		}

		public void ShowArticle (string text) {
			scene.progression.AddMessage (text);
		}

		public void MakeReport (string text) {
			scene.progression.AddReport (text);
		}
		
		public Progression.InventarisationResult AddInventarisation(string name, Data data) {
			Progression.InventarisationResult ir = new Progression.InventarisationResult (year, name, data, basicAction.id);
			scene.progression.inventarisations.Add (ir);
			return ir;
		}
		
		public void AddGameMarkers (string dataName, string[] models, RenderGameMarkersMgr.ClickHandler fn) {
			RenderGameMarkersMgr.AddGameMarkers (dataName, models, fn);
		}

		public void AddGameMarkers (string dataName) {
			RenderGameMarkersMgr.AddGameMarkers (dataName, null, null);
		}
		
		public void RemoveGameMarkers (string dataName) {
			RenderGameMarkersMgr.RemoveGameMarkers (dataName);
		}
		
		public Progression progression {
			get {
				return scene.progression;
			}
		}
				
		public int year {
			get {
				return scene.progression.year;
			}
		}

		public int startYear {
			get {
				return scene.progression.startYear;
			}
		}
		
		public long budget {
			get {
				return scene.progression.budget;
			}
			
			set {
				scene.progression.budget = value;
			}
		}
		
		public bool allowResearch {
			get {
				return scene.progression.allowResearch;
			}
			
			set {
				scene.progression.allowResearch = value;
			}			
		}
		
		public bool allowMeasures {
			get {
				return scene.progression.allowMeasures;
			}
			
			set {
				scene.progression.allowMeasures = value;
			}			
		}

		public Data GetTargetArea (int index) {
			return scene.progression.GetData (Progression.TARGET_ID + index.ToString());
		}
		
		public void EnableBuilding (int buildingId, bool enable) {
			scene.buildings.GetBuilding (buildingId).isActive = enable;
		}
		
		public Data GetBuildingData (int buildingId) {
			string mapName = Buildings.GetMapNameForBuildingId (buildingId);
			if (progression.HasData (mapName)) {
				return progression.GetData (mapName);
			}
			else {
				return null;
			}
		}
		
		public int[] GetBuildingIdsForName (string buildingName) {
			List<Buildings.Building> buildings = scene.buildings.GetAllBuildings ();
			List<int> idList = new List<int> ();
			foreach (Buildings.Building building in buildings) {
				if (building.name == buildingName) {
					idList.Add (building.id);
				}
			}
			return idList.ToArray ();
		}

		public AnimalType GetAnimal (string name)
		{
			name = name.ToLower ();
			name = name.Replace (" ", "");
			name = name.Trim ();
			foreach (AnimalType at in scene.animalTypes) {
				string atName = at.name;
				atName = atName.ToLower ();
				atName = atName.Replace (" ", "");
				atName = atName.Trim ();
				if (atName == name)
					return at;
			}
			return null;
		}

		/**
		 * Enables/Disables action with given id, enable == true => enble action otherwise disable
		 */
		public void EnableAction (int id, bool enable) {
			scene.actions.GetAction (id).isActive = enable;
		}
		
		public void LogDebug(string str) {
			Log.LogDebug (str);
		}
		
		public void LogWarning(string str) {
			Log.LogWarning (str);
		}

		public void LogError(string str) {
			Log.LogError (str);
		}
		
		/**
		 * Returns true if UserInteraction ui is set to acceptable for the given vegetation
		 * at location x, y. The action ui is part of should be an area action and in vegetation
		 * type definition it should be defined as acceptable.
		 */
		public bool ActionAcceptableForVegetation (int x, int y, UserInteraction ui) {
			return progression.vegetation.GetVegetationType (x, y).IsAcceptableAction (ui);
		}
	}
}