using Parkitect.UI;
using UnityEngine;

namespace UltimateCam
{
	public class UltimateFader : MonoBehaviour
	{
		private bool forward;
		private int step = 0;
		private Camera[] tmpCams = { null, null };
		private bool switchTmpCams = false;
		private float alpha = 0.0f;
		private bool _destroy;
		private GameObject toDestroy = null;
		private bool _disableUI;

		internal static bool active = false;

		internal void fade(Camera from, Camera to, bool destroy, bool disableUI)
		{
			switch (step)
			{
				case 0:
					tmpCams[0] = from;
					tmpCams[1] = to;
					_destroy = destroy;
					_disableUI = disableUI;
					forward = UltimateFader.active = true;
					break;
				case 1:
					forward = false;
					switchTmpCams = true;
					break;
				default:
					step = 0;
					UltimateFader.active = false;
					alpha = 0.0f;
					if (_destroy)
						toDestroy = tmpCams[0].gameObject;
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
			if (!UltimateFader.active)
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
				tmpCams[0].enabled = false;
				CameraController cc = tmpCams[1].GetComponent<CameraController>();
				if (cc != null)
						cc.enabled = true;
				tmpCams[1].enabled = true;

				bool vis = !_disableUI;
				GameController.Instance.setUICanvasVisibility(UICanvas.UICanvasTag.GameUI, vis);
				Cursor.lockState = vis ? CursorLockMode.None : CursorLockMode.Locked;
				Cursor.visible = vis;
				UIWorldOverlayController.Instance.gameObject.SetActive(vis);
				if(vis)
					GameController.Instance.popGameInputLock();
				else
					GameController.Instance.pushGameInputLock();

				switchTmpCams = false;
			}
			else if (toDestroy != null)
			{
				Destroy(toDestroy);
				toDestroy = null;
			}
		}
	}
}
