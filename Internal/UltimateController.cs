using System;
using System.Collections;
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
		private readonly float width = 0.4f;

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

		private ArrayList imps = new ArrayList();
		private class UltimateRay
		{
			internal readonly Color color;
			internal readonly Vector3 start;
			internal readonly Vector3 end;

			internal UltimateRay(Color c, Vector3 s, Vector3 e)
			{
				color = c;
				start = s;
				end = e;
			}
		}

		public void OnGUI()
		{
			UltimateRay ray;
			Vector3[][] p = new Vector3[2][];
			p[0] = new Vector3[2];
			p[1] = new Vector3[2];
			GUI.depth = -998;
			Rect pos;
			GUIStyle font = new GUIStyle();
			font.fontSize = 12;
			font.stretchHeight = font.stretchWidth = true;
			bool hasEnd;
			float[] y = new float[2];
			float ground = transform.position.y - config.Height;

			for (int i = 0; i < imps.Count; i++)
			{
				ray = (UltimateRay)imps[i];
				GUI.color = ray.color;

				y[0] = ray.start.y;
				p[0][0] = ray.start;
				p[0][0].y = ground;
				p[0][1] = Camera.main.WorldToScreenPoint(p[0][0]);
				hasEnd = ray.end != Vector3.one;
				if (hasEnd)
				{
					y[1] = ray.end.y;
					p[1][0] = ray.end;
					p[1][0].y = ground;
					p[1][1] = Camera.main.WorldToScreenPoint(p[1][0]);
				}

				pos = new Rect(p[0][1].x + 4.0f, Screen.height - p[0][1].y - 2.5f, 4.0f, 4.0f);
				GUI.Label(pos, y[0] + " / " + (hasEnd ? y[1].ToString() : "No hit"), font);

				int c = hasEnd ? 2 : 1;
				for (int j = 0; j < c; j++)
				{
					pos = new Rect(p[j][1].x - 2.5f, Screen.height - p[j][1].y - 2.5f, 4.0f, 4.0f);
					GUI.DrawTexture(pos, Texture2D.whiteTexture, ScaleMode.StretchToFill);
				}
			}
		}

		private MouseCollider.HitInfo rayFromTo(Vector3 from, Vector3 direction, float md, bool down, bool ignoreTerrain)
		{

			float steps;
			if (down)
				steps = 16.0f;
			else
				steps = 4.0f;
			float stepsize = width / steps;

			int c = (int)steps + 1;
			MouseCollider.HitInfo[] result = new MouseCollider.HitInfo[c];
			Ray ray;
			MouseCollider.HitInfo[] array;
			Vector3 tFrom;

			Quaternion offset;
			{
				double _yaw = 0;
				// Set yaw
				if (moveDirection.x != 0.0f)
				{
					// Set yaw start value based on dx
					_yaw = moveDirection.x < 0.0f ? 1.5 : 0.5;
					_yaw *= Math.PI;
					_yaw -= Math.Atan(moveDirection.z / moveDirection.x);
				}
				else if (moveDirection.z < 0)
					_yaw = Math.PI;
				float yaw = (float)(_yaw * 180.0d / Math.PI);
				offset = Quaternion.Euler(0.0f, yaw, 0.0f);
			}

			float rss;
			Park park = GameController.Instance.park;
			float terrain;

			for (int i = 0; i < c; i++)
			{
				tFrom = from;
				rss = stepsize * (i - (steps / 2.0f));
				if (rss != 0.0f)
				{
					if (down)
						tFrom.z += rss;
					else
						tFrom.x += rss;
					tFrom = (offset * (tFrom - from)) + from;
				}

				ray = new Ray(tFrom, direction);
				result[i].hitDistance = md;
				result[i].hitObject = null;
				result[i].hitSomething = false;

				array = MouseCollisions.Instance.raycastAll(ray, md);
				for (int j = 0; j < array.Length; j++)
				{
					if (array[j].hitDistance <= md)
					{
						SerializedMonoBehaviour componentInParent = array[j].hitObject.GetComponentInParent<SerializedMonoBehaviour>();
						if (componentInParent != null)
						{
							if (componentInParent is Person)
								continue;

							if (componentInParent.canBeSelected())
							{
								result[i] = array[j];
								result[i].hitObject = componentInParent.gameObject;
							}
						}
						else
							result[i].hitSomething = false;
					}
				}

				if (!ignoreTerrain && !result[i].hitSomething)
				{
					if (down)
					{
						terrain = park.getHeightAt(tFrom);
						if (terrain < tFrom.y)
						{
							result[i].hitDistance = tFrom.y - terrain;
							if (result[i].hitDistance <= md)
							{
								result[i].hitSomething = true;
								result[i].hitObject = null;
								result[i].hitPosition = new Vector3(tFrom.x, terrain, tFrom.z);
							}
						}
					}
				}

				Color co;
				Vector3 hitP;
				if (result[i].hitSomething)
				{
					co = down ? Color.blue : Color.yellow;
					hitP = result[i].hitPosition;
				}
				else
				{
					co = down ? Color.cyan : Color.green;
					hitP = Vector3.one;
				}
				imps.Add(new UltimateRay(co, tFrom, hitP));
			}

			int s = 0;
			steps = md;
			for (int i = 1; i < c; i++)
			{
				if (result[i].hitSomething && result[i].hitDistance < steps)
				{
					s = i;
					steps = result[i].hitDistance;
				}
			}

			return result[s];
		}

		void Update()
		{
			float speed = config.WalkingSpeed * 50.0f * Time.deltaTime;
			if (Input.GetKey(config.GetKey(UltimateSettings.ROTATE_LEFT_KEY_SETTING)))
				mouse.yaw -= speed;
			else if (Input.GetKey(config.GetKey(UltimateSettings.ROTATE_RIGHT_KEY_SETTING)))
				mouse.yaw += speed;

			if (!jetpack && !onGround && transform.position.y < 0.0f)
			{
				Vector3 pos = transform.position;
				pos.y = LandPatch.maxTerrainHeight;
				transform.position = pos;
				onGround = false;
				return;
			}

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

			imps.Clear();

			float height = config.Height;
			Vector3 feet = transform.position;
			Vector3 to = new Vector3(feet.x + moveDirection.x, feet.y + moveDirection.y, feet.z + moveDirection.z);

			MouseCollider.HitInfo result = rayFromTo(feet, moveDirection, moveDirection.magnitude + (width / 2.0f), false, true);
			bool underground;
			Park park = GameController.Instance.park;
			if (result.hitSomething)
			{
				SerializedMonoBehaviour smb = result.hitObject.GetComponent<SerializedMonoBehaviour>();
				underground = smb is Path && ((Path)smb).isUnderground();
				bool ignore;
				if (underground)
				{
					Block block = park.blockData.getBlock(feet);
					ignore = block != null && block is Path && !block.isUnderground();
				}
				else
					ignore = false;

				if (!ignore)
				{
					//UltimateMain.Instance.Log("rx: " + moveDirection.x + " / hpx: " + hit.x + " / posx: " + pos.x, UltimateMain.LogLevel.INFO);
					//UltimateMain.Instance.Log("rz: " + moveDirection.z + " / hpz: " + hit.z + " / posz: " + pos.z, UltimateMain.LogLevel.INFO);

					// Right / Left
					if ((moveDirection.x > 0.0f && result.hitPosition.x >= to.x) || (moveDirection.x < 0.0f && result.hitPosition.x <= to.x))
					{
						to.x = feet.x;
						moveDirection.x = 0.0f;
					}
					// Forward / Backward
					if ((moveDirection.z > 0.0f && result.hitPosition.z >= to.z) || (moveDirection.z < 0.0f && result.hitPosition.z <= to.z))
					{
						to.z = feet.z;
						moveDirection.z = 0.0f;
					}
				}
			}
			else
				underground = false;

			to.y += 0.001f;
			result = rayFromTo(to, Vector3.down, height * 2.0f, true, underground);
			to.y -= 0.001f;
			to.y -= height;
			feet.y -= height;
			float top;

			if (result.hitSomething)
			{
				if (result.hitObject != null)
				{
					SerializedMonoBehaviour smb = result.hitObject.GetComponent<SerializedMonoBehaviour>();
					if (smb is Path || smb is AttractionPlatform)
					{
						Block block = (Block)smb;
						top = block.getTopSideY(to);
					}
					else // TODO
						top = result.hitPosition.y;
				}
				else
					top = result.hitPosition.y;
			}
			else
				top = park.getHeightAt(to);

			if (to.y > top)
				onGround = false;
			else
			{
				if (to.y < top)
				{
					if (top - to.y <= height)
					{
						UltimateMain.Instance.Log("Stepping up!", UltimateMain.LogLevel.INFO);
						to.y = top;
						moveDirection.y = 0.0f;
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
