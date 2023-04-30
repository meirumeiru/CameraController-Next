using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace CameraController_Next
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class ControllerFlight : Controller
	{
		public override string AddonName { get { return this.name; } }
	}

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class ControllerEditor : Controller
	{
		public override string AddonName { get { return this.name; } }
	}

	public class Controller : MonoBehaviour
	{
		public virtual String AddonName { get; set; }

		protected static Controller ControllerInstance;

		public static Controller Instance { get { return ControllerInstance; } }

		public static bool APIReady { get { return ControllerInstance != null; } }

		public static bool bUserInput = false;
		public static bool bMove = false;

		public void Start()
		{
	//		InitializeLines(null);
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

/*
		public Ray ScreenPointToRay(Vector3 pos);
		public Ray ScreenPointToRay(Vector3 pos, MonoOrStereoscopicEye eye);
		public Vector3 ScreenToViewportPoint(Vector3 position);
		public Vector3 ScreenToWorldPoint(Vector3 position);
		public Vector3 ScreenToWorldPoint(Vector3 position, MonoOrStereoscopicEye eye);
		public void SetReplacementShader(Shader shader, string replacementTag);
		public void SetStereoProjectionMatrix(StereoscopicEye eye, Matrix4x4 matrix);
		public void SetStereoViewMatrix(StereoscopicEye eye, Matrix4x4 matrix);
		public void SetTargetBuffers(RenderBuffer[] colorBuffer, RenderBuffer depthBuffer);
		public void SetTargetBuffers(RenderBuffer colorBuffer, RenderBuffer depthBuffer);
		public bool TryGetCullingParameters(bool stereoAware, out ScriptableCullingParameters cullingParameters);
		public bool TryGetCullingParameters(out ScriptableCullingParameters cullingParameters);
		public Ray ViewportPointToRay(Vector3 pos);
		public Ray ViewportPointToRay(Vector3 pos, MonoOrStereoscopicEye eye);
		public Vector3 ViewportToScreenPoint(Vector3 position);
		public Vector3 ViewportToWorldPoint(Vector3 position, MonoOrStereoscopicEye eye);
		public Vector3 ViewportToWorldPoint(Vector3 position);
		public Vector3 WorldToScreenPoint(Vector3 position);
		public Vector3 WorldToScreenPoint(Vector3 position, MonoOrStereoscopicEye eye);
		public Vector3 WorldToViewportPoint(Vector3 position, MonoOrStereoscopicEye eye);
		public Vector3 WorldToViewportPoint(Vector3 position);
*/

static float factor = 0.1f; // FEHLER, temp, später anders lösen... so mit coroutine oder was weiss ich
		static float scrollfactor = 10f;
static float ractor = 1f;
static float rfactor = 1f;

static int mode = 1;
static int rmode = 1;



static float tms = 0.2f; // 0.2 Sekunden zusammenrechnen
static float tm = 0f; // delta-time-summe
		static float scrollMagnitude = 0f;
		static Vector2 tvl; // letzte Summe
static Vector2 tv = Vector2.zero; // summe des Scrollens im aktuellen Zeitschlitz

static float scrollFactor = 0.004f;

static float maxff = 1000f;

static bool active = false;

static bool firstIdea = true;


// FEHLER, neue Idee
static Vector3 pivot;			// Hilfspunkt zum drum herum drehen

static Vector3 position;		// Kamera
static Quaternion rotation;		// Kamera

static Vector3 position0;
static Quaternion rotation0;

static float lerp;

static float distance;			// von position zu pivot in Richtung forward


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

	//		if(Input.GetKeyDown(KeyCode.V) && (Input.GetKey(KeyCode.LeftAlt) | Input.GetKey(KeyCode.RightAlt)))

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

					distance = (pivot - position).magnitude;

position0 = position;
rotation0 = rotation;

lerp = 0f;
				}
				else
				{
//					FlightCamera.fetch.ActivateUpdate();

	//				FlightCamera.fetch.SetCamCoordsFromPosition(position);
				}

				ScreenMessages.PostScreenMessage(active ? "CameraController active" : "normal camera", 3, ScreenMessageStyle.UPPER_CENTER);
			}

// jetzt, wenn aktiv, die Inputs fressen?

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

/*

ok, ich hab ein pivot -> punkt mit bestimmter distanz direkt vor mir -> um den drehe ich
				ctrl und scroll verschiebt ihn rein und raus (fixes delta)

	scroll in und out ist nur von scrollmagnitude abhängig -> also, viel scroll = grösserer Faktor (sagen wir mal bis... 10? oder 100? weiss nicht)
				und wenig scroll = kleiner Faktor (der dann evtl. wirklich klein ist)

und min und max beachten von der distanz die dann auch bei mir gelten würde, aber halt irgendwie... nur
				das max -> distanz vom schiff... fertig... ja, ist gut so

	will ich noch andere Modi? fixe modi? glaube nicht...
		und den Wert der Position relativ zum pivot sollte ich mir merken, damit bei einem change
				das nicht an Arsch geht...? oder so

*/

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
					pivot = position + positionToPivot;

				FlightCamera.fetch.GetCameraTransform().position = position;
			}
		}

/*
			if(GameSettings.ZOOM_IN.GetKey())
			{
				pos += cm.forward * factor;
			}

			if(GameSettings.ZOOM_OUT.GetKey())
			{
				pos -= cm.forward * factor;
			}
FEHLER FEHLER, Zoom fehlt noch



			jetzt noch wie oben, aber rotation einbauen damit um den Pivot herum


// sich relativ zum Schiff positionieren... immer, weil das bewegt sich? oder so? oder ist's nur ungenau? hmm...

	if(!firstIdea)
			FlightCamera.fetch.GetCameraTransform().position = pos;


		}
*/
//			if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
//			{
//				Quaternion qt = FlightCamera.fetch.GetPivot().rotation;

//				Quaternion r = Quaternion.AngleAxis(ractor, FlightCamera.fetch.GetPivot().right);
//				Quaternion u = Quaternion.AngleAxis(ractor, FlightCamera.fetch.GetPivot().up);

//				float p = FlightCamera.fetch.camPitch;
//				float h = FlightCamera.fetch.camHdg;

//bool wasInput = false;

//				if(Input.GetKey(KeyCode.RightArrow))
//				{
//					qt = r * qt;
//					h -= ractor * Mathf.PI / 180f;
//					wasInput = true;
//				}
				
//				if(Input.GetKey(KeyCode.LeftArrow))
//				{
//					qt = Quaternion.Inverse(r) * qt;
//					h += ractor * Mathf.PI / 180f;
//					wasInput = true;
//				}

//				if(Input.GetKey(KeyCode.UpArrow))
//				{
//					qt = u * qt;
//					p -= ractor * Mathf.PI / 180f;
//					wasInput = true;
//				}

//				if(Input.GetKey(KeyCode.DownArrow))
//				{
//					qt = Quaternion.Inverse(u) * qt;
//					p += ractor * Mathf.PI / 180f;
//					wasInput = true;
//				}

//				if(wasInput)
//				{

//		Quaternion tgtFoR = FlightCamera.fetch.getReferenceFrame();

//					switch(rmode)
//					{
//				case 0:
//						FlightCamera.fetch.camPitch = 90f * Mathf.PI / 180f;
//						FlightCamera.fetch.camHdg = 45f * Mathf.PI / 180f;

//	//					FlightCamera.fetch.enabled = true;

//		FlightCamera.fetch.GetPivot().rotation = tgtFoR * Quaternion.AngleAxis(h * 57.29578f, Vector3.up) * Quaternion.AngleAxis(p * 57.29578f, Vector3.right);;

//break;

//					case 1:
//						FlightCamera.fetch.camPitch = p;
//						FlightCamera.fetch.camHdg = h;
////						FlightCamera.fetch.enabled = true;

//FlightCamera.fetch.GetCameraTransform().rotation =
//	/*	FlightCamera.fetch.GetPivot().rotation =*/ tgtFoR * Quaternion.AngleAxis(h * 57.29578f, Vector3.up) * Quaternion.AngleAxis(p * 57.29578f, Vector3.right);;

//						break;


//					case 2:
//						FlightCamera.fetch.GetPivot().rotation = qt;
//						break;
//					}
//				}

////				if(Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Alpha0))
////					FlightCamera.fetch.ActivateUpdate();
//			}

//			if(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr))
//			{
////	FlightCamera.fetch.GetPivot().position = position;

//				Quaternion qt = FlightCamera.fetch.GetPivot().rotation;

//				Transform cm = FlightCamera.fetch.GetCameraTransform();
//				Vector3 pos = cm.position;

//bool wasInput = false;

//				if(Input.GetKey(KeyCode.RightArrow))
//				{
//					pos += cm.right * factor;
//					wasInput = true;
//				}
				
//				if(Input.GetKey(KeyCode.LeftArrow))
//				{
//					pos -= cm.right * factor;
//					wasInput = true;
//				}

//				if(Input.GetKey(KeyCode.UpArrow))
//				{
//					pos += cm.up * factor;
//					wasInput = true;
//				}

//				if(Input.GetKey(KeyCode.DownArrow))
//				{
//					pos -= cm.up * factor;
//					wasInput = true;
//				}

//				if(Input.mouseScrollDelta.y != 0f)
//				{
//					pos += Input.mouseScrollDelta.y * cm.forward;
//					wasInput = true;
//				}

//				if(GameSettings.ZOOM_IN.GetKey())
//				{
//					pos += cm.forward * factor;
//					wasInput = true;
//				}
//				if(GameSettings.ZOOM_OUT.GetKey())
//				{
//					pos -= cm.forward * factor;
//					wasInput = true;
//				}

//				if(wasInput)
//				{
//					switch(mode)
//					{
//					case 0:
//						break;

//						case 1:
////der pivot könnte auch noch verschoben werden, wenn man freude hätte

//			FlightCamera.fetch.GetCameraTransform().position = pos;
//						break;

//						case 2:
//			FlightCamera.fetch.SetCamCoordsFromPosition(pos);
//						break;

//						case 3:
//			FlightCamera.fetch.SetCamCoordsFromPosition(pos);
//			FlightCamera.fetch.GetCameraTransform().position = pos;
//						break;
//					}
//				}

//				if(Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Alpha0))
//					FlightCamera.fetch.ActivateUpdate();
//			}

			/*
StartCoroutine(ahi(FlightCamera.fetch.GetPivot().position, FlightCamera.fetch.GetCameraTransform().position, FlightCamera.fetch.GetCameraTransform().position));
IEnumerator ahi(Vector3 position, Vector3 position2, Vector3 position3)
{

	FlightCamera.fetch.GetPivot().position = position;
	FlightCamera.fetch.SetCamCoordsFromPosition(position2);
	FlightCamera.fetch.GetCameraTransform().position = position3;
}
*/

/*

			if(InputLockManager.IsUnlocked(ControlTypes.LINEAR))
			{
				byte DownType = (byte)((Input.GetKey(KeyCode.LeftControl) | Input.GetKey(KeyCode.RightControl)) ? 2 : 1);
byte UpType = 0; // FEHLER, oder etwas, wenn wir den haben wollen -> temp, bin am experimentieren

				key_l += (byte)((Input.GetKeyDown(KeyCode.L) ? DownType : 0) + (Input.GetKeyUp(KeyCode.L) ? UpType : 0));
				key_j += (byte)((Input.GetKeyDown(KeyCode.J) ? DownType : 0) + (Input.GetKeyUp(KeyCode.J) ? UpType : 0));
				key_i += (byte)((Input.GetKeyDown(KeyCode.I) ? DownType : 0) + (Input.GetKeyUp(KeyCode.I) ? UpType : 0));
				key_k += (byte)((Input.GetKeyDown(KeyCode.K) ? DownType : 0) + (Input.GetKeyUp(KeyCode.K) ? UpType : 0));
				key_h += (byte)((Input.GetKeyDown(KeyCode.H) ? DownType : 0) + (Input.GetKeyUp(KeyCode.H) ? UpType : 0));
				key_n += (byte)((Input.GetKeyDown(KeyCode.N) ? DownType : 0) + (Input.GetKeyUp(KeyCode.N) ? UpType : 0));

				key_w += (byte)((Input.GetKeyDown(KeyCode.W) ? DownType : 0) + (Input.GetKeyUp(KeyCode.W) ? UpType : 0));
				key_s += (byte)((Input.GetKeyDown(KeyCode.S) ? DownType : 0) + (Input.GetKeyUp(KeyCode.S) ? UpType : 0));
				key_a += (byte)((Input.GetKeyDown(KeyCode.A) ? DownType : 0) + (Input.GetKeyUp(KeyCode.A) ? UpType : 0));
				key_d += (byte)((Input.GetKeyDown(KeyCode.D) ? DownType : 0) + (Input.GetKeyUp(KeyCode.D) ? UpType : 0));
				key_q += (byte)((Input.GetKeyDown(KeyCode.Q) ? DownType : 0) + (Input.GetKeyUp(KeyCode.Q) ? UpType : 0));
				key_e += (byte)((Input.GetKeyDown(KeyCode.E) ? DownType : 0) + (Input.GetKeyUp(KeyCode.E) ? UpType : 0));
			}

			if(bAction2)
			{
				if(Input.GetKeyDown(KeyCode.Mouse0))
				{
					Part p = GetPartUnderCursor();
					if(p)
					{
						pActiveGroup.SetTargetPosition(
							p.transform.position,
							Quaternion.LookRotation(p.transform.forward, -p.transform.up)); // point towards the part / "invert" p.transform.rotation
					}

					bAction2 = false;
				}
			}
		}
*/
/*		void ProcessInput()
		{
			if(!InputLockManager.IsUnlocked(ControlTypes.LINEAR))
			{
				key_l = 0; key_j = 0; key_i = 0; key_k = 0; key_h = 0; key_n = 0;
				key_w = 0; key_s = 0; key_a = 0; key_d = 0; key_q = 0; key_e = 0;
				return;
			}

			byte DownType = 1;
byte PressDownType = 1;

			if(Input.GetKey(KeyCode.LeftControl) | Input.GetKey(KeyCode.RightControl))
			{
DownType = 2;

				fastCounter += Time.fixedDeltaTime;

				if(fastCounter < 1f)
					PressDownType = 0;
				else
				{
					PressDownType = 2;
					fastCounter = 0f;
				}
			}
			else
				fastCounter = 1f;

			key_l += (byte)(Input.GetKey(KeyCode.L) ? PressDownType : 0);
			key_j += (byte)(Input.GetKey(KeyCode.J) ? PressDownType : 0);
			key_i += (byte)(Input.GetKey(KeyCode.I) ? PressDownType : 0);
			key_k += (byte)(Input.GetKey(KeyCode.K) ? PressDownType : 0);
			key_h += (byte)(Input.GetKey(KeyCode.H) ? PressDownType : 0);
			key_n += (byte)(Input.GetKey(KeyCode.N) ? PressDownType : 0);

			key_w += (byte)(Input.GetKey(KeyCode.W) ? PressDownType : 0);
			key_s += (byte)(Input.GetKey(KeyCode.S) ? PressDownType : 0);
			key_a += (byte)(Input.GetKey(KeyCode.A) ? PressDownType : 0);
			key_d += (byte)(Input.GetKey(KeyCode.D) ? PressDownType : 0);
			key_q += (byte)(Input.GetKey(KeyCode.Q) ? PressDownType : 0);
			key_e += (byte)(Input.GetKey(KeyCode.E) ? PressDownType : 0);

			bool bFast = (DownType > 1);

			// FEHLER, Versuch -> ich probiere mal das aktuelle Teil anzuzeigen... seine Pfeile sozusagen
			// und die dann herumzuschieben... und das später als Ziel nutzen...
			if((pActiveGroup != null) && (pActiveGroup.pEndEffector != null))
			{
				Controller.Instance.lm.maxDistance = (pActiveGroup.servoGroup.GroupSpeedFactor / 10f) * maxDistance;
				Controller.Instance.lm.accelerationFactor =
					1 / speedFactor;
			
				Controller.Instance.lm.endEffector = pActiveGroup.pEndEffector;	// FEHLER, alles optimieren, nicht jedes mal neu setzen und so Scheisse
				Controller.Instance.lm.Update(bLimiter);

				// Position wo ich bin -> zeichnet die ServoGroup schon -> ShowLines
	//			DrawRelative(0, pActiveGroup.pEndEffector.transform.position,
	//				pActiveGroup.pEndEffector.transform.up);
	//			DrawRelative(1, pActiveGroup.pEndEffector.transform.position,
	//				pActiveGroup.pEndEffector.transform.right);

				float factor = bFast ? 0.1f : 0.001f;

				//	w, s -> vor, zurück kippen (höhen) um rechts herum
				//	a, d -> links rechts drehen (seiten) um up herum, in unserem fall forward
				//	q, e -> links rechts drehen (quer) um forward herum in unserem fall up

				bool wasThisAKey = false;

				Vector3 r = pActiveGroup.localEndEffectorRight;
				Vector3 u = pActiveGroup.localEndEffectorUp;
				Vector3 f = Vector3.Cross(r, u);

				Vector3 targetPosition = pActiveGroup.targetPosition;
				Quaternion targetRotation = pActiveGroup.targetRotation;

				if((key_l & DownType) != 0) // rechts
				{ targetPosition += targetRotation * pActiveGroup.localEndEffectorRight * factor; wasThisAKey = true; }
				if((key_j & DownType) != 0) // links
				{ targetPosition -= targetRotation * pActiveGroup.localEndEffectorRight * factor; wasThisAKey = true; }
				if((key_i & DownType) != 0) // rauf / vorwärts
				{ targetPosition += targetRotation * Quaternion.AngleAxis(90f, pActiveGroup.localEndEffectorUp) * pActiveGroup.localEndEffectorRight * factor; wasThisAKey = true; }
				if((key_k & DownType) != 0) // runter / rückwärts
				{ targetPosition -= targetRotation * Quaternion.AngleAxis(90f, pActiveGroup.localEndEffectorUp) * pActiveGroup.localEndEffectorRight * factor; wasThisAKey = true; }
				if((key_h & DownType) != 0) // vorwärts / rauf
				{ targetPosition += targetRotation * pActiveGroup.localEndEffectorUp * factor; wasThisAKey = true; }
				if((key_n & DownType) != 0) // rückwärts / runter
				{ targetPosition -= targetRotation * pActiveGroup.localEndEffectorUp * factor; wasThisAKey = true; }

				float _s = bFast ? 15f : 1f;

				if((key_w & DownType) != 0)
				{ targetRotation *= Quaternion.AngleAxis(_s, r); wasThisAKey = true; }
				if((key_s & DownType) != 0)
				{ targetRotation *= Quaternion.AngleAxis(-_s, r); wasThisAKey = true; }
				if((key_a & DownType) != 0)
				{ targetRotation *= Quaternion.AngleAxis(_s, u); wasThisAKey = true; }
				if((key_d & DownType) != 0)
				{ targetRotation *= Quaternion.AngleAxis(-_s, u); wasThisAKey = true; }
				if((key_q & DownType) != 0)
				{ targetRotation *= Quaternion.AngleAxis(_s, f); wasThisAKey = true; }
				if((key_e & DownType) != 0)
				{ targetRotation *= Quaternion.AngleAxis(-_s, f); wasThisAKey = true; }

				if(wasThisAKey)
					Controller.bUserInput = true; // dann gab's Input

			key_l = 0; key_j = 0; key_i = 0; key_k = 0; key_h = 0; key_n = 0;
			key_w = 0; key_s = 0; key_a = 0; key_d = 0; key_q = 0; key_e = 0;
		}
*/
		////////////////////////////////////////
		// Debug
/*
		private MultiLineDrawer ld;

		private void InitializeLines(Transform t)
		{
			if(ld != null)
				ld.Destroy();
			else
				ld = new MultiLineDrawer();

			ld.Create(t);
		}

		public void UninitializeLines()
		{
			ld.Destroy();
			ld = null;
		}

		private void DrawRelative(int idx, Vector3 p_from, Vector3 p_vector)
		{
			ld.Draw(idx, p_from, p_from + p_vector);
		}
*/
	}
}
