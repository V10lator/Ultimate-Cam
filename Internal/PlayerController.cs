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

		// Use this for initialization
		void Start()
		{
			config = UltimateMain.Instance.config;

			controller = GetComponent<CharacterController>();
			controller.detectCollisions = controller.enableOverlapRecovery = true;
		}

		// Update is called once per frame
		void Update()
		{
			float speed = config.WalkingSpeed * 50.0f * Time.deltaTime;
			if (Input.GetKey(config.GetKey(UltimateSettings.ROTATE_LEFT_KEY_SETTING)))
				Camera.main.gameObject.GetComponent<UltimateMouse>().yaw -= speed;
			else if(Input.GetKey(config.GetKey(UltimateSettings.ROTATE_RIGHT_KEY_SETTING)))
				Camera.main.gameObject.GetComponent<UltimateMouse>().yaw += speed;
			speed = config.WalkingSpeed;

			bool falling;
			bool jetpack;
			if (controller.isGrounded || config.Jetpack)
			{
				moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
				moveDirection = transform.TransformDirection(moveDirection);
				moveDirection *= speed * Time.deltaTime;

				bool jump = Input.GetKey(config.GetKey(UltimateSettings.JUMP_KEY_SETTING));
				if (controller.isGrounded)
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
			if (config.MoreCols) {
				Utility.ObjectBelowMouseInfo result = default(Utility.ObjectBelowMouseInfo);
				Ray ray = Camera.main.ScreenPointToRay (moveDirection);
				result.hitDistance = float.MaxValue;
				result.hitObject = null;
				GameObject sl = Collisions.Instance.checkSelectables (ray, out result.hitDistance);
				if (sl != null) {
					SerializedMonoBehaviour component = sl.GetComponent<SerializedMonoBehaviour> ();
					if (component != null && component.canBeSelected ()) {
						result.hitObject = component;
						result.hitPosition = ray.GetPoint (result.hitDistance);
						result.hitLayerMask = 1 << component.gameObject.layer;
					} else
						result.hitDistance = float.MaxValue;
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

				if (result.hitObject != null && result.hitDistance < 0.2f) {
					Vector3 pos = this.transform.position;
					Vector3 hit = result.hitPosition;

					// Some magic...
					UltimateMouse mouse = gameObject.GetComponent<UltimateMouse>();
					if (mouse.yaw > 90.0f && mouse.yaw < 270.0f)
					{
						float tmp = hit.z;
						hit.z = pos.z;
						pos.z = tmp;
					}
					if (mouse.yaw > 180.0f)
					{
						float tmp = hit.x;
						hit.x = pos.x;
						pos.x = tmp;
					}

					// More magic... TODO: 1.0 doesn't seem to do anything...
					if (moveDirection.y < 0.0f)
						hit.x += 1.0f;
					if (moveDirection.x < 0.0f)
						hit.z += 1.0f;

					/* TODO: Left here for debugging later
					UltimateMain.Instance.Log("Pitch: " + mouse.pitch + " / Yaw: " + mouse.yaw, UltimateMain.LogLevel.INFO);
					UltimateMain.Instance.Log("rx: " + moveDirection.x + " / hpx: " + hit.x + " / posx: " + pos.x, UltimateMain.LogLevel.INFO);
					UltimateMain.Instance.Log("rz: " + moveDirection.z + " / hpz: " + hit.z + " / posz: " + pos.z, UltimateMain.LogLevel.INFO);
					*/

					// Right / Left
					if (hit.x >= pos.x)
						moveDirection.x = -moveDirection.x * 1.5f;
					// Forward / Backward
					if (hit.z >= pos.z)
						moveDirection.z = -moveDirection.z * 1.5f;
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
