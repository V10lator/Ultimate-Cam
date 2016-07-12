﻿using System;
using System.Collections;
using System.Collections.Generic;
using Parkitect.UI;
using UnityEngine;

namespace UltimateCam
{
	class UltimateCam : MonoBehaviour
	{
		private GameObject _headCam = null;
		public static UltimateCam Instance;
		private Camera _cam;
		float fps = 0.0f;
		float startX, startZ;
		bool active {
			get { return _headCam != null; }
		}
		bool riding = false;
		bool disableUI;

		void Awake()
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}

		void Update()
		{
			if (Input.GetKeyUp (UltimateMain.Instance.config.GetKey (UltimateSettings.TOGGLE_KEY_SETTING))) {
				if (!active) {
					Utility.ObjectBelowMouseInfo mi = Utility.getObjectBelowMouse ();
					if (mi.hitSomething) {
						Vector3 vec = mi.hitPosition;
						vec.Set (vec.x, vec.y + UltimateMain.Instance.config.Height, vec.z);
						EnterHeadCam (vec);
					}
				} else
					LeaveHeadCam ();
			} else if (Input.GetMouseButtonUp ((int)FpsMouse.MOUSEBUTTON.LEFT)) {
				if (active) {
					Utility.ObjectBelowMouseInfo result = default(Utility.ObjectBelowMouseInfo);
					Ray ray = _cam.ScreenPointToRay (Input.mousePosition);
					result.hitDistance = float.MaxValue;
					result.hitObject = null;
					GameObject gameObject = Collisions.Instance.checkSelectables(ray, out result.hitDistance);
					if (gameObject != null)
					{
						SerializedMonoBehaviour component = gameObject.GetComponent<SerializedMonoBehaviour>();
						if (component != null && component.canBeSelected())
						{
							result.hitObject = component;
							result.hitPosition = ray.GetPoint(result.hitDistance);
							result.hitLayerMask = 1 << component.gameObject.layer;
						}
						else
							result.hitDistance = float.MaxValue;
					}
					GameController.Instance.enableVisibleMouseColliders();
					RaycastHit[] array = Physics.RaycastAll(ray, result.hitDistance, LayerMasks.MOUSECOLLIDERS | LayerMasks.TERRAIN);
					for (int i = 0; i < array.Length; i++)
					{
						RaycastHit raycastHit = array[i];
						if (raycastHit.distance < result.hitDistance)
						{
							SerializedMonoBehaviour componentInParent = raycastHit.collider.gameObject.GetComponentInParent<SerializedMonoBehaviour>();
							bool flag = false;
							if (componentInParent != null && componentInParent.canBeSelected())
							{
								result.hitObject = componentInParent;
								result.hitLayerMask = 1 << raycastHit.collider.gameObject.layer;
								flag = true;
							}
							if (componentInParent == null || flag)
							{
								result.hitPosition = raycastHit.point;
								result.hitDistance = raycastHit.distance;
								result.hitNormal = raycastHit.normal;
							}
							if (componentInParent == null)
							{
								result.hitObject = null;
								result.hitLayerMask = 0;
							}
						}
					}
					GameController.Instance.disableMouseColliders();

					if (result.hitObject != null) {
						Attraction attr = result.hitObject.GetComponentInParent<Attraction> ();
						if (attr != null)
							EnterCoasterCam (attr);
					}
				}
			} else if (Input.GetMouseButtonUp ((int)FpsMouse.MOUSEBUTTON.RIGHT) && active && riding)
				LeaveCoasterCam ();

			if (active) {
				if (disableUI) {
					GameController.Instance.setUICanvasVisibility (UICanvas.UICanvasTag.GameUI, false);
					disableUI = false;
				}
				if (InputManager.getKeyDown ("HotkeyToggleUI"))
					disableUI = true;
				
				AdaptFarClipPaneToFPS ();
			}
		}

		private void AdaptFarClipPaneToFPS()
		{
			fps = 1.0f / Time.deltaTime;

			if (fps < 50)
				_cam.farClipPlane = Math.Max(40, _cam.farClipPlane - 0.3f);
			else if (fps > 55)
				_cam.farClipPlane = Math.Min(120, _cam.farClipPlane + 0.3f);
		}

		public void EnterCoasterCam(Attraction attraction)
		{
			if (!active)
				return;

			List<Transform> seats = new List<Transform> ();
			Utility.recursiveFindTransformsStartingWith("seat", attraction.transform, seats);
			if (seats.Count == 0)
				return;
			Transform seat = null;
			for (int i = 0; i < 100 && seat == null; i++)
				seat = seats [UnityEngine.Random.Range(0, seats.Count - 1)];
			if (seat == null)
				return;

			_headCam.GetComponent<PlayerController> ().active = false;

			if (!riding) {
				EscapeHierarchy.Instance.push (new EscapeHierarchy.OnEscapeHandler(this.LeaveCoasterCam));
				riding = true;
			}
			_cam.transform.parent = seat.transform;
			_cam.transform.localPosition = new Vector3(0, 0.35f, 0.1f);
		}

		public void LeaveCoasterCam()
		{
			if (!active || !riding)
				return;

			Vector3 position = _cam.transform.parent.GetComponentInParent<Attraction> ().getRandomExit ().transform.position;
			position = new Vector3 (position.x, position.y + UltimateMain.Instance.config.Height, position.z);
			_cam.transform.parent = null;
			_cam.transform.position = position;
			EscapeHierarchy.Instance.remove (new EscapeHierarchy.OnEscapeHandler(this.LeaveCoasterCam));
			_headCam.GetComponent<PlayerController> ().active = true;
			riding = false;
		}

		public void EnterHeadCam(Vector3 position)
		{
			if (active)
				return;

			_headCam = new GameObject();
			_headCam.layer = LayerMask.NameToLayer("CoasterCars");

			_cam = _headCam.AddComponent<Camera>();
			_cam.nearClipPlane = 0.025f; // 0.05
			_cam.farClipPlane = UltimateMain.Instance.config.ViewDistance;
			_cam.fieldOfView = UltimateMain.Instance.config.FoV;
			_cam.depthTextureMode = DepthTextureMode.DepthNormals;
			_cam.hdr = true;
			_cam.orthographic = false;
			_headCam.AddComponent<AudioListener>();
			_headCam.AddComponent<FpsMouse>();
			_headCam.AddComponent<PlayerController>();
			CharacterController cc = _headCam.AddComponent<CharacterController>();

			cc.radius = 0.1f;
			cc.height = UltimateMain.Instance.config.Height; //UltimateMain.Instance.height;
			float h = UltimateMain.Instance.config.Height / 2.0f;
			cc.center = new Vector3(0.0f, -h, 0.0f);
			cc.slopeLimit = 60.0f;
			cc.stepOffset = h;

			startX = position.x;
			startZ = position.z;
			_headCam.transform.position = position;

			UIWorldOverlayController.Instance.gameObject.SetActive(false);

			Camera.main.GetComponent<CameraController>().enabled = false;

			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;

			disableUI = true;

			GameController.Instance.pushGameInputLock ();

			EscapeHierarchy.Instance.push (new EscapeHierarchy.OnEscapeHandler(this.LeaveHeadCam));
		}

		public void LeaveHeadCam()
		{
			if (!active)
				return;

			if (riding)
				LeaveCoasterCam ();

			Vector3 mod = _headCam.transform.position;
			Destroy(_headCam);
			_headCam = null;

			Vector3 position = Camera.main.transform.position;
			float modX = mod.x - startX;
			float modZ = mod.z - startZ;
			position = new Vector3 (position.x + modX, position.y, position.z + modZ);
			Camera.main.transform.position = position;
			Camera.main.GetComponent<CameraController>().enabled = true;

			UIWorldOverlayController.Instance.gameObject.SetActive(true);

			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;

			GameController.Instance.setUICanvasVisibility (UICanvas.UICanvasTag.GameUI, true);

			GameController.Instance.popGameInputLock ();

			EscapeHierarchy.Instance.remove (new EscapeHierarchy.OnEscapeHandler(this.LeaveHeadCam));

			_headCam = null;
		}

		void OnDestroy()
		{
			LeaveHeadCam();
			Instance = null;
			_cam = null;
		}
	}
}
