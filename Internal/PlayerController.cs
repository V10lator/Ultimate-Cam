using UltimateCam.API;
using UnityEngine;

namespace UltimateCam.Internal
{
	internal class PlayerController : MonoBehaviour
	{
		private float upSpeed = 0.0f;

		private Vector3 moveDirection = Vector3.zero;
		private UltimateSettings config;
		private CharacterController controller;
		private UltimateMouse mouse;

		private bool tunnelGate = false;

		// Use this for initialization
		void Start()
		{
			config = UltimateMain.Instance.config;
			mouse = API.UltimateCam.Instance.mouse;

			controller = GetComponent<CharacterController>();
			controller.detectCollisions = controller.enableOverlapRecovery = true;
		}

		// Update is called once per frame
		void Update()
		{
			float speed = config.WalkingSpeed * 50.0f * Time.deltaTime;
			if (Input.GetKey(config.GetKey(UltimateSettings.ROTATE_LEFT_KEY_SETTING)))
				mouse.yaw -= speed;
			else if(Input.GetKey(config.GetKey(UltimateSettings.ROTATE_RIGHT_KEY_SETTING)))
				mouse.yaw += speed;
			speed = config.WalkingSpeed;

			// Detect tunnels
			bool grounded;
			Vector3 feet = transform.position;
			float height = config.Height;
			feet.y -= height;
			Park park = GameController.Instance.park;
			float th = park.getHeightAt(feet);
			Block block = park.blockData.getBlock(feet);
			if (config.TunnelMode)
			{
				if (controller.isGrounded || tunnelGate)
				{
					bool texit = false;
					if (block != null && block is Path)
					{
						float top = block.getTopSideY(feet);
						if (top < th && top + 0.95f >= th)
						{
							if (!tunnelGate)
							{
								controller.detectCollisions = controller.enableOverlapRecovery = false;
								tunnelGate = true;
							}

							if (top < feet.y || top > feet.y) // Tunnel down || We somehow felt into the ground (tunnel up?) - fixing... TODO: This prevents jumping / jetpack
							{
								transform.position = new Vector3(feet.x, top + height, feet.z);
								grounded = true;
							}
							else // All fine
								grounded = true;
						}
						else
						{
							texit = true;
							grounded = controller.isGrounded;
						}
					}
					else
					{
						if (tunnelGate)
							texit = true;
						grounded = true;
					}

					if (texit && tunnelGate)
					{
						controller.detectCollisions = controller.enableOverlapRecovery = true;
						tunnelGate = false;
					}
				}
				else
					grounded = false;
			}
			else
				grounded = controller.isGrounded;

			bool jetpack = config.Jetpack;
			if (!grounded && !jetpack && block == null && th > feet.y) // Corner case: Walked under the map
			{
				moveDirection = transform.position;
				moveDirection.y += 0.1f * Time.deltaTime;
				transform.position = moveDirection;
				moveDirection = Vector3.zero;
				return;
			}

			bool falling;
			if (grounded || jetpack)
			{
				moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
				moveDirection = transform.TransformDirection(moveDirection);
				moveDirection *= speed * Time.deltaTime;

				bool jump = Input.GetKey(config.GetKey(UltimateSettings.JUMP_KEY_SETTING));
				if (grounded)
				{
					upSpeed = jump ? 0.1f : 0.0f;
					moveDirection.y = upSpeed * Time.deltaTime;
					falling = jetpack = false;
				}
				else // config.Jetpack
				{
					if (jump)
					{
						upSpeed = speed * Time.deltaTime;
						moveDirection.y = upSpeed;
						falling = false;
					}
					else
						falling = true;
					jetpack = true;
				}
			}
			else
			{
				falling = true;
				jetpack = false;
			}

			if (falling)
			{
				upSpeed -= config.Gravity * Time.deltaTime;
				if (!jetpack)
					moveDirection *= 0.999f;
			}

			moveDirection.y = upSpeed;

			//EXPERIMENTAL: More collissions...
			if (!tunnelGate && config.MoreCols) {
				Utility.ObjectBelowMouseInfo result = default(Utility.ObjectBelowMouseInfo);
				Ray ray = new Ray(transform.position, moveDirection);
				result.hitDistance = 0.5f;
				result.hitObject = null;
				GameObject sl = Collisions.Instance.checkSelectables (ray, out result.hitDistance);
				if (sl != null) {
					SerializedMonoBehaviour component = sl.GetComponent<SerializedMonoBehaviour> ();
					if (component != null && component.canBeSelected ()) {
						result.hitObject = component;
						result.hitPosition = ray.GetPoint (result.hitDistance);
						result.hitLayerMask = 1 << component.gameObject.layer;
					} else
						result.hitDistance = 0.5f;
				}
				MouseCollider.HitInfo[] array = MouseCollisions.Instance.raycastAll(ray, result.hitDistance);
				for (int i = 0; i < array.Length; i++) {
					MouseCollider.HitInfo raycastHit = array [i];
					if (raycastHit.hitDistance < result.hitDistance) {
						SerializedMonoBehaviour componentInParent = raycastHit.hitObject.GetComponentInParent<SerializedMonoBehaviour>();
						bool flag = false;
						if (componentInParent != null && componentInParent.canBeSelected ()) {
							result.hitObject = componentInParent;
							result.hitLayerMask = 1 << raycastHit.hitObject.layer;
							flag = true;
						}
						if (componentInParent == null || flag) {
							result.hitPosition = raycastHit.hitPosition;
							result.hitDistance = raycastHit.hitDistance;
							result.hitNormal = raycastHit.hitNormal;
						}
						if (componentInParent == null) {
							result.hitObject = null;
							result.hitLayerMask = 0;
						}
					}
				}

				if (result.hitObject != null && result.hitDistance < 0.2f &&
					(moveDirection.x != 0.0f || moveDirection.z != 0.0f) &&
				    !(result.hitObject is Path && ((Path)result.hitObject).isUnderground()))
				{
					Vector3 pos = transform.position;
					Vector3 hit = result.hitPosition;

					//UltimateMain.Instance.Log("rx: " + moveDirection.x + " / hpx: " + hit.x + " / posx: " + pos.x, UltimateMain.LogLevel.INFO);
					//UltimateMain.Instance.Log("rz: " + moveDirection.z + " / hpz: " + hit.z + " / posz: " + pos.z, UltimateMain.LogLevel.INFO);

					// Right / Left
					if ((moveDirection.x > 0.0f && hit.x >= pos.x) || (moveDirection.x < 0.0f && hit.x <= pos.x))
						moveDirection.x = -moveDirection.x;
					// Forward / Backward
					if ((moveDirection.z > 0.0f && hit.z >= pos.z) || (moveDirection.z < 0.0f && hit.z <= pos.z))
						moveDirection.z = -moveDirection.z;
				}

			}

			controller.Move(moveDirection);
		}

		// Set everything to 0 / null so it can be garbage collected
		void OnDestroy()
		{
			moveDirection = Vector3.zero;
			config = null;
			controller = null;
		}
	}
}
