using System.Collections.Generic;
using System.Reflection;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.AnimalPopulationModel;
using Ecosim.SceneData.Action;
using Ecosim.GameCtrl.GameButtons;

namespace Ecosim.EcoScript
{
	public abstract class EcoBase
	{
		private static System.Random random;

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

		public void ShowArticleByName (string name) {
			name = name.ToLower ().Trim ();
			foreach (KeyValuePair<int, Articles.Article> p in scene.articles.articles) {
				if (p.Value.description.ToLower ().Trim () == name)
					scene.progression.AddMessage (p.Key);
			}
		}

		public void MakeReport (string text) {
			scene.progression.AddReport (text);
		}

		public Progression.InventarisationResult AddInventarisation(string name, Data area) 
		{
			return AddInventarisation (name, area, area);
		}

		public Progression.InventarisationResult AddInventarisation (string name, Data area, Data data) 
		{
			Progression.InventarisationResult ir = new Progression.InventarisationResult (year, name, area, data, basicAction.id);
			scene.progression.inventarisations.Add (ir);
			return ir;
		}

		/**
		 * Container class that contains data needed for the custom scripts.
		 */
		public class Inventarisation 
		{
			public readonly string Name;
			public readonly Data SelectionMap;
			public readonly int Cost;
			public readonly int UIIndex;

			public Inventarisation (string name, Data selection, int uiIndex, int cost)
			{
				this.Name = name;
				this.SelectionMap = selection;
				this.Cost = cost;
				this.UIIndex = uiIndex;
			}
		}

		public IEnumerable<Inventarisation> EnumerateActiveInventarisations (string areaName)
		{
			foreach (Progression.Inventarisation inv in scene.progression.activeInventarisations) {
				// Check if year is still valid, if the lastYear is the same year as the current year + 1
				// then the inventarisation has finished. This is because the startYear is always the currentYear and there's
				// always 1 or more added to the last year. Then we check if the areaName matches.
				if ((inv.lastYear >= scene.progression.year + 1) && inv.ActionAreaName == areaName) 
				{
					bool firstTime = (inv.startYear == scene.progression.year);
					Inventarisation i = new Inventarisation (
						inv.name,
						scene.progression.GetData (inv.areaName),
						inv.uiIndex,
						// We only calculate the costs the first time
						(firstTime) ? inv.cost : 0
					);
					yield return i;
				}
			}
		}

		public int ProcessBias (int value, BasicAction action, int uiIndex)
		{
			// Get the inventarisation action
			InventarisationAction invAction = (InventarisationAction)action;
			if (invAction != null)
			{
				// Find the distortion range in the ui list
				if (uiIndex < invAction.uiList.Count) 
				{
					// Check if we have a bias
					if (uiIndex < invAction.biasses.Count)
					{
						try {
							// Return the processed value using a random number
							float rnd = GetRandomNumber ();
							InventarisationAction.BiasRange b = invAction.GetBias (uiIndex);
							float prc = (rnd * (b.max - b.min)) + b.min;
							float biasValue = (value * prc);

							switch (b.roundType)
							{
							case InventarisationAction.BiasRange.RoundTypes.RoundUp :
								return UnityEngine.Mathf.CeilToInt (biasValue);
							case InventarisationAction.BiasRange.RoundTypes.RoundDown :
								return UnityEngine.Mathf.FloorToInt (biasValue);
							default :
								return (int)biasValue;
							}
						} catch (System.Exception ex) {
							LogError ("ProcessBias Error: " + ex);
							return value;
						}
					}
					return value;
				}
				return value;
			}
			return value;
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

		public void ResearchConducted () {
			scene.actions.ResearchConducted ();
		}

		/**
		 * Enables/Disables action with given id, enable == true => enble action otherwise disable
		 */
		public void EnableAction (int id, bool enable) {
			scene.actions.GetAction (id).isActive = enable;
		}

		public float GetRandomNumber ()
		{
			if (random == null)
				random = new System.Random ((int)System.DateTime.Now.Ticks);
			return (float)random.NextDouble ();
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

		#region Variable and formula representation

		public void AddVarData (string variable, string name, string category) {
			AddVarRepresentation (variable, name, category);
		}

		public void AddVarRepresentation (string variable, string name, string category) {
			AddVariableRepresentation (variable, name, category);
		}

		public void AddVariableRepresentation (string variable, string name, string category) 
		{
			if (scene.progression.variablesData.ContainsKey (variable)) 
			{
				LogDebug (string.Format ("Variable '{0}'s data already exists. Updating with new data.\nName:{1}, Category:{2}", variable, name, category));
				Progression.VariableData vd = scene.progression.variablesData [variable];
				vd.category = category;
				vd.name = name;
				return;
			} 

			Progression.VariableData newvd = new Progression.VariableData (variable, name, category);
			//newvd.doSave = false;
			scene.progression.variablesData.Add (variable, newvd);
		}

		public void AddFormulaData (string name, string category, string body) {
			AddFormulaRepr (name, category, body);
		}

		public void AddFormulaRepr (string name, string category, string body) {
			AddFormulaRepresentation (name, category, body);
		}

		public void AddFormulaRepresentation (string name, string category, string body) {
			Progression.FormulaData fd = new Progression.FormulaData (name, category, body);
			//fd.doSave = false;
			scene.progression.formulasData.Add (fd);
		}

		#endregion Variable and formula representation

		#region Variable Links

		private class VariableLinkData
		{
			public string variable;
			public object obj;
			public FieldInfo fieldInfo;
			public MethodInfo getMethodInfo;
			public MethodInfo setMethodInfo;
			
			public VariableLinkData (string variable, object obj, string field)
			{
				this.variable = variable;
				this.obj = obj;
				
				System.Type type = obj.GetType ();
				fieldInfo = type.GetField (field);
				if (fieldInfo == null) {
					PropertyInfo pi = type.GetProperty (field);
					setMethodInfo = pi.GetSetMethod ();
					getMethodInfo = pi.GetGetMethod ();
				}
			}
			
			public void Set (object value)
			{
				if (fieldInfo != null)
					fieldInfo.SetValue (obj, value);
				else if (setMethodInfo != null)
					setMethodInfo.Invoke (obj, new object [] { value });
			}
			
			public object Get ()
			{
				if (fieldInfo != null)
					return fieldInfo.GetValue (obj);
				else if (getMethodInfo != null)
					return getMethodInfo.Invoke (obj, null);
				return null;
			}
		}
		
		private static List<VariableLinkData> variableLinks;
		
		private static void UpdateVariable (string key, object value) {
			for (int i = 0; i < variableLinks.Count; i++) {
				if (variableLinks [i].variable == key) {
					variableLinks [i].Set (value);
				}
			}
		}

		public void LinkVar (object obj, string field, string variable) {
			SetupVariableLink (obj, field, variable);
		}

		public void SetupVarLink (object obj, string field, string variable) {
			SetupVariableLink (obj, field, variable);
		}

		public void SetupVariableLink (object obj, string field, string variable) 
		{
			VariableLinkData ld = new VariableLinkData (variable, obj, field);
			
			// Add the variable if it doesn't exist already
			if (scene.progression.variables.ContainsKey (variable) == false) {
				scene.progression.variables.Add (variable, ld.Get ());
			}
			
			// Add update callback
			scene.progression.variables.AddUpdateCallback (variable, UpdateVariable);
			
			// Check if we have a list
			if (variableLinks == null)
				variableLinks = new List<VariableLinkData> ();
			variableLinks.Add (ld);
		}

		#endregion Variable Links
	}
}