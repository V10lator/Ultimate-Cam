using UnityEngine;

namespace UltimateCam.Internal
{
	internal class UltimateMouse : MonoBehaviour
	{
		internal enum MOUSEBUTTON
		{
			LEFT = 0,
			MIDDLE = 2,
			RIGHT = 1
		}

		private const float _sensitivity = 10.0F;
		private const float _yRad = 180.0f;
		private const float _xRad = 222.22f;

		private float _yaw = 0.0f;
		private float _pitch = 0.0f;

		internal float pitch
		{
			get
			{
				return _pitch;
			}
			set
			{
				_pitch = keepInCircle(value);
			}
		}
		internal float yaw
		{
			get
			{
				return _yaw;
			}
			set
			{
				_yaw = keepInCircle(value);
			}
		}

		internal void reset()
		{
			_yaw = _pitch = 0.0f;
		}

		private float keepInCircle(float f)
		{
			while (f > 360)
				f -= 360;
			while (f < 0)
				f += 360;
			return f;
		}

		private float limit(float f, float rad)
		{
			float h = rad / 2.0f;
			if (f > h)
				f = h;
			else if (f < -h)
				f = -h;
			return f;
		}

		void Update()
		{
			_yaw += Input.GetAxis ("Mouse X") * _sensitivity;
			_pitch -= Input.GetAxis ("Mouse Y") * _sensitivity;

			// Limit vertical head movement
			_pitch = limit(pitch, _yRad);

			Vector3[] eulers = { Vector3.zero, Vector3.zero };
			_yaw = API.UltimateCam.sitting ? limit(yaw, _xRad) : keepInCircle(yaw); // Limit horizontal head movement

			eulers[0] = new Vector3(pitch, 0.0f, 0.0f);
			eulers[1] = new Vector3(0.0f, yaw, 0.0f);
			
			transform.localEulerAngles = eulers[0];
			UltimateController.Instance().transform.localEulerAngles = eulers[1];
		}
	}
}
