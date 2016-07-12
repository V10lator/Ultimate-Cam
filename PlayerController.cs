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
			// WARNING: THE CODE HERE NEEDS LEGAL REVIEW
	/*		float ground = float.MinValue;
			Block block = GameController.Instance.park.blockData.getBlock(transform.position);
			if (block != null) {
				float top = block.getTopSideY(transform.position);
				if (top < transform.position.y)
					ground = top;
			}
			if (ground == float.MinValue)
				ground = GameController.Instance.park.getHeightAt(transform.position);
			// END OF WARNING
			ground += UltimateMain.Instance.height;
			bool onGround;

			float bp = ground - transform.position.y;
			if (bp == 0.0f) { // All fine, no height adjustment needed.
				onGround = true;
			} else if (bp > 0.0f) {
				transform.position = new Vector3 (transform.position.x, ground, transform.position.z);
				onGround = true;
			} else { // falling
				moveDirection.y -= gravity * Time.deltaTime;
				onGround = false;
			}
*/
			if (controller.isGrounded) {
				moveDirection = new Vector3 (Input.GetAxis ("Horizontal"), 0.0f, Input.GetAxis ("Vertical"));
				moveDirection = transform.TransformDirection (moveDirection);
				moveDirection *= speed * Time.deltaTime;

				moveDirection.y = Input.GetKeyDown (UltimateMain.Instance.config.GetKey (UltimateSettings.JUMP_KEY_SETTING)) ? jumpSpeed : 0.0f;
			}
			else
				moveDirection.y -= gravity * Time.deltaTime;

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
