using Parkitect.UI;
using UnityEngine;

namespace UltimateCam.Internal
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
		private bool _sitting;

		internal static bool active = false;

		internal void fade(Camera from, Camera to, bool destroy, bool disableUI)
		{
			if (teleportToTransform != null)
			{
				fade(null, false);
				return;
			}
			if (teleportToPosition != Vector3.zero)
			{
				fade(Vector3.zero, 0.0f, false);
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

		internal void fade(Transform to, bool sitting)
		{
			switch (step)
			{
				case 0:
					teleportToTransform = to;
					_sitting = sitting;
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

		internal void fade(Vector3 to, float yaw, bool sitting)
		{
			switch (step)
			{
				case 0:
					teleportToPosition = to;
					_yaw = yaw;
					_sitting = sitting;
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
				UltimateCross uc = tmpCams[0].GetComponent<UltimateCross>();
				if (uc != null)
					uc.enabled = false;
				CameraController cc = tmpCams[1].GetComponent<CameraController>();
				if (cc != null)
					cc.enabled = true;
				tmpCams[1].enabled = true;
				CullingGroupManager.Instance.setTargetCamera(tmpCams[1]);
				uc = tmpCams[1].GetComponent<UltimateCross>();
				if (uc != null)
					uc.enabled = true;

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
			else if (teleportTmpCam)
			{
				if (teleportToTransform != null)
				{
					Camera.main.transform.parent = teleportToTransform;
					Camera.main.GetComponent<UltimateMouse>().reset();
					if (_sitting)
					{
						Camera.main.transform.localPosition = new Vector3(0.0f, 0.35f, 0.1f);
						API.UltimateCam.sitting = true;
					}
					else
					{
						Camera.main.transform.localPosition = new Vector3(-0.1f, -0.1f, 0.0f);
						Camera.main.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 90.0f);
						Camera.main.GetComponent<UltimateCross>().enabled = false;
						API.UltimateCam.following = true;
					}
				}
				else
				{
					if (_sitting)
					{
						Camera.main.transform.parent = null;
						Camera.main.transform.position = teleportToPosition;
						UltimateMouse mouse = Camera.main.GetComponent<UltimateMouse>();
						mouse.yaw = _yaw;
						mouse.pitch = 0.0f;
						Camera.main.GetComponent<PlayerController>().enabled = true;
						API.UltimateCam.sitting = false;
					}
					else
						API.UltimateCam.Instance.cleanupFollowerCam(teleportToPosition, _yaw);
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
