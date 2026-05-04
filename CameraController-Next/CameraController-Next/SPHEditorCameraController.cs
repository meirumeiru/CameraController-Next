using System;

using UnityEngine;
using UnityEngine.EventSystems;

namespace CameraController_Next
{
	[KSPAddon(KSPAddon.Startup.EditorSPH, false)]
	public class SPHEditorCameraController : MonoBehaviour
	{
		public virtual String AddonName { get; set; }

		protected static SPHEditorCameraController ControllerInstance;

		public static SPHEditorCameraController Instance { get { return ControllerInstance; } }

	//	public static bool APIReady { get { return ControllerInstance != null; } }


		// "settings"

		public float minHeight = 2f;
		public float maxHeight = 50f;

		public float minPitch = -30f;
		public float maxPitch = 60f;

		public float minDistance = 3f;
		public float maxDistance = 35f;

		public float maxOffset = 25.5f;
		public float maxRealDistance = 30f;


		public float orbitSensitivity = 0.05f;
		public float mouseZoomSensitivity = 0.1f;
		public float offsetSensitivity = 0.1f;
		public float sharpness = 0.2f;

		// key bindings

		static KeyCode OnOffKey = KeyCode.KeypadDivide;

		// input factors

		static float inputfactor_move = 4f;
		static float mousefactor_move = 0.4f;

//		static float inputfactor_roll = 1f;
//		static float mousefactor_roll = 0.04f;

//		static float wheelfactor = 20f;

		// revert factors

		static float lerpfactor = 2f;

		// position and rotation

		public SPHCamera sph;		// camera

		public Vector3 originalPosition;
		public Quaternion originalRotation;
		public float originalDistance;

		public float distance;

		public float clampedScrollHeight;
		public float scrollHeight;

		public float camPitch;
		public float camHdg;

		public Vector3 offset;

		public Vector3 endPos;
		public Quaternion endRot;

		public float lerp;


		public KerbalFSM fsm;

		public KFSMState st_normal;

		public KFSMState st_active;
		public KFSMState st_normalizing;


		public KFSMEvent on_activate;
		public KFSMEvent on_normalize;
		public KFSMEvent on_normalized;


		public static void LoadSettings()
		{
			ConfigNode node = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/CameraControllerNext/Plugins/settings.cfg");

			if(node != null)
				node = node.GetNode("CameraControllerNextConfig");

			if(node != null)
			{
				if(!Enum.TryParse(node.GetValue("OnOffKey"), out OnOffKey))
					OnOffKey = KeyCode.KeypadDivide;
			}
		}

		private void Awake()
		{
			LoadSettings();

			if(!HighLogic.LoadedSceneIsEditor || (EditorDriver.editorFacility != EditorFacility.SPH))
				ControllerInstance = null;
			else
				ControllerInstance = this;
		}

		public void Start()
		{
			if(EditorDriver.editorFacility != EditorFacility.SPH)
				return;

			orbitSensitivity = GameSettings.VAB_CAMERA_ORBIT_SENS;
			mouseZoomSensitivity = GameSettings.VAB_CAMERA_ZOOM_SENS;

			SetupFSM();

			fsm.StartFSM("normal");
		}

		public void OnDestroy()
		{
		}

		private void SetupFSM()
		{
			fsm = new KerbalFSM();

			st_normal = new KFSMState("normal");
			st_normal.OnEnter = delegate(KFSMState from)
			{
			};
			st_normal.OnLateUpdate = delegate
			{
				// activate camera controller if key is pressed
				if(Input.GetKeyDown(OnOffKey))
					fsm.RunEvent(on_activate);
			};
			st_normal.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_normal);

			st_active = new KFSMState("active");
			st_active.OnEnter = delegate(KFSMState from)
			{
				sph = EditorLogic.fetch.editorCamera.GetComponent<SPHCamera>();
				sph.enabled = false;

				CaptureCamera();

				ScreenMessages.PostScreenMessage("CameraController active", 3, ScreenMessageStyle.UPPER_CENTER);
			};
			st_active.OnLateUpdate = delegate
			{
				// process inputs
				if(InputLockManager.IsUnlocked(ControlTypes.CAMERACONTROLS) || !EventSystem.current.IsPointerOverGameObject())
					ProcessInput();

				// set position of the camera
				UpdateCamera();

				// fix setting in case it was changed
			//	sph.enabled = false; // reset this in every frame -> sometimes it suddendly changes

				// deactivate camera controller if key is pressed
				if(Input.GetKeyDown(OnOffKey))
					fsm.RunEvent(on_normalize);
			};
			st_active.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_active);

			st_normalizing = new KFSMState("normalizing");
			st_normalizing.OnEnter = delegate(KFSMState from)
			{
				// set camera to our values instead of returning to original values

				Vector3 camCoordsFromPosition = KSPCameraUtil.GetCamCoordsFromPosition(sph.transform.position, Vector3.up * scrollHeight, Quaternion.identity);

				camPitch = camCoordsFromPosition.x;
				camHdg = camCoordsFromPosition.y;

				originalDistance = camCoordsFromPosition.z;

				originalRotation = Quaternion.AngleAxis(camHdg * 57.29578f, Vector3.up);
				originalRotation *= Quaternion.AngleAxis(camPitch * 57.29578f, Vector3.right);

				originalPosition = EditorBounds.ClampToCameraBounds(Vector3.up * scrollHeight, endRot * Vector3.forward, ref clampedScrollHeight);

				lerp = 0f;
			};
			st_normalizing.OnLateUpdate = delegate
			{
				lerp += Time.deltaTime * lerpfactor; // revert in 0.5 seconds

				Transform pivotTransform = sph.GetCameraTransform();

				pivotTransform.position = Vector3.Lerp(endPos, originalPosition, lerp);
				pivotTransform.rotation = Quaternion.Lerp(endRot, originalRotation, lerp);
				sph.transform.localPosition = Vector3.Lerp(Vector3.back * distance, Vector3.back * originalDistance, lerp);

				if(lerp >= 1.0f)
				{
					// set camera to our values instead of returning to original values

					sph.PlaceCamera(new Vector3(0, scrollHeight), originalDistance);

					sph.camPitch = camPitch;
					sph.camHdg = camHdg;

					fsm.RunEvent(on_normalized);
				}
			};
			st_normalizing.OnLeave = delegate(KFSMState to)
			{
				sph.enabled = true;

				ScreenMessages.PostScreenMessage("normal camera", 3, ScreenMessageStyle.UPPER_CENTER);
			};
			fsm.AddState(st_normalizing);

			on_activate = new KFSMEvent("activate");
			on_activate.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_activate.GoToStateOnEvent = st_active;
			fsm.AddEvent(on_activate, st_normal);

			on_normalize = new KFSMEvent("normalize");
			on_normalize.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_normalize.GoToStateOnEvent = st_normalizing;
			fsm.AddEvent(on_normalize, st_active);

			on_normalized = new KFSMEvent("normalized");
			on_normalized.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_normalized.GoToStateOnEvent = st_normal;
			fsm.AddEvent(on_normalized, st_normalizing);
		}

		private void CaptureCamera()
		{
			sharpness = sph.sharpness;

			Transform pivotTransform = sph.GetCameraTransform();

			originalPosition = pivotTransform.position;
			originalRotation = pivotTransform.rotation;
			originalDistance = sph.Distance;

			distance = sph.Distance;

			clampedScrollHeight = sph.scrollHeight;
			scrollHeight = sph.scrollHeight;

			camPitch = sph.camPitch;
			camHdg = sph.camHdg;

			offset = Vector3.zero;

			endPos = originalPosition;
			endRot = originalRotation;
		}

		private Vector3 Clamp(Vector3 value, float max)
		{ return new Vector3(Mathf.Clamp(value.x, -max, max), Mathf.Clamp(value.y, -max, max), Mathf.Clamp(value.z, -max, max)); }

		private void ProcessInput()
		{
			if(!InputLockManager.IsUnlocked(ControlTypes.CAMERACONTROLS) || EventSystem.current.IsPointerOverGameObject())
				return;

			if(GameSettings.AXIS_MOUSEWHEEL.GetAxis() != 0f)
			{
				if(Input.GetKey(KeyCode.LeftControl) | Input.GetKey(KeyCode.RightControl))
					offset = Clamp(offset + endRot * Vector3.forward * Mathf.Clamp(GameSettings.AXIS_MOUSEWHEEL.GetAxis() * 5f, -10f, 10f), maxOffset);
				else if (GameSettings.Editor_zoomScrollModifier.GetKey())
					distance = Mathf.Clamp(distance - GameSettings.AXIS_MOUSEWHEEL.GetAxis() * 5f, minDistance, maxDistance);
				else
					scrollHeight = Mathf.Clamp(clampedScrollHeight + GameSettings.AXIS_MOUSEWHEEL.GetAxis() * 5f, minHeight, maxHeight);
			}
			if(GameSettings.SCROLL_VIEW_UP.GetKey())
				scrollHeight = Mathf.Clamp(clampedScrollHeight + 0.1f, minHeight, maxHeight);
			if(GameSettings.SCROLL_VIEW_DOWN.GetKey())
				scrollHeight = Mathf.Clamp(clampedScrollHeight - 0.1f, minHeight, maxHeight);
			if(Input.GetMouseButton(2))
				distance = Mathf.Clamp(distance - Input.GetAxis("Mouse Y") * mouseZoomSensitivity, minDistance, maxDistance);
			if(GameSettings.ZOOM_IN.GetKey())
				distance = Mathf.Clamp(distance - mouseZoomSensitivity, minDistance, maxDistance);
			if(GameSettings.ZOOM_OUT.GetKey())
				distance = Mathf.Clamp(distance + mouseZoomSensitivity, minDistance, maxDistance);
			if(CameraMouseLook.GetMouseLook())
			{
				camHdg += Input.GetAxis("Mouse X") * orbitSensitivity;
				camPitch -= Input.GetAxis("Mouse Y") * orbitSensitivity;
			}
			if(Input.GetMouseButton(3))
			{
				float x = Input.GetAxis("Mouse X");
				offset = Clamp(offset + endRot * Vector3.right * (-mousefactor_move * x), maxOffset);

				float y = Input.GetAxis("Mouse Y");
				scrollHeight = Mathf.Clamp(clampedScrollHeight - y * mousefactor_move, minHeight, maxHeight);
			}
			camHdg -= GameSettings.AXIS_CAMERA_HDG.GetAxis() * orbitSensitivity;
			camPitch -= GameSettings.AXIS_CAMERA_PITCH.GetAxis() * orbitSensitivity;
			if(!InputLockManager.IsLocked(ControlTypes.GUI))
			{
				if(GameSettings.CAMERA_ORBIT_UP.GetKey())
					camPitch += 1f * Time.deltaTime;
				if(GameSettings.CAMERA_ORBIT_DOWN.GetKey())
					camPitch -= 1f * Time.deltaTime;
				if(Input.GetKey(KeyCode.LeftControl) | Input.GetKey(KeyCode.RightControl))
				{
					if(GameSettings.CAMERA_ORBIT_LEFT.GetKey())
						offset = Clamp(offset + endRot * Vector3.right * (-inputfactor_move * Time.deltaTime), maxOffset);
					if(GameSettings.CAMERA_ORBIT_RIGHT.GetKey())
						offset = Clamp(offset + endRot * Vector3.right * (+inputfactor_move * Time.deltaTime), maxOffset);
				}
				else
				{
					if(GameSettings.CAMERA_ORBIT_LEFT.GetKey())
						camHdg += 1f * Time.deltaTime;
					if(GameSettings.CAMERA_ORBIT_RIGHT.GetKey())
						camHdg -= 1f * Time.deltaTime;
				}
			}
		}

		private void UpdateCamera(bool smooth = true)
		{
			camPitch = Mathf.Clamp(camPitch, Mathf.Max(minPitch, Mathf.Atan2(minHeight - scrollHeight, distance)), maxPitch);
			endRot = Quaternion.AngleAxis(camHdg * 57.29578f, Vector3.up);
			endRot *= Quaternion.AngleAxis(camPitch * 57.29578f, Vector3.right);
			clampedScrollHeight = scrollHeight;

			endPos = EditorBounds.ClampToCameraBounds(Vector3.up * scrollHeight, endRot * Vector3.forward, ref clampedScrollHeight);
			endPos += offset;

			distance = EditorBounds.ClampCameraDistance(distance);

			Transform pivotTransform = sph.GetCameraTransform();

			Vector3 predictedPos = endRot * Vector3.back * distance;

			if((Mathf.Abs(predictedPos.x + endPos.x) > maxRealDistance) || (Mathf.Abs(predictedPos.z + endPos.z) > maxRealDistance))
			{
				float factorX = 1.0f;

				if((-maxRealDistance - offset.x) > predictedPos.x)
					factorX = Mathf.Abs(-maxRealDistance - offset.x) / Mathf.Abs(predictedPos.x);
				else if((maxRealDistance - offset.x) < predictedPos.x)
					factorX = Mathf.Abs(maxRealDistance - offset.x) / Mathf.Abs(predictedPos.x);

				float factorZ = 1.0f;

				if((-maxRealDistance - offset.z) > predictedPos.z)
					factorZ = Mathf.Abs(-maxRealDistance - offset.z) / Mathf.Abs(predictedPos.z);
				else if((maxRealDistance - offset.z) < predictedPos.z)
					factorZ = Mathf.Abs(maxRealDistance - offset.z) / Mathf.Abs(predictedPos.z);

				distance *= Mathf.Min(factorX, factorZ);
			}

			if(smooth)
			{
				pivotTransform.rotation = Quaternion.Lerp(pivotTransform.rotation, endRot, sharpness * Time.deltaTime);
				pivotTransform.position = Vector3.Lerp(pivotTransform.position, endPos, sharpness * Time.deltaTime);
				sph.transform.localPosition = Vector3.Lerp(sph.transform.localPosition, Vector3.back * distance, sharpness * Time.deltaTime);
			}
			else
			{
				pivotTransform.rotation = endRot;
				pivotTransform.position = endPos;
				sph.transform.localPosition = Vector3.back * distance;
			}
		}

		public void LateUpdate()
		{
			if((fsm != null) && fsm.Started)
				fsm.LateUpdateFSM();
		}
	}
}
