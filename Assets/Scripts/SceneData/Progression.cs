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
		public const string HEIGHTMAP_ID = "heightmap";
		public const string WATERHEIGHTMAP_ID = "waterheightmap";
		public const string CALCULATED_WATERHEIGHTMAP_ID = "calculatedwaterheightmap";
		public const string VEGETATION_ID = "vegetation";
		public const string CANAL_ID = "canals";
		public const string ANIMAL_ID = "animals";
		public const string SUCCESSION_ID = "_succession";
		public const string MANAGED_ID = "_managed";
		public const string PURCHASABLE_ID = "_purchasable";

		public long budget = 0; // budget left
		public int year = 0; // year of this progression
		public int startYear = 2013; // not really progression, but for determining first progression data
		public bool gameEnded = false;
		public bool allowResearch = true;
		public bool allowMeasures = true;
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
		 * Inventarisation result class, it stores the area map (Data) with result values,
		 * the actionId is used to determine the meaning of the area map values and the
		 * used texture/material.
		 */
		public class InventarisationResult
		{
			/**
			 * Constructor, used to reconstruct the inventarisation results from file
			 */
			public InventarisationResult (int year, string name, string areaName, int actionId)
			{
				this.year = year;
				this.name = name;
				this.areaName = areaName;
				this.actionId = actionId;
			}
			
			/**
			 * Constructor can only be used when game is running. It creates a new
			 * inventarisation result using a given year, name, area data set, actionid.
			 */
			public InventarisationResult (int year, string name, Data area, int actionId)
			{
				Progression progression = GameControl.self.scene.progression;
				areaName = "inventarisation" + progression.inventarisations.Count;
				progression.AddData (areaName, area);
				areaMap = area;
				this.year = year;
				this.name = name;
				this.actionId = actionId;
				
			}
			
			public readonly int year;
			public readonly string name;
			public readonly string areaName;
			public readonly int actionId;
			private Data areaMap;
			
			public Data AreaMap {
				get {
					if (areaMap == null) {
						areaMap = GameControl.self.scene.progression.GetData (areaName);
					}
					return areaMap;
				}
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
		 * index in message queue of first unread message (if all messages are read
		 * messageUnreadIndex will equal the length of the queue)
		 */
		private int messageUnreadIndex = 0;
		private int messageIndex = 0; // message currently looking at...
		public List<Message> messages;
		public List<Message> reports;
		public List<InventarisationResult> inventarisations;
		public Dictionary<string, object> variables;
		Dictionary<string, DataInfo> dataDict;
		List<ActionState> actionStates;
		
		public void AddMessage (int id)
		{
			Articles.Article article = scene.articles.GetArticleWithId (id);
			Message msg = new Message (id, RenderFontToTexture.SubstituteExpressions (article.text, scene));
			messages.Add (msg);
			ShowArticles.NotifyUnreadMessages ();
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
			variables = new Dictionary<string, object> ();
			actionStates = new List<ActionState> ();
			messages = new List<Message> ();
			reports = new List<Message> ();
			inventarisations = new List<InventarisationResult> ();
		}
		
		public void CreateBasicData ()
		{
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
			waterHeightMap = GetData <HeightMap> (WATERHEIGHTMAP_ID);
			canals = GetData <BitMap2> (CANAL_ID);
			successionArea = GetData <BitMap1> (SUCCESSION_ID);
			managedArea = GetData <BitMap1> (MANAGED_ID);
			purchasableArea = GetData <BitMap8> (PURCHASABLE_ID);
		}
		
		/**
		 * Returns a list of all data keys
		 */
		public List<string> GetAllDataNames ()
		{
			List<string> keys = new List<string> (dataDict.Keys);
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
			budget = long.Parse (reader.GetAttribute ("budget"));
			if (reader.GetAttribute ("allowresearch") == "false")
				allowResearch = false;
			if (reader.GetAttribute ("allowmeasures") == "false")
				allowMeasures = false;
			string gameEndedStr = reader.GetAttribute ("gameended");
			gameEnded = ((gameEndedStr != null) && (gameEndedStr == "true"));
			
			string msgindexstr = reader.GetAttribute ("messageindex");
			if (msgindexstr != null) {
				messageUnreadIndex = int.Parse (msgindexstr);
			}
			
			
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
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "inventarisation")) {
						int year = int.Parse (reader.GetAttribute ("year"));
						string name = reader.GetAttribute ("name");
						int actionId = int.Parse (reader.GetAttribute ("actionid"));
						string areaName = reader.GetAttribute ("areaname");
						InventarisationResult ir = new InventarisationResult (year, name, areaName, actionId);
						inventarisations.Add (ir);
						IOUtil.ReadUntilEndElement (reader, "inventarisation");
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "data")) {
						DataInfo info = new DataInfo ();
						info.name = reader.GetAttribute ("name");
						int year = int.Parse (reader.GetAttribute ("year"));
						if (year < startYear)
							year = 0;
						info.year = year;
						dataDict.Add (info.name, info);
						IOUtil.ReadUntilEndElement (reader, "data");
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "variable")) {
						string name = reader.GetAttribute ("name");
						string type = reader.GetAttribute ("type");
						object var = ReadType (reader, type);
						variables.Add (name, var);
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "action")) {
						LoadActionState (reader);
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == "progress")) {
						break;
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
//			writer.WriteAttributeString ("year", year.ToString ());
			writer.WriteAttributeString ("startyear", startYear.ToString ());
			writer.WriteAttributeString ("allowresearch", allowResearch ? "true" : "false");
			writer.WriteAttributeString ("allowmeasures", allowMeasures ? "true" : "false");
			writer.WriteAttributeString ("gameended", gameEnded ? "true" : "false");
			writer.WriteAttributeString ("messageindex", messageUnreadIndex.ToString ());
			// we write out the data dictionary, the data itself is saved seperately
			foreach (DataInfo info in dataDict.Values) {
				if (!info.isInternal) {
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
				writer.WriteStartElement ("inventarisation");
				writer.WriteAttributeString ("year", ir.year.ToString ());
				writer.WriteAttributeString ("name", ir.name);
				writer.WriteAttributeString ("areaname", ir.areaName);
				writer.WriteAttributeString ("actionid", ir.actionId.ToString ());
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