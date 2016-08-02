using Parkitect.UI;
using UnityEngine;

namespace UltimateCam
{
	public class UltimateFader : MonoBehaviour
	{
		private bool forward;
		private int step = 0;
		private Transform teleportToTransform = null;
		private Vector3 teleportToPosition = Vector3.zero;
		private Camera[] tmpCams = { null, null };
		private bool switchTmpCams = false;
		private bool teleportTmpCam = false;
		private float alpha = 0.0f;
		private float _yaw;
		private bool _destroy;
		private GameObject toDestroy = null;
		private bool _disableUI;

		internal static bool active = false;

		internal void fade(Camera from, Camera to, bool destroy, bool disableUI)
		{
			if (teleportToTransform != null)
			{
				fade(null);
				return;
			}
			if (teleportToPosition != Vector3.zero)
			{
				fade(Vector3.zero, 0.0f);
				return;
			}
			
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

		internal void fade(Transform to)
		{
			switch (step)
			{
				case 0:
					teleportToTransform = to;
					forward = UltimateFader.active = true;
					break;
				case 1:
					forward = false;
					teleportTmpCam = true;
					break;
				default:
					step = 0;
					UltimateFader.active = false;
					teleportToTransform = null;
					return;
			}
			step++;
		}

		internal void fade(Vector3 to, float yaw)
		{
			switch (step)
			{
				case 0:
					teleportToPosition = to;
					forward = UltimateFader.active = true;
					break;
				case 1:
					forward = false;
					teleportTmpCam = true;
					break;
				default:
					step = 0;
					UltimateFader.active = false;
					teleportToPosition = Vector3.zero;
					return;
			}
			step++;
		}

		public void OnGUI()
		{
			if (!UltimateFader.active)
				return;

			alpha += (forward ? 2.4f : -2.4f) * Time.deltaTime;
			alpha = Mathf.Clamp01(alpha);
			GUI.depth = -1000;
			if (!forward && alpha <= 0.1f)
			{
				fade(null, null, false, false);
				GUI.color = Color.clear;
			}
			else
			{
				Color c = Color.black;
				if (forward && alpha >= 0.9f)
					fade(null, null, false, false);
				else
					c.a = alpha;
				GUI.color = c;
			}

			GUI.DrawTexture(new Rect(0.0f, 0.0f, Screen.width, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
		}

		public void Update()
		{
			if (switchTmpCams)
			{
				tmpCams[0].enabled = false;
				UltimateCross uc = tmpCams[0].gameObject.GetComponent<UltimateCross>();
				if (uc != null)
					uc.enabled = false;
				CameraController cc = tmpCams[1].GetComponent<CameraController>();
				if (cc != null)
					cc.enabled = true;
				tmpCams[1].enabled = true;
				CullingGroupManager.Instance.setTargetCamera(tmpCams[1]);
				uc = tmpCams[1].gameObject.GetComponent<UltimateCross>();
				if (uc != null)
					uc.enabled = true;

				bool vis = !_disableUI;
				GameController.Instance.setUICanvasVisibility(UICanvas.UICanvasTag.GameUI, vis);
				Cursor.lockState = vis ? CursorLockMode.None : CursorLockMode.Locked;
				Cursor.visible = vis;
				if (vis)
					GameController.Instance.popGameInputLock();
				else
					GameController.Instance.pushGameInputLock();

				switchTmpCams = false;
			}
			else if (teleportTmpCam)
			{
				if (teleportToTransform != null)
				{
					Camera.main.transform.parent = teleportToTransform;
					Camera.main.transform.localPosition = new Vector3(0, 0.35f, 0.1f);
					Camera.main.gameObject.GetComponent<UltimateMouse>().reset();
				}
				else
				{
					Camera.main.transform.parent = null;
					Camera.main.transform.position = teleportToPosition;
					UltimateMouse mouse = Camera.main.gameObject.GetComponent<UltimateMouse>();
					mouse.yaw = _yaw;
					mouse.pitch = 0.0f;
					Camera.main.gameObject.GetComponent<PlayerController>().enabled = true;
				}
				teleportTmpCam = false;

			}
			else if (toDestroy != null)
			{
				Destroy(toDestroy);
				toDestroy = null;
			}
		}
	}
}
