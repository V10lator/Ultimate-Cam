﻿using System.Collections.Generic;
using Parkitect.UI;
using UnityEngine;

namespace UltimateCam
{
	public class UltimateCam : MonoBehaviour
	{
		private static UltimateCam _Instance = null;
		public static UltimateCam Instance
		{
			get
			{
				return _Instance;
			}
		}
		private Camera mainCam = null;
		private float startX, startZ;
		public static bool active { get { return Instance.mainCam != null; } }
		private bool _riding = false;
		public static bool riding { get { return Instance._riding; } }
		private bool disableUI;

		void Awake()
		{
			_Instance = this;
			DontDestroyOnLoad(gameObject);
		}

		void Update()
		{
			if (Input.GetKeyUp(UltimateMain.Instance.config.GetKey(UltimateSettings.TOGGLE_KEY_SETTING)))
			{
				if (!active)
				{
					Utility.ObjectBelowMouseInfo mi = Utility.getObjectBelowMouse();
					if (mi.hitSomething)
					{
						Vector3 vec = mi.hitPosition;
						vec.Set(vec.x, vec.y + UltimateMain.Instance.config.Height, vec.z);
						EnterHeadCam(vec);
					}
				}
				else
					LeaveHeadCam();
			}
			else if (Input.GetMouseButtonUp((int)FpsMouse.MOUSEBUTTON.LEFT))
			{
				if (active)
				{
					Utility.ObjectBelowMouseInfo result = Utility.getObjectBelowMouse();

					if (result.hitObject != null)
					{
						Attraction attr = result.hitObject.GetComponentInParent<Attraction>();
						if (attr != null)
							EnterCoasterCam(attr);
					}
				}
			}
			else if (Input.GetMouseButtonUp((int)FpsMouse.MOUSEBUTTON.RIGHT) && active && riding)
				LeaveCoasterCam();

			if (active)
			{
				if (disableUI)
				{
					GameController.Instance.setUICanvasVisibility(UICanvas.UICanvasTag.GameUI, false);
					disableUI = false;
				}
				if (InputManager.getKeyDown("HotkeyToggleUI"))
					disableUI = true;

				SmoothFPS();
			}
		}

		private void SmoothFPS()
		{
			float fps = 1.0f / Time.deltaTime;

			if (fps < 27 && Camera.main.farClipPlane > UltimateMain.Instance.config.ViewDistance / 1.25f)
				Camera.main.farClipPlane -= 0.3f;
			else if (fps > 28 && Camera.main.farClipPlane < UltimateMain.Instance.config.ViewDistance)
				Camera.main.farClipPlane += 0.3f;
		}

		public void EnterCoasterCam(Attraction attraction)
		{
			if (!active)
				return;

			List<Transform> seats = new List<Transform>();
			Utility.recursiveFindTransformsStartingWith("seat", attraction.transform, seats);
			if (seats.Count == 0)
				return;
			Transform seat = null;
			for (int i = 0; i < 100 && seat == null; i++)
				seat = seats[UnityEngine.Random.Range(0, seats.Count - 1)];
			if (seat == null)
				return;

			Camera.main.gameObject.GetComponent<PlayerController>().active = false;

			if (!riding)
			{
				EscapeHierarchy.Instance.push(new EscapeHierarchy.OnEscapeHandler(this.LeaveCoasterCam));
				_riding = true;
			}
			Camera.main.transform.parent = seat.transform;
			Camera.main.transform.localPosition = new Vector3(0, 0.35f, 0.1f);
			Camera.main.gameObject.GetComponent<FpsMouse>().reset();
		}

		public void LeaveCoasterCam()
		{
			if (!active || !riding)
				return;
			Exit ex = Camera.main.transform.parent.GetComponentInParent<Attraction>().getRandomExit();

			Vector3 position = ex.centerPosition;
			position.z += UltimateMain.Instance.config.Height;
			Camera.main.transform.parent = null;
			Camera.main.transform.position = position;

			// Path direction to yaw
			position = ex.getPathDirection();
			float yaw;
			if (position.x == 1.0f)
				yaw = 90.0f;
			else if (position.x == 1.0f)
				yaw = -90.0f;
			else if (position.z == -1.0f)
				yaw = 180.0f;
			else
				yaw = 0.0f;

			FpsMouse mouse = Camera.main.gameObject.GetComponent<FpsMouse>();
			mouse.yaw = yaw;
			mouse.pitch = 0.0f;
			Camera.main.gameObject.GetComponent<PlayerController>().active = true;
			EscapeHierarchy.Instance.remove(new EscapeHierarchy.OnEscapeHandler(this.LeaveCoasterCam));
			_riding = false;
		}

		public void EnterHeadCam(Vector3 position)
		{
			if (active)
				return;
			
			GameObject headCam = new GameObject();
			headCam.layer = LayerMasks.ID_DEFAULT;

			Camera cam = headCam.AddComponent<Camera>();
			cam.nearClipPlane = 0.025f; // 0.05
			cam.farClipPlane = UltimateMain.Instance.config.ViewDistance;
			cam.fieldOfView = UltimateMain.Instance.config.FoV;
			cam.depthTextureMode = DepthTextureMode.DepthNormals;
			cam.hdr = UltimateMain.Instance.config.HDR;
			cam.orthographic = false;
			headCam.AddComponent<AudioListener>();
			headCam.AddComponent<FpsMouse>();
			headCam.AddComponent<PlayerController>();

			CharacterController cc = headCam.AddComponent<CharacterController>();
			cc.radius = 0.1f;
			cc.height = UltimateMain.Instance.config.Height; //UltimateMain.Instance.height;
			float h = UltimateMain.Instance.config.Height / 2.0f;
			cc.center = new Vector3(0.0f, -h, 0.0f);
			cc.slopeLimit = 60.0f;
			cc.stepOffset = h;

			startX = position.x;
			startZ = position.z;
			headCam.transform.position = position;

			UIWorldOverlayController.Instance.gameObject.SetActive(false);

			mainCam = Camera.main;
			mainCam.enabled = mainCam.GetComponent<CameraController>().enabled = false;
			cam.tag = "MainCamera";
			cam.enabled = true;

			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;

			disableUI = true;

			GameController.Instance.pushGameInputLock();

			EscapeHierarchy.Instance.push(new EscapeHierarchy.OnEscapeHandler(this.LeaveHeadCam));
		}

		public void LeaveHeadCam()
		{
			if (!active)
				return;

			if (riding)
				LeaveCoasterCam();

			Camera cam = Camera.main;
			Vector3 mod = cam.gameObject.transform.position;
			mainCam.tag = cam.tag;
			cam.enabled = false;
			Destroy(cam.gameObject);
			mainCam.enabled = true;
			mainCam = null;

			Vector3 position = Camera.main.transform.position;
			float modX = mod.x - startX;
			float modZ = mod.z - startZ;
			position.x += modX;
			position.z += modZ;
			Camera.main.transform.position = position;
			Camera.main.GetComponent<CameraController>().enabled = true;

			UIWorldOverlayController.Instance.gameObject.SetActive(true);

			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;

			GameController.Instance.setUICanvasVisibility(UICanvas.UICanvasTag.GameUI, true);

			GameController.Instance.popGameInputLock();

			EscapeHierarchy.Instance.remove(new EscapeHierarchy.OnEscapeHandler(this.LeaveHeadCam));
		}

		void OnDestroy()
		{
			LeaveHeadCam();
			_Instance = null;
		}
	}
}
