using System;
using UltimateCam.API;
using UnityEngine;

namespace UltimateCam.Internal
{
	public class UltimateController : MonoBehaviour
	{
		private UltimateSettings config;
		private GameObject go;
		private GameObject head;
		private Camera parkitectCam;
		internal UltimateMouse mouse
		{
			get;
			private set;
		}
		private float[] start = new float[2];

		private float upSpeed = 0.0f;
		private Vector3 moveDirection = Vector3.zero;
		private bool onGround = false;
		private bool jetpack;
		private readonly float width = 0.4f;

		private static UltimateController _Instance = null;
		internal static UltimateController Instance()
		{
			return _Instance;
		}

		internal static UltimateController Instance(Vector3 position)
		{
			if (_Instance != null)
				return _Instance;

			GameObject tgo = new GameObject();
			tgo.layer = LayerMasks.ID_DEFAULT;

			GameObject hgo = new GameObject();
			hgo.transform.SetParent(tgo.transform);

			UltimateSettings config = API.UltimateCam.Instance.config;

			Camera cam = hgo.AddComponent<Camera>();
			cam.enabled = false;
			cam.nearClipPlane = 0.0275f; // 0.025
			cam.farClipPlane = config.ViewDistance;
			cam.fieldOfView = config.FoV;
			cam.depthTextureMode = DepthTextureMode.DepthNormals;
			cam.hdr = config.HDR;
			cam.orthographic = false;
			cam.transform.localPosition = new Vector3(0.0f, config.Height * 0.2f, 0.0f); //TODO: Ugly hack
			hgo.AddComponent<AudioListener>();

			_Instance = tgo.AddComponent<UltimateController>();
			_Instance.go = tgo;
			_Instance.head = hgo;
			_Instance.parkitectCam = Camera.main;
			_Instance.config = config;
			_Instance.mouse = hgo.AddComponent<UltimateMouse>();

			hgo.AddComponent<UltimateFader>();
			if (config.Crosshair)
				hgo.AddComponent<UltimateCross>().enabled = false;

			_Instance.start[0] = position.x;
			_Instance.start[1] = position.z;
			position.y += 0.2f;
			tgo.transform.position = position;

			cam.tag = "MainCamera";

			_Instance.jetpack = config.Jetpack;

			return _Instance;
		}

		void OnDestroy()
		{
			Destroy(head);
			Destroy(go);
			_Instance = null;
		}

		internal Camera getUltimateCam()
		{
			return head.GetComponent<Camera>();
		}

		internal Camera getParkitectCam()
		{
			return parkitectCam;
		}

		internal UltimateFader getFader()
		{
			return head.GetComponent<UltimateFader>();
		}

		internal void parkitectFollowUltimate()
		{
			Camera main = getParkitectCam();
			Vector3 mod = getUltimateCam().gameObject.transform.position;
			Vector3 position = main.transform.position;
			float modX = mod.x - start[0];
			float modZ = mod.z - start[1];
			position.x += modX;
			position.z += modZ;
			main.transform.position = position;
		}

		internal UltimateMouse getMouse()
		{
			return head.GetComponent<UltimateMouse>();
		}

		private MouseCollider.HitInfo rayFromTo(Vector3 from, Vector3 direction, float md, bool down)
		{
			int c = down ? 1 : 3;
			MouseCollider.HitInfo[] result = new MouseCollider.HitInfo[c];
			Ray ray;
			MouseCollider.HitInfo[] array;
			Vector3 tFrom;
			for (int i = 0; i < c; i++)
			{
				tFrom = from;
				if (i == 1)
					tFrom.z -= width / 2.0f;
				else if (i == 2)
					tFrom.z += width / 2.0f;
				
				ray = new Ray(tFrom, direction);
				result[i].hitDistance = md;
				result[i].hitObject = null;
				result[i].hitSomething = false;

				array = MouseCollisions.Instance.raycastAll(ray, md);
				for (int j = 0; j < array.Length; j++)
				{
					if (array[j].hitDistance < md)
					{
						SerializedMonoBehaviour componentInParent = array[j].hitObject.GetComponentInParent<SerializedMonoBehaviour>();
						if (componentInParent != null)
						{
							if (componentInParent is Person)
								continue;

							if (componentInParent.canBeSelected())
							{
								result[i] = array[j];
								result[i].hitObject = componentInParent.gameObject;
							}
						}
						else
							result[i].hitSomething = false;
					}
				}
			}

			int s = 0;
			if(!down)
				for (int i = 1; i < c; i++)
					if (result[i].hitSomething && result[i].hitDistance < result[s].hitDistance)
						s = i;
			return result[s];
		}

		void Update()
		{
			float speed = config.WalkingSpeed * 50.0f * Time.deltaTime;
			if (Input.GetKey(config.GetKey(UltimateSettings.ROTATE_LEFT_KEY_SETTING)))
				mouse.yaw -= speed;
			else if (Input.GetKey(config.GetKey(UltimateSettings.ROTATE_RIGHT_KEY_SETTING)))
				mouse.yaw += speed;

			if (!jetpack && !onGround && transform.position.y < 0.0f)
			{
				Vector3 pos = transform.position;
				pos.y = LandPatch.maxTerrainHeight;
				transform.position = pos;
				onGround = false;
				return;
			}

			if (onGround || jetpack)
			{
				speed = config.WalkingSpeed;

				moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
				moveDirection = transform.TransformDirection(moveDirection);
				moveDirection *= speed * Time.deltaTime;

				bool jump = Input.GetKey(config.GetKey(UltimateSettings.JUMP_KEY_SETTING));
				if (onGround)
				{
					upSpeed = jump ? 0.1f /* * Time.deltaTime */ : 0.0f;
					moveDirection.y = upSpeed * Time.deltaTime;
				}
				else if (jump) // Jetpack
				{
					upSpeed = speed * Time.deltaTime;
					moveDirection.y = upSpeed;
					onGround = true;
				}
			}

			if (!onGround)
			{
				upSpeed -= config.Gravity * Time.deltaTime;
				moveDirection *= 0.999f;
			}

			moveDirection.y = upSpeed;

			if (moveDirection == Vector3.zero)
				return;

			float height = config.Height;
			Vector3 feet = transform.position;
			float stepHeight = height / 2.0f;
			feet.y -= stepHeight;
			Vector3 to = new Vector3(feet.x + moveDirection.x, feet.y + moveDirection.y, feet.z + moveDirection.z);
			Park park = GameController.Instance.park;
			MouseCollider.HitInfo result = rayFromTo(to, Vector3.down * height, height, true);
			to.y -= stepHeight;
			float top;

			if (result.hitSomething)
			{
				SerializedMonoBehaviour smb = result.hitObject.GetComponent<SerializedMonoBehaviour>();
				if (smb is Path || smb is AttractionPlatform)
				{
					Block block = (Block)smb;
					top = block.getTopSideY(to);
				}
				else // TODO
					top = result.hitPosition.y;
			}
			else
				top = park.getHeightAt(to);

			if (to.y > top)
				onGround = false;
			else
			{
				if (to.y < top)
				{
					if (top - to.y <= height / 2.0f)
					{
						to.y = feet.y = top;
						moveDirection.y = 0.0f;
						onGround = true;
					}
					else
					{
						to.x = feet.x;
						to.z = feet.z;
						moveDirection.x = moveDirection.z = 0.0f;
					}
				}
				else
					onGround = true;
			}

			//UltimateMain.Instance.Log("obj: " + (result.hitObject != null ? result.hitObject.GetType().ToString() : "NULL") + " Distance: " + result.hitDistance + " / " + md, UltimateMain.LogLevel.INFO);
			result = rayFromTo(feet, moveDirection, moveDirection.magnitude, false);
			if (result.hitSomething)
			{
				SerializedMonoBehaviour smb = result.hitObject.GetComponent<SerializedMonoBehaviour>();
				if (!(smb is Path) || !((Path)smb).isUnderground())
				{
					//UltimateMain.Instance.Log("rx: " + moveDirection.x + " / hpx: " + hit.x + " / posx: " + pos.x, UltimateMain.LogLevel.INFO);
					//UltimateMain.Instance.Log("rz: " + moveDirection.z + " / hpz: " + hit.z + " / posz: " + pos.z, UltimateMain.LogLevel.INFO);

					// Right / Left
					UltimateMain.Instance.Log("X: " + moveDirection.x + " / " + result.hitPosition.x + " / " + to.x, UltimateMain.LogLevel.INFO);
					if ((moveDirection.x > 0.0f && result.hitPosition.x >= to.x) || (moveDirection.x < 0.0f && result.hitPosition.x <= to.x))
						to.x = feet.x;
					// Forward / Backward
					UltimateMain.Instance.Log("Z: " + moveDirection.z + " / " + result.hitPosition.z + " / " + to.z, UltimateMain.LogLevel.INFO);
					if ((moveDirection.z > 0.0f && result.hitPosition.z >= to.z) || (moveDirection.z < 0.0f && result.hitPosition.z <= to.z))
						to.z = feet.z;
				}
			}

			to.y += height;
			transform.position = to;
		}
	}
}
