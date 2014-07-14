using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Xml;
using System.IO;
using System.Reflection;
using UnityEngine;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.EcoScript;

namespace Ecosim.SceneData.Action
{
	public class VariablePresentData	
	{
		public string name;
		public string category;
	}

	public class FormulaPresentData : VariablePresentData
	{
		public string formula;
	}

	/**
	 * Actions can be a lot, it can be a measure taken in the field, like starting mowing, or it can be some research
	 * like placing a pijlbuis or counting species
	 */
	public abstract class BasicAction
	{		
		public const string GENERATED_DIR = "GeneratedScripts";
		
		public volatile bool finishedProcessing;
		public readonly int id = -1;
		public bool isActive = true;
		protected readonly Scene scene;
		protected EcoBase ecoBase; // the compiled ecoscript holder

		private string _affectedAreaName;
		public string affectedAreaName { // Use to store the affected area for internal purposes
			get {
				// Save the affected area (if it's not yet there)
				if (_affectedAreaName == null) {
					_affectedAreaName = "_area" + id.ToString();
				}
				return _affectedAreaName;
			}
		}

		public Data AffectedArea {
			get {
				if (scene == null || scene.progression == null)
					return null;

				Data data = null;
				if (scene.progression.HasData (affectedAreaName)) {
					data = scene.progression.GetData (affectedAreaName);
				} else {
					data = new BitMap1 (scene);
					scene.progression.AddData (affectedAreaName, AffectedArea);
				}
				return data;
			}
		}

		private string ecoScript = null;
		private System.DateTime fileLastModified = System.DateTime.MinValue;
		public CompilerErrorCollection errors = null;
		public List<UserInteraction> uiList;
		private MethodInfo prepareSuccessionMI;
		private MethodInfo doSuccessionMI;
		private MethodInfo finalizeSuccessionMI;
		private MethodInfo loadProgressMI;
		private MethodInfo saveProgressMI;
		private MethodInfo encyclopediaOpenedMI;
		private MethodInfo measureTakenMI;
		private MethodInfo researchConductedMI;
		private MethodInfo actionSelectedMI;
		private MethodInfo getVariablePresentationDataMI;
		private MethodInfo getFormulaPresentationDataMI;
		private MethodInfo debugFnMI;
		
		public bool HasDebugFn {
			get { return (debugFnMI != null); }
		}
				
		protected BasicAction (Scene scene, int id)
		{
			this.scene = scene;
			this.id = id;
			uiList = new List<UserInteraction> ();
		}
		
		public virtual string GetDescription ()
		{
			return GetType ().Name;
		}
		
		public virtual void SetDescription (string descr)
		{
			throw new System.ArgumentException ("Description can not be set for " + GetType ().ToString ());
		}
		
		public virtual bool DescriptionIsWritable ()
		{
			return false;
		}
		
		public virtual int GetMinUICount ()
		{
			return 0;
		}
		
		public virtual int GetMaxUICount ()
		{
			return 0;
		}
		
		public string Script {
			get { return ecoScript; }
			set {
				string newScript = value.Trim ();
				if (newScript != ecoScript) {
					ecoScript = newScript;
				}
			}
		}
		
		/// <summary>
		/// Creates the default script (loaded as template from Resources.
		/// The script will be compiled (any errors/warnings will be stored in errors)
		/// </summary>
		/// <returns>
		/// true if template was found, otherwise false
		/// </returns>
		public virtual bool CreateDefaultScript ()
		{
			string resourcePath = "EcoScriptTemplates/" + GetType ().Name;
			TextAsset textAsset = Resources.Load (resourcePath) as TextAsset;
			if (textAsset != null) {
				Debug.Log ("Loaded script for '" + GetType ().Name + "'");
				Script = textAsset.text;
			} else {
				Debug.Log ("Couldn't load script for '" + GetType ().Name + "'");
				return false;
			}
			return true;
		}
		
		/// <summary>
		/// Deletes the script from the Scripts directory, unloads the class if compiled and set action to have no script
		/// </summary>
		public virtual void DeleteScript ()
		{
			string path = GetScriptPath ();
			ecoScript = null;
			if (File.Exists (path)) {
				try {
					File.Delete (path);
				} catch (System.Exception e) {
					Log.LogException (e);
				}
			}
		}
		
		
		public string GetScriptPath ()
		{
			return GetScriptPath (GameSettings.GetPathForScene (scene.sceneName));
		}

		public string GetScriptPath (string scenePath)
		{
			return scenePath + "Scripts" + Path.DirectorySeparatorChar + "script" + id + ".txt";
		}
		
		public bool HasScript ()
		{
			return ecoScript != null;
		}
		
		/**
		 * Link the generated EcoScript class instance to this action
		 */
		public void SetEcoScriptInstance(EcoBase instance) {
			if (ecoBase != null) {
				UnlinkEcoBase ();
			}
			ecoBase = instance;
			LinkEcoBase ();
			
		}		
		
		/**
		 * Generates C# code from EcoScript (actual compiling is not done yet)
		 */
		public virtual bool CompileScript ()
		{
			return CompileScript (null);
		}
		
		protected virtual void UnlinkEcoBase ()
		{
			ecoBase = null;
			prepareSuccessionMI = null;
			doSuccessionMI = null;
			finalizeSuccessionMI = null;
			loadProgressMI = null;
			saveProgressMI = null;
			encyclopediaOpenedMI = null;
			measureTakenMI = null;
			researchConductedMI = null;
			actionSelectedMI = null;
			getVariablePresentationDataMI = null;
			getFormulaPresentationDataMI = null;
			debugFnMI = null;
		}
		
		protected virtual void LinkEcoBase ()
		{
			if (ecoBase != null) {
				prepareSuccessionMI = ecoBase.GetType ().GetMethod ("PrepareSuccession",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {}, null);
				doSuccessionMI = ecoBase.GetType ().GetMethod ("DoSuccession",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {}, null);
				finalizeSuccessionMI = ecoBase.GetType ().GetMethod ("FinalizeSuccession",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {}, null);
				loadProgressMI = ecoBase.GetType ().GetMethod ("LoadProgress",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {typeof(bool)}, null);
				saveProgressMI = ecoBase.GetType ().GetMethod ("SaveProgress",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {}, null);
				encyclopediaOpenedMI = ecoBase.GetType ().GetMethod ("EncyclopediaOpened",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(int), typeof(string) }, null);
				measureTakenMI = ecoBase.GetType ().GetMethod ("MeasureTaken",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string), typeof(string), typeof(int) }, null);
				researchConductedMI = ecoBase.GetType ().GetMethod ("ResearchConducted",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string), typeof(string), typeof(int) }, null);
				actionSelectedMI = ecoBase.GetType ().GetMethod ("ActionSelected",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(UserInteraction) }, null);
				getVariablePresentationDataMI = ecoBase.GetType ().GetMethod ("GetVariablePresentationData",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type [] {}, null);
				getFormulaPresentationDataMI = ecoBase.GetType ().GetMethod ("GetFormulaPresentationData",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type [] {}, null);
				debugFnMI = ecoBase.GetType ().GetMethod ("Debug",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string) }, null);
			}
		}
		
		/**
		 * Loads in script if script is not already loaded in or script on disk has changed
		 * returns true if a script is attached to this action.
		 */
		public bool LoadScript () {
			string path = GetScriptPath ();
			if (File.Exists (path)) {
				DateTime modified = File.GetLastWriteTime (path);
				if ((ecoScript == null) || (DateTime.Compare (fileLastModified, modified) < 0)) {
					// script not yet loaded or older than currently on disk
					ecoScript = File.ReadAllText (path).Trim ();
					fileLastModified = modified;
					if (ecoScript == "") {
						ecoScript = null;
					}
				}
			}

			return (ecoScript != null);
		}
		
		/**
		 * Saves script if a script is attached to this action
		 */
		public void SaveScriptIfNeeded (string newPath)
		{
			if (ecoScript != null) {
				ecoScript = ecoScript.Trim ();
				if (ecoScript != "") {
					File.WriteAllText (GetScriptPath (newPath), ecoScript);
					fileLastModified = File.GetLastWriteTime (GetScriptPath ());
				} else {
					ecoScript = null;
				}
			}
		}
		
		public string ClassName () {
			return "EcoScript" + id;
		}
		
		public string GetFullClassPath () {
			string dir = GameSettings.GetPathForScene (scene.sceneName) + GENERATED_DIR;
			return dir + Path.DirectorySeparatorChar + ClassName() + ".cs";
		}
		
		/**
		 * Generates the actual C# code from the EcoScript
		 */
		protected bool CompileScript (Dictionary<string, string> constants)
		{
			LoadScript ();
			if (ecoScript == null) return false;
			
			try {
				string script = Compiler.GenerateCode (scene, this, ecoScript, constants);
				string dir = GameSettings.GetPathForScene (scene.sceneName) + GENERATED_DIR;
				if (!Directory.Exists(dir)) {
					Directory.CreateDirectory (dir);
				}
				string path = dir + Path.DirectorySeparatorChar + ClassName() + ".cs";
				File.WriteAllText (path, script);
				// ecoBase = Compiler.EcoCompile (scene, this, script, out errors);
				return true;
			}
			catch (Exception e) {
				Log.LogException (e);
			}
			return false;
		}
		
		/**
		 * Called at start of succession, before any DoSuccessions are called, given the action
		 * a chance to prepare data for handling during DoSuccession
		 */
		public virtual void PrepareSuccession ()
		{
			if (prepareSuccessionMI != null) {
				try {
					prepareSuccessionMI.Invoke (ecoBase, null);
				} catch (Exception e) {
					Log.LogException (e);
				}
			}
		}
		
		/**
		 * Called when succession is done for this action
		 */
		public virtual void DoSuccession ()
		{
			if (doSuccessionMI != null) {
				try {
					doSuccessionMI.Invoke (ecoBase, null);
				} catch (Exception e) {
					Log.LogException (e);
				}
			}
		}
		
		/**
		 * Called to finalize succession, called after all DoSuccessions have been called to
		 * all action. Here cleanup of temporary data can be done.
		 */
		public virtual void FinalizeSuccession ()
		{
			if (finalizeSuccessionMI != null) {
				try {
					finalizeSuccessionMI.Invoke (ecoBase, null);
				} catch (Exception e) {
					Log.LogException (e);
				}
			}
		}

		/**
		 * When user selects an action from game interface this method is called to present
		 * the user with the necessary interface (dialog or anything).
		 * ui is the user interaction element that the user selected.
		 */
		public virtual void ActionSelected (UserInteraction ui)
		{
			if (actionSelectedMI != null) {
				try {
					actionSelectedMI.Invoke (ecoBase, new object[] { ui });
				} catch (Exception e) {
					Log.LogException (e);
				}
			}
		}
		
		/**
		 * Debug function, has string argument
		 */
		public virtual bool DebugFn (string str)
		{
			if (debugFnMI != null) {
				try {
					debugFnMI.Invoke (ecoBase, new object[] { str });
					return true;
				} catch (Exception e) {
					Log.LogException (e);
				}
			}
			return false;
		}
		
		/**
		 * Called when game is started or continue (progress is loaded)
		 * initScene will be true when game is new game intead of a save game
		 * properties will contain a property dictionary, can _not_ be null if no properties are
		 * defined.
		 */
		public virtual void LoadProgress (bool initScene, Dictionary <string, string> properties)
		{
			if (ecoBase != null) {
				ecoBase.properties = properties;
				if (loadProgressMI != null) {
					try {
						loadProgressMI.Invoke (ecoBase, new object[] { initScene });
					} catch (Exception e) {
						Log.LogException (e);
					}
				}
			}
		}
		
		/**
		 * Gets a dictionary of properties to persistence the action. The dictionary can be empty.
		 * The dictionary will be used again when loading the save game or starting new game to
		 * restore the data in the action.
		 */
		public virtual Dictionary<string, string> SaveProgress ()
		{
			if (saveProgressMI != null) {
				try {
					saveProgressMI.Invoke (ecoBase, null);
				} catch (Exception e) {
					Log.LogException (e);
				}
			}
			if ((ecoBase != null) && (ecoBase.properties.Count > 0)) {
				return ecoBase.properties;
			} else {
				return null;
			}
		}

		/**
		 * Called when a measure is taken.
		 */
		// TODO: Add to .txt script files
		public virtual void MeasureTaken (string name, string group, int count)
		{
			// TODO:
			if (measureTakenMI != null) {
				try {
					measureTakenMI.Invoke (ecoBase, new object[] {name, group, count});
				} catch (Exception e) {
					Log.LogException (e);
				}
			}
		}

		/**
		 * Called when research is conducted.
		 */
		// TODO: Add to .txt script files
		public virtual void ResearchConducted (string name, string group, int count)
		{
			// TODO:
			if (researchConductedMI != null) {
				try {
					researchConductedMI.Invoke (ecoBase, new object[] {name, group, count});
				} catch (Exception e) {
					Log.LogException (e);
				}
			}
		}

		/**
		 * Called when a encylcopedia item is consulted.
		 */
		// TODO: Add to .txt script files
		public virtual void EncyclopediaOpened (int itemNr, string itemTitle)
		{
			if (encyclopediaOpenedMI != null) {
				try {
					encyclopediaOpenedMI.Invoke (ecoBase, new object[] {itemNr, itemTitle});
				} catch (Exception e) {
					Log.LogException (e);
				}
			}
		}

		public virtual List<VariablePresentData> GetVariablePresentationsData ()
		{
			if (getVariablePresentationDataMI != null) {
				try {
					object result = getVariablePresentationDataMI.Invoke (ecoBase, null);
					return result as List<VariablePresentData>;
				} catch (Exception e) {
					Log.LogException (e);
				}
			}

			List<VariablePresentData> list = new List<VariablePresentData> ();
			return list;
		}

		public virtual List<FormulaPresentData> GetFormulaPresentationData ()
		{
			if (getFormulaPresentationDataMI != null) {
				try {
					object result = getFormulaPresentationDataMI.Invoke (ecoBase, null);
					return result as List<FormulaPresentData>;
				} catch (Exception e) {
					Log.LogException (e);
				}
			}

			List<FormulaPresentData> list = new List<FormulaPresentData> ();
			return list;
		}
		
		public abstract void Save (XmlTextWriter writer);
		
		public virtual void UpdateReferences ()
		{
		}
	}
}