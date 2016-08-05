using System.Collections.Generic;
using System.Reflection;
using Parkitect.UI;
using UltimateCam.Internal;
using UnityEngine;

namespace UltimateCam.API
{
	public class UltimateCam : MonoBehaviour
	{
		public static UltimateCam Instance
		{
			get;
			private set;
		}
		private Camera mainCam = null;
		private float startX, startZ;
		public static bool active {get { return Instance.mainCam != null; } }
		public static bool sitting {
			get;
			internal set;
		}
		public static bool following {
			get;
			internal set;
		}

		private bool disableUI;
		private int seat = -1;

		internal UltimateMouse mouse;

		public UltimateSettings config
		{
			get;
			private set;
		}

		void Awake()
		{
			config = UltimateMain.Instance.config;
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}

		void Update()
		{
			GameController gc = GameController.Instance;
			if (gc == null || !gc.enabled || gc.isLoadingGame || gc.isQuittingGame || OptionsMenu.instance != null || UIWindowsController.Instance.getWindows().Count > 0)
				return;

			if (Input.GetKeyUp(config.GetKey(UltimateSettings.TOGGLE_KEY_SETTING)))
			{
				if (!active)
				{
					Utility.ObjectBelowMouseInfo mi = Utility.getObjectBelowMouse();
					if (mi.hitSomething)
					{
						Vector3 vec = mi.hitPosition;
						vec.Set(vec.x, vec.y + config.Height, vec.z);
						EnterHeadCam(vec);
					}
				}
				else
					LeaveHeadCam();
			}
			else if(active)
			{
				if (!following && Input.GetMouseButtonUp((int)UltimateMouse.MOUSEBUTTON.LEFT))
				{
					Utility.ObjectBelowMouseInfo result = Utility.getObjectBelowMouse();
					SerializedMonoBehaviour smb = result.hitObject;
					if (smb != null)
					{
						if (smb is Seating)
							EnterSeatCam((Seats)typeof(Seating).GetField("seats", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(smb));
						else if (smb is Person)
							EnterFollowerCam((Person)smb);
						{
							Attraction attr = smb.GetComponentInParent<Attraction>();
							if (attr != null)
								EnterSeatCam(attr);
							else
							{
								Person person = smb.GetComponentInParent<Person>();
								if (person != null)
									EnterFollowerCam(person);
							}
						}
					}
				}
				else if (Input.GetMouseButtonUp((int)UltimateMouse.MOUSEBUTTON.RIGHT))
				{
					if (sitting)
						LeaveSeatCam();
					else if (following)
						LeaveFollowerCam();
					else
						LeaveHeadCam();
				}
			}

			if (active)
			{
				if (disableUI)
				{
					gc.setUICanvasVisibility(UICanvas.UICanvasTag.GameUI, false);
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

			if (fps < 27 && Camera.main.farClipPlane > config.ViewDistance / 1.25f)
				Camera.main.farClipPlane -= 0.3f;
			else if (fps > 28 && Camera.main.farClipPlane < config.ViewDistance)
				Camera.main.farClipPlane += 0.3f;
		}

		public void EnterFollowerCam(Person person)
		{
			if (!active || UltimateFader.active)
				return;

			if (sitting)
			{
				cleanupSeatCam();
				sitting = false;
			}
			
			Camera.main.GetComponentInParent<PlayerController>().enabled = false;
			mouse.enabled = false;
			person.OnKilled += this.LeaveFollowerCamFast;

			EscapeHierarchy.Instance.push(new EscapeHierarchy.OnEscapeHandler(this.LeaveFollowerCam));

			Camera.main.GetComponent<UltimateFader>().fade(person.head, false);
		}

		public void LeaveFollowerCam()
		{
			LeaveFollowerCam(false);
		}

		public void LeaveFollowerCamFast()
		{
			LeaveFollowerCam(true);
		}

		internal void cleanupFollowerCam(Vector3 to, float yaw)
		{
			Camera.main.transform.GetComponentInParent<Person>().OnKilled -= this.LeaveFollowerCamFast;
			Camera.main.transform.parent.parent = null;
			Camera.main.transform.parent.position = to;
			mouse.yaw = yaw;
			mouse.pitch = 0.0f;
			Camera.main.GetComponentInParent<PlayerController>().enabled = true;
			mouse.enabled = true;
			Camera.main.GetComponent<UltimateCross>().enabled = true;
			UltimateCam.following = false;
		}

		public void LeaveFollowerCam(bool fast)
		{
			if (!active || !following)
				return;

			if (!fast)
			{
				if (!UltimateFader.active)
					Camera.main.GetComponent<UltimateFader>().fade(Camera.main.transform.parent.parent.position, Camera.main.transform.parent.parent.eulerAngles.y - 90.0f, false);
			}
			else
				cleanupFollowerCam(Camera.main.transform.parent.parent.position, Camera.main.transform.parent.parent.eulerAngles.y - 90.0f);

			EscapeHierarchy.Instance.remove(new EscapeHierarchy.OnEscapeHandler(this.LeaveFollowerCam));
		}

		private void EnterSeatCam(Transform s)
		{
			Camera.main.GetComponentInParent<PlayerController>().enabled = false;

			if (!sitting)
				EscapeHierarchy.Instance.push(new EscapeHierarchy.OnEscapeHandler(this.LeaveSeatCam));
			
			Camera.main.GetComponent<UltimateFader>().fade(s, true);
		}

		public void EnterSeatCam(Seats seats)
		{
			if (!active || UltimateFader.active)
				return;

			if (seats.Count == 0)
				return;
			if (++seat >= seats.Count)
				seat = 0;
			Transform s = seats[seat].transform;
			if (s != null)
				EnterSeatCam(s);
		}

		public void EnterSeatCam(Attraction attraction)
		{
			if (!active || UltimateFader.active)
				return;

			List<Transform> seats = new List<Transform>();
			Utility.recursiveFindTransformsStartingWith("seat", attraction.transform, seats);

			if (seats.Count == 0)
				return;
			if (++seat >= seats.Count)
				seat = 0;
			Transform s = seats[seat];
			if (s != null)
				EnterSeatCam(s);
		}

		private void cleanupSeatCam()
		{
			EscapeHierarchy.Instance.remove(new EscapeHierarchy.OnEscapeHandler(this.LeaveSeatCam));
			seat = -1;
		}

		public void LeaveSeatCam()
		{
			if (!active || !sitting || UltimateFader.active)
				return;

			Vector3 position;
			float yaw;
			Attraction attr = Camera.main.transform.parent.parent.GetComponentInParent<Attraction>();
			if (attr != null)
			{
				Exit ex = attr.getRandomExit();

				position = ex.centerPosition;
				position.y += config.Height;

				// Path direction to yaw
				Vector3 dir = ex.getPathDirection();
				if (dir.x == 1.0f)
					yaw = 90.0f;
				else if (dir.x == -1.0f)
					yaw = -90.0f;
				else if (dir.z == -1.0f)
					yaw = 180.0f;
				else
					yaw = 0.0f;
			}
			else
			{
				position = Camera.main.transform.parent.parent.position;
				yaw = Camera.main.transform.parent.parent.eulerAngles.y;
			}

			Camera.main.GetComponent<UltimateFader>().fade(position, yaw, true);

			cleanupSeatCam();
		}

		public void EnterHeadCam(Vector3 position)
		{
			if (active || UltimateFader.active)
				return;
			
			GameObject headCam = new GameObject();
			headCam.layer = LayerMasks.ID_DEFAULT;

			GameObject head = new GameObject();
			head.transform.SetParent(headCam.transform);

			Camera cam = head.AddComponent<Camera>();
			cam.nearClipPlane = 0.0275f; // 0.025
			cam.farClipPlane = config.ViewDistance;
			cam.fieldOfView = config.FoV;
			cam.depthTextureMode = DepthTextureMode.DepthNormals;
			cam.hdr = config.HDR;
			cam.orthographic = false;
			head.AddComponent<AudioListener>();
			mouse = head.AddComponent<UltimateMouse>();

			mouse.controller = headCam.AddComponent<PlayerController>();
			UltimateFader fader = head.AddComponent<UltimateFader>();
			if (config.Crosshair)
				head.AddComponent<UltimateCross>().enabled = false;

			CharacterController cc = headCam.AddComponent<CharacterController>();
			cc.radius = 0.1f;
			cc.height = config.Height;
			float h = cc.height / 2.0f;
			cc.center = new Vector3(0.0f, -h, 0.0f);
			cc.slopeLimit = 60.0f;
			cc.stepOffset = h;

			startX = position.x;
			startZ = position.z;
			headCam.transform.position = position;

			mainCam = Camera.main;
			cam.tag = "MainCamera";
			cam.enabled = mainCam.GetComponent<CameraController>().enabled = false;
			fader.fade(mainCam, cam, false, true);

			EscapeHierarchy.Instance.push(new EscapeHierarchy.OnEscapeHandler(this.LeaveHeadCam));
		}

		public void LeaveHeadCam()
		{
			if (!active || UltimateFader.active)
				return;

			if (sitting)
			{
				cleanupSeatCam();
				sitting = false;
			}
			else if (following)
			{
				EscapeHierarchy.Instance.remove(new EscapeHierarchy.OnEscapeHandler(this.LeaveFollowerCam));
				following = false;
			}

			Vector3 mod = Camera.main.gameObject.transform.position;
			Vector3 position = mainCam.transform.position;
			float modX = mod.x - startX;
			float modZ = mod.z - startZ;
			position.x += modX;
			position.z += modZ;
			mouse.enabled = false;
			Camera.main.GetComponentInParent<PlayerController>().enabled = false;
			mainCam.transform.position = position;

			Camera.main.GetComponent<UltimateFader>().fade(Camera.main, mainCam, true, false);
			mainCam = null;

			EscapeHierarchy.Instance.remove(new EscapeHierarchy.OnEscapeHandler(this.LeaveHeadCam));
		}

		void OnDestroy()
		{
			LeaveHeadCam();
			config = null;
			Instance = null;
		}
	}
}
