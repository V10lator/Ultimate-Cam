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
		private bool jetpack;

		private static UltimateController _Instance = null;
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

		void Update()
		{
			float speed = config.WalkingSpeed * 50.0f * Time.deltaTime;
			if (Input.GetKey(config.GetKey(UltimateSettings.ROTATE_LEFT_KEY_SETTING)))
				mouse.yaw -= speed;
			else if (Input.GetKey(config.GetKey(UltimateSettings.ROTATE_RIGHT_KEY_SETTING)))
				mouse.yaw += speed;

			Vector3 feet = transform.position;
			float height = config.Height;
			feet.y -= height;
			Park park = GameController.Instance.park;
			float th = park.getHeightAt(feet);
			Block block = park.blockData.getBlock(feet);
			float top;
			bool grounded;
			if (block != null) // Tunnel / Bridge / Path
			{
				top = block.getTopSideY(feet);
				if (block is Path)
				{
					if (top < th)
					{
						if (top > feet.y) // We somehow felt into the ground (tunnel up?) - fixing...
							grounded = false; // This will be catched later on...
						else
							grounded = !(top < feet.y);
					}
					else
					{
						if (top > feet.y)
							grounded = false; //  Will be catched later on...
						else
							grounded = top == feet.y;
					}
				}
				else
					grounded = false;
				UltimateMain.Instance.Log("Block: " + block.GetType(), UltimateMain.LogLevel.INFO);
			}
			else // Terrain or worse
			{
				UltimateMain.Instance.Log("Block: NULL", UltimateMain.LogLevel.INFO);
				top = th;
				grounded = top == feet.y;

				/*if (!grounded && !jetpack && top > feet.y) // Corner case: Walked under the map
				{
					moveDirection = transform.position;
					moveDirection.y = top + height;
					transform.position = moveDirection;
					moveDirection = Vector3.zero;
					return;
				}*/
			}

			if (!grounded) // Check round two...
			{
				if (top > feet.y) // catch for the same ifs as above.
				{
					UltimateMain.Instance.Log("Fixing FTM (from " + feet.y + " to " + top + ")", UltimateMain.LogLevel.INFO);
					transform.position = new Vector3(feet.x, top + height, feet.z);
					grounded = true;
				}
				else if (top < feet.y)
					grounded = false;
				else
					grounded = true;
			}


			if (grounded || jetpack)
			{
				speed = config.WalkingSpeed;

				moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
				moveDirection = transform.TransformDirection(moveDirection);
				moveDirection *= speed * Time.deltaTime;

				bool jump = Input.GetKey(config.GetKey(UltimateSettings.JUMP_KEY_SETTING));
				if (grounded)
				{
					upSpeed = jump ? 0.1f /* * Time.deltaTime */ : 0.0f;
					moveDirection.y = upSpeed * Time.deltaTime;
				}
				else if (jump) // Jetpack
				{
					upSpeed = speed * Time.deltaTime;
					moveDirection.y = upSpeed;
					grounded = true;
				}
			}

			if (!grounded)
			{
				upSpeed -= config.Gravity * Time.deltaTime;
				moveDirection *= 0.999f;
			}

			moveDirection.y = upSpeed;

			if (moveDirection == Vector3.zero)
				return;

			Vector3 to = new Vector3(feet.x + moveDirection.x, feet.y + moveDirection.y, feet.z + moveDirection.z);

			Utility.ObjectBelowMouseInfo result = default(Utility.ObjectBelowMouseInfo);
			Ray ray = new Ray(transform.position, moveDirection);
			float md = moveDirection.magnitude + 0.2f;
			result.hitDistance = md;
			result.hitObject = null;
			GameObject sl = Collisions.Instance.checkSelectables(ray, out result.hitDistance);
			if (sl != null)
			{
				SerializedMonoBehaviour component = sl.GetComponent<SerializedMonoBehaviour>();
				if (component != null && component.canBeSelected())
				{
					result.hitObject = component;
					result.hitPosition = ray.GetPoint(result.hitDistance);
					result.hitLayerMask = 1 << component.gameObject.layer;
				}
				else
					result.hitDistance = md;
			}

			MouseCollider.HitInfo[] array = MouseCollisions.Instance.raycastAll(ray, result.hitDistance);
			for (int i = 0; i < array.Length; i++)
			{
				MouseCollider.HitInfo raycastHit = array[i];
				if (raycastHit.hitDistance < result.hitDistance)
				{
					SerializedMonoBehaviour componentInParent = raycastHit.hitObject.GetComponentInParent<SerializedMonoBehaviour>();
					bool flag = false;
					if (componentInParent != null && componentInParent.canBeSelected())
					{
						result.hitObject = componentInParent;
						result.hitLayerMask = 1 << raycastHit.hitObject.layer;
						flag = true;
					}
					if (componentInParent == null || flag)
					{
						result.hitPosition = raycastHit.hitPosition;
						result.hitDistance = raycastHit.hitDistance;
						result.hitNormal = raycastHit.hitNormal;
					}
					if (componentInParent == null)
					{
						result.hitObject = null;
						result.hitLayerMask = 0;
					}
				}
			}
			//UltimateMain.Instance.Log("obj: " + (result.hitObject != null ? result.hitObject.GetType().ToString() : "NULL") + " Distance: " + result.hitDistance + " / " + md, UltimateMain.LogLevel.INFO);
			if (result.hitObject != null)
			{
				if (result.hitObject is Person)
				{
					Person p = (Person)result.hitObject;
					Vector3 direction = p.currentPosition - transform.position;
					direction /= direction.magnitude;
					direction = Quaternion.Euler(180.0f, 0.0f, 0.0f) * direction;
					direction.x += p.currentPosition.x;
					direction.y = p.currentPosition.y;
					direction.z += p.currentPosition.z;
					block = park.blockData.getBlock(direction);
					direction.y = block == null ? park.getHeightAt(direction) : block.getTopSideY(direction);

					p.instantlyChangeBehaviour<RoamingBehaviour>();
					p.setPosition(direction);
				}
				else if (/*!(result.hitObject is Path) &&*/ result.hitDistance <= md && !(result.hitObject is Person))
				{
					Vector3 hit = result.hitPosition;

					//UltimateMain.Instance.Log("rx: " + moveDirection.x + " / hpx: " + hit.x + " / posx: " + pos.x, UltimateMain.LogLevel.INFO);
					//UltimateMain.Instance.Log("rz: " + moveDirection.z + " / hpz: " + hit.z + " / posz: " + pos.z, UltimateMain.LogLevel.INFO);

					// Right / Left
					if ((moveDirection.x > 0.0f && hit.x >= to.x) || (moveDirection.x < 0.0f && hit.x <= to.x))
						to.x = feet.x;
					// Forward / Backward
					if ((moveDirection.z > 0.0f && hit.z >= to.z) || (moveDirection.z < 0.0f && hit.z <= to.z))
						to.z = feet.z;
					/*if (moveDirection.y > 0.0f && hit.y + height <= to.y)
					{
						while (hit.y + height <= to.y)
							to.y -= 0.01f;
						upSpeed = 0.0f;
					}
					else if (moveDirection.y < 0.0f && hit.y >= to.y)
					{
						while (hit.y <= to.y)
							to.y += 0.01f;
					}*/
				}
			}

			bool ignore = false;
			if (result.hitObject == null)
				top = park.getHeightAt(to);
			else if (result.hitObject is Block)
				top = ((Block)result.hitObject).getTopSideY(to);
			else
				ignore = true;

			if (!ignore)
			{
				//top = result.hitObject == null ? park.getHeightAt(to) : block.getTopSideY(to);
				if (grounded && top > to.y)
				{
					if (top > to.y + (height / 1.2f))
						to = feet;
					else
						to.y = top;
				}
			}

			to.y += height;
			transform.position = to;
		}
	}
}
