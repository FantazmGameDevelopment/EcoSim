using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

using Ecosim;
using Ecosim.SceneData.Action;

namespace Ecosim.SceneData
{
	/**
	 * Progression keeps track of the changes of the scenario as result of player actions
	 * For every year a progression file in saved in the savegame slot as progress<year>.xml
	 * For initial info there's a initialprogress.xml stored in the scene folder
	 */
	public class Progression
	{
		public enum PredefinedVariables
		{
			lastMeasure,
			lastMeasureGroup,
			lastMeasureCount,
			lastResearch,
			lastResearchGroup,
			lastResearchCount
		}

		public static List<string> predefinedVariables {
			get { return new List<string>(System.Enum.GetNames (typeof (PredefinedVariables))); }
		}

		public event System.Action<bool> onGameEndChanged;

		public const string HEIGHTMAP_ID = "heightmap";
		public const string WATERHEIGHTMAP_ID = "waterheightmap";
		public const string CALCULATED_WATERHEIGHTMAP_ID = "calculatedwaterheightmap";
		public const string VEGETATION_ID = "vegetation";
		public const string CANAL_ID = "canals";
		public const string ANIMAL_ID = "animals";
		public const string SUCCESSION_ID = "_succession";
		public const string MANAGED_ID = "_managed";
		public const string TARGET_ID = "_targetarea";
		public const string PURCHASABLE_ID = "_purchasable";

		public long budget = 0; // budget left
		public int yearBudget = 0; // budget added per year
		public long expenses = 0; // only used for get purposes
		public int year = 0; // year of this progression
		public int startYear = 2013; // not really progression, but for determining first progression data
		public int yearsPerTurn = 1;

		private bool _gameEnded = false;
		public bool gameEnded {
			get { return _gameEnded; }
			set {
				_gameEnded = value;
				if (onGameEndChanged != null)
					onGameEndChanged (_gameEnded);
			}
		}
		public bool allowResearch = true;
		public bool allowMeasures = true;
		public int targetAreas = 0;
		public readonly Scene scene;
		
		// important much used data set are stored in variables
		// for quick access (they can still be accessed through GetData of course)
		public VegetationData vegetation;
		public HeightMap heightMap;
		public HeightMap waterHeightMap;
		public HeightMap calculatedWaterHeightMap;
		public BitMap2 canals;
		public BitMap1 successionArea;
		public BitMap1 managedArea;
		public BitMap8 purchasableArea;

		/**
		 * Stores the extra budget that a user gets at year x
		 */ 
		public class VariableYearBudget
		{
			public int year;
			public int budget;
		}

		/**
		 * Stores 2-dimensional data, like pH, heightmap, ...
		 */
		class DataInfo
		{
			public string name; // name (key) of data
			public int year; // year data is last updated
			public bool isInternal = false; // if true data will never be saved to disk
			public Data data; // the actual data
		}
		
		/**
		 * Temporarily stores state of actions so we can delay initializing them
		 * as we cannot do it yet when loading progression (UpdateReferences needs to be called first)
		 */
		class ActionState
		{
			public int actionId;
			public bool isEnabled;
			public Dictionary<string, string> properties;
		}

		/**
		 * Container class. This class stores the years
		 * in which an action has been taken
		 */
		public class ActionTaken
		{
			public int id;
			public List<int> years;
		}

		/**
		 * Inventarisation class. It stores the InventarisationAction,
		 * the given name and amount of years it should last.
		 */
		public class Inventarisation
		{
			public const string XML_ELEMENT = "inventarisation";

			/**
 			 * Constructor can only be used when game is running. It creates a new
			 * inventarisation result using a given year, name, area data set, actionid.
			 */
			public Inventarisation (Scene scene, int startYear, int lastYear, string name, string areaName, int actionId, int uiIndex, int cost)
			{
				this.scene = scene;
				this.startYear = startYear;
				this.lastYear = lastYear;
				this.name = name;
				this.areaName = areaName;
				this.actionId = actionId;
				this.uiIndex = uiIndex;
				this.cost = cost;
			}

			public readonly Scene scene;
			public readonly int startYear;
			public readonly int lastYear;
			public readonly string name;
			public readonly string areaName;
			public readonly int actionId;
			public readonly int uiIndex;
			public readonly int cost;

			private string actionAreaName = null;
			public string ActionAreaName {
				get {
					if (actionAreaName == null)
						actionAreaName = ((InventarisationAction)scene.actions.GetAction (this.actionId)).areaName;
					return actionAreaName;
				}
			}

			public void Save (XmlTextWriter w, Scene scene)
			{
				w.WriteStartElement (XML_ELEMENT);
				w.WriteAttributeString ("start", startYear.ToString());
				w.WriteAttributeString ("end", lastYear.ToString());
				w.WriteAttributeString ("name", name);
				w.WriteAttributeString ("areaname", areaName);
				w.WriteAttributeString ("actionid", actionId.ToString ());
				w.WriteAttributeString ("uiindex", uiIndex.ToString ());
				w.WriteAttributeString ("cost", cost.ToString());
				w.WriteEndElement ();
			}

			public static Inventarisation Load (XmlTextReader reader, Scene scene)
			{
				int uiIndex = -1;
				if (!string.IsNullOrEmpty (reader.GetAttribute ("uiindex"))) {
					uiIndex = int.Parse (reader.GetAttribute ("uiindex"));
				}
				Inventarisation i = new Inventarisation (
					scene,
					int.Parse (reader.GetAttribute ("start")),
					int.Parse (reader.GetAttribute ("end")),
					reader.GetAttribute ("name"),
					reader.GetAttribute ("areaname"),
					int.Parse (reader.GetAttribute ("actionid")),
					uiIndex,
					int.Parse (reader.GetAttribute ("cost"))
				);
				IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
				return i;
			}
		}

		/**
		 * Inventarisation result class, it stores the area map (Data) with result values,
		 * the actionId is used to determine the meaning of the area map values and the
		 * used texture/material.
		 */
		public class InventarisationResult
		{
			/**
			 * Constructor, used to reconstruct the inventarisation results from file
			 */
			public InventarisationResult (int year, string name, string areaName, string dataName, int actionId)
			{
				this.year = year;
				this.name = name;
				this.areaName = areaName;
				this.dataName = dataName;
				this.actionId = actionId;
			}
			
			/**
			 * Constructor can only be used when game is running. It creates a new
			 * inventarisation result using a given year, name, area and data set, actionid.
			 */
			public InventarisationResult (int year, string name, Data area, Data data, int actionId)
			{
				Progression progression = GameControl.self.scene.progression;
				areaName = "_inv" + progression.inventarisations.Count + "area";
				dataName = "_inv" + progression.inventarisations.Count + "data";
				progression.AddData (areaName, area);
				progression.AddData (dataName, data);
				//this.areaName = areaName;
				//this.dataName = dataName;
				this.year = year;
				this.name = name;
				this.actionId = actionId;
			}
			
			public readonly int year;
			public readonly string name;
			public readonly string areaName;
			public readonly string dataName;
			public readonly int actionId;

			private Data dataMap;
			/// <summary>
			/// Gets the data map. This data contains the actual values at the given year.
			/// Used for actual data calculations etc.
			/// </summary>
			/// <value>The data map.</value>
			public Data DataMap {
				get {
					if (dataMap == null) {
						dataMap = EditorCtrl.self.scene.progression.GetData (dataName);
					}
					return dataMap;
				}
			}

			private Data areaMap;
			/// <summary>
			/// Gets the area map. This data contains values used for visualisation.
			/// </summary>
			public Data AreaMap {
				get {
					if (areaMap == null) {
						areaMap = EditorCtrl.self.scene.progression.GetData (areaName);
					}
					return areaMap;
				}
			}

			// Temp vars
			public bool selected;
		}

		public class QuestionnaireState
		{
			public static string XML_ELEMENT = "questionnaire";

			public int id;
			public List<QuestionState> questionStates;
			public int totalScore {
				get {
					int total = 0;
					foreach (QuestionState qs in questionStates) {
						total += qs.score;
					}
					return total;
				}
			}
			public int totalMoneyEarned {
				get {
					int total = 0;
					foreach (QuestionState qs in questionStates) {
						total += qs.moneyGained;
					}
					return total;
				}
			}

			public class QuestionState
			{
				public static string XML_ELEMENT = "qs";

				public readonly int index;
				public string questionName;
				public string questionAnswer;
				public int moneyGained;
				public int score;

				public QuestionState (int index)
				{
					this.index = index;
				}
			}

			public QuestionnaireState (int id)
			{
				this.id = id;
				this.questionStates = new List<QuestionState>();
			}

			public QuestionState GetQuestionState (int index)
			{
				foreach (QuestionState qs in this.questionStates) {
					if (qs.index == index) 
						return qs;
				}
				QuestionState newQs = new QuestionState (index);
				this.questionStates.Add (newQs);
				return newQs;
			}

			public void Reset ()
			{
				this.questionStates = new List<QuestionState>();
			}
		}

		public class ReportState
		{
			public static string XML_ELEMENT = "report";
			
			public int id;
			public string name;
			public string number;
			public List<ParagraphState> paragraphStates;
			
			public class ParagraphState
			{
				public static string XML_ELEMENT = "ps";
				public string body;

				public ParagraphState ()
				{
					this.body = "Write your answer here...";
				}
			}
			
			public ReportState (int id)
			{
				this.id = id;
				this.name = "Write your name here...";
				this.number = "Write your number here...";
				this.paragraphStates = new List<ParagraphState>();
			}
		}

		/**
		 * Stores a message/article/report. Messages can be derived from articles using id
		 * or created on the fly using a text string. The text stored in the Message class
		 * is already processed for expressions.
		 */
		public class Message
		{
			public Message (int id, string text)
			{
				this.id = id;
				this.text = text;
			}

			public readonly int id;
			public readonly string text;
		}

		/**
		 * Stores the data of a variable.
		 */ 
		public class VariableData
		{
			public bool enabled;
			public bool doSave;
			public string variable;
			public string name;
			public string category;

			public VariableData (string variable, string name, string category)
			{
				this.enabled = true;
				this.doSave = true;
				this.variable = variable;
				this.name = name;
				this.category = category;
			}
		}

		/**
		 * Formula the data of a variable.
		 */ 
		public class FormulaData
		{
			public bool enabled;
			public bool doSave;

			public string name;
			public string body;
			public string category;

			/*private string[] _bodyLines = null;
			public string[] bodyLines {
				get {
					if (_bodyLines == null) {
						if (body.IndexOf ('\n') >= 0) {
							_bodyLines = body.Split ('\n');
						} else {
							_bodyLines = new string[] { body };
						}
					}
					return _bodyLines;
				}
			}*/

			public bool opened = false;
			
			public FormulaData (string name, string category, string body)
			{
				this.enabled = true;
				this.doSave = true;
				this.name = name;
				this.body = body;
				this.category = category;
			}

			public FormulaData Copy ()
			{
				FormulaData copy = new FormulaData (name, category, body);
				copy.enabled = enabled;
				copy.doSave = doSave;
				copy.opened = opened;
				return copy;
			}
		}
		
		/**
		 * index in message queue of first unread message (if all messages are read
		 * messageUnreadIndex will equal the length of the queue)
		 */
		private int messageUnreadIndex = 0;
		private int messageIndex = 0; // message currently looking at...
		public List<Message> messages;
		public List<Message> reports;
		public List<QuestionnaireState> questionnaireStates;
		public List<ReportState> reportStates;
		public List<Inventarisation> activeInventarisations;
		public List<InventarisationResult> inventarisations;
		public List<ResearchPoint> researchPoints;
		public ManagedDictionary<string, object> variables;
		public Dictionary<string, VariableData> variablesData;
		public List<FormulaData> formulasData;
		public bool showVariablesInGame;
		public List<ActionTaken> actionsTaken;
		public List<VariableYearBudget> variableYearBudgets;
		Dictionary<string, DataInfo> dataDict;
		List<ActionState> actionStates;

		/// <summary>
		/// Gets questionnaire state by id. Creates a new instance if there's none.
		/// </summary>
		public QuestionnaireState GetQuestionnaireState (int id)
		{
			return GetQuestionnaireState (id, true);
		}

		public QuestionnaireState[] GetQuestionnaireStates (int id)
		{
			List<QuestionnaireState> list = new List<QuestionnaireState> ();
			foreach (QuestionnaireState qs in this.questionnaireStates) {
				if (qs.id == id)
					list.Add (qs);
			}
			return list.ToArray ();
		}

		public QuestionnaireState GetQuestionnaireState (int id, bool createNewIfNull)
		{
			foreach (QuestionnaireState qs in this.questionnaireStates) {
				if (qs.id == id) 
					return qs;
			}
			if (createNewIfNull) {
				QuestionnaireState newQs = new QuestionnaireState (id);
				this.questionnaireStates.Add (newQs);
				return newQs;
			}
			return null;
		}

		/// <summary>
		/// Gets report state by id. Creates a new instance if there's none.
		/// </summary>
		public ReportState GetReportState (int id)
		{
			return GetReportState (id, true);
		}

		public ReportState GetReportState (int id,  bool createNewIfNull)
		{
			foreach (ReportState rs in this.reportStates) {
				if (rs.id == id)
					return rs;
			}
			if (createNewIfNull) {
				ReportState newRs = new ReportState (id);
				this.reportStates.Add (newRs);
				return newRs;	
			}
			return null;
		}

		public void AddActionTaken (int id) {
			// Find the container
			ActionTaken at = actionsTaken.Find (a => a.id == id);
			if (at == null) {
				at = new ActionTaken ();
				at.id = id;
				at.years = new List<int>();
				this.actionsTaken.Add (at);
			}
			// Add the year
			if (!at.years.Contains (year)) {
				at.years.Add (year + 1); // Or else the matching is wrong
			}
		}

		/*public void AddQuestionState (QuestionnaireState.QuestionState qState, int questionnaireId)
		{
			QuestionnaireState qs = GetQuestionnaireState (questionnaireId);
			qs.questionStates.Add (qState);
		}*/

		public void AddMessage (int id)
		{
			Articles.Article article = scene.articles.GetArticleWithId (id);
			if (article != null) {
				Message msg = new Message (id, RenderFontToTexture.SubstituteExpressions (article.text, scene));
				messages.Add (msg);
				ShowArticles.NotifyUnreadMessages ();
			} else {
				Log.LogError (string.Format("Article with id '{0}' could not be found", id));
			}
		}
		
		public void AddMessage (string text)
		{
			Message msg = new Message (-1, RenderFontToTexture.SubstituteExpressions (text, scene));
			messages.Add (msg);
			ShowArticles.NotifyUnreadMessages ();
		}
		
		public void AddReport (string text)
		{
			Message msg = new Message (-1, RenderFontToTexture.SubstituteExpressions (text, scene));
			reports.Add (msg);
		}
		
		public Message CurrentMessage ()
		{
			if (messageIndex < messages.Count) {
				return messages [messageIndex];
			}
			return null;
		}
		
		public bool ToPreviousMessage ()
		{
			if (messageIndex > 0) {
				messageIndex--;
			}
			return (messageIndex > 0);
		}
		
		public bool ToNextMessage ()
		{
			int count = messages.Count;
			if (messageIndex < count) {
				messageIndex++;
			}
			if (messageIndex > messageUnreadIndex) {
				messageUnreadIndex = messageIndex;
			}
			return (messageIndex < count);
		}
		
		public Progression (Scene scene)
		{
			this.scene = scene;
			dataDict = new Dictionary<string, DataInfo> ();
			variables = new ManagedDictionary<string, object> ();
			variablesData = new Dictionary<string, VariableData> ();
			formulasData = new List<FormulaData> ();
			actionStates = new List<ActionState> ();
			questionnaireStates = new List<QuestionnaireState>();
			reportStates = new List<ReportState>();
			actionsTaken = new List<ActionTaken>();
			variableYearBudgets = new List<VariableYearBudget> ();
			messages = new List<Message> ();
			reports = new List<Message> ();
			inventarisations = new List<InventarisationResult> ();
			activeInventarisations = new List<Inventarisation> ();
			researchPoints = new List<ResearchPoint>();
		}
		
		public void CreateBasicData ()
		{
			// Data maps
			AddData (VEGETATION_ID, new VegetationData (scene));
			AddData (HEIGHTMAP_ID, new HeightMap (scene));
			AddData (WATERHEIGHTMAP_ID, new HeightMap (scene));
			AddData (CALCULATED_WATERHEIGHTMAP_ID, new HeightMap (scene));
			AddData (CANAL_ID, new BitMap2 (scene));
			AddData (SUCCESSION_ID, new BitMap1 (scene));
			AddData (MANAGED_ID, new BitMap1 (scene));
			AddData (PURCHASABLE_ID, new BitMap8 (scene));

			vegetation = GetData <VegetationData> (VEGETATION_ID);
			heightMap = GetData <HeightMap> (HEIGHTMAP_ID);
			calculatedWaterHeightMap = GetData <HeightMap> (CALCULATED_WATERHEIGHTMAP_ID);
			waterHeightMap = GetData <HeightMap> (WATERHEIGHTMAP_ID);
			canals = GetData <BitMap2> (CANAL_ID);
			successionArea = GetData <BitMap1> (SUCCESSION_ID);
			managedArea = GetData <BitMap1> (MANAGED_ID);
			purchasableArea = GetData <BitMap8> (PURCHASABLE_ID);

			// Default variables
			foreach (string s in predefinedVariables) {
				if (!variables.ContainsKey (s)) {
					if (s.EndsWith ("Count")) {
						variables.Add (s, 0);
					} else {
						variables.Add (s, "");
					}
				}
			}
		}
		
		/**
		 * Returns a list of all data keys
		 */
		public List<string> GetAllDataNames ()
		{
			return GetAllDataNames (true);
		}

		/**
		 * Returns a list of all data keys
		 */
		public List<string> GetAllDataNames (bool includeExternal)
		{
			List<string> keys = new List<string> (dataDict.Keys);
			if (!includeExternal) {
				for (int i = keys.Count - 1; i >= 0; i--) {
					if (keys[i].StartsWith ("_"))
						keys.RemoveAt (i);
				}
			}
			return keys;
		}
		
		/**
		 * Adds a new data type, if data type with name already existed, it is overwritten.
		 */
		public void AddData (string name, Data data)
		{
			if (dataDict.ContainsKey (name)) {
				dataDict.Remove (name);
			}
			DataInfo info = new DataInfo ();
			info.name = name;
			info.year = (year < startYear) ? 0 : year;
			info.data = data;
			data.hasChanged = true; // force to be saved the first time...
			dataDict.Add (name, info);
		}
		
		/**
		 * true if progression has data with name name
		 */
		public bool HasData (string name)
		{
			return dataDict.ContainsKey (name);
		}

		/**
		 * Returns a data type with given name, if necessary the data is loaded in from disk
		 */
		public Data GetData (string name)
		{
			DataInfo result;
			if (dataDict.TryGetValue (name, out result)) {
				// we have data defined in this progression with name
				if (result.data == null) {
					// actual data isn't yet loaded in, do it now...
					if (File.Exists (GetDataPath (result.name, result.year))) {
						result.data = Data.Load (GetDataPath (result.name, result.year), this);
					} else {
						Log.LogError ("Data not found for '" + name + "' using dummy data.");
						result.data = new DummyData (scene);
					}
				}
				return result.data;
			} else {
				// we don't have data defined in progression, if we're the initial progression
				// we have a hack here that if there _is_ a data file, we just add it to the
				// initial progression, this way it's easier to import old ecosim scenes
				if (year < startYear) {
					// we're initial progress...
					// try to make right for missing data type...
					result = new DataInfo ();
					result.name = name;
					result.year = 0;
					if (File.Exists (GetDataPath (name, 0))) {
						// we found a matching data file, we're gonna use it!
						result.data = Data.Load (GetDataPath (name, 0), this);
						Log.LogWarning ("Added found data '" + name + "' to initial progress");
					} else {
						result.data = new DummyData (scene);
						Log.LogWarning ("Added empty data for '" + name + "' to initial progress");
					}
					dataDict.Add (name, result);
					return result.data;
				}
				Log.LogError ("Can't find Data with name '" + name + "'");
				return new DummyData (scene);
			}
		}

		// (Temp) save the data's from other years
		Dictionary<string, Dictionary<int, Data>> tempDataDict = new Dictionary<string, Dictionary<int, Data>>();

		public Data GetData (string name, int year)
		{
			if (year == scene.progression.year) {
				return GetData (name);
			}
			else {
				// Exception time! If year equals the start year we subtract one so 
				// we have the non _year.dat version (see GetDataPath (name, year))
				if (year <= startYear) year--;

				// Check if we already have loaded it
				Dictionary<int, Data> dict;
				if (tempDataDict.TryGetValue (name, out dict)) {
					Data data;
					if (dict.TryGetValue (year, out data)) {
						return data;
					}
				}

				// Data does not exist, so load the data from scene/save
				if (File.Exists (GetDataPath (name, year))) {
					Data data = Data.Load (GetDataPath (name, year), this);
					Dictionary<int, Data> yearDict;
					if (tempDataDict.TryGetValue (name, out yearDict)) {
						yearDict.Add (year, data);
						return data;
					} else {
						yearDict = new Dictionary<int, Data> ();
						yearDict.Add (year, data);
						tempDataDict.Add (name, yearDict);
						return data;
					}
				}
				return null;
			}
		}
		
		public T GetData<T> (string name) where T : Data
		{
			Data data = GetData (name);
			if (data is T) {
				return (T)data;
			}
			T newData = (T)System.Activator.CreateInstance (typeof(T), scene);
			int min = newData.GetMin ();
			int max = newData.GetMax ();
			for (int y = 0; y < data.height; y++) {
				for (int x = 0; x < data.width; x++) {
					int val = data.Get (x, y);
					val = (val < min) ? min : ((val > max) ? max : val);
					newData.Set (x, y, val);
				}
			}
			Log.LogWarning ("Converting " + data.GetType ().ToString () + " to " + newData.GetType ().ToString ());
			if (dataDict.ContainsKey (name)) {
				dataDict [name].data = newData;
			} else {
				Log.LogWarning ("Creating new data '" + name + "' for year " + year);
				DataInfo newDataInfo = new DataInfo ();
				newDataInfo.data = newData;
				newDataInfo.name = name;
				newDataInfo.year = year;
				dataDict.Add (name, newDataInfo);
			}
			return newData;
		}

		public Data GetTargetArea (int targetArea)
		{
			return GetData (Progression.TARGET_ID + targetArea.ToString());
		}

		/**
		 * Helper method: Finds the data name of the given plant name and returns the data, if necessary the data is loaded in from disk
		 */ 
		public Data GetPlantData  (string name)
		{
			name = name.ToLower();
			foreach (PlantType t in scene.plantTypes)
			{
				if (t.name.ToLower() == name) {
					return GetData (t.dataName);
				}
			}
			return null;
		}
		
		/**
		 * Deletes data with given name, if data doesn't exist, does nothing
		 */
		public void DeleteData (string dataName)
		{
			if (dataDict.ContainsKey (dataName)) {
				dataDict.Remove (dataName);
			}
		}
		
		public ConversionAction conversionHandler = null;
		public WaterAction waterHandler = null;

		public float ConvertToFloat (string dataName, int val)
		{
			if (conversionHandler != null) {
				return conversionHandler.ConvertToFloat (dataName, val);
			}
			return (float)val;
		}
		
		public string ConvertToString (string dataName, int val)
		{
			if (conversionHandler != null) {
				return conversionHandler.ConvertToString (dataName, val);
			}
			return val.ToString ();
		}

		object ReadType (XmlTextReader reader, string type)
		{
			object result = null;
			if (type == "int") {
				result = int.Parse (reader.GetAttribute ("value"));
			} else if (type == "long") {
				result = long.Parse (reader.GetAttribute ("value"));
			} else if (type == "float") {
				result = float.Parse (reader.GetAttribute ("value"));
			} else if (type == "string") {
				result = reader.GetAttribute ("value");
			} else if (type == "bool") {
				result = (reader.GetAttribute ("value") == "true");
			} else if (type == "coord") {
				string[] split = reader.GetAttribute ("value").Split (',');
				result = new Coordinate (int.Parse (split [0]), int.Parse (split [1]));
			} else if (type == "int[]") {
				int i = 0;
				List<int> l = new List<int> ();
				string v;
				while ((v = reader.GetAttribute ("value" + (i++))) != null) {
					l.Add (int.Parse (v));
				}
				result = l;
			} else if (type == "long[]") {
				int i = 0;
				List<long> l = new List<long> ();
				string v;
				while ((v = reader.GetAttribute ("value" + (i++))) != null) {
					l.Add (long.Parse (v));
				}
				result = l;
			} else if (type == "float[]") {
				int i = 0;
				List<float> l = new List<float> ();
				string v;
				while ((v = reader.GetAttribute ("value" + (i++))) != null) {
					l.Add (float.Parse (v));
				}
				return l;
			} else if (type == "string[]") {
				int i = 0;
				List<string> l = new List<string> ();
				string v;
				while ((v = reader.GetAttribute ("value" + (i++))) != null) {
					l.Add (v);
				}
				result = l;
			} else if (type == "bool[]") {
				int i = 0;
				List<bool> l = new List<bool> ();
				string v;
				while ((v = reader.GetAttribute ("value" + (i++))) != null) {
					l.Add (v == "true");
				}
				result = l;
			} else if (type == "coord[]") {
				int i = 0;
				List<Coordinate> l = new List<Coordinate> ();
				string v;
				while ((v = reader.GetAttribute ("value" + (i++))) != null) {
					string[] split = v.Split (',');
					l.Add (new Coordinate (int.Parse (split [0]), int.Parse (split [1])));
				}
				result = l;
			} else {
				throw new EcoException ("unknown type '" + type + "'");
			}
			IOUtil.ReadUntilEndElement (reader, "variable");
			return result;
		}

		void LoadQuestionnaireState (XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			QuestionnaireState qs = new QuestionnaireState (id);
			qs.questionStates = new List<QuestionnaireState.QuestionState> ();

			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == QuestionnaireState.QuestionState.XML_ELEMENT)) 
					{
						int index = int.Parse (reader.GetAttribute ("index"));
						QuestionnaireState.QuestionState questionState = new QuestionnaireState.QuestionState (index);
						questionState.moneyGained = int.Parse (reader.GetAttribute ("money"));
						questionState.score = int.Parse (reader.GetAttribute ("score"));
						questionState.questionAnswer = reader.GetAttribute ("qanswer");
						questionState.questionName = reader.GetAttribute ("qname");
						qs.questionStates.Add (questionState);
						IOUtil.ReadUntilEndElement (reader, QuestionnaireState.QuestionState.XML_ELEMENT);
					} 
					else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == QuestionnaireState.XML_ELEMENT)) {
						break;
					}
				}
			}

			this.questionnaireStates.Add (qs);
		}

		void LoadReportState (XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			ReportState rs = new ReportState (id);
			rs.name = reader.GetAttribute ("name");
			rs.number = reader.GetAttribute ("number");
			
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == ReportState.ParagraphState.XML_ELEMENT)) 
					{
						ReportState.ParagraphState ps = new ReportState.ParagraphState();
						ps.body = reader.GetAttribute ("body");
						rs.paragraphStates.Add (ps);
						IOUtil.ReadUntilEndElement (reader, ReportState.ParagraphState.XML_ELEMENT);
					} 
					else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == ReportState.XML_ELEMENT)) {
						break;
					}
				}
			}
			
			this.reportStates.Add (rs);
		}

		void LoadActionState (XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			bool enabled = reader.GetAttribute ("enabled") != "false";
			ActionState actionState = new ActionState ();
			actionState.actionId = id;
			actionState.isEnabled = enabled;
			
			Dictionary<string, string> properties = new Dictionary<string, string> ();
			
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "property")) {
						string key = reader.GetAttribute ("key");
						string val = reader.GetAttribute ("value");
						properties.Add (key, val);
						IOUtil.ReadUntilEndElement (reader, "property");
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == "action")) {
						break;
					}
				}
			}
			actionState.properties = properties;
			actionStates.Add (actionState);
		}
		
		void Load (XmlTextReader reader)
		{
			dataDict = new Dictionary<string, DataInfo> ();
			startYear = int.Parse (reader.GetAttribute ("startyear"));
			if (!string.IsNullOrEmpty (reader.GetAttribute ("yearsperturn"))) {
				yearsPerTurn = int.Parse (reader.GetAttribute ("yearsperturn"));
			}
			budget = long.Parse (reader.GetAttribute ("budget"));
			if (!string.IsNullOrEmpty (reader.GetAttribute ("yearbudget"))) {
				yearBudget = int.Parse (reader.GetAttribute ("yearbudget"));
			}
			if (!string.IsNullOrEmpty (reader.GetAttribute ("showvarsingame"))) {
				showVariablesInGame = bool.Parse (reader.GetAttribute ("showvarsingame"));
			}
			if (reader.GetAttribute ("allowresearch") == "false")
				allowResearch = false;
			if (reader.GetAttribute ("allowmeasures") == "false")
				allowMeasures = false;
			string gameEndedStr = reader.GetAttribute ("gameended");
			gameEnded = ((gameEndedStr != null) && (gameEndedStr == "true"));
			if (!string.IsNullOrEmpty (reader.GetAttribute ("targetareas")))
				targetAreas = int.Parse (reader.GetAttribute ("targetareas"));

			string msgindexstr = reader.GetAttribute ("messageindex");
			if (msgindexstr != null) {
				messageUnreadIndex = int.Parse (msgindexstr);
			}

			// Used for the action object groups
			List<Buildings.Building> allBuildings = scene.buildings.GetAllBuildings ();

			/**
			 * After ReadElementContentAsString the reader has already read in next
			 * element, so we must not do reader.Read here then. We set
			 * skipReadHack to true after using ReadElementContentAsString so
			 * the while loop will still be executed, but without doing a Read.
			 */
			bool skipReadHack = false;
			if (!reader.IsEmptyElement) {
				while (skipReadHack || reader.Read()) {
					skipReadHack = false;
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "message")) {
						int id = int.Parse (reader.GetAttribute ("id"));
						string text = reader.ReadElementContentAsString ();
						Message msg = new Message (id, text);
						messages.Add (msg);
						skipReadHack = true;
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "report")) {
						int id = int.Parse (reader.GetAttribute ("id"));
						string text = reader.ReadElementContentAsString ();
						Message msg = new Message (id, text);
						reports.Add (msg);
						skipReadHack = true;
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "invresults")) {
						int year = int.Parse (reader.GetAttribute ("year"));
						string name = reader.GetAttribute ("name");
						int actionId = int.Parse (reader.GetAttribute ("actionid"));
						string areaName = reader.GetAttribute ("areaname");
						string dataName = reader.GetAttribute ("dataname");
						InventarisationResult ir = new InventarisationResult (year, name, areaName, dataName, actionId);
						inventarisations.Add (ir);
						IOUtil.ReadUntilEndElement (reader, "invresults");
					} 
					else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == Inventarisation.XML_ELEMENT)) 
					{
						this.activeInventarisations.Add (Inventarisation.Load (reader, scene));
					} 
					else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "takenaction"))
					{
						ActionTaken at = new ActionTaken ();
						at.id = int.Parse (reader.GetAttribute ("id"));
						at.years = new List<int> ();
						string years = reader.GetAttribute ("years");
						if (years.Length > 0) {
							string [] ys = years.Split (new string[] {","}, System.StringSplitOptions.RemoveEmptyEntries);
							if (ys != null) {
								foreach (string y in ys) {
									at.years.Add (int.Parse (y));
								}
							}
						}
						this.actionsTaken.Add (at);
						IOUtil.ReadUntilEndElement (reader, "invresults");
					}
					else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "varyearbudget"))
					{
						VariableYearBudget yb = new VariableYearBudget ();
						yb.year = int.Parse (reader.GetAttribute ("y"));
						yb.budget = int.Parse (reader.GetAttribute ("b"));
						this.variableYearBudgets.Add (yb);
						IOUtil.ReadUntilEndElement (reader, "varyearbudget");
					}
					else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "data")) {
						DataInfo info = new DataInfo ();
						info.name = reader.GetAttribute ("name");
						int year = int.Parse (reader.GetAttribute ("year"));
						if (year < startYear)
							year = 0;
						info.year = year;
						dataDict.Add (info.name, info);
						IOUtil.ReadUntilEndElement (reader, "data");
					} 
					else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "variable")) {
						string name = reader.GetAttribute ("name");
						string type = reader.GetAttribute ("type");
						object var = ReadType (reader, type);

						if (!variables.ContainsKey (name)) {
							variables.Add (name, var);
						} else variables[name] = var;
					} 
					else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "variabledata")) 
					{
						string var = reader.GetAttribute ("var");
						string name = reader.GetAttribute ("name");
						string category = reader.GetAttribute ("cat");

						VariableData vd = new VariableData (var, name, category);
						if (!string.IsNullOrEmpty (reader.GetAttribute ("enabled"))) {
							vd.enabled = bool.Parse (reader.GetAttribute ("enabled"));
						}

						if (!variablesData.ContainsKey (var)) {
							variablesData.Add (var, vd);
						}
						variablesData [var] = vd;
					}
					else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "formuladata")) 
					{
						string name = reader.GetAttribute ("name");
						string cat = reader.GetAttribute ("cat");
						string body = reader.GetAttribute ("body");
						bool enabled = bool.Parse (reader.GetAttribute ("enabled"));
						FormulaData fd = new FormulaData (name, cat, body);
						fd.enabled = enabled;
						formulasData.Add (fd);
					} 
					else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "action")) {
						LoadActionState (reader);
					} 
					else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == QuestionnaireState.XML_ELEMENT)) {
						LoadQuestionnaireState (reader);
					}
					else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "actionobjectgroup")) 
					{
						string name = reader.GetAttribute ("name");

						// Get the correct action group
						ActionObjectsGroup objGroup = null;
						foreach (ActionObjectsGroup gr in scene.actionObjectGroups) {
							if (gr.name == name) {
								objGroup = gr;
								break;
							}
						} 

						// Check the group type
						if (objGroup != null)
						{
							ActionObjectsGroup.GroupType groupType = (ActionObjectsGroup.GroupType)System.Enum.Parse(typeof(ActionObjectsGroup.GroupType), reader.GetAttribute("grouptype"));
							switch (groupType)
							{
							case ActionObjectsGroup.GroupType.Combined :
								// We should parse the enabled state of the action object group
								objGroup.enabled = (reader.GetAttribute ("enabled") == "true") ? true : false;
								if (objGroup.enabled)
								{
									// Find all buildings and mark them as active
									foreach (ActionObject obj in objGroup.actionObjects) 
									{
										foreach (Buildings.Building b in allBuildings) 
										{
											// Found a match, mark as active and remove it from 
											// the list to increase performance
											if (obj.buildingId == b.id) 
											{
												b.isActive = true;
												allBuildings.Remove (b);
												break;
											}
										}
									}
								}
								break;

							case ActionObjectsGroup.GroupType.Collection : 
								//  Get the individual enabled states of all objects in the group
								int objIdx = 0;
								while (reader.Read()) 
								{
									if ((reader.NodeType == XmlNodeType.Element) && reader.Name.ToLower() == "aobj") {
										if (objIdx < objGroup.actionObjects.Length) 
										{
											bool enabled = (reader.GetAttribute ("enabled") == "true") ? true : false;
											objGroup.actionObjects[objIdx].enabled = enabled;

											// Find all buildings and mark them as active
											if (enabled)
											{
												foreach (Buildings.Building b in allBuildings) 
												{
													// Found a match, mark as active and remove it from 
													// the list to increase performance
													if (objGroup.actionObjects[objIdx].buildingId == b.id) 
													{
														b.isActive = true;
														allBuildings.Remove (b);
														break;
													}
												}
											}
										}
										IOUtil.ReadUntilEndElement (reader, "aobj");
										objIdx++;
									} else if ((reader.NodeType == XmlNodeType.EndElement) && reader.Name.ToLower() == "actionobjectgroup") {
										break;
									}
								}
								break;
							}
						}
					} 
					else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == ResearchPoint.XML_ELEMENT)) {
						ResearchPoint rp = ResearchPoint.Load (reader, scene);
						researchPoints.Add (rp);
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == "progress")) {
						break;
					}
				}
			}

			// Add the predefined variables if necessary
			foreach (string v in predefinedVariables) {
				if (!variables.ContainsKey (v)) {
					if (v.Contains ("Count")) {
						variables.Add (v, 0);
					} else {
						variables.Add (v, "");
					}
				}
			}
		}
		
		string GetPath (int year)
		{
			if ((year < startYear) || (scene.slotNr == -1)) {
				// we load the start progression state
				// that is stored in the scene folder
				return GameSettings.GetPathForScene (scene.sceneName) + "initialprogress.xml";
			} else {
				// we load actual progress for given year
				return GameSettings.GetPathForSlotNr (scene.slotNr) + "progress" + year + ".xml";
			}
		}
		
		string GetDataPath (string name, int year)
		{
			if (year < startYear) {
				return GameSettings.GetPathForScene (scene.sceneName) + name.ToLower () + ".dat";
			} else {
				return GameSettings.GetPathForSlotNr (scene.slotNr) + name.ToLower () + "_" + year + ".dat";
			}
		}
		
		public static Progression Load (Scene scene, int year)
		{
			Progression progression = new Progression (scene);
			string path = progression.GetPath (year);
			if (!File.Exists (path))
				return null; // not found
			XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (path));
			try {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "progress")) {
						progression.Load (reader);
					}
				}
			} finally {
				reader.Close ();
			}
			if (year >= progression.startYear) {
				progression.year = year;
			}
			progression.messageIndex = progression.messageUnreadIndex;
			if (progression.messageUnreadIndex < progression.messages.Count) {
				ShowArticles.NotifyUnreadMessages ();
			}
			
			progression.vegetation = progression.GetData <VegetationData> (VEGETATION_ID);
			progression.heightMap = progression.GetData <HeightMap> (HEIGHTMAP_ID);
			progression.waterHeightMap = progression.GetData <HeightMap> (WATERHEIGHTMAP_ID);
			progression.calculatedWaterHeightMap = progression.GetData <HeightMap> (CALCULATED_WATERHEIGHTMAP_ID);
			progression.canals = progression.GetData <BitMap2> (CANAL_ID);
			progression.successionArea = progression.GetData <BitMap1> (SUCCESSION_ID);
			progression.managedArea = progression.GetData <BitMap1> (MANAGED_ID);
			progression.purchasableArea = progression.GetData <BitMap8> (PURCHASABLE_ID);
			return progression;
		}
		
		/**
		 * Advance to next year, store the current progression to the save game folder
		 */
		public void Advance ()
		{
			year ++;
			foreach (DataInfo d in dataDict.Values) {
				if ((d.data != null) && (d.data.hasChanged)) {
					// if data has changed we must save it...
					d.year = year;
					d.data.Save (GetDataPath (d.name, year), this);
					d.data.hasChanged = false;
				}
			}
			Save ();
			SaveGame saveGame = new SaveGame (scene);
			saveGame.Save (scene);
		}
		
		public void Save ()
		{
			Save (year);
		}
				
		public void Save (int year)
		{
			if (year < startYear) {
				// if year is earlier than start year, it's the initial progression file
				// we save the progression in the scenario folder (GetPath handles this)
				// and also save the data set here....
				foreach (DataInfo info in dataDict.Values) {
					if (!info.isInternal) {
						if (info.data == null) {
							Log.LogError ("data '" + info.name + "' not loaded, so will not be written away.");
						} else if (!(info.data is DummyData)) {
							info.data.Save (GetDataPath (info.name, 0), this);
						}
					}
				}
			}
			string path = GetPath (year);
			XmlTextWriter writer = new XmlTextWriter (path, System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement ("progress");
			writer.WriteAttributeString ("budget", budget.ToString ());
			writer.WriteAttributeString ("yearbudget", yearBudget.ToString ());
			writer.WriteAttributeString ("startyear", startYear.ToString ());
			writer.WriteAttributeString ("yearsperturn", yearsPerTurn.ToString ());
			writer.WriteAttributeString ("allowresearch", allowResearch ? "true" : "false");
			writer.WriteAttributeString ("allowmeasures", allowMeasures ? "true" : "false");
			writer.WriteAttributeString ("gameended", gameEnded ? "true" : "false");
			writer.WriteAttributeString ("messageindex", messageUnreadIndex.ToString ());
			writer.WriteAttributeString ("showvarsingame", showVariablesInGame.ToString ().ToLower ());
			writer.WriteAttributeString ("targetareas", targetAreas.ToString());

			// We write out the data dictionary, the data itself is saved seperately
			foreach (DataInfo info in dataDict.Values) 
			{
				if (!info.isInternal)
				{
					writer.WriteStartElement ("data");
					writer.WriteAttributeString ("name", info.name);
					writer.WriteAttributeString ("year", info.year.ToString ());
					writer.WriteEndElement ();
				}
			}
			
			// write progression variables... probably there exists an easier
			// way, but as long as this works, why bother....
			foreach (KeyValuePair <string, object> kv in variables) {
				writer.WriteStartElement ("variable");
				writer.WriteAttributeString ("name", kv.Key);
				object obj = kv.Value;
				if (obj is string) {
					writer.WriteAttributeString ("type", "string");
					writer.WriteAttributeString ("value", (string)obj);
				} else if (obj is List<string>) {
					writer.WriteAttributeString ("type", "string[]");
					List<string> v = (List<string>)obj;
					for (int i = 0; i < v.Count; i++) {
						writer.WriteAttributeString ("value" + i, v [i]);
					}
				} else if (obj is int) {
					writer.WriteAttributeString ("type", "int");
					writer.WriteAttributeString ("value", ((int)obj).ToString ());
				} else if (obj is List<int>) {
					writer.WriteAttributeString ("type", "int[]");
					List<int> v = (List<int>)obj;
					for (int i = 0; i < v.Count; i++) {
						writer.WriteAttributeString ("value" + i, v [i].ToString ());
					}
				} else if (obj is long) {
					writer.WriteAttributeString ("type", "long");
					writer.WriteAttributeString ("value", ((long)obj).ToString ());
				} else if (obj is List<long>) {
					writer.WriteAttributeString ("type", "long[]");
					List<long> v = (List<long>)obj;
					for (int i = 0; i < v.Count; i++) {
						writer.WriteAttributeString ("value" + i, v [i].ToString ());
					}
				} else if (obj is float) {
					writer.WriteAttributeString ("type", "float");
					writer.WriteAttributeString ("value", ((float)obj).ToString ());
				} else if (obj is List<float>) {
					writer.WriteAttributeString ("type", "float[]");
					List<float> v = (List<float>)obj;
					for (int i = 0; i < v.Count; i++) {
						writer.WriteAttributeString ("value" + i, v [i].ToString ());
					}
				} else if (obj is bool) {
					writer.WriteAttributeString ("type", "bool");
					writer.WriteAttributeString ("value", ((bool)obj) ? "true" : "false");
				} else if (obj is List<bool>) {
					writer.WriteAttributeString ("type", "bool[]");
					List<bool> v = (List<bool>)obj;
					for (int i = 0; i < v.Count; i++) {
						writer.WriteAttributeString ("value" + i, v [i] ? "true" : "false");
					}
				} else if (obj is Coordinate) {
					writer.WriteAttributeString ("type", "coord");
					writer.WriteAttributeString ("value", ((Coordinate)obj).x.ToString () + "," + ((Coordinate)obj).y.ToString ());
				} else if (obj is List<Coordinate>) {
					writer.WriteAttributeString ("type", "coord[]");
					List<Coordinate> v = (List<Coordinate>)obj;
					for (int i = 0; i < v.Count; i++) {
						writer.WriteAttributeString ("value" + i, v [i].x.ToString () + "," + v [i].y.ToString ());
					}
				}

				writer.WriteEndElement ();				
			}

			// Write variables data
			foreach (KeyValuePair <string, VariableData> kv in variablesData) 
			{
				if (!kv.Value.doSave) continue;
				writer.WriteStartElement ("variabledata");
				writer.WriteAttributeString ("var", kv.Key);
				writer.WriteAttributeString ("name", kv.Value.name);
				writer.WriteAttributeString ("cat", kv.Value.category);
				writer.WriteAttributeString ("enabled", kv.Value.enabled.ToString ().ToLower ());
				writer.WriteEndElement ();			
			}

			// Write formulas data
			foreach (FormulaData fd in formulasData) 
			{
				if (!fd.doSave) continue;
				writer.WriteStartElement ("formuladata");
				writer.WriteAttributeString ("name", fd.name);
				writer.WriteAttributeString ("cat", fd.category);
				writer.WriteAttributeString ("body", fd.body);
				writer.WriteAttributeString ("enabled", fd.enabled.ToString ().ToLower ());
				writer.WriteEndElement ();	
			}
			
			// write action states
			foreach (BasicAction action in scene.actions.EnumerateActions()) {
				Dictionary<string, string> properties = action.SaveProgress ();
				writer.WriteStartElement ("action");
				writer.WriteAttributeString ("id", action.id.ToString ());
				writer.WriteAttributeString ("enabled", action.isActive ? "true" : "false");
				if (properties != null) {
					foreach (KeyValuePair<string, string> propkv in properties) {
						writer.WriteStartElement ("property");
						writer.WriteAttributeString ("key", propkv.Key);
						writer.WriteAttributeString ("value", propkv.Value);
						writer.WriteEndElement ();
					}
				}
				writer.WriteEndElement ();
			}
			
			foreach (Message msg in messages) {
				writer.WriteStartElement ("message");
				writer.WriteAttributeString ("id", msg.id.ToString ());
				writer.WriteCData (msg.text);
				writer.WriteEndElement ();
			}
			foreach (Message msg in reports) {
				writer.WriteStartElement ("report");
				writer.WriteAttributeString ("id", msg.id.ToString ());
				writer.WriteCData (msg.text);
				writer.WriteEndElement ();
			}
			
			foreach (InventarisationResult ir in inventarisations) {
				writer.WriteStartElement ("invresults");
				writer.WriteAttributeString ("year", ir.year.ToString ());
				writer.WriteAttributeString ("name", ir.name);
				writer.WriteAttributeString ("areaname", ir.areaName);
				writer.WriteAttributeString ("dataname", ir.dataName);
				writer.WriteAttributeString ("actionid", ir.actionId.ToString ());
				writer.WriteEndElement ();
			}

			// Save the current current inventarisations
			foreach (Inventarisation inv in activeInventarisations) {
				inv.Save (writer, scene);
			}

			// Questionnaire states
			foreach (QuestionnaireState qs in this.questionnaireStates) {
				writer.WriteStartElement (QuestionnaireState.XML_ELEMENT);
				writer.WriteAttributeString ("id", qs.id.ToString());
				foreach (QuestionnaireState.QuestionState q in qs.questionStates) {
					writer.WriteStartElement (QuestionnaireState.QuestionState.XML_ELEMENT);
					writer.WriteAttributeString ("index", q.index.ToString());
					writer.WriteAttributeString ("money", q.moneyGained.ToString());
					writer.WriteAttributeString ("score", q.score.ToString());
					writer.WriteAttributeString ("qanswer", q.questionAnswer);
					writer.WriteAttributeString ("qname", q.questionName);
					writer.WriteEndElement ();
				}
				writer.WriteEndElement ();
			}

			// Report states
			foreach (ReportState rs in this.reportStates) {
				writer.WriteStartElement (ReportState.XML_ELEMENT);
				writer.WriteAttributeString ("id", rs.id.ToString());
				writer.WriteAttributeString ("name", rs.name);
				writer.WriteAttributeString ("number", rs.number);
				foreach (ReportState.ParagraphState ps in rs.paragraphStates) {
					writer.WriteStartElement (ReportState.ParagraphState.XML_ELEMENT);
					writer.WriteAttributeString ("body", ps.body);
					writer.WriteEndElement ();
				}
				writer.WriteEndElement ();
			}

			// Action object groups
			foreach (ActionObjectsGroup gr in scene.actionObjectGroups) {
				writer.WriteStartElement ("actionobjectgroup");
				writer.WriteAttributeString ("name", gr.name);
				writer.WriteAttributeString ("grouptype", gr.groupType.ToString());

				switch (gr.groupType)
				{
				case ActionObjectsGroup.GroupType.Combined :
					// We save the 'enabled' state of the combined group
					writer.WriteAttributeString ("enabled", gr.enabled.ToString().ToLower());
					break;
				case ActionObjectsGroup.GroupType.Collection : 
					// Save the enabled state of every object seperately
					foreach (ActionObject obj in gr.actionObjects) {
						writer.WriteStartElement ("aobj");
						writer.WriteAttributeString ("enabled", obj.enabled.ToString().ToLower());
						writer.WriteEndElement ();
					}
					break;
				}
				writer.WriteEndElement ();
			}

			// Taken actions
			foreach (ActionTaken at in this.actionsTaken) {
				writer.WriteStartElement ("takenaction");
				writer.WriteAttributeString ("id", at.id.ToString());
				// Save the years in a string (less xml)
				string years = "";
				foreach (int y in at.years) {
					years += y + ",";
				}
				years = years.Trim (',');
				writer.WriteAttributeString ("years", years);
				writer.WriteEndElement ();
			}

			// Research points
			foreach (ResearchPoint rp in researchPoints) {
				rp.Save (writer, scene);
			}

			// Variable year budgets
			foreach (VariableYearBudget yb in this.variableYearBudgets) {
				writer.WriteStartElement ("varyearbudget");
				writer.WriteAttributeString ("y", yb.year.ToString());
				writer.WriteAttributeString ("b", yb.budget.ToString());
				writer.WriteEndElement ();
			}

			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();
		}
		
		/**
		 * Do the delayed initialization of the actions (calling LoadProgress).
		 * initScene == false only when loading a savegame
		 */
		public void InitActions (bool initScene)
		{
			foreach (ActionState actionState in actionStates) {
				BasicAction action = scene.actions.GetAction (actionState.actionId);
				if (action != null) {
					action.isActive = actionState.isEnabled;
					if (year > 0) {
						action.LoadProgress (initScene, actionState.properties);
					}
				} else {
					Log.LogError ("No action with id '" + actionState.actionId + "' found.");
				}
			}
			actionStates = null;
		}
		
		/**
		 * Updates CalculatedWaterHeightMap using WaterAction if available, otherwise
		 * CalculatedWaterHeightMap will just be a copy of WaterHeightMap.
		 */
		public void CalculateWater () {
			if (waterHandler != null) {
				waterHandler.CalculateWater ();
			}
			else {
				calculatedWaterHeightMap.CopyFrom (waterHeightMap);
			}
		}
	}
}