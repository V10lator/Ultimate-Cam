using Parkitect.UI;
using System.Collections;
using UnityEngine;

namespace UltimateCam
{
	public class UltimateFader : MonoBehaviour
	{
		private bool fading = false;
		private bool forward;
		private int step = 0;
		private Camera[] tmpCams = { null, null };
		private bool switchTmpCams = false;
		private float alpha = 0.0f;
		private bool _destroy;
		private ArrayList toDestroy = new ArrayList();
		private bool _disableUI;

		internal void fade(Camera from, Camera to, bool destroy, bool disableUI)
		{
			if (to != null)
			{
				tmpCams[1] = to;
				if (tmpCams[0] == null)
				{
					tmpCams[0] = from;
					_destroy = destroy;
				}
				else if (destroy)
					toDestroy.Add(from.gameObject);

				if (tmpCams[0] == tmpCams[1])
				{
					if (step != 2)
					{
						forward = false;
						step = 2;
						return;
					}
					else
						step = 0;
				}
				else if (step == 2)
					step = 0;
				_disableUI = disableUI;
			}


			switch (step)
			{
				case 0:
					forward = fading = true;
					break;
				case 1:
					forward = false;
					switchTmpCams = true;
					break;
				default:
					step = 0;
					fading = false;
					alpha = 0.0f;
					if (_destroy)
						toDestroy.Add(tmpCams[0].gameObject);
					tmpCams[0] = tmpCams[1] = null;
					return;
			}
			step++;
		}

		internal void fade(GameObject cam, Vector3 to, bool movable)
		{
			//TODO
		}

		public void OnGUI()
		{
			if (!fading)
				return;

			alpha += (forward ? 0.6f : -0.6f) * Time.deltaTime;
			alpha = Mathf.Clamp01(alpha);
			if (!forward && alpha <= 0.1f)
			{
				fade(null, null, false, false);
				GUI.color = Color.clear;
			}
			if (forward && alpha >= 0.9f)
			{
				fade(null, null, false, false);
				GUI.color = Color.black;
			}
			else
				GUI.color = new Color() { a = alpha };

			GUI.DrawTexture(new Rect(0.0f, 0.0f, Screen.width, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
			GUI.depth = -1000;
		}

		public void Update()
		{
			if (switchTmpCams)
			{
				CameraController cc = tmpCams[0].GetComponent<CameraController>();
				tmpCams[0].enabled = false;
				if (cc != null)
					cc.enabled = false;
				else
				{
					cc = tmpCams[1].GetComponent<CameraController>();
					if (cc == null)
						UltimateMain.Instance.Log("No CameraController found!", UltimateMain.LogLevel.ERROR);
					else
						cc.enabled = true;
				}

				tmpCams[1].enabled = true;

				GameController.Instance.setUICanvasVisibility(UICanvas.UICanvasTag.GameUI, !_disableUI);

				switchTmpCams = false;
			}
			else if (toDestroy.Count > 0)
			{
				IEnumerator enu = toDestroy.GetEnumerator();
				while (enu.MoveNext())
					Destroy((GameObject)enu.Current);
				toDestroy.Clear();
			}
		}
	}
}
