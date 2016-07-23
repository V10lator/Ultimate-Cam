using UnityEngine;

namespace UltimateCam
{
	internal class FpsMouse : MonoBehaviour
	{
		internal enum MOUSEBUTTON
		{
			LEFT = 0,
			MIDDLE = 1,
			RIGHT = 2
		}

		private float _sensitivity = 10.0F;
		private float _yRad = 180.0f;
		private float _xRad = 222.22f;

		private float yaw = 0.0f;
		private float pitch = 0.0f;

		internal void reset()
		{
			yaw = pitch = 0.0f;
		}

		private float keepInCircle(float f)
		{
			while (f > 360)
				f -= 360;
			while (f < 0)
				f += 360;
			return f;
		}

		void Update()
		{
			yaw += Input.GetAxis ("Mouse X") * _sensitivity;
			pitch -= Input.GetAxis ("Mouse Y") * _sensitivity;

			// Limit vertical head movement
			float h = _yRad / 2.0f;
			if (pitch > h)
				pitch = h;
			else if (pitch < -h)
				pitch = -h;

			Vector3 euler;
			if (UltimateCam.riding)
			{
				// Limit horizontal head movement
				h = _xRad / 2.0f;
				if (yaw > h)
					yaw = h;
				else if (yaw < -h)
					yaw = -h;
				
				euler = transform.parent.eulerAngles;
				euler = new Vector3(keepInCircle(euler.x + pitch), keepInCircle(euler.y + yaw), euler.z);
			}
			else
			{
				yaw = keepInCircle(yaw);
				euler = new Vector3(pitch, yaw, 0.0f);
			}
			
			transform.eulerAngles = euler;
		}
	}
}
