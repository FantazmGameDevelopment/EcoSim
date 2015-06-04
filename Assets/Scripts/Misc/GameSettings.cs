using UnityEngine;
using System.Collections;
using System.IO;

namespace Ecosim
{
	public enum PlatformType
	{
		UNKNOWN = -1,
		MACOSX,
		WINDOWS,
		LINUX
	};
	
	public static class GameSettings
	{
		public const string VERSION_STR = "0.1.52";
		public const bool MULTITHREAD_RENDERING = false; // in current unity version true gives corruption of data
		public const bool SUCCESSION_IN_BG_THREAD = true;
		public const bool VEGETATION_SUCCESSION_MULTITHREADED = true;
		public const bool PLANTS_LOGIC_MULTITHREADED = true;
		public const bool ANIMALS_LOGIC_MULTITHREADED = false;
		public const bool ALLOW_EDITOR = true;
		public const int MAX_SAVEGAME_SLOTS = 9;
		private static PlatformType platformType = PlatformType.UNKNOWN;
		private static string _ScenePath = null;
		private static string _SaveGamesPath = null;
		private static string _MonoPath = null;
		
		/**
		 * Setup should be called at initialisation of EcoSim2
		 * The reason for this is that PlayerPrefs can only be used from mainthread.
		 * To overcome this limitation, we read the values in Setup so we can use
		 * the properties from other threads as well...
		 */
		public static void Setup ()
		{
			if (_ScenePath != null)
				return;
			
			if (PlayerPrefs.HasKey ("ScenePath")) {
				_ScenePath = PlayerPrefs.GetString ("ScenePath");
			} else {
				_ScenePath = DefaultScenePath;
			}
			if (PlayerPrefs.HasKey ("SaveGamesPath")) {
				_SaveGamesPath = PlayerPrefs.GetString ("SaveGamesPath");
			} else {
				_SaveGamesPath = DefaultSaveGamesPath;
			}
			if (PlayerPrefs.HasKey ("MonoPath")) {
				_MonoPath = PlayerPrefs.GetString ("MonoPath");
			} else {
				_MonoPath = DefaultMonoPath;
			}
		}
		
		public static char EnvPathSeparatorChar {
			get {
				if ((Application.platform == RuntimePlatform.WindowsPlayer) || (Application.platform == RuntimePlatform.WindowsEditor)) {
					return ';';
				}
				else {
					return ':';
				}
			}
		}
		
		public static PlatformType Platform {
			get {
				if (platformType == PlatformType.UNKNOWN) {
					if ((Application.platform == RuntimePlatform.OSXPlayer) || (Application.platform == RuntimePlatform.OSXDashboardPlayer) ||
			(Application.platform == RuntimePlatform.OSXEditor)) {
						platformType = PlatformType.MACOSX;
					} else if (Application.platform == RuntimePlatform.LinuxPlayer) {
						platformType = PlatformType.LINUX;
					} else if ((Application.platform == RuntimePlatform.WindowsPlayer) || (Application.platform == RuntimePlatform.WindowsEditor)) {
						platformType = PlatformType.WINDOWS;
					}
				}
				return platformType;
			}
		}
		
		public static string ScenePath {
			get {
				if (_ScenePath == null) {
					Log.LogError ("Setup not called!");
					Setup ();
				}
				return _ScenePath;
			}
			set {
				string path = value;
				if (path.StartsWith (".") && ((path.Length == 1) || (path[1] == Path.DirectorySeparatorChar))) {
					// Relative path...
				}
				else {
					path = Path.GetFullPath (value);
				}
				if (!path.EndsWith (Path.DirectorySeparatorChar.ToString ())) {
					path += Path.DirectorySeparatorChar;
				}
				PlayerPrefs.SetString ("ScenePath", path);
				PlayerPrefs.Save ();
				_ScenePath = path;
			}
		}
		
		public static string DefaultScenePath {
			get {
				return "." + Path.DirectorySeparatorChar + "Scenes" + Path.DirectorySeparatorChar;
			}
		}
		
		public static string DefaultMonoPath {
			get {
				if (Platform == PlatformType.MACOSX) {
					if (Directory.Exists ("/Applications/Unity/Unity.app/Contents/Frameworks/Mono")) {
						// default unity MONO path
						return "/Applications/Unity/Unity.app/Contents/Frameworks/Mono/";
					} else if (Directory.Exists ("/Library/Frameworks/Mono.framework/Versions/Current")) {
						// default Mac OS X MONO path
						return "/Library/Frameworks/Mono.framework/Versions/Current/";
					}
				}
				string monoVar = System.Environment.GetEnvironmentVariable ("MONO");
				if (monoVar == null) {
					return "." + Path.DirectorySeparatorChar + "Mono" + Path.DirectorySeparatorChar;
				}
				return monoVar;
			}
		}
		
		public static string MonoPath {
			get {
				if (_MonoPath == null) {
					Log.LogError ("Setup not called!");
					Setup ();
				}
				return _MonoPath;
			}
			set {
				string path = value;
				if (path.StartsWith (".") && ((path.Length == 1) || (path[1] == Path.DirectorySeparatorChar))) {
					// Relative path...
				}
				else {
					path = Path.GetFullPath (value);
				}
				if (!path.EndsWith (Path.DirectorySeparatorChar.ToString ())) {
					path += Path.DirectorySeparatorChar;
				}
				PlayerPrefs.SetString ("MonoPath", path);
				PlayerPrefs.Save ();
				_MonoPath = path;
			}
		}

		public static string SaveGamesPath {
			get {
				if (_SaveGamesPath == null) {
					Log.LogError ("Setup not called!");
					Setup ();
				}
				return _SaveGamesPath;
			}
			set {
				string path = value;
				if (path.StartsWith (".") && ((path.Length == 1) || (path[1] == Path.DirectorySeparatorChar))) {
					// Relative path...
				}
				else {
					path = Path.GetFullPath (value);
				}
				if (!path.EndsWith (Path.DirectorySeparatorChar.ToString ())) {
					path += Path.DirectorySeparatorChar;
				}
				PlayerPrefs.SetString ("SaveGamesPath", path);
				PlayerPrefs.Save ();
				_SaveGamesPath = path;
			}
		}
		
		public static string DefaultSaveGamesPath {
			get {
				switch (Platform) {
				case PlatformType.MACOSX :
					return System.Environment.GetEnvironmentVariable ("HOME") + "/Library/Application Support/EcoSim/SaveGames/";
				case PlatformType.LINUX :
					return System.Environment.GetEnvironmentVariable ("HOME") + ".ecosim/SaveGames/";
				case PlatformType.WINDOWS :
					return System.Environment.GetEnvironmentVariable ("APPDATA") + "\\EcoSim\\SaveGames\\";
				default :
					return "." + Path.DirectorySeparatorChar;
				}
			}
		}
		
		public static string DesktopPath {
			get {
				switch (Platform) {
				case PlatformType.MACOSX :
					return System.Environment.GetEnvironmentVariable ("HOME") + "/Desktop/";
				case PlatformType.LINUX :
					{
						string home = System.Environment.GetEnvironmentVariable ("HOME");
						if (Directory.Exists (home + "/Desktop")) {
							return home + "/Desktop/";
						} else if (Directory.Exists (home + "/desktop")) {
							return home + "/desktop/";
						}
						return home;
					}
				case PlatformType.WINDOWS :
					
					return System.Environment.GetEnvironmentVariable ("HOMEDRIVE") +
				System.Environment.GetEnvironmentVariable ("HOMEPATH") + "\\Desktop\\";
				default :
					return "." + Path.DirectorySeparatorChar;
				}
			}
		}
				
		public static bool DebugScripts {
			get {
				return (PlayerPrefs.GetString ("debugscripts", "false") == "true");
			}
			set {
				bool current = DebugScripts;
				if (value != current) {
					PlayerPrefs.SetString ("debugscripts", value ? "true" : "false");
					PlayerPrefs.Save ();
				}
			}
		}
		
		/**
		 * Setup must be called before this can be used...
		 * Thread safe
		 */
		public static string GetPathForScene (string name)
		{
			return _ScenePath + name + Path.DirectorySeparatorChar;
		}

		/**
		 * Setup must be called before this can be used...
		 * Thread safe
		 */
		public static string GetPathForSlotNr (int slotNr)
		{
			return _SaveGamesPath + slotNr + Path.DirectorySeparatorChar;
		}
	}

}