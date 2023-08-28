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

static float factor = 0.4f; // FEHLER, temp, später anders lösen... so mit coroutine oder was weiss ich
static float mousefactor = 0.4f;

		static float scrollfactor = 10f;

static float rfactor = 1f;
static float rmousefactor = 0.1f; // FEHLER, ich rate...

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


static float otherAngle = 0f;




// FEHLER, noch neuere Idee
static Quaternion frameOfReference;
static Vector3 relPosition; // position = vessel.position (com oder position, weiss noch nicht) + frameOfReference * relPosition
static Vector3 relPivot;
static Quaternion relRotation;

static float hdg;
static float pitch;
static float rot;


static Vector3 relPosition0; // position = vessel.position (com oder position, weiss noch nicht) + frameOfReference * relPosition
static Quaternion relRotation0;


static bool ref1 = false;


		public void CalcHdgPitchFromVector(Vector3 vector, out float hdg, out float pitch) // vector zeigt von Kamera zum Pivot (world)
		{
				// FEHLER, bin nicht sicher ob's stimmt, das noch klären
	//		Quaternion frameOfReference =
	//			FlightCamera.fetch.getReferenceFrame();

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

static bool blerp = false;
static float lerpend = 0.96f;

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

// FEHLER, neue Idee -> aktuell kein "sanfter" Übergang, sondern einfach "peng"

if(ref1)
frameOfReference = FlightGlobals.GetFoR(FoRModes.SHP_REL);
else
frameOfReference = FlightCamera.fetch.getReferenceFrame();

relPosition = Quaternion.Inverse(frameOfReference) * (position - FlightGlobals.ActiveVessel.transform.position);
relPivot = Quaternion.Inverse(frameOfReference) * (pivot - FlightGlobals.ActiveVessel.transform.position);

CalcHdgPitchFromVector(position - pivot, out hdg, out pitch);
rot = 0;

// FEHLER, hier rotation errechnen... irgendwie... nicht übernehmen... weil, muss man später ja auch können
//rotation = frameOfReference * Quaternion.AngleAxis(hdg * 57.29578f, Vector3.up)
//							* Quaternion.AngleAxis(pitch * 57.29578f, Vector3.right)
//							* Quaternion.AngleAxis(rot * 57.29578f, Vector3.forward);

rotation = FlightCamera.fetch.transform.rotation;
					relRotation = Quaternion.Inverse(frameOfReference) * rotation;


relPosition0 = Quaternion.Inverse(frameOfReference) * (position0 - FlightGlobals.ActiveVessel.transform.position);
relRotation0 = Quaternion.Inverse(frameOfReference) * rotation0;
				}

				ScreenMessages.PostScreenMessage(active ? "CameraController active" : "normal camera", 3, ScreenMessageStyle.UPPER_CENTER);
			}

			if(!active)
			{
// FEHLER, geht nicht mehr, das ist mir aber im Moment furzegal

				if(lerp < 1.0f)
				{
					lerp += Time.deltaTime * 2f; // revert in 0.5 seconds

			position = FlightGlobals.ActiveVessel.transform.position + frameOfReference * relPosition;
			pivot = FlightGlobals.ActiveVessel.transform.position + frameOfReference * relPivot;
			rotation = frameOfReference * relRotation;

			position0 = FlightGlobals.ActiveVessel.transform.position + frameOfReference * relPosition0;
			rotation0 = frameOfReference * relRotation0;

if(blerp)
					FlightCamera.fetch.GetCameraTransform().position = Vector3.Lerp(position, position0, lerp);
else
					FlightCamera.fetch.GetCameraTransform().position = Vector3.Slerp(position, position0, lerp);
					FlightCamera.fetch.GetCameraTransform().rotation = Quaternion.Slerp(rotation, rotation0, lerp);

					if(lerp >= lerpend)
						FlightCamera.fetch.ActivateUpdate();
				}

				return;
			}

// FEHLER, es fehlt noch was... manchmal (bei hohen Geschwindigkeiten) springt was... hab aber noch nicht rausgefunden was genau
// FEHLER, manchmal (im Orbit), da dreht's nicht korrekt, sondern in andere Richtungen? sehr komisch...


			// Update der aktuellen Infos
// FEHLER, alles experimentell und neu

if(ref1)
			frameOfReference = FlightGlobals.GetFoR(FoRModes.SHP_REL);
else
			frameOfReference = FlightCamera.fetch.getReferenceFrame();

			position = FlightGlobals.ActiveVessel.transform.position + frameOfReference * relPosition;
			pivot = FlightGlobals.ActiveVessel.transform.position + frameOfReference * relPivot;

//			rotation = frameOfReference * Quaternion.AngleAxis(hdg * 57.29578f, Vector3.up)
//										* Quaternion.AngleAxis(pitch * 57.29578f, Vector3.right)
//										* Quaternion.AngleAxis(rot * 57.29578f, Vector3.forward);

			hdg = 0; pitch = 0; rot = 0; // FEHLER, neue Idee
			rotation = frameOfReference * relRotation;

			bool hasInput = false;


			if(Input.GetKey(KeyCode.RightShift))
			{
				//////////////////////////////
				// Translation

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

				if(Input.GetMouseButton(1))
				{
					float x = Input.GetAxis("Mouse X");
					float y = Input.GetAxis("Mouse Y");

					if(x != 0f)
					{
						position -= rotation * Vector3.right * x * mousefactor;
						hasInput = true;
					}

					if(y != 0)
					{
						position -= rotation * Vector3.up * y * mousefactor;
						hasInput = true;
					}
				}

				if(hasInput)
				{
					ClampPosition();

					pivot = position + positionToPivot;

					relPosition = Quaternion.Inverse(frameOfReference) * (position - FlightGlobals.ActiveVessel.transform.position);
					relPivot = Quaternion.Inverse(frameOfReference) * (pivot - FlightGlobals.ActiveVessel.transform.position);
				}
			}
			else if(Input.GetKey(KeyCode.RightControl))
			{
				//////////////////////////////
				// Rotation um die Sichtachse

				if(Input.GetKey(KeyCode.RightArrow))
				{
					rot -= (Mathf.PI / 180f) * rfactor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.LeftArrow))
				{
					rot += (Mathf.PI / 180f) * rfactor;
					hasInput = true;
				}

				if(Input.GetMouseButton(1))
				{
					float x = Input.GetAxis("Mouse X");

					if(x != 0f)
					{
						rot -= x * rmousefactor;
						hasInput = true;
					}
				}

				if(hasInput)
				{
//					rotation = frameOfReference * Quaternion.AngleAxis(hdg * 57.29578f, Vector3.up)
//												* Quaternion.AngleAxis(pitch * 57.29578f, Vector3.right)
//												* Quaternion.AngleAxis(rot * 57.29578f, Vector3.forward);

					rotation = rotation * Quaternion.AngleAxis(hdg * 57.29578f, Vector3.up)
										* Quaternion.AngleAxis(pitch * 57.29578f, Vector3.right)
										* Quaternion.AngleAxis(rot * 57.29578f, Vector3.forward);

					relRotation = Quaternion.Inverse(frameOfReference) * rotation;
				}
			}
			else
			{
				//////////////////////////////
				// Rotation

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

				if(Input.GetMouseButton(1))
				{
					float x = Input.GetAxis("Mouse X");
					float y = Input.GetAxis("Mouse Y");

					if(x != 0f)
					{
						hdg -= x * rmousefactor;
						hasInput = true;
					}

					if(y != 0)
					{
						pitch -= y * rmousefactor;
						hasInput = true;
					}
				}

				if(hasInput)
				{
					Vector3 localPivotToPosition = Quaternion.Inverse(rotation) * (position - pivot);

//					rotation = frameOfReference * Quaternion.AngleAxis(hdg * 57.29578f, Vector3.up)
//												* Quaternion.AngleAxis(pitch * 57.29578f, Vector3.right)
//												* Quaternion.AngleAxis(rot * 57.29578f, Vector3.forward);

					rotation = rotation * Quaternion.AngleAxis(hdg * 57.29578f, Vector3.up)
										* Quaternion.AngleAxis(pitch * 57.29578f, Vector3.right)
										* Quaternion.AngleAxis(rot * 57.29578f, Vector3.forward);
					
					position = pivot + rotation * localPivotToPosition;

					ClampPosition();

					relPosition = Quaternion.Inverse(frameOfReference) * (position - FlightGlobals.ActiveVessel.transform.position);
					relPivot = Quaternion.Inverse(frameOfReference) * (pivot - FlightGlobals.ActiveVessel.transform.position);

					relRotation = Quaternion.Inverse(frameOfReference) * rotation;
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

				if(Input.GetKey(KeyCode.RightShift))
				{
					//////////////////////////////
					// Translation

					pivot = position + positionToPivot;
				}

				relPosition = Quaternion.Inverse(frameOfReference) * (position - FlightGlobals.ActiveVessel.transform.position);
				relPivot = Quaternion.Inverse(frameOfReference) * (pivot - FlightGlobals.ActiveVessel.transform.position);
			}

			//////////////////////////////
			// Zoom

			{
				hasInput = false;

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
				}

				relPosition = Quaternion.Inverse(frameOfReference) * (position - FlightGlobals.ActiveVessel.transform.position);
				relPivot = Quaternion.Inverse(frameOfReference) * (pivot - FlightGlobals.ActiveVessel.transform.position);
			}

			// grundsätzlich immer die Position und so anpassen, weil... evtl. bewegt sich ja etwas anderes?
//rotation = frameOfReference * Quaternion.AngleAxis(hdg * 57.29578f, Vector3.up)
//							* Quaternion.AngleAxis(pitch * 57.29578f, Vector3.right)
//							* Quaternion.AngleAxis(rot * 57.29578f, Vector3.forward);

FlightCamera.fetch.GetCameraTransform().rotation = rotation;

FlightCamera.fetch.GetCameraTransform().position = position;
		}
	}
}
