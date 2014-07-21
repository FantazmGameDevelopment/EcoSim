using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Text;
using System.Threading;
using Ecosim.SceneData;
using Ecosim.SceneData.Action;
using Ecosim.SceneData.AnimalPopulationModel;

namespace Ecosim.SceneData
{
	public class InventarisationsData
	{
		public class YearData
		{
			public readonly int year;
			public float lowestValue = Mathf.Infinity;
			public float highestValue = -Mathf.Infinity;
			
			private Dictionary<string, float> values;
			
			public YearData (int year)
			{
				this.year = year;
				this.values = new Dictionary<string, float> ();
			}
			
			public void AddValue (string name, float value)
			{
				// Check the lowest and highest values
				if (value < lowestValue) {
					lowestValue = value;
				}
				if (value > highestValue) {
					highestValue = value;
				}
				
				// Choose the highest value if it already exists
				if (values.ContainsKey (name)) {
					float newValue = Mathf.Max (value, values[name]);
					values [name] = newValue;
					return;
				}
				values.Add (name, value);
			}
			
			/// <summary>
			/// Gets the value. Returns true/false whether the name exists, and if so it sets the out value.
			/// </summary>
			public bool GetValue (string name, out float value)
			{
				if (values.ContainsKey (name)) {
					value = values[name];
					return true;
				}
				value = 0f;
				return false;
			}
			
			public override string ToString ()
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
				sb.AppendFormat ("Year {0}\n", year);
				foreach (KeyValuePair<string, float> pair in values) {
					sb.AppendFormat ("{0}:{1}\n", pair.Key, pair.Value);
				}
				sb.AppendFormat ("Min:{0}, Max:{1}\n", lowestValue, highestValue);
				return string.Format ("[YearData] {0}", sb.ToString());
			}
		}

		protected List<string> values;
		protected List<YearData> years;
		
		public InventarisationsData ()
		{
			this.values = new List<string> ();
			this.years = new List<YearData> ();
		}
		
		public IEnumerable<string> EnumerateValues ()
		{
			foreach (string s in this.values) {
				yield return s;
			}
		}
		
		public IEnumerable<YearData> EnumerateYears ()
		{
			foreach (YearData y in this.years) {
				yield return y;
			}
		}
		
		public YearData GetYear (int year)
		{
			return GetYear (year, false);
		}
		
		public YearData GetYear (int year, bool createNewIfNull)
		{
			foreach (YearData y in years) {
				if (y.year == year) 
					return y;
			}
			if (createNewIfNull) {
				YearData y = new YearData (year);
				this.years.Add (y);
				return y;
			}
			return null;
		}
		
		public int GetYearsCount ()
		{
			return this.years.Count;
		}
		
		public float GetLowestValue ()
		{
			float value = Mathf.Infinity;
			foreach (YearData y in this.years) {
				if (y.lowestValue < value) {
					value = y.lowestValue;
				}
			}
			return value;
		}
		
		public float GetHighestValue ()
		{
			float value = -Mathf.Infinity;
			foreach (YearData y in this.years) {
				if (y.highestValue > value) {
					value = y.highestValue;
				}
			}
			return value;
		}
		
		public void AddValue (string name)
		{
			if (!this.values.Contains (name))
			{
				this.values.Add (name);
				this.values.Sort ();
			}
		}
		
		public int GetValuesCount ()
		{
			return this.values.Count;
		}
		
		public override string ToString ()
		{
			string years = "";
			foreach (YearData y in this.years) {
				years += y.ToString () + "\n";
			}
			return string.Format ("[BaseData]\n{0}", years);
		}
	}

	public class ExportData
	{
		public class YearData
		{
			public class CoordinateData
			{
				public readonly Coordinate coord;
				private Dictionary<string, string> values;

				public CoordinateData (Coordinate coord)
				{
					this.coord = coord;
					this.values = new Dictionary<string, string> ();
				}

				public string this[string key]
				{
					get {
						if (values.ContainsKey (key)) {
							return values [key];
						}
						return "";
					}
					set {
						if (!values.ContainsKey (key)) {
							values.Add (key, "");
						}
						values [key] = value;
					}
				}

				public IEnumerable<string> EnumerateKeys ()
				{
					foreach (KeyValuePair<string, string> pair in this.values) {
						yield return pair.Key;
					}
				}
			}

			public readonly int year;
			public List<CoordinateData> coords;

			public YearData (int year)
			{
				this.year = year;
				this.coords = new List<CoordinateData> ();
			}

			public CoordinateData NewCoord (Coordinate coord)
			{
				if (this[coord] == null)
				{
					CoordinateData cd = new CoordinateData (coord);
					this.coords.Add (cd);
					return cd;
				}
				else return this [coord];
			}

			public CoordinateData this[Coordinate coord]
			{
				get 
				{
					foreach (CoordinateData cd in this.coords) {
						if (cd.coord.x == coord.x &&
						    cd.coord.y == coord.y) {
							return cd;
						}
					}
					return null;
				}
			}
			
			public IEnumerable<CoordinateData> EnumerateCoords ()
			{
				foreach (CoordinateData cd in this.coords) {
					yield return cd;
				}
			}
		}

		public List<string> columns;
		public List<YearData> years;

		public ExportData ()
		{
			this.columns = new List<string> ();
			this.years = new List<YearData> ();
		}

		public YearData NewYear (int year)
		{
			if (this[year] == null)
			{
				YearData ny = new YearData (year);
				this.years.Add (ny);
				return ny;
			}
			else return this [year];
		}

		public YearData this[int year]
		{
			get 
			{
				foreach (YearData y in this.years) {
					if (y.year == year) {
						return y;
					}
				}
				return null;
			}
		}

		public void SortYears ()
		{
			this.years.Sort (
			delegate (YearData x, YearData y) {
				if (x.year > y.year) return 1;
				if (x.year < y.year) return -1;
				return 0;
			});
		}

		public IEnumerable<string> EnumerateColumns ()
		{
			foreach (string c in this.columns) {
				yield return c;
			}
		}

		public IEnumerable<YearData> EnumerateYears ()
		{
			foreach (YearData y in this.years) {
				yield return y;
			}
		}

		public void AddColumn (string column)
		{
			if (!this.columns.Contains (column)) {
				this.columns.Add (column);
			}
		}

		public bool HasColumn (string column) {
			return this.columns.Contains (column);
		}

		public string ToCSV ()
		{
			string delimiter = ";";
			StringBuilder sb = new StringBuilder ();

			// Add all columns
			foreach (string c in this.EnumerateColumns ())
			{
				sb.Append (c);
				sb.Append (delimiter);
			}
			sb.AppendLine ();

			// Loop through all years
			foreach (ExportData.YearData y in EnumerateYears ())
			{
				foreach (ExportData.YearData.CoordinateData c in y.EnumerateCoords ())
				{
					// Add year
					sb.Append (y.year);
					sb.Append (delimiter);

					// Add x,y
					sb.Append (c.coord.x);
					sb.Append (delimiter);
					sb.Append (c.coord.y);
					sb.Append (delimiter);

					// Loop through all columns
					int si = 0;
					foreach (string s in this.EnumerateColumns ())
					{
						// We need to skip the year, x and y (0,1,2)
						if (si++ <= 2) continue;

						// Append row value
						sb.Append (c[s]);
						sb.Append (delimiter);
					}
					sb.AppendLine ();
				}
			}
			return sb.ToString ();
		}
	}

	public class ExportMgr
	{
		public enum SelectionTypes
		{
			All,
			Selection
		}
		
		public enum DataTypes
		{
			Always,
			OnlyWhenSurveyed
		}

		public static ExportMgr self { get; private set; }
		private readonly Scene scene;

		public bool exportEnabled;
		public SelectionTypes selectionType;
		public DataTypes dataType;
		public bool exportVegetationTypes;
		public bool exportSuccessionTypes;

		public List<int> targetAreas;
		public List<string> parameters;
		public List<string> animals;
		public List<string> plants;

		public ExportData currentExportData;

		private volatile bool isWorking = false;

		public ExportMgr (Scene scene)
		{
			self = this;
			this.scene = scene;
			this.targetAreas = new List<int> ();
			this.parameters = new List<string> ();
			this.animals = new List<string> ();
			this.plants = new List<string> ();
		}

		public InventarisationsData GetInventarisationsData ()
		{
			InventarisationsData id = new InventarisationsData ();

			foreach (Progression.InventarisationResult ir in scene.progression.inventarisations)
			{
				if (ir.selected) 
				{
					id.AddValue (ir.name);
					id.GetYear (ir.year, true).AddValue (ir.name, (float)ir.DataMap.GetSum ());
				}
			}
			return id;
		}

		private void RetrieveExportData (System.Object args)
		{
			// We'll use an abbreviation for less code
			ExportData ed = new ExportData ();
			currentExportData = ed;

			// Temp vars
			ExportData.YearData year;
			ExportData.YearData.CoordinateData coordData;

			/** Default Columns **/

			// Add default columns
			ed.AddColumn ("year");
			ed.AddColumn ("x");
			ed.AddColumn ("y");

			// Add target areas
			for (int i = 1; i < scene.progression.targetAreas + 1; i++) {
				if (ShouldExportTargetArea (i)) {
					ed.AddColumn ("targetarea " + i);
				}
			}

			// Add Vegetation type
			if (exportVegetationTypes) {
				ed.AddColumn ("vegetation");
			}
			// Add Succession type
			if (exportSuccessionTypes) {
				ed.AddColumn ("succession");
			}

			/** Selection type **/
			switch (this.selectionType) 
			{
				case SelectionTypes.All : 
				{
					// If we select all, then we should add columns for all data names
					foreach (string p in scene.progression.GetAllDataNames (false)) {
						ed.AddColumn (p);
					}
				}
				break;

				case SelectionTypes.Selection : 
				{
					// Check if we must add the parameter
					foreach (string p in scene.progression.GetAllDataNames (false)) {
						if (ShouldExportParameter (p)) {	
							ed.AddColumn (p);
						}
					}
					break;
				}
			}

			/** Data type **/
			switch (this.dataType)
			{
				case DataTypes.Always :
				{
					// Loop through all tiles of the managed area and add them to all tiles of every (past) year
					for (int i = 0; i < (scene.progression.year - scene.progression.startYear); i++) {
						// New/get year
						year = ed.NewYear (scene.progression.startYear + i);
						foreach (ValueCoordinate vc in scene.progression.managedArea.EnumerateNotZero ()) {
							// New/get coord
							coordData = year.NewCoord (vc);
						}
					}
				}
				break;

				case DataTypes.OnlyWhenSurveyed : break;
			}

			/** Inventarisations **/
			foreach (Progression.InventarisationResult ir in scene.progression.inventarisations) {
				// Setup columns
				ed.AddColumn (ir.name);
				
				// Setup years and coords
				year = ed.NewYear (ir.year);
				foreach (ValueCoordinate vc in ir.AreaMap.EnumerateNotZero ()) {
					coordData = year.NewCoord ((Coordinate)vc);
					coordData[ir.name] = ir.DataMap.Get (vc).ToString();

					// Costs
					BasicAction action = scene.actions.GetAction (ir.actionId);
					if (action != null) {
						// Update costs
						int prevCosts = 0;
						int.TryParse (coordData["costs"], out prevCosts);
						coordData["costs"] = (prevCosts + (int)action.uiList [0].cost).ToString();
					}
				}
			}
			
			/** Research points **/
			foreach (ResearchPoint r in scene.progression.researchPoints) {
				foreach (ResearchPoint.Measurement rm in r.measurements) {
					// Setup years and coords
					year = ed.NewYear (rm.year);
					coordData = year.NewCoord (new Coordinate (r.x, r.y));
					
					// Setup columns and values
					foreach (KeyValuePair<string, string> p in rm.data.values) {
						if (ShouldExportParameter (p.Key)) {
							ed.AddColumn (p.Key);
							coordData[p.Key] = p.Value;
						}
					}

					// Costs
					BasicAction action = scene.actions.GetAction (rm.actionId);
					if (action != null) {
						// Update costs
						int prevCosts = 0;
						int.TryParse (coordData["costs"], out prevCosts);
						coordData["costs"] = (prevCosts + (int)action.uiList [0].cost).ToString();
					}
				}
			}

			/** Measures (actions) **/
			Debug.Log ("TakenActions: " + scene.progression.actionsTaken.Count);
			foreach (Progression.ActionTaken ta in scene.progression.actionsTaken) {
				BasicAction a = scene.actions.GetAction (ta.id);
				if (a != null && ta.years.Count > 0) {
					// Setup column
					string key = a.GetDescription ();
					ed.AddColumn (key);
					
					// Loop through all years
					foreach (int y in ta.years) {
						// Get the affected area
						Data area = scene.progression.GetData (a.affectedAreaName, y);
						if (area != null) {
							// Setup years and coords
							year = ed.NewYear (y);
							foreach (ValueCoordinate vc in area.EnumerateNotZero ()) {
								coordData = year.NewCoord (vc);
								coordData[key] = "1";

								// Update costs
								int prevCosts = 0;
								int.TryParse (coordData["costs"], out prevCosts);
								coordData["costs"] = (prevCosts + (int)a.uiList [0].cost).ToString();
							}
						}
					}
				}
			}

			// Loop through all coordinates
			foreach (ExportData.YearData y in ed.EnumerateYears ()) {
				year = y;
				foreach (ExportData.YearData.CoordinateData cd in y.EnumerateCoords ()) {
					coordData = cd;
					/** Target areas **/
					foreach (ValueCoordinate vc in scene.progression.managedArea.EnumerateNotZero ()) {
						// Setup target areas
						for (int a = 1; a < scene.progression.targetAreas + 1; a++) {
							Data targetArea = scene.progression.GetData (Progression.TARGET_ID + a);
							if (ed.HasColumn ("targetarea " + a)) {
								cd ["targetarea " + a] = (targetArea.Get (cd.coord) > 0) ? "1" : "0";
							}
						}
					}

					/** Vegetation types **/
					if (exportVegetationTypes || exportSuccessionTypes) {
						// Get the tile type
						TileType tile = scene.progression.vegetation.GetTileType (cd.coord.x, cd.coord.y);

						// Set the data
						if (exportVegetationTypes) {
							// Set vegetation and succession type
							cd["vegetation"] = tile.vegetationType.name;
						}
						if (exportSuccessionTypes) {
							cd["succession"] = tile.vegetationType.successionType.name;
						}
					}

					/** Parameters **/
					foreach (string p in scene.progression.GetAllDataNames (false)) {
						// Check if we should set parameter
						if (string.IsNullOrEmpty (cd [p]) && ShouldExportParameter (p)) {

							// Get the data, if it's null try the default (init) value
							Data data = scene.progression.GetData (p, y.year) ?? scene.progression.GetData (p);

							// Exception: calculated data, we need to manually set the year
							bool isCalcData = (data is CalculatedData);
							if (isCalcData) {
								((CalculatedData)data).year = y.year;
							}

							// Set the value
							if (data != null) {
								cd [p] = data.Get (cd.coord).ToString ();
							}

							// Reset the calc data to the current year to avoid messing up the game logic
							if (isCalcData) {
								((CalculatedData)data).year = -1;
							}
						}
					}

					/** Plants **/
					foreach (PlantType p in scene.plantTypes) {
						// Check if we should export the plant
						if (ShouldExportPlant (p.name)) {
							// Add the column
							ed.AddColumn (p.name);
							
							// Get the data
							Data data = scene.progression.GetData (p.dataName, y.year);
							if (data != null) {
								cd [p.name] = data.Get (cd.coord).ToString ();
							}
						}
					}

					/** Animals **/
					foreach (AnimalType a in scene.animalTypes) {
						// Large animal
						if (ShouldExportAnimal (a.name)) {
							// Check the animal type
							if (a is LargeAnimalType) 
							{
								LargeAnimalType la = (LargeAnimalType)a;
								foreach (AnimalStartPopulationModel.Nests.Nest nest in la.startPopModel.nests.nests) {
									// We check if we have a coord data of the location of the nest
									if ((coordData.coord.x == nest.x) && (coordData.coord.y == nest.y)) {
										// Add the column
										ed.AddColumn (a.name);

										// Count the total animals in the nest
										int males = nest.GetMalesAt (y.year);
										int females = nest.GetFemalesAt(y.year);
										coordData [a.name] = (males + females).ToString ();
									}
								}
							}
							// TODO: Add more animal types as they come
						}
					}
				}
			}

			// Cost
			ed.AddColumn ("costs");

			// Thread finished
			isWorking = false;
		}

		private bool ShouldExportParameter (string param)
		{
			if (this.selectionType == SelectionTypes.Selection) {
				return this.parameters.Contains (param);
			}
			return true;
		}

		private bool ShouldExportAnimal (string animal)
		{
			if (this.selectionType == SelectionTypes.Selection) {
				return this.animals.Contains (animal);
			}
			return true;
		}

		private bool ShouldExportTargetArea (int area)
		{
			if (this.selectionType == SelectionTypes.Selection) {
				return this.targetAreas.Contains (area);
			}
			return true;
		}

		private bool ShouldExportPlant (string plant)
		{
			if (this.selectionType == SelectionTypes.Selection) {
				return this.plants.Contains (plant);
			}
			return true;
		}

		public void ExportCurrentData (System.Action onComplete)
		{
			System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog ();
			sfd.Filter = "csv files (*.csv)|*.csv";
			
			if (sfd.ShowDialog () == System.Windows.Forms.DialogResult.OK)
			{
				// Get the export data
				GameControl.self.StartCoroutine (COExportCurrentData(sfd.FileName, onComplete));
			}
		}

		public void GetNewExportData ()
		{
			currentExportData = null;
			GameControl.self.StartCoroutine (COGetExportData());
		}

		private IEnumerator<object> COGetExportData ()
		{
			#pragma warning disable 162
			isWorking = true;
			// Get the export data
			ThreadPool.QueueUserWorkItem (RetrieveExportData);
			
			while (isWorking) {
				yield return 0;
			}
			#pragma warning restore 162

			// Sort the years
			currentExportData.SortYears ();
		}

		private IEnumerator<object> COExportCurrentData (string filePath, System.Action onComplete)
		{
			// Enable spinner and hide interface
			SimpleSpinner.ActivateSpinner ();
			GameControl.self.isProcessing = true;
			GameMenu.show = false;

			yield return 0;

			// Disable spinner and show interface
			SimpleSpinner.DeactivateSpinner ();
			GameControl.self.isProcessing = false;
			GameMenu.show = true;

			// Create new file
			FileStream fs = File.Create (filePath);

			// Stringify and save
			System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding ();
			string txt = currentExportData.ToCSV ();
			fs.Write (enc.GetBytes (txt), 0, enc.GetByteCount (txt));
			
			// Close and dispose the stream
			fs.Close ();
			fs.Dispose ();
			fs = null;

			if (onComplete != null)
				onComplete ();
		}

		public void AddTargetArea (int area)
		{
			if (this.targetAreas.Contains (area)) return;
			this.targetAreas.Add (area);
			this.targetAreas.Sort ();
		}

		public void RemoveTargetArea (int area)
		{
			if (!this.targetAreas.Contains (area)) return;
			this.targetAreas.Remove (area);
			this.targetAreas.Sort ();
		}

		public void AddParameter (string param)
		{
			if (this.parameters.Contains (param)) return;
			this.parameters.Add (param);
			this.parameters.Sort ();
		}

		public void RemoveParameter (string param)
		{
			if (!this.parameters.Contains (param)) return;
			this.parameters.Remove (param);
			this.parameters.Sort ();
		}

		public void AddAnimal (string name)
		{
			if (this.animals.Contains (name)) return;
			this.animals.Add (name);
			this.animals.Sort ();
		}

		public void RemoveAnimal (string name)
		{
			if (!this.animals.Contains (name)) return;
			this.animals.Remove (name);
			this.animals.Sort ();
		}

		public void AddPlant (string name)
		{
			if (this.plants.Contains (name)) return;
			this.plants.Add (name);
			this.plants.Sort ();
		}
		
		public void RemovePlant (string name)
		{
			if (!this.plants.Contains (name)) return;
			this.plants.Remove (name);
			this.plants.Sort ();
		}

		public static ExportMgr Load (string path, Scene scene)
		{
			ExportMgr mgr = new ExportMgr (scene);
			if (File.Exists (path + "exportsettings.xml")) {
				XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (path + "exportsettings.xml"));
				try {
					while (reader.Read()) {
						XmlNodeType nType = reader.NodeType;
						if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "export")) {
							mgr.Load (reader);
						}
					}
				} finally {
					reader.Close ();
				}
			}
			return mgr;
		}

		private void Load (XmlTextReader reader)
		{
			this.exportEnabled = bool.Parse (reader.GetAttribute ("enabled"));
			this.selectionType = (SelectionTypes)System.Enum.Parse (typeof(SelectionTypes), reader.GetAttribute ("selectiontype"));
			this.dataType = (DataTypes)System.Enum.Parse (typeof(DataTypes), reader.GetAttribute ("datatype"));
			this.exportSuccessionTypes = bool.Parse (reader.GetAttribute ("exportsucctypes"));
			this.exportVegetationTypes = bool.Parse (reader.GetAttribute ("exportvegtypes"));

			List<int> targetAreaList = new List<int>();
			List<string> paramList = new List<string>();
			List<string> animalList = new List<string>();
			List<string> plantList = new List<string>();

			while (reader.Read()) 
			{
				XmlNodeType nType = reader.NodeType;
				
				if (nType == XmlNodeType.Element)
				{
					switch (reader.Name.ToLower ())
					{
					case "targetarea" :
						targetAreaList.Add (int.Parse (reader.GetAttribute ("id")));
						break;
					case "param" :
						paramList.Add (reader.GetAttribute ("name"));
						break;
					case "animal" :
						animalList.Add (reader.GetAttribute ("name"));
						break;
					case "plant" :
						plantList.Add (reader.GetAttribute ("name"));
						break;
					}
				} 
				else if ((nType == XmlNodeType.EndElement) && 
				         (reader.Name.ToLower () == "export")) {
					break;
				}
			}

			this.targetAreas = targetAreaList;
			this.parameters = paramList;
			this.animals = animalList;
			this.plants = plantList;
		}

		public void Save (string path)
		{
			XmlTextWriter writer = new XmlTextWriter (path + "exportsettings.xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement ("export");
			writer.WriteAttributeString ("enabled", this.exportEnabled.ToString().ToLower());
			writer.WriteAttributeString ("exportvegtypes", this.exportVegetationTypes.ToString().ToLower());
			writer.WriteAttributeString ("exportsucctypes", this.exportSuccessionTypes.ToString().ToLower());
			writer.WriteAttributeString ("selectiontype", this.selectionType.ToString());
			writer.WriteAttributeString ("datatype", this.dataType.ToString());
			foreach (string s in this.parameters) {
				writer.WriteStartElement ("param");
				writer.WriteAttributeString ("name", s);
				writer.WriteEndElement ();
			}
			foreach (string s in this.animals) {
				writer.WriteStartElement ("animal");
				writer.WriteAttributeString ("name", s);
				writer.WriteEndElement ();
			}
			foreach (string s in this.plants) {
				writer.WriteStartElement ("plant");
				writer.WriteAttributeString ("name", s);
				writer.WriteEndElement ();
			}
			foreach (int i in this.targetAreas) {
				writer.WriteStartElement ("targetarea");
				writer.WriteAttributeString ("id", i.ToString());
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();		
		}
		
		public void UpdateReferences ()
		{
			List<string> list = new List<string> ();
			foreach (string s in this.parameters) {
				if (scene.progression.HasData (s))
					list.Add (s);
			}
			this.parameters = list;

			list = new List<string> ();
			foreach (string s in this.animals) {
				foreach (AnimalType a in scene.animalTypes) {
					if (a.name.ToLower () == s.ToLower ()) {
						list.Add (a.name);
						break;
					}
				}
			}
			this.animals = list;

			list = new List<string> ();
			foreach (string s in this.plants) {
				foreach (PlantType p in scene.plantTypes) {
					if (p.name.ToLower () == s.ToLower ()) {
						list.Add (p.name);
						break;
					}
				}
			}
			this.plants = list;
		}
	}
}
