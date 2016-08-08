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

		//DUMMY:
		internal readonly bool isGrounded = true;

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

		private MouseCollider.HitInfo rayFromTo(Vector3 from, Vector3 direction, float md)
		{
			MouseCollider.HitInfo result = default(MouseCollider.HitInfo);
			Ray ray = new Ray(from, direction);
			result.hitDistance = md;
			result.hitObject = null;

			MouseCollider.HitInfo[] array = MouseCollisions.Instance.raycastAll(ray, result.hitDistance);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].hitDistance < result.hitDistance)
				{
					SerializedMonoBehaviour componentInParent = array[i].hitObject.GetComponentInParent<SerializedMonoBehaviour>();
					if (componentInParent != null)
					{
						if (componentInParent is Person)
							continue;

						if (componentInParent.canBeSelected())
						{
							result = array[i];
							result.hitObject = componentInParent.gameObject;
						}
					}
					else
						result.hitObject = null;
				}
			}

			return result;
		}

		void Update()
		{
			float speed = config.WalkingSpeed * 50.0f * Time.deltaTime;
			if (Input.GetKey(config.GetKey(UltimateSettings.ROTATE_LEFT_KEY_SETTING)))
				mouse.yaw -= speed;
			else if (Input.GetKey(config.GetKey(UltimateSettings.ROTATE_RIGHT_KEY_SETTING)))
				mouse.yaw += speed;

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

			float md = moveDirection.magnitude + 0.2f;
			MouseCollider.HitInfo result = rayFromTo(transform.position, moveDirection, md);

			float height = config.Height;
			Vector3 feet = transform.position;
			feet.y -= height;
			Vector3 to = new Vector3(feet.x + moveDirection.x, feet.y + moveDirection.y, feet.z + moveDirection.z);
			Park park = GameController.Instance.park;

			//UltimateMain.Instance.Log("obj: " + (result.hitObject != null ? result.hitObject.GetType().ToString() : "NULL") + " Distance: " + result.hitDistance + " / " + md, UltimateMain.LogLevel.INFO);
			if (result.hitObject != null && result.hitDistance <= md)
			{
				SerializedMonoBehaviour smb = result.hitObject.GetComponent<SerializedMonoBehaviour>();
				if (!(smb is Path) || !((Path)smb).isUnderground())
				{
					//UltimateMain.Instance.Log("rx: " + moveDirection.x + " / hpx: " + hit.x + " / posx: " + pos.x, UltimateMain.LogLevel.INFO);
					//UltimateMain.Instance.Log("rz: " + moveDirection.z + " / hpz: " + hit.z + " / posz: " + pos.z, UltimateMain.LogLevel.INFO);

					// Right / Left
					if ((moveDirection.x > 0.0f && result.hitPosition.x >= to.x) || (moveDirection.x < 0.0f && result.hitPosition.x <= to.x))
						result.hitPosition.x = to.x = feet.x;
					// Forward / Backward
					if ((moveDirection.z > 0.0f && result.hitPosition.z >= to.z) || (moveDirection.z < 0.0f && result.hitPosition.z <= to.z))
						result.hitPosition.z = to.z = feet.z;
				}
			}

			Block block = park.blockData.getBlock(to);
			float top = block == null ? float.MinValue : block is Path || block is AttractionPlatform ? block.getTopSideY(to) : result.hitPosition.y;
			if (top == float.MinValue)
			{
				md = 0.5f;
				Vector3 np = to;
				np.y += md;
				md *= 2.0f;
				result = rayFromTo(np, Vector3.down * md, md);
				if (result.hitObject != null && result.hitDistance <= md)
				{
					SerializedMonoBehaviour smb = result.hitObject.GetComponent<SerializedMonoBehaviour>();
					if (smb is Path || smb is AttractionPlatform)
					{
						block = (Block)smb;
						top = block.getTopSideY(to);
					}
					else // TODO
						top = result.hitPosition.y;
				}
			}

			if (top == float.MinValue)
				top = park.getHeightAt(to);
			
			if (to.y > top)
				onGround = false;
			else
			{
				if (to.y < top)
				{
					if (top - to.y <= height / 2.0f)
					{
						to.y = top;
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
			to.y += height;
			transform.position = to;
		}
	}
}
