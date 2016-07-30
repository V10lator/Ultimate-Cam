using System;
using System.Reflection;
using UnityEngine;

namespace UltimateCam
{
	public class UltimateMain : IMod, IModSettings
	{
		private GameObject go;
		private UltimateSettings _config = null;
		internal UltimateSettings config
		{
			get {
				if (_config == null)
					_config = new UltimateSettings ();
				return _config;
			}
		}
		private UltimateConfigScreen configScreen = null;

		public void onEnabled()
		{
			if (enabled) {
				Log ("Something tried to enable us but we are already enabled! Ignoring.", LogLevel.WARNING);
				return;
			}
			Instance = this;
			go = new GameObject();
			go.AddComponent<UltimateCam>();
			this.Log ("Enabled!", UltimateMain.LogLevel.INFO);
		}

		public void onDisabled()
		{
			UnityEngine.Object.Destroy(go);
			_config.cleanup ();
			_config = null;
			Instance = null;
			_version = null;
			_majorVersion = _minorVersion = 0;
			this.Log ("Disabled!", UltimateMain.LogLevel.INFO);
		}

		private string _version = null;
		private int _majorVersion;
		private int _minorVersion;

		private Assembly getAssembly()
		{
			return @Assembly.GetAssembly (typeof(UltimateCam));
		}

		private void cacheVersion()
		{
			if (_version != null)
				return;
			string vers = getAssembly ().GetName ().Version.ToString ();
			string[] vs = vers.Split ('.');
			_majorVersion = int.Parse (vs [0]);
			_minorVersion = int.Parse (vs [1]);
			_version = _majorVersion + "." + _minorVersion;
			if (int.Parse (vs [2]) != 0 || int.Parse (vs [3]) != 0)
				_version += "-dev" + vs [2] + "." + vs [3];
		}

		public string Name { get { return getAssembly().GetName ().Name.ToString (); } }
		public int MajorVersion {
			get {
				cacheVersion ();
				return _majorVersion;
			}
		}
		public int MinorVersion {
			get {
				cacheVersion ();
				return _minorVersion;
			}
		}
		public string Version { 
			get {
				cacheVersion ();
				return _version;
			}
		}
		
		public string Description {
			get {
				Assembly a = getAssembly ();
				Type t = typeof(AssemblyDescriptionAttribute);
				if (Attribute.IsDefined (a, t)) {
					AssemblyDescriptionAttribute attr = (AssemblyDescriptionAttribute)Attribute.GetCustomAttribute (a, t);
					if (attr != null)
						return attr.Description.ToString ();
				}
				return "N/A";
			}
		}
		public string Identifier { get { return "V10lator:" + Name + "@" + Version; } }
		public string ModPath { get { return System.IO.Path.GetDirectoryName(getAssembly().Location.ToString()); } }
		public static UltimateMain Instance = null;
		private bool enabled { get { return Instance != null; } }

		public void onDrawSettingsUI()
		{
			configScreen.draw ();
		}

		public void onSettingsOpened()
		{
			onEnabled (); // Make sure we are enabled when rendering settings
			configScreen = new UltimateConfigScreen (config);
		}

		public void onSettingsClosed()
		{
			config.WriteSettingsFile ();
			configScreen = null;
		}

		internal void Log(string msg, LogLevel level)
		{
			msg = "[" + Name + " v" + Version + "] " + msg;

			switch (level) {
			case LogLevel.INFO:
				Debug.Log (msg);
				break;
			case LogLevel.WARNING:
				Debug.LogWarning (msg);
				break;
			default: // LogLevel.ERROR
				Debug.LogError (msg);
				break;
			}
		}

		internal enum LogLevel
		{
			INFO,
			WARNING,
			ERROR
		};
	}
}
