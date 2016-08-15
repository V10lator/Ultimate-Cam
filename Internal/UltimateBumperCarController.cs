using BumperCars.CustomFlatRide.BumperCars;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UltimateCam.Internal
{
	public class UltimateBumperCarController : MonoBehaviour
	{
		private Rigidbody _rigidbody;
		private float _movingSpeed;
		private float _oldHeading;
		private BumperCars.CustomFlatRide.BumperCars.BumperCars _bumperCars = null;
		private static UltimateBumperCarController controller = null;
		private static GameObject seat;
		private PeepDummy dummy;

		void Start()
		{
			BumperCarAi ai = GetComponent<BumperCarAi>();
			if (ai == null)
			{
				UltimateMain.Instance.Log("BumperCarAi not found!", UltimateMain.LogLevel.INFO);
				Destroy(this);
				return;
			}
			Type type = typeof(BumperCarAi);
			FieldInfo field = type.GetField("_rigidbody", BindingFlags.NonPublic | BindingFlags.Instance);
			_rigidbody = (Rigidbody)field.GetValue(ai);
			field = type.GetField("_movingSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
			_movingSpeed = (float)field.GetValue(ai);

			//TODO: Just for testing
			field = type.GetField("_bumperCars", BindingFlags.NonPublic | BindingFlags.Instance);
			List<BumperCar> cars = (List<BumperCar>)field.GetValue(ai);
			Collider c = GetComponent<Collider>();
			Collider cc;
			type = typeof(BumperCar);
			field = type.GetField("_physicsCar", BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (BumperCar car in cars)
			{
				cc = ((GameObject)field.GetValue(car)).GetComponent<Collider>();
				Physics.IgnoreCollision(c, cc);
				Physics.IgnoreCollision(cc, c);
			}

			Destroy(ai);
			dummy = seat.AddComponent<PeepDummy>(); // HACK: So BumperCars doesn't set the whole car (and with it the controller) to disabled if no real Peep is inside.
			seat = null;
			UltimateMain.Instance.Log("Car highjacked!", UltimateMain.LogLevel.INFO);
		}

		void Destroy()
		{
			Destroy(gameObject.GetComponent<CapsuleCollider>());
			Vector3 pos = _rigidbody.position;
			Quaternion rot = _rigidbody.rotation;
			Destroy(_rigidbody);
			BumperCarAi ai = gameObject.AddComponent<BumperCarAi>();
			ai.BumperCars = _bumperCars; //TODO
			_rigidbody = (Rigidbody)typeof(BumperCarAi).GetField("_rigidbody", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ai);
			_rigidbody.position = pos;
			_rigidbody.rotation = rot;
			Destroy(dummy);
			dummy = null;
			controller = null;
		}

		void FixedUpdate()
		{
			BumperCarAi ai = gameObject.GetComponent<BumperCarAi>();
			UltimateMain.Instance.Log("AI " + (ai == null ? "still death!" : "alive?!?"), UltimateMain.LogLevel.INFO);
		}/*
			_rigidbody.AddForce(Vector3.up * 10.0f);
			//UltimateMain.Instance.Log("Car tick.", UltimateMain.LogLevel.INFO);
		}/*
			if ((int)originalAi.BumperCars.CurrentState != 1)
				return;

			Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), 0.0f);
			direction = transform.TransformDirection(direction);

			//_rigidbody.AddForce(direction.normalized * _movingSpeed / 10.0f);

			float wantedheading = Mathf.Atan2(_rigidbody.velocity.x, _rigidbody.velocity.z);

			float heading = Mathf.Lerp(_oldHeading, wantedheading, 4f * Time.deltaTime);

			_oldHeading = heading;

			//transform.rotation = Quaternion.Euler(0, heading * Mathf.Rad2Deg, 0);
		}*/

		void OnCollisionEnter(Collision collision)
		{
			if (collision.gameObject.name.StartsWith("Bound"))
			{
				//_rigidbody.velocity = new Vector3(0.0f, 0.0f, 0.0f);
			}
		}

		void OnCollisionStay(Collision collision)
		{
			if (collision.gameObject.name.StartsWith("Mouse") || collision.gameObject.name.StartsWith("Land"))
			{
				Physics.IgnoreCollision(GetComponent<Collider>(), collision.collider);
			}
		}

		private static GameObject getPhysicsCar(Transform seat)
		{
			/*BumperCar[] bCars = seat.parent.gameObject.GetComponentsInParent<BumperCar>();
			BumperCar bCar = null;
			foreach (BumperCar bc in bCars)
				if (bc.transform.FindChild("seat") == seat)
				{
					bCar = bc;
					break;
				}*/

			BumperCar bCar = seat.parent.gameObject.GetComponent<BumperCar>();
			GameObject ret = null;

			if (bCar == null)
				UltimateMain.Instance.Log("BumperCar not found!", UltimateMain.LogLevel.INFO);
			else
			{
				Type type = typeof(BumperCar);
				if (type == null)
					UltimateMain.Instance.Log("Error accessing bumpercar!", UltimateMain.LogLevel.INFO);
				else
				{
					FieldInfo field = type.GetField("_physicsCar", BindingFlags.NonPublic | BindingFlags.Instance);
					if (field == null)
						UltimateMain.Instance.Log("Error accessing field!", UltimateMain.LogLevel.INFO);
					else
					{
						object o = field.GetValue(bCar);
						if (!(o is GameObject))
							UltimateMain.Instance.Log("Field of wrong type!", UltimateMain.LogLevel.INFO);
						else
							ret = o as GameObject;
					}
				}
			}
			return ret;
		}

		internal static void tryEnterBumperCar(Attraction attraction, Transform seat)
		{
			if (!(attraction is BumperCars.CustomFlatRide.BumperCars.BumperCars))
				return;

			if (controller != null)
				Destroy(controller);
			
			GameObject physicsCar = getPhysicsCar(seat);
			if (physicsCar != null)
			{
				UltimateBumperCarController.seat = seat.gameObject;
				physicsCar.AddComponent<UltimateBumperCarController>();
			}
		}

		private class PeepDummy : MonoBehaviour
		{
		}
	}
}
