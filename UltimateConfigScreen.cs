using System;
using UnityEngine;

namespace UltimateCam
{
	internal class UltimateConfigScreen
	{
		private readonly UltimateSettings config;
		private bool tt = false;
		private bool tj = false;
		private bool tl = false;
		private bool tr = false;

		internal UltimateConfigScreen (UltimateSettings config)
		{
			this.config = config;
		}

		internal void draw()
		{
			GUILayoutOption minWidth = GUILayout.MinWidth (200.0f);
			GUILayoutOption toggleWidth = GUILayout.MinWidth (50.0f);

			// Toggle key
			GUILayout.BeginHorizontal ();
			GUILayout.Label (UltimateSettings.TOGGLE_KEY_SETTING, minWidth);
			GUILayout.FlexibleSpace ();
			if (!tt) {
				if (GUILayout.Button (config.GetKey(UltimateSettings.TOGGLE_KEY_SETTING).ToString (), minWidth))
					tt = true;
			} else {
				GUILayout.Button ("Press key", minWidth);
				if (Input.anyKeyDown) {
					KeyCode toggle = GetPressedKey ();
					if (toggle == KeyCode.None)
						return;
					tt = false;
					config.SetSetting (UltimateSettings.TOGGLE_KEY_SETTING, toggle.ToString ());
				}
			}
			GUILayout.EndHorizontal ();

			// Jump key
			GUILayout.BeginHorizontal ();
			GUILayout.Label (UltimateSettings.JUMP_KEY_SETTING, minWidth);
			GUILayout.FlexibleSpace ();
			if (!tj) {
				if (GUILayout.Button (config.GetKey(UltimateSettings.JUMP_KEY_SETTING).ToString (), minWidth))
					tj = true;
			} else {
				GUILayout.Button ("Press key", minWidth);
				if (Input.anyKeyDown) {
					KeyCode jump = GetPressedKey ();
					if (jump == KeyCode.None)
						return;
					tj = false;
					config.SetSetting (UltimateSettings.JUMP_KEY_SETTING, jump.ToString ());
				}
			}
			GUILayout.EndHorizontal ();

			// Left rotation key
			GUILayout.BeginHorizontal();
			GUILayout.Label(UltimateSettings.ROTATE_LEFT_KEY_SETTING, minWidth);
			GUILayout.FlexibleSpace();
			if (!tl)
			{
				if (GUILayout.Button(config.GetKey(UltimateSettings.ROTATE_LEFT_KEY_SETTING).ToString(), minWidth))
					tl = true;
			}
			else {
				GUILayout.Button("Press key", minWidth);
				if (Input.anyKeyDown)
				{
					KeyCode left = GetPressedKey();
					if (left == KeyCode.None)
						return;
					tl = false;
					config.SetSetting(UltimateSettings.ROTATE_LEFT_KEY_SETTING, left.ToString());
				}
			}
			GUILayout.EndHorizontal();

			// Right rotation key
			GUILayout.BeginHorizontal();
			GUILayout.Label(UltimateSettings.ROTATE_RIGHT_KEY_SETTING, minWidth);
			GUILayout.FlexibleSpace();
			if (!tr)
			{
				if (GUILayout.Button(config.GetKey(UltimateSettings.ROTATE_RIGHT_KEY_SETTING).ToString(), minWidth))
					tr = true;
			}
			else {
				GUILayout.Button("Press key", minWidth);
				if (Input.anyKeyDown)
				{
					KeyCode right = GetPressedKey();
					if (right == KeyCode.None)
						return;
					tr = false;
					config.SetSetting(UltimateSettings.ROTATE_RIGHT_KEY_SETTING, right.ToString());
				}
			}
			GUILayout.EndHorizontal();

			config.Height = UISlider (UltimateSettings.HEIGHT_SETTING, UltimateSettings.DEFAULT_HEIGHT, UltimateSettings.MIN_HEIGHT, UltimateSettings.MAX_HEIGHT, config.Height, minWidth);

			config.WalkingSpeed = UISlider (UltimateSettings.SPEED_SETTING, UltimateSettings.DEFAULT_SPEED, UltimateSettings.MIN_SPEED, UltimateSettings.MAX_SPEED, config.WalkingSpeed, minWidth);

			config.Gravity = UISlider (UltimateSettings.GRAVITY_SETTING, UltimateSettings.DEFAULT_GRAVITY, UltimateSettings.MIN_GRAVITY, UltimateSettings.MAX_GRAVITY, config.Gravity, minWidth);

			config.FoV = UISlider (UltimateSettings.FOV_SETTING, UltimateSettings.DEFAULT_FOV, UltimateSettings.MIN_FOV, UltimateSettings.MAX_FOV, config.FoV, minWidth);

			config.ViewDistance = UISlider (UltimateSettings.VD_SETTING, UltimateSettings.DEFAULT_VD, UltimateSettings.MIN_VD, UltimateSettings.MAX_VD, config.ViewDistance, minWidth);

			// HDR?
			GUILayout.BeginHorizontal ();
			GUILayout.Label (UltimateSettings.HDR_SETTING + "?", minWidth);
			GUILayout.FlexibleSpace ();
			config.HDR = GUILayout.Toggle (config.HDR, "", toggleWidth);
			GUILayout.EndHorizontal ();

			// Experimental?
			GUILayout.BeginHorizontal();
			GUILayout.Label(UltimateSettings.EXPERIMENTAL_SETTING + "?", minWidth);
			GUILayout.FlexibleSpace();
			config.Experimental = GUILayout.Toggle(config.Experimental, "", toggleWidth);
			GUILayout.EndHorizontal();

			if (config.Experimental)
			{
				// More collisions?
				GUILayout.BeginHorizontal();
				GUILayout.Label(UltimateSettings.MORE_COLS_SETTING + "?", minWidth);
				GUILayout.FlexibleSpace();
				config.MoreCols = GUILayout.Toggle(config.MoreCols, "", toggleWidth);
				GUILayout.EndHorizontal();

				// Jetpack?
				GUILayout.BeginHorizontal();
				GUILayout.Label(UltimateSettings.JETPACK_SETTING + "?", minWidth);
				GUILayout.FlexibleSpace();
				config.Jetpack = GUILayout.Toggle(config.Jetpack, "", toggleWidth);
				GUILayout.EndHorizontal();
			}
		}

		private float UISlider(string label, float def, float min, float max, float value, params GUILayoutOption[] options)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label (label, options);
			GUILayout.FlexibleSpace ();

			GUILayout.BeginVertical ();
			GUILayout.Space (25.0f);
			float newValue = GUILayout.HorizontalSlider (value, min, max, options);
			GUILayout.EndVertical ();

			if (GUILayout.Button ("Reset"))
				newValue = def;
			GUILayout.EndHorizontal ();
			return newValue;
		}

		private KeyCode GetPressedKey()
		{
			foreach(KeyCode key in Enum.GetValues(typeof(KeyCode)))
				if (Input.GetKeyDown (key))
					return key;
			return KeyCode.None;
		}
	}
}

