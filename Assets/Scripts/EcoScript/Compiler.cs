/*
 * Compiler.cs
 * Based on code Copyright (c) 2012 Nick Gravelyn
*/
using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.CSharp;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Action;

namespace Ecosim.EcoScript
{
	public static class Compiler
	{
		private const string ASSEMBLY_NAME = "EcoSim-{0}.dll";
		private const string TEMP_ASSEMBLY_NAME = "Tmp-{0}{1}.dll";
		
		private static int temp_index = 0;
		
		private static Random rnd = new Random ();
		
		/**
		 * Loads an existing Ecosim assembly from disk and links it ot the action instances
		 * scene is the current scene for which we are loading the EcoSim assembly.
		 */
		public static bool LoadAssemblyFromDisk (Scene scene)
		{
			if (scene.actions.assemblyId == null) {
				return false;
			}
			String assemblyPath = GameSettings.GetPathForScene (scene.sceneName) +
				Path.DirectorySeparatorChar + String.Format (ASSEMBLY_NAME, scene.actions.assemblyId);
			if (File.Exists (assemblyPath)) {
				Assembly assembly = Assembly.LoadFrom (assemblyPath);
				if (assembly != null) {
					return LinkAssembly (scene, assembly);
				}
			}
			return false;
		}

		/**
		 * Loads an existing Ecosim assembly from disk and links it ot the action instances
		 * scene is the current scene for which we are loading the EcoSim assembly.
		 */
		public static bool LoadTempAssemblyFromDisk (Scene scene, string assemblyPath)
		{
			if (File.Exists (assemblyPath)) {
				Assembly assembly = Assembly.LoadFrom (assemblyPath);
				if (assembly != null) {
					return LinkAssembly (scene, assembly);
				}
			}
			return false;
		}
		
		/**
		 * Links assembly 'assembly' with actions in 'scene'.
		 */
		private static bool LinkAssembly (Scene scene, Assembly assembly)
		{
			foreach (Type t in assembly.GetTypes()) {
				if (t.IsSubclassOf (typeof(EcoBase))) {
					string className = t.Name;
					foreach (BasicAction action in scene.actions.EnumerateActions ()) {
						if (action.ClassName () == className) {
							ConstructorInfo cinf = t.GetConstructor (new[] { typeof(Scene), action.GetType () });
							if (cinf != null) {
								try {
									EcoBase instance = cinf.Invoke (new object[] { scene, action }) as EcoBase;
									action.SetEcoScriptInstance (instance);
								} catch (System.Exception ex) {
									UnityEngine.Debug.LogError (ex);
									return false;
								}
							}
						}
					}
				}
			}
			return true;
		}
		
		// names of libraries we don't want included in compilation
		static readonly string[] systemLibraries = new string[] { "system.xml.dll", "system.core.dll", "system.dll", "mscorlib.dll" };
		
		static bool IsSystemLibrary (string name) {
			name = name.ToLower ();
			foreach (string sl in systemLibraries) {
				if (name.Contains (sl)) return true;
			}
			return false;
		}
		
		/**
		 * Compiles EcoSim scripts for scene 'scene', returns true on succes. 'errors' will contain all errors
		 * and warnings generated during compiling (on successfull compilation there still can be warnings!)
		 * If tempAssembly is set to true, the resulting assembly will be linked immediately and the assembly
		 * deleted again from disk (the linking is not automatically done if tempAssembly is set to false!).
		 */
		public static bool CompileScripts (Scene scene, out CompilerErrorCollection errors, bool tempAssembly)
		{
			// clear errors stored in action and generate C# for action (if it has a script)
			foreach (BasicAction action in scene.actions.EnumerateActions ()) {
				action.errors = null;
				action.CompileScript ();
			}
			if (!tempAssembly) {
				// as we are going to write to disk, delete previous version of assembly
				String assemblyPath = GameSettings.GetPathForScene (scene.sceneName) +
				Path.DirectorySeparatorChar + String.Format (ASSEMBLY_NAME, scene.actions.assemblyId);
				if (File.Exists (assemblyPath)) {
					// delete old assembly...
					try {
						File.Delete (assemblyPath);
					} catch (System.Exception e) {
						Log.LogDebug("Deleting old assembly failed: " + e.ToString());
					}
				}
			}
			bool success = false;
			string originalPath = Environment.GetEnvironmentVariable ("PATH");
			try {
				string myPath = GameSettings.MonoPath + Path.DirectorySeparatorChar + "bin";
			
				
				string path = String.Format ("{0}{1}{2}", myPath, GameSettings.EnvPathSeparatorChar, originalPath);
				Environment.SetEnvironmentVariable ("PATH", path);
				CodeDomProvider codeProvider;
				if ((UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WindowsPlayer) ||
					(UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WindowsEditor)) {
					// Windows!
					// Use my hacked version to actually listen to the mono path defined in GameSettings
					codeProvider = new MyCSharpCodeProvider (new Dictionary<String, String> { { "CompilerVersion", "v3.5" } });
				}
				else {
					// Use normal provider
					codeProvider = new CSharpCodeProvider (new Dictionary<String, String> { { "CompilerVersion", "v3.5" } });
				}
				var compilerOptions = new CompilerParameters ();
				compilerOptions.GenerateExecutable = false;
				compilerOptions.GenerateInMemory = false;
				compilerOptions.TreatWarningsAsErrors = false;
			
				// add references to all currently loaded assemblies
				foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
					string name = assembly.GetName ().Name;
					if (!(name.StartsWith ("EcoSim")) && !(name.StartsWith ("Tmp")) && !IsSystemLibrary (assembly.Location)) {
						compilerOptions.ReferencedAssemblies.Add (assembly.Location);
					}
				}
			
				compilerOptions.GenerateExecutable = false;
				compilerOptions.WarningLevel = 3;
				if (!tempAssembly) {
					// we are going to write to disk.
					// we need an unique (or at least a name unlikely already used before since we started
					// running EcoSim and not used in another scene as well) name. The reason is that
					// if the name is not unique we risk that .NET or Mono uses an already loaded version
					// of the assembly with the same name. There doesn't seem to be a way to force load
					// an assembly (if there is it would be a good idea to use it here ;-).
					scene.actions.assemblyId = RndUtil.RandomString (ref rnd, 8);
					compilerOptions.OutputAssembly = GameSettings.GetPathForScene (scene.sceneName) +
					Path.DirectorySeparatorChar + String.Format (ASSEMBLY_NAME, scene.actions.assemblyId);
				}
				else {
					// we now write the temporary assembly to disk as well as generating to memory wasn't
					// very reliable
					compilerOptions.OutputAssembly = GameSettings.GetPathForScene (scene.sceneName) +
						Path.DirectorySeparatorChar + String.Format (TEMP_ASSEMBLY_NAME, RndUtil.RandomString (ref rnd, 6), ++temp_index);
				}
				List<string> files = new List<string> ();
				foreach (BasicAction action in scene.actions.EnumerateActions ()) {
					if (action.HasScript ()) {
						// add scripts to list of files needed to compile
						files.Add (action.GetFullClassPath ());
					}
				}
				// do the compiling
				CompilerResults result = codeProvider.CompileAssemblyFromFile (compilerOptions, files.ToArray ());
				errors = result.Errors;
				
				// remainingErrors will hold the errors not linked to an action
				// this can happen when the error points to a line outside the script
				// part but in the auto generated part of the code.
				CompilerErrorCollection remainingErrors = new CompilerErrorCollection ();
				
				// go over errors and try to allocate the errors (and warnings) to
				// the right action.
				foreach (CompilerError e in errors) {
					UnityEngine.Debug.Log (e.ToString ());
					bool isProcessed = false;
					foreach (BasicAction action in scene.actions.EnumerateActions ()) {
						if (action.ClassName () == e.FileName) {
							if (action.errors == null) {
								action.errors = new CompilerErrorCollection ();
							}
							action.errors.Add (e);
							isProcessed = true;
						}
					}
					if (!isProcessed) {
						remainingErrors.Add (e);
					}
				}
				errors = remainingErrors;
				if (errors.Count == 0) {
					// there where no errors that aren't assigned to an action
					errors = null;
				}
				if (result.PathToAssembly != null) {
					// if we have a valid path to the assembly we have written
					// it successfully to disk...
					if (tempAssembly) {
						// when generating temp assembly we load it in immediately and
						// then delete it from disk...
						success = LoadTempAssemblyFromDisk (scene, result.PathToAssembly);
						try {
							//if (File.Exists(result.PathToAssembly)) 
								//File.Delete (result.PathToAssembly);
						}
						catch (System.Exception e) {
							Log.LogException (e);
						}
					}
					else {
						success = true;
					}
				}
			} finally {
				// restore PATH variable
				Environment.SetEnvironmentVariable ("PATH", originalPath);
			}
			return success;
		}
		
		/**
		 * Compiles EcoSim scripts for scene 'scene', returns true on succes. 'errors' will contain all errors
		 * and warnings generated during compiling (on successfull compilation there still can be warnings!)
		 * If loadInMemory is set to true, the resulting assembly will not be written to disk but is loaded into
		 * memory and also linked to the actions of scene (the linking is not automatically done if loadInMemory
		 * is set to false!).
		 */
		public static bool CompileScriptsOrig (Scene scene, out CompilerErrorCollection errors, bool loadInMemory)
		{
			// clear errors stored in action and generate C# for action (if it has a script)
			foreach (BasicAction action in scene.actions.EnumerateActions ()) {
				action.errors = null;
				action.CompileScript ();
			}
			if (!loadInMemory) {
				// as we are going to write to disk, delete previous version of assembly
				String assemblyPath = GameSettings.GetPathForScene (scene.sceneName) +
				Path.DirectorySeparatorChar + String.Format (ASSEMBLY_NAME, scene.actions.assemblyId);
				if (File.Exists (assemblyPath)) {
					// delete old assembly...
					File.Delete (assemblyPath);
				}
			}
			bool success = false;
			string originalPath = Environment.GetEnvironmentVariable ("PATH");
			try {
				// string myPath = "/Applications/Unity/Unity.app/Contents/Frameworks/Mono/bin/";
				string myPath = GameSettings.MonoPath + Path.DirectorySeparatorChar + "bin";
			
				string path = String.Format ("{0}:{1}", myPath, originalPath);
				Environment.SetEnvironmentVariable ("PATH", path);
				var codeProvider = new CSharpCodeProvider (new Dictionary<String, String> { { "CompilerVersion", "v3.5" } });
				var compilerOptions = new CompilerParameters ();
				compilerOptions.GenerateExecutable = false;
				compilerOptions.GenerateInMemory = loadInMemory;
				compilerOptions.TreatWarningsAsErrors = false;
			
				// add references to all currently loaded assemblies
				foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
					compilerOptions.ReferencedAssemblies.Add (assembly.Location);
					//	UnityEngine.Debug.Log (assembly.GetName ());
				}
			
				compilerOptions.GenerateExecutable = false;
//				compilerOptions.WarningLevel = 3;
				if (!loadInMemory) {
					// we are going to write to disk.
					// we need an unique (or at least a name unlikely already used before since we started
					// running EcoSim and not used in another scene as well) name. The reason is that
					// if the name is not unique we risk that .NET or Mono uses an already loaded version
					// of the assembly with the same name. There doesn't seem to be a way to force load
					// an assembly (if there is it would be a good idea to use it here ;-).
					scene.actions.assemblyId = RndUtil.RandomString (ref rnd, 8);
					compilerOptions.OutputAssembly = GameSettings.GetPathForScene (scene.sceneName) +
					Path.DirectorySeparatorChar + String.Format (ASSEMBLY_NAME, scene.actions.assemblyId);
				}
				List<string> files = new List<string> ();
				foreach (BasicAction action in scene.actions.EnumerateActions ()) {
					if (action.HasScript ()) {
						// add scripts to list of files needed to compile
						files.Add (action.GetFullClassPath ());
					}
				}
				// do the compiling
				CompilerResults result = codeProvider.CompileAssemblyFromFile (compilerOptions, files.ToArray ());
				errors = result.Errors;
				
				// remainingErrors will hold the errors not linked to an action
				// this can happen when the error points to a line outside the script
				// part but in the auto generated part of the code.
				CompilerErrorCollection remainingErrors = new CompilerErrorCollection ();
				
				// go over errors and try to allocate the errors (and warnings) to
				// the right action.
				foreach (CompilerError e in errors) {
					UnityEngine.Debug.Log (e.ToString ());
					bool isProcessed = false;
					foreach (BasicAction action in scene.actions.EnumerateActions ()) {
						if (action.ClassName () == e.FileName) {
							if (action.errors == null) {
								action.errors = new CompilerErrorCollection ();
							}
							action.errors.Add (e);
							isProcessed = true;
						}
					}
					if (!isProcessed) {
						remainingErrors.Add (e);
					}
				}
				errors = remainingErrors;
				if (errors.Count == 0) {
					// there where no errors that aren't assigned to an action
					errors = null;
				}
				if ((loadInMemory) && (result.CompiledAssembly != null)) {
					// we tried to compile assembly into memory...
					// if we actually have an assembly in memory try to link it
					success = LinkAssembly (scene, result.CompiledAssembly);
				} else if (result.PathToAssembly != null) {
					// if we have a valid path to the assembly we have written
					// it successfully to disk...
					success = true;
				}
			} finally {
				// restore PATH variable
				Environment.SetEnvironmentVariable ("PATH", originalPath);
			}
			Log.LogDebug ("Compile EcoScript: " + success);
			return success;
		}
				
		const string classHeader = @"
using System.Collections.Generic;
using Ecosim;
using Ecosim.EcoScript;
using Ecosim.SceneData;
using Ecosim.SceneData.AnimalPopulationModel;
using Ecosim.SceneData.VegetationRules;
using Ecosim.SceneData.Action;

public class EcoScript{0} : EcoBase
{{

{3}

public readonly {1} action;

public EcoScript{0} (Scene scene, {1} action) : base (scene, action) {{
	this.action = action;
	{2}
}}
";
		const string classFooter = @"
}
";
		const string listInitializer = @"
		{1} = ({0}) scene.progression.variables[""{1}""];
";
		const string listVar = @"
	protected readonly {0} {1};
";
		const string normalVar = @"
	protected {0} {1} {{
		get {{ return ({0}) scene.progression.variables[""{1}""]; }}
		set {{ scene.progression.variables[""{1}""] = value; }}
	}}
";
		const string define = @"
	protected const {0} = {1};
";
		
		private static string TypeToStr (object o)
		{
			if (o is bool)
				return "bool";
			else if (o is int)
				return "int";
			else if (o is long)
				return "long";
			else if (o is float)
				return "float";
			else if (o is string)
				return "string";
			else if (o is Coordinate)
				return "Coordinate";
			else if (o is List<bool>)
				return "List<bool>";
			else if (o is List<int>)
				return "List<int>";
			else if (o is List<long>)
				return "List<long>";
			else if (o is List<float>)
				return "List<float>";
			else if (o is List<string>)
				return "List<string>";
			else if (o is List<Coordinate>)
				return "List<Coordinate>";
			else
				throw new System.Exception ("unknown type '" + o.GetType () + "'");
		}
		
		public static string GenerateCode (Scene scene, BasicAction action, string code, Dictionary<string, string> constantsDict)
		{
			ManagedDictionary <string, object> variables = scene.progression.variables;
			StringBuilder classCode = new StringBuilder (20000);
			StringBuilder listInitializers = new StringBuilder (10000);
			StringBuilder constants = new StringBuilder (10000);
			
			if (constantsDict != null) {
				foreach (KeyValuePair<string, string> kv in constantsDict) {
					constants.Append (string.Format (define, kv.Key, kv.Value));
				}
			}

			foreach (KeyValuePair<string, object> kv in variables) {
				object val = kv.Value;
				if (val is IList) {
					listInitializers.Append (string.Format (listInitializer, TypeToStr (val), kv.Key));
				}
			}
			
			classCode.Append (string.Format (classHeader, action.id.ToString (), action.GetType ().Name, listInitializers.ToString (), constants.ToString ()));
			
			// first make properties to access progression variables....
			foreach (KeyValuePair<string, object> kv in variables) {
				object val = kv.Value;
				if (val is IList) {
					classCode.Append (string.Format (listVar, TypeToStr (val), kv.Key));
				} else {
					classCode.Append (string.Format (normalVar, TypeToStr (val), kv.Key));
				}
			}
			
			classCode.Append ("#line 1 \"" + action.ClassName () + "\"\n");
			classCode.Append (code);
			classCode.Append (classFooter);
			
//			string.Format (methodScriptWrapper, className, code)
			
			string result = classCode.ToString ();
			if (GameSettings.DebugScripts) {
				System.IO.File.WriteAllText (GameSettings.DesktopPath + "script " + action.id.ToString () + ".txt", result);
			}
			return result;
		}
	
	}	
}