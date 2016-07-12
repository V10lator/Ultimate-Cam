using UnityEngine;
using System.Collections;

namespace UltimateCam
{
	public class PlayerController : MonoBehaviour
	{
		private float speed; // = 7.0f;
		private float jumpSpeed; // = 6.0f;
		private float gravity; // = 20.0f;

		private Vector3 moveDirection = Vector3.zero;
		private CharacterController controller;
		public bool active = false;

		// Use this for initialization
		void Start()
		{
			speed = UltimateMain.Instance.config.WalkingSpeed;
			jumpSpeed = speed / 100.0f * 86.0f; // 86% of speed
			if (jumpSpeed < UltimateSettings.MIN_SPEED)
				jumpSpeed = UltimateSettings.MIN_SPEED;
			gravity = UltimateMain.Instance.config.Gravity;

			controller = GetComponent<CharacterController>();
			controller.detectCollisions = true;
			active = true;
		}

		// Update is called once per frame
		void Update()
		{
			if (!active)
				return;

			if (controller.isGrounded) {
				moveDirection = new Vector3 (Input.GetAxis ("Horizontal"), 0.0f, Input.GetAxis ("Vertical"));
				moveDirection = transform.TransformDirection (moveDirection);
				moveDirection *= speed * Time.deltaTime;

				moveDirection.y = Input.GetKeyDown (UltimateMain.Instance.config.GetKey (UltimateSettings.JUMP_KEY_SETTING)) ? jumpSpeed : 0.0f;
			}
			else
				moveDirection.y -= gravity * Time.deltaTime;

			//EXPERIMENTAL: More collissions...
			if (UltimateMain.Instance.config.Experimental) {
				Utility.ObjectBelowMouseInfo result = default(Utility.ObjectBelowMouseInfo);
				Camera cam = UltimateMain.go.GetComponent<UltimateCam> ()._cam;
				Ray ray = cam.ScreenPointToRay (moveDirection);
				result.hitDistance = float.MaxValue;
				result.hitObject = null;
				GameObject gameObject = Collisions.Instance.checkSelectables (ray, out result.hitDistance);
				if (gameObject != null) {
					SerializedMonoBehaviour component = gameObject.GetComponent<SerializedMonoBehaviour> ();
					if (component != null && component.canBeSelected ()) {
						result.hitObject = component;
						result.hitPosition = ray.GetPoint (result.hitDistance);
						result.hitLayerMask = 1 << component.gameObject.layer;
					} else
						result.hitDistance = float.MaxValue;
				}
				GameController.Instance.enableVisibleMouseColliders ();
				RaycastHit[] array = Physics.RaycastAll (ray, result.hitDistance, LayerMasks.MOUSECOLLIDERS | LayerMasks.TERRAIN);
				for (int i = 0; i < array.Length; i++) {
					RaycastHit raycastHit = array [i];
					if (raycastHit.distance < result.hitDistance) {
						SerializedMonoBehaviour componentInParent = raycastHit.collider.gameObject.GetComponentInParent<SerializedMonoBehaviour> ();
						bool flag = false;
						if (componentInParent != null && componentInParent.canBeSelected ()) {
							result.hitObject = componentInParent;
							result.hitLayerMask = 1 << raycastHit.collider.gameObject.layer;
							flag = true;
						}
						if (componentInParent == null || flag) {
							result.hitPosition = raycastHit.point;
							result.hitDistance = raycastHit.distance;
							result.hitNormal = raycastHit.normal;
						}
						if (componentInParent == null) {
							result.hitObject = null;
							result.hitLayerMask = 0;
						}
					}
				}
				GameController.Instance.disableMouseColliders ();

				if (result.hitObject != null && result.hitDistance < 0.2f) {
					Vector3 pos = cam.transform.position;
					if ((moveDirection.x > 0.0f && result.hitPosition.x > pos.x) || (moveDirection.x < 0.0f && result.hitPosition.x < pos.x))
						moveDirection.x = 0.0f;
					if ((moveDirection.z > 0.0f && result.hitPosition.z > pos.z) || (moveDirection.z < 0.0f && result.hitPosition.z < pos.z))
						moveDirection.z = 0.0f;
				}
			}

			controller.Move(moveDirection * Time.deltaTime);
		}

		// Set everything to 0 / null so it can be garbage collected
		void OnDestroy()
		{
			speed = jumpSpeed = gravity = 0.0f;
			moveDirection = Vector3.zero;
			controller = null;
			active = false;
		}
	}
}
