using MiniJSON;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UltimateCam
{
	public class UltimateSettings
	{
		private Dictionary<string, object> settingsValueDictionary = new Dictionary<string, object>();
		public const string TOGGLE_KEY_SETTING = "Toggle key";
		public const string JUMP_KEY_SETTING = "Jump key";
		public const string HEIGHT_SETTING = "Player height";
		public const float MIN_HEIGHT = 0.3f;
		public const float MAX_HEIGHT = 2.0f;
		public const float DEFAULT_HEIGHT = 0.503f;
		public const string FOV_SETTING = "Field of view";
		public const float MIN_FOV = 20.0f;
		public const float MAX_FOV = 100.0f;
		public const float DEFAULT_FOV = 60.0f;
		public const string VD_SETTING = "Viewing distance";
		public const float MIN_VD = 0.5f;
		public const float MAX_VD = 200.0f;
		public const float DEFAULT_VD = 50.0f;
		public const string SPEED_SETTING = "Walking speed";
		public const float MIN_SPEED = 0.1f;
		public const float MAX_SPEED = 20.0f;
		public const float DEFAULT_SPEED = 5.0f;
		public const string GRAVITY_SETTING = "Gravity";
		public const float MIN_GRAVITY = 0.25f; // 0.1 = too low, 0.5 = too high...
		public const float MAX_GRAVITY = 100.0f;
		public const float DEFAULT_GRAVITY = 20.0f;

		private string _file = null;
		private string file
		{
			get {
				if(_file == null)
					_file = UltimateMain.Instance.Path + @"/" + @UltimateMain.Instance.Name + @".json";
				return _file;
			}
		}

		private bool needSave = false;

		public float Height {
			get {
				return float.Parse (settingsValueDictionary[HEIGHT_SETTING].ToString ());
			}
			set {
				SetSetting (HEIGHT_SETTING, value, MIN_HEIGHT, MAX_HEIGHT);
			}
		}

		public float FoV {
			get {
				return float.Parse (settingsValueDictionary[FOV_SETTING].ToString ());
			}
			set {
				SetSetting (FOV_SETTING, value, MIN_FOV, MAX_FOV);
			}
		}

		public float ViewDistance {
			get {
				return float.Parse (settingsValueDictionary[VD_SETTING].ToString ());
			}
			set {
				SetSetting (VD_SETTING, value, MIN_VD, MAX_VD);
			}
		}

		public float WalkingSpeed {
			get {
				return float.Parse (settingsValueDictionary[SPEED_SETTING].ToString ());
			}
			set {
				SetSetting (SPEED_SETTING, value, MIN_SPEED, MAX_SPEED);
			}
		}

		public float Gravity {
			get {
				return float.Parse (settingsValueDictionary[GRAVITY_SETTING].ToString ());
			}
			set {
				SetSetting (GRAVITY_SETTING, value, MIN_GRAVITY, MAX_GRAVITY);
			}
		}

		public UltimateSettings()
		{
			if (!File.Exists (file)) {
				UltimateMain.Instance.Log (file + " not found, creating!", UltimateMain.LogLevel.WARNING);
				validateSettings ();
			} else {
				try {
					settingsValueDictionary = Json.Deserialize (File.ReadAllText (file)) as Dictionary<string, object>;
				} catch (Exception) {
					UltimateMain.Instance.Log ("Can't read " + file + "!", UltimateMain.LogLevel.WARNING);
				}
				UltimateMain.Instance.Log (file + " readed!", UltimateMain.LogLevel.INFO);
				validateSettings ();
			}
		}

		public void SetSetting(string key, object value)
		{
			if(settingsValueDictionary.ContainsKey(key)) {
				if(settingsValueDictionary[key] != value) {
					settingsValueDictionary[key] = value;
					needSave = true;
				}
				return;
			}

			settingsValueDictionary.Add(key, value);
			needSave = true;
		}

		public object GetSetting(string key)
		{
			return settingsValueDictionary.ContainsKey (key) ? settingsValueDictionary [key] : null;
		}

		private void SetSetting(string setting, float value, float min, float max)
		{
			float old = float.Parse (settingsValueDictionary[setting].ToString ());
			if (value < min || value > max || value == old)
				return;
			settingsValueDictionary[setting] = value;
			needSave = true;
		}

		// WARNING: This invalidates the instance!
		public void cleanup()
		{
			WriteSettingsFile ();
			settingsValueDictionary.Clear ();
			needSave = false;
			_file = null;
		}

		public void WriteSettingsFile()
		{
			if (!needSave)
				return;
			File.WriteAllText(file, Json.Serialize(settingsValueDictionary));
			UltimateMain.Instance.Log (file + " written!", UltimateMain.LogLevel.INFO);
			needSave = false;
		}

		private void validateSettings ()
		{
			object obj;
			if (!settingsValueDictionary.TryGetValue ("Version", out obj)) {
				settingsValueDictionary.Add ("Version", 1);
				UltimateMain.Instance.Log ("Unversioned config file, fixing!", UltimateMain.LogLevel.WARNING);
				needSave = true;
			} else {
				int v = 0;
				try {
					v = int.Parse (obj.ToString ());
				} catch (Exception) {
				}
				if (v < 2) {
					UltimateMain.Instance.Log ("Config file version " + v + " found, updating!", UltimateMain.LogLevel.WARNING);
					settingsValueDictionary["Version"] = 2;
					needSave = true;
				} else if (v > 2) {
					settingsValueDictionary["Version"] = 2;
					UltimateMain.Instance.Log ("Config file version " + v + " not supported! Assuming version 2", UltimateMain.LogLevel.WARNING);
					needSave = true;
				}
			}

			validateKeySetting (TOGGLE_KEY_SETTING);
			validateKeySetting (JUMP_KEY_SETTING);

			validateFloatSetting (HEIGHT_SETTING, MIN_HEIGHT, MAX_HEIGHT, DEFAULT_HEIGHT);
			validateFloatSetting (SPEED_SETTING, MIN_SPEED, MAX_SPEED, DEFAULT_SPEED);
			validateFloatSetting (GRAVITY_SETTING, MIN_GRAVITY, MAX_GRAVITY, DEFAULT_GRAVITY);
			validateFloatSetting (FOV_SETTING, MIN_FOV, MAX_FOV, DEFAULT_FOV);
			validateFloatSetting (VD_SETTING, MIN_VD, MAX_VD, DEFAULT_VD);
		}

		private void validateKeySetting(string setting)
		{
			if (!settingsValueDictionary.ContainsKey (setting)) {
				settingsValueDictionary.Add (setting, GetKey (setting).ToString ());
				UltimateMain.Instance.Log ("Invalid setting for " + setting+ ", fixing!", UltimateMain.LogLevel.WARNING);
				needSave = true;
			}
		}

		private void validateFloatSetting(string setting, float min, float max, float def)
		{
			if (!settingsValueDictionary.ContainsKey(setting)) {
				UltimateMain.Instance.Log ("No " + setting + "! Assming " + def, UltimateMain.LogLevel.WARNING);
				settingsValueDictionary.Add (setting, def);
				needSave = true;
			} else {
				float f = float.MinValue;
				try {
					f = float.Parse (settingsValueDictionary [setting].ToString ());
				} catch (Exception) {
				}
				if (f < min || f > max) {
					UltimateMain.Instance.Log ("Invalid " + setting + "! Assmung " + def, UltimateMain.LogLevel.WARNING);
					settingsValueDictionary [setting] = def;
					needSave = true;
				}
			}
		}

		public KeyCode GetKey(string key)
		{
			object ok;
			KeyCode ret = KeyCode.None;
			if (!settingsValueDictionary.TryGetValue (key, out ok)) {
				switch (key) {
				case TOGGLE_KEY_SETTING:
					ret = KeyCode.Tab;
					break;
				case JUMP_KEY_SETTING:
					ret = KeyCode.Space;
					break;
				default:
					UltimateMain.Instance.Log ("Invalid key requested: " + key + "!", UltimateMain.LogLevel.ERROR);
					break;
				}
				return ret;
			}

			string sk = (string)ok;
			Type t = typeof (KeyCode);
			if (Enum.IsDefined (t, sk))
				ret = (KeyCode) Enum.Parse (t, sk);

			return ret;
		}
	}
}
