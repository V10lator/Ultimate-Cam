using UnityEngine;

namespace UltimateCam
{
	public class FpsMouse : MonoBehaviour
	{
		public enum MOUSEBUTTON
		{
			LEFT = 0,
			MIDDLE = 1,
			RIGHT = 2
		}

		private float _sensitivity = 10.0F;
		private float _yRad = 180.0f;

		private float yaw = 0.0f;
		private float pitch = 0.0f;

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
			yaw = keepInCircle (yaw);
			float tmp = pitch - (Input.GetAxis ("Mouse Y") * _sensitivity);
			float yh = _yRad / 2.0f;
			if (tmp > yh)
				pitch = yh;
			else if (tmp < -yh)
				pitch = -yh;
			else
				pitch = tmp;

			Vector3 euler;
			if (UltimateCam.riding)
			{
				euler = transform.parent.eulerAngles;
				euler = new Vector3(keepInCircle(euler.x + pitch), keepInCircle(euler.y + yaw), euler.z);
			}
			else
				euler = new Vector3(pitch, yaw, 0.0f);
			
			transform.eulerAngles = euler;
		}
	}
}
