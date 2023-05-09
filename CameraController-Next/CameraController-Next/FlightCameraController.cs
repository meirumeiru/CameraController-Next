using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace CameraController_Next
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class FlightCameraController : MonoBehaviour
	{
		public virtual String AddonName { get; set; }

		protected static FlightCameraController ControllerInstance;

		public static FlightCameraController Instance { get { return ControllerInstance; } }

		public static bool APIReady { get { return ControllerInstance != null; } }

		private static bool active = false;

		public void Start()
		{
		}

		private void Awake()
		{
			if(!HighLogic.LoadedSceneIsFlight)
				ControllerInstance = null;
			else
				ControllerInstance = this;
		}

		private void OnDestroy()
		{
		}

		private static Part GetPartUnderCursor()
		{
			Ray ray;
			if(HighLogic.LoadedSceneIsFlight)
				ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
			else
				ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			RaycastHit hit;
			if(Physics.Raycast(ray, out hit, 557059))
				return hit.transform.gameObject.GetComponent<Part>();
			else
				return null;
		}

static float factor = 0.1f; // FEHLER, temp, später anders lösen... so mit coroutine oder was weiss ich
		static float scrollfactor = 10f;
static float rfactor = 1f;

static float tms = 0.2f; // 0.2 Sekunden zusammenrechnen
static float tm = 0f; // delta-time-summe
		static float scrollMagnitude = 0f;
		static Vector2 tvl; // letzte Summe
static Vector2 tv = Vector2.zero; // summe des Scrollens im aktuellen Zeitschlitz

// FEHLER, neue Idee
static Vector3 pivot;			// Hilfspunkt zum drum herum drehen

static Vector3 position;		// Kamera
static Quaternion rotation;		// Kamera

static Vector3 position0;
static Quaternion rotation0;

static float lerp;

		public void CalcHdgPitchFromVector(Vector3 vector, out float hdg, out float pitch) // vector zeigt von Kamera zum Pivot (world)
		{
				// FEHLER, bin nicht sicher ob's stimmt, das noch klären
			Quaternion frameOfReference =
				FlightCamera.fetch.getReferenceFrame();

			vector = Quaternion.Inverse(frameOfReference) * vector.normalized;

			hdg = Mathf.Atan2(0f - vector.z, vector.x) - (float)Math.PI / 2f;
			pitch = Mathf.Atan2(vector.y, Mathf.Sqrt(vector.x * vector.x + vector.z * vector.z));
		}

		private void ClampPosition()
		{
			Vector3 vesselToCamera = position - FlightGlobals.ActiveVessel.transform.position;

			if(vesselToCamera.sqrMagnitude > (FlightCamera.fetch.maxDistance * FlightCamera.fetch.maxDistance))
				position = FlightGlobals.ActiveVessel.transform.position + vesselToCamera.normalized * FlightCamera.fetch.maxDistance;
		}

		public void Update()
		{
			tm += Time.deltaTime;
			tv += Input.mouseScrollDelta;

			if(tm >= tms)
			{
				tvl = tv;

				scrollMagnitude = 0.3f * scrollMagnitude + 0.7f * tvl.magnitude;
				scrollMagnitude =
					(scrollMagnitude > 0.1f) ? scrollMagnitude - 0.1f : 0f;

				tv = Vector3.zero;
				tm = 0f;
			}

// ein und ausschalten? -> wie könnte man das? *hmm* na ja oder ich übernehm einfach alles? nein, nicht gut, muss auch umschalten können dann

			if(Input.GetKeyDown(KeyCode.KeypadDivide))
			{
				active = !active;

				if(active)
				{
					FlightCamera.fetch.DeactivateUpdate();

// FEHLER, ginge das? oder müsste ich das immer wieder neu holen?
					pivot = FlightCamera.fetch.GetPivot().position;

					Transform cm = FlightCamera.fetch.GetCameraTransform();

					position = cm.position;
					rotation = cm.rotation;

					position0 = position;
					rotation0 = rotation;

					lerp = 0f;
				}

				ScreenMessages.PostScreenMessage(active ? "CameraController active" : "normal camera", 3, ScreenMessageStyle.UPPER_CENTER);
			}

			if(!active)
			{
				if(lerp < 1.0f)
				{
					lerp += Time.deltaTime * 2f; // revert in 0.5 seconds

					FlightCamera.fetch.GetCameraTransform().position = Vector3.Lerp(position, position0, lerp);
					FlightCamera.fetch.GetCameraTransform().rotation = Quaternion.Slerp(rotation, rotation0, lerp);

					if(lerp >= 1.0f)
						FlightCamera.fetch.ActivateUpdate();
				}

				return;
			}

// FEHLER, es fehlt noch was... manchmal (bei hohen Geschwindigkeiten) springt was... hab aber noch nicht rausgefunden was genau

			if(Input.GetKey(KeyCode.LeftAlt) | Input.GetKey(KeyCode.RightAlt))
			{
				//////////////////////////////
				// Translation

				bool hasInput = false;

				Vector3 positionToPivot = pivot - position;

				if(Input.GetKey(KeyCode.RightArrow))
				{
					position += rotation * Vector3.right * factor;
					hasInput = true;
				}
				
				if(Input.GetKey(KeyCode.LeftArrow))
				{
					position -= rotation * Vector3.right * factor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.UpArrow))
				{
					position += rotation * Vector3.up * factor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.DownArrow))
				{
					position -= rotation * Vector3.up * factor;
					hasInput = true;
				}

				if(hasInput)
				{
					ClampPosition();

					pivot = position + positionToPivot;
					FlightCamera.fetch.GetCameraTransform().position = position;
				}
			}
			else
			{
				//////////////////////////////
				// Rotation

				bool hasInput = false;

				Quaternion frameOfReference = FlightCamera.fetch.getReferenceFrame();

				float hdg, pitch;
				CalcHdgPitchFromVector(position - pivot, out hdg, out pitch);

				if(Input.GetKey(KeyCode.RightArrow))
				{
					hdg -= (Mathf.PI / 180f) * rfactor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.LeftArrow))
				{
					hdg += (Mathf.PI / 180f) * rfactor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.UpArrow))
				{
					pitch += (Mathf.PI / 180f) * rfactor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.DownArrow))
				{
					pitch -= (Mathf.PI / 180f) * rfactor;
					hasInput = true;
				}

				if(hasInput)
				{
					FlightCamera.fetch.GetCameraTransform().rotation =
						frameOfReference * Quaternion.AngleAxis(hdg * 57.29578f, Vector3.up) * Quaternion.AngleAxis(pitch * 57.29578f, Vector3.right);

					Vector3 localPivotToPosition = Quaternion.Inverse(rotation) * (position - pivot);

					rotation = FlightCamera.fetch.GetCameraTransform().rotation;

					position = pivot + rotation * localPivotToPosition;

					ClampPosition();

					FlightCamera.fetch.GetCameraTransform().position = position;
				}
			}

			if(GameSettings.AXIS_MOUSEWHEEL.GetAxis() != 0f)
			{
				//////////////////////////////
				// Zoom (or Translation)

				float sm = 0.3f + Mathf.Min(scrollMagnitude / 4f, 0.7f);
				float z = sm * GameSettings.AXIS_MOUSEWHEEL.GetAxis();

// FEHLER, hier noch Faktor bauen für Distanz Pivot-Position -> je weiter, um so grösser
float ef = (pivot - position).magnitude;

ef /= 15;
				z *= ef;

				z *= scrollfactor;

				Vector3 positionToPivot = pivot - position;

				position += rotation * Vector3.forward * z;

				ClampPosition();

				if(Input.GetKey(KeyCode.LeftAlt) | Input.GetKey(KeyCode.RightAlt))
				{
					//////////////////////////////
					// Translation

					pivot = position + positionToPivot;
				}

				FlightCamera.fetch.GetCameraTransform().position = position;
			}

			//////////////////////////////
			// Zoom

			{
				bool hasInput = false;

				Vector3 positionToPivot = pivot - position;

				if(GameSettings.ZOOM_IN.GetKey())
				{
					position += rotation * Vector3.forward * factor;
					hasInput = true;
				}

				if(GameSettings.ZOOM_OUT.GetKey())
				{
					position -= rotation * Vector3.forward * factor;
					hasInput = true;
				}

				if(hasInput)
				{
					ClampPosition();

					pivot = position + positionToPivot;
					FlightCamera.fetch.GetCameraTransform().position = position;
				}
			}
		}
	}
}
