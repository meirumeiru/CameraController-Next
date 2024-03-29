﻿using System;
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

	//	public static bool APIReady { get { return ControllerInstance != null; } }


		private Type FlightCameraType = null;
		private System.Reflection.MethodInfo updateFoR_Info;
		private System.Reflection.MethodInfo GetChaseFoR_Info;
		private System.Reflection.FieldInfo FoRlerp_Info;
		private System.Reflection.FieldInfo terrainPitch_Info;
		private System.Reflection.FieldInfo meshPitch_Info;
		private System.Reflection.FieldInfo distance_Info;
		private System.Reflection.FieldInfo endPos_Info;
		private System.Reflection.FieldInfo endPitch_Info;
		private System.Reflection.FieldInfo localPitch_Info;
		private System.Reflection.FieldInfo lastLocalPitch_Info;
		private System.Reflection.FieldInfo offsetHdg_Info;
		private System.Reflection.FieldInfo offsetPitch_Info;
		private System.Reflection.FieldInfo camFXPos_Info;
		private System.Reflection.FieldInfo camFXRot_Info;
		private System.Reflection.FieldInfo target_Info;
		private System.Reflection.FieldInfo pivot_Info;
		private System.Reflection.FieldInfo _rigidbody_Info;
		private System.Reflection.FieldInfo hit_Info;

		private GameObject ShadowCamera = null;


		// input factors

		static float inputfactor_move = 0.2f;
		static float mousefactor_move = 0.4f;

		static float inputfactor_roll = 1f;
		static float mousefactor_roll = 0.1f;

		static float wheelfactor = 10f;

		// revert factors

		static float lerpfactor = 2f;

		// scroll acceleration

		static float tms = 0.2f; // delta-time

		float tm = 0f; // time sum
		Vector2 tv = Vector2.zero; // current scroll sum
		float scrollMagnitude = 0f;

		// position and rotation
		
		Quaternion frameOfReference;
		Part partOfReference;

		Vector3 position;		// camera
		Quaternion rotation;	// camera

		Vector3 pivot;
		Quaternion pivotRotation;

		Vector3 relPosition;	// position = vessel.position (com oder position, weiss noch nicht) + frameOfReference * relPosition
		Quaternion relRotation;

		Vector3 relPivot;
		Quaternion relPivotRotation;

		float lerp;


		public KerbalFSM fsm;

		public KFSMState st_normal;

		public KFSMState st_active;
		public KFSMState st_normalizing;


		public KFSMEvent on_activate;
		public KFSMEvent on_normalize;
		public KFSMEvent on_normalized;


		private void Awake()
		{
			if(!HighLogic.LoadedSceneIsFlight)
				ControllerInstance = null;
			else
				ControllerInstance = this;
		}

		public void Start()
		{
	//		GameEvents.OnCameraChange.Add(onCamChange);
			GameEvents.onVesselChange.Add(OnVesselChange);

			GameEvents.onVesselDocking.Add(OnDocking);
			GameEvents.onPartUndock.Add(OnUndocking);
			GameEvents.onPartDeCouple.Add(OnUndocking);

			Init();

			SetupFSM();

			fsm.StartFSM("normal");
		}

		public void OnDestroy()
		{
		//	Destroy(ShadowCamera);

	//		GameEvents.OnCameraChange.Remove(onCamChange);
			GameEvents.onVesselChange.Remove(OnVesselChange);

			GameEvents.onVesselDocking.Add(OnDocking);
			GameEvents.onPartUndock.Add(OnUndocking);
			GameEvents.onPartDeCouple.Add(OnUndocking);
		}

		private void Init()
		{
			FlightCameraType = typeof(FlightCamera);
			updateFoR_Info = FlightCameraType.GetMethod("updateFoR", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			GetChaseFoR_Info = FlightCameraType.GetMethod("GetChaseFoR", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			FoRlerp_Info = FlightCameraType.GetField("FoRlerp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			terrainPitch_Info = FlightCameraType.GetField("terrainPitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			meshPitch_Info = FlightCameraType.GetField("meshPitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			distance_Info = FlightCameraType.GetField("distance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			endPos_Info = FlightCameraType.GetField("endPos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			endPitch_Info = FlightCameraType.GetField("endPitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			localPitch_Info = FlightCameraType.GetField("localPitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			lastLocalPitch_Info = FlightCameraType.GetField("lastLocalPitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			offsetHdg_Info = FlightCameraType.GetField("offsetHdg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			offsetPitch_Info = FlightCameraType.GetField("offsetPitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			camFXPos_Info = FlightCameraType.GetField("camFXPos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			camFXRot_Info = FlightCameraType.GetField("camFXRot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			target_Info = FlightCameraType.GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			pivot_Info = FlightCameraType.GetField("pivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			_rigidbody_Info = FlightCameraType.GetField("_rigidbody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			hit_Info = FlightCameraType.GetField("hit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			ShadowCamera = new GameObject();
			ShadowCamera.SetActive(true);
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
				if(Input.GetKeyDown(KeyCode.KeypadDivide))
					fsm.RunEvent(on_activate);
			};
			st_normal.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_normal);

			st_active = new KFSMState("active");
			st_active.OnEnter = delegate(KFSMState from)
			{
				ShadowCamera.transform.parent = FlightCamera.fetch.transform.parent;
				ShadowCamera.transform.position = FlightCamera.fetch.transform.position;
				ShadowCamera.transform.rotation = FlightCamera.fetch.transform.rotation;

				FlightCamera.fetch.DeactivateUpdate();

				CaptureCamera();

				ScreenMessages.PostScreenMessage("CameraController active", 3, ScreenMessageStyle.UPPER_CENTER);
			};
			st_active.OnLateUpdate = delegate
			{
				// calculate the original position of the camera
				ShadowLateUpdate();

				// calculate our position of the camera
				CalculateCameraPosition();

				// process inputs
				ProcessInput();

				// set position of the camera
				FlightCamera.fetch.transform.rotation = rotation;
				FlightCamera.fetch.transform.position = position;

				// fix setting in case it was changed
				FlightCamera.fetch.DeactivateUpdate(); // reset this in every frame -> sometimes it suddendly changes

				// switch reference to new part in case it changed
				if(FlightCamera.fetch.Target && (partOfReference.transform != FlightCamera.fetch.Target))
					CaptureCamera();

				// deactivate camera controller if key is pressed
				if(Input.GetKeyDown(KeyCode.KeypadDivide))
					fsm.RunEvent(on_normalize);
			};
			st_active.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_active);

			st_normalizing = new KFSMState("normalizing");
			st_normalizing.OnEnter = delegate(KFSMState from)
			{
				lerp = 0f;
			};
			st_normalizing.OnLateUpdate = delegate
			{
				ShadowLateUpdate();

				CalculateCameraPosition();

				lerp += Time.deltaTime * lerpfactor; // revert in 0.5 seconds

				FlightCamera.fetch.transform.position = Vector3.Lerp(position, ShadowCamera.transform.position, lerp);
				FlightCamera.fetch.transform.rotation = Quaternion.Lerp(rotation, ShadowCamera.transform.rotation, lerp);

				if(lerp >= 1.0f)
					fsm.RunEvent(on_normalized);
			};
			st_normalizing.OnLeave = delegate(KFSMState to)
			{
				FlightCamera.fetch.ActivateUpdate();

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

		private void CaptureCamera(bool keepPosition = false)
		{
			if(FlightCamera.fetch.Target != null)
				partOfReference = FlightCamera.fetch.Target.GetComponent<Part>();
			else
				partOfReference = null;

			if(partOfReference == null)
				partOfReference = FlightGlobals.ActiveVessel.rootPart;

			frameOfReference = FlightGlobals.GetFoR(FlightCamera.fetch.FoRMode,
				partOfReference.transform, partOfReference.vessel.orbit);

			if(!keepPosition)
			{
				position = FlightCamera.fetch.transform.position;
				rotation = FlightCamera.fetch.transform.rotation;
			}

			pivot = FlightCamera.fetch.GetPivot().position;
			pivotRotation = frameOfReference;

			relPosition = Quaternion.Inverse(frameOfReference) * (position - partOfReference.transform.position);
			relRotation = Quaternion.Inverse(frameOfReference) * rotation;
	
			relPivot = Quaternion.Inverse(frameOfReference) * (pivot - partOfReference.transform.position);
			relPivotRotation = Quaternion.identity;
		}

		private void ShadowLateUpdate()
		{
			float FoRlerp = (float)FoRlerp_Info.GetValue(FlightCamera.fetch);
			FoRlerp = Mathf.Clamp01(FoRlerp + Time.unscaledDeltaTime * FlightCamera.fetch.orientationSharpness);
			FoRlerp_Info.SetValue(FlightCamera.fetch, FoRlerp);

			Action<Quaternion, float> updateFoR_Delegate = (Action<Quaternion, float>)Delegate.CreateDelegate(typeof(Action<Quaternion, float>), FlightCamera.fetch, updateFoR_Info);

			switch(FlightCamera.fetch.mode)
			{
			case FlightCamera.Modes.FREE:
				updateFoR_Delegate(FlightCamera.fetch.GetCameraFoR(FoRModes.SRF_NORTH), FoRlerp);
				break;
			case FlightCamera.Modes.CHASE:
				{
				Func<Vessel, Quaternion> GetChaseFoR_Delegate = (Func<Vessel, Quaternion>)Delegate.CreateDelegate(typeof(Func<Vessel, Quaternion>), FlightCamera.fetch, GetChaseFoR_Info);
				updateFoR_Delegate(GetChaseFoR_Delegate(FlightGlobals.ActiveVessel), FoRlerp);
				}
				break;
			case FlightCamera.Modes.LOCKED:
				if(FlightGlobals.ActiveVessel.isEVA)
					FlightCamera.fetch.setMode(FlightCamera.Modes.FREE);
				else
					updateFoR_Delegate(FlightCamera.fetch.GetCameraFoR(FoRModes.SHP_REL), FoRlerp);
				break;
			case FlightCamera.Modes.ORBITAL:
				if(FlightGlobals.ActiveVessel.radarAltitude < 2000.0)
					FlightCamera.fetch.setMode(FlightCamera.Modes.CHASE);
				else
					updateFoR_Delegate(FlightCamera.fetch.GetCameraFoR(FoRModes.OBT_ABS), FoRlerp);
				break;
			case FlightCamera.Modes.AUTO:
			{
				FlightCamera.Modes modes = FlightCamera.fetch.autoMode;
				if(modes != FlightCamera.Modes.FREE)
				{
					if(modes == FlightCamera.Modes.ORBITAL)
						updateFoR_Delegate(FlightCamera.fetch.GetCameraFoR(FoRModes.OBT_ABS), FoRlerp);
				}
				else
					updateFoR_Delegate(FlightCamera.fetch.GetCameraFoR(FoRModes.SRF_NORTH), FoRlerp);
				FlightCamera.Modes autoModeForVessel = FlightCamera.GetAutoModeForVessel(FlightGlobals.ActiveVessel);
				if(FlightCamera.fetch.autoMode == autoModeForVessel)
					break;
				FlightCamera.fetch.autoMode = autoModeForVessel;
				break;
			}
			}
			if(FoRlerp != 1f)
				FlightCamera.fetch.SetCamCoordsFromPosition(ShadowCamera.transform.position);

			ShadowUpdateCameraTransform();
		}

		private void ShadowUpdateCameraTransform()
		{
			FlightCamera.fetch.upAxis = FlightGlobals.upAxis;
			terrainPitch_Info.SetValue(FlightCamera.fetch, FlightCamera.fetch.minPitch);
			meshPitch_Info.SetValue(FlightCamera.fetch, FlightCamera.fetch.minPitch);
			FlightCamera.fetch.minHeight = Mathf.Lerp(FlightCamera.fetch.minHeightAtMinDist, FlightCamera.fetch.minHeightAtMaxDist, Mathf.InverseLerp(FlightCamera.fetch.minDistance, FlightCamera.fetch.maxDistance, (float)distance_Info.GetValue(FlightCamera.fetch)));
			FlightCamera.fetch.camPitch = Mathf.Min(FlightCamera.fetch.maxPitch, FlightCamera.fetch.camPitch);
			if((FlightCamera.fetch.FoRMode == FoRModes.SRF_HDG)
			|| (FlightCamera.fetch.FoRMode == 0)
			|| (FlightCamera.fetch.FoRMode == FoRModes.SRF_VEL))
			{
				RaycastHit hit;
				if(Physics.Raycast(((Transform)pivot_Info.GetValue(FlightCamera.fetch)).position + FlightCamera.fetch.upAxis * (float)distance_Info.GetValue(FlightCamera.fetch), -FlightCamera.fetch.upAxis, out hit, (float)distance_Info.GetValue(FlightCamera.fetch) * 2f, 32768, QueryTriggerInteraction.Ignore))
				{
					hit_Info.SetValue(FlightCamera.fetch, hit);
					meshPitch_Info.SetValue(FlightCamera.fetch, Mathf.Atan2(FlightGlobals.getAltitudeAtPos(hit.point) + FlightCamera.fetch.minHeight - FlightGlobals.getAltitudeAtPos(((Transform)pivot_Info.GetValue(FlightCamera.fetch)).position), (float)distance_Info.GetValue(FlightCamera.fetch) * Mathf.Cos(FlightCamera.fetch.camPitch)));
					terrainPitch_Info.SetValue(FlightCamera.fetch, Mathf.Max((float)terrainPitch_Info.GetValue(FlightCamera.fetch), (float)meshPitch_Info.GetValue(FlightCamera.fetch)));
				}
			}
			float num = 0f;
			switch(FlightCamera.fetch.targetMode)
			{
			case FlightCamera.TargetMode.Vessel:
				num = FlightGlobals.getAltitudeAtPos(ShadowCamera.transform.TransformPoint((Vector3)endPos_Info.GetValue(FlightCamera.fetch)), FlightCamera.fetch.vesselTarget.mainBody);
				endPos_Info.SetValue(FlightCamera.fetch, FlightCamera.fetch.vesselTarget.localCoM);
				break;
			case FlightCamera.TargetMode.Part:
				num = FlightGlobals.getAltitudeAtPos(ShadowCamera.transform.TransformPoint((Vector3)endPos_Info.GetValue(FlightCamera.fetch)), FlightCamera.fetch.partTarget.vessel.mainBody);
				endPos_Info.SetValue(FlightCamera.fetch, FlightCamera.fetch.partTarget.CoMOffset);
				break;
			case FlightCamera.TargetMode.Transform:
				num = FlightGlobals.getAltitudeAtPos(ShadowCamera.transform.TransformPoint((Vector3)endPos_Info.GetValue(FlightCamera.fetch)));
				endPos_Info.SetValue(FlightCamera.fetch, Vector3.zero);
				break;
			case FlightCamera.TargetMode.None:
				num = FlightGlobals.getAltitudeAtPos(((Transform)pivot_Info.GetValue(FlightCamera.fetch)).TransformPoint((Vector3)endPos_Info.GetValue(FlightCamera.fetch)));
				endPos_Info.SetValue(FlightCamera.fetch, Vector3.zero);
				break;
			}
			if(num > PhysicsGlobals.CameraDepthToUnlock)
				endPitch_Info.SetValue(FlightCamera.fetch, (float)Math.Max(FlightCamera.fetch.camPitch, Math.Max((float)terrainPitch_Info.GetValue(FlightCamera.fetch), Math.Atan2(FlightCamera.fetch.minHeight - num, (float)distance_Info.GetValue(FlightCamera.fetch) * Mathf.Cos(FlightCamera.fetch.camPitch)))));
			else
				endPitch_Info.SetValue(FlightCamera.fetch, Math.Max(FlightCamera.fetch.camPitch, (float)terrainPitch_Info.GetValue(FlightCamera.fetch)));
			((Transform)pivot_Info.GetValue(FlightCamera.fetch)).rotation = frameOfReference * Quaternion.AngleAxis(FlightCamera.fetch.camHdg * 57.29578f, Vector3.up) * Quaternion.AngleAxis((float)endPitch_Info.GetValue(FlightCamera.fetch) * 57.29578f, Vector3.right);
			if(FlightCamera.fetch.targetMode != 0)
			{
				((Transform)pivot_Info.GetValue(FlightCamera.fetch)).localPosition = Vector3.Lerp(((Transform)pivot_Info.GetValue(FlightCamera.fetch)).localPosition, (Vector3)endPos_Info.GetValue(FlightCamera.fetch), FlightCamera.fetch.pivotTranslateSharpness * Time.unscaledDeltaTime);
			}
			if(((Transform)target_Info.GetValue(FlightCamera.fetch)) != ((Transform)pivot_Info.GetValue(FlightCamera.fetch)))
			{
				Rigidbody _rigidbody;
				_rigidbody = (Rigidbody)_rigidbody_Info.GetValue(FlightCamera.fetch);
				((Transform)target_Info.GetValue(FlightCamera.fetch)).GetComponentCached(ref _rigidbody);
				_rigidbody_Info.SetValue(FlightCamera.fetch, _rigidbody);

				if(_rigidbody != null)
					((Transform)pivot_Info.GetValue(FlightCamera.fetch)).rotation *= Quaternion.Euler(Vector3.ProjectOnPlane((Quaternion.Inverse(((Transform)pivot_Info.GetValue(FlightCamera.fetch)).rotation) * _rigidbody.angularVelocity).normalized, ShadowCamera.transform.up) * _rigidbody.angularVelocity.magnitude * FlightCamera.fetch.cameraWobbleSensitivity);
			}
			camFXPos_Info.SetValue(FlightCamera.fetch, Vector3.back * (float)distance_Info.GetValue(FlightCamera.fetch));
			camFXRot_Info.SetValue(FlightCamera.fetch, Quaternion.LookRotation(-ShadowCamera.transform.localPosition, Vector3.up));
			if(CameraFX.Instance != null)
			{
				if(CameraFX.Instance.FX.Count > 0)
				{
					camFXPos_Info.SetValue(FlightCamera.fetch, CameraFX.Instance.FX.GetLocalPositionFX((Vector3)camFXPos_Info.GetValue(FlightCamera.fetch), 1f * GameSettings.CAMERA_FX_EXTERNAL, CameraFXModules.Views.FlightExternal));
					camFXRot_Info.SetValue(FlightCamera.fetch, CameraFX.Instance.FX.GetLocalRotationFX((Quaternion)camFXRot_Info.GetValue(FlightCamera.fetch), 0.06f * GameSettings.CAMERA_FX_EXTERNAL, CameraFXModules.Views.FlightExternal));
				}
			}
			ShadowCamera.transform.localPosition = Vector3.Lerp(ShadowCamera.transform.localPosition, (Vector3)camFXPos_Info.GetValue(FlightCamera.fetch), FlightCamera.fetch.sharpness * Time.unscaledDeltaTime);
			ShadowCamera.transform.localRotation = (Quaternion)camFXRot_Info.GetValue(FlightCamera.fetch);
			if(((float)terrainPitch_Info.GetValue(FlightCamera.fetch) > FlightCamera.fetch.minPitch)
			&& (FlightCamera.fetch.camPitch < (float)terrainPitch_Info.GetValue(FlightCamera.fetch)))
			{
				localPitch_Info.SetValue(FlightCamera.fetch, Mathf.Max(Mathf.Min(FlightCamera.fetch.camPitch - (float)endPitch_Info.GetValue(FlightCamera.fetch), -0.879645944f)));
				if((float)localPitch_Info.GetValue(FlightCamera.fetch) <= -0.879645944f)
					FlightCamera.fetch.camPitch = (float)lastLocalPitch_Info.GetValue(FlightCamera.fetch);
				else
					lastLocalPitch_Info.SetValue(FlightCamera.fetch, FlightCamera.fetch.camPitch);
				ShadowCamera.transform.Rotate(Vector3.right, (float)localPitch_Info.GetValue(FlightCamera.fetch) * 57.29578f, Space.Self);
			}
			else
			{
				FlightCamera.fetch.camPitch = (float)endPitch_Info.GetValue(FlightCamera.fetch);
				localPitch_Info.SetValue(FlightCamera.fetch, 0f);
				lastLocalPitch_Info.SetValue(FlightCamera.fetch, 0f);
			}
			ShadowCamera.transform.Rotate(((Transform)pivot_Info.GetValue(FlightCamera.fetch)).up, (float)offsetHdg_Info.GetValue(FlightCamera.fetch) * 57.29578f, Space.World);
			ShadowCamera.transform.Rotate(Vector3.right, (float)offsetPitch_Info.GetValue(FlightCamera.fetch) * 57.29578f, Space.Self);
			if(!GameSettings.TRACKIR_ENABLED)
				return;
/* FEHLER, was ist IR?
			if(!FlightCamera.fetch.TrackIRisActive())
				return;
	FlightCamera.fetch.transform.Rotate(Vector3.up, FlightCamera.fetch.tIRyaw * 57.29578f, Space.Self);
	FlightCamera.fetch.transform.Rotate(Vector3.right, FlightCamera.fetch.tIRpitch * 57.29578f, Space.Self);
	FlightCamera.fetch.transform.Rotate(Vector3.forward, FlightCamera.fetch.tIRroll * 57.29578f, Space.Self);
*/
		}

		private void ClampPosition()
		{
			Vector3 vesselToCamera = position - FlightGlobals.ActiveVessel.transform.position;

			if(vesselToCamera.sqrMagnitude > (FlightCamera.fetch.maxDistance * FlightCamera.fetch.maxDistance))
				position = FlightGlobals.ActiveVessel.transform.position + vesselToCamera.normalized * FlightCamera.fetch.maxDistance;
		}

		bool docking = false;

		protected void OnDocking(uint id1, uint id2)
		{
			docking = (partOfReference != null) ? ((partOfReference.vessel.persistentId == id1) | (partOfReference.vessel.persistentId == id2)) : false;
		}

		bool undocking = false;

		protected void OnUndocking(Part part)
		{
			undocking = (partOfReference != null) ? (part.vessel == partOfReference.vessel) : false;
		}

		protected void OnVesselChange(Vessel vessel)
		{
			CaptureCamera(docking | undocking);

			docking = false;
			undocking = false;
		}

		private void CalculateCameraPosition()
		{
			frameOfReference = FlightGlobals.GetFoR(FlightCamera.fetch.FoRMode,
				partOfReference.transform, partOfReference.vessel.orbit);

			position = partOfReference.transform.position + frameOfReference * relPosition;
			rotation = frameOfReference * relRotation;

			pivot = partOfReference.transform.position + frameOfReference * relPivot;
			pivotRotation = frameOfReference * relPivotRotation;
		}

		private void ProcessInput()
		{
			// Scroll Acceleration

			tm += Time.deltaTime;
			tv += Input.mouseScrollDelta;

			if(tm >= tms)
			{
				scrollMagnitude = 0.3f * scrollMagnitude + 0.7f * tv.magnitude;
				scrollMagnitude = (scrollMagnitude > 0.1f) ? scrollMagnitude - 0.1f : 0f;

				tm = 0f;
				tv = Vector3.zero;
			}

			// Input

			float slowFactor = Input.GetKey(KeyCode.KeypadMultiply) ? 0.1f : 1f; // if you want to move the camera very slow e.g. for filming or precise movements

			float hdg = 0; float pitch = 0; float rot = 0; float rot2 = 0;
			bool hasInput = false;

			//////////////////////////////
			// Translation

			{
			hasInput = false;

			Vector3 positionToPivot = pivot - position;

			if(Input.GetKey(KeyCode.RightShift))
			{
				if(Input.GetKey(KeyCode.RightArrow))
				{
					position += rotation * Vector3.right * inputfactor_move * slowFactor;
					hasInput = true;
				}
				
				if(Input.GetKey(KeyCode.LeftArrow))
				{
					position -= rotation * Vector3.right * inputfactor_move * slowFactor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.UpArrow))
				{
					position += rotation * Vector3.up * inputfactor_move * slowFactor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.DownArrow))
				{
					position -= rotation * Vector3.up * inputfactor_move * slowFactor;
					hasInput = true;
				}

				if(Input.GetMouseButton(1))
				{
					float x = Input.GetAxis("Mouse X");
					float y = Input.GetAxis("Mouse Y");

					if(x != 0f)
					{
						position -= rotation * Vector3.right * x * mousefactor_move * slowFactor;
						hasInput = true;
					}

					if(y != 0)
					{
						position -= rotation * Vector3.up * y * mousefactor_move * slowFactor;
						hasInput = true;
					}
				}
			}

			if(Input.GetMouseButton(3))
			{
				float x = Input.GetAxis("Mouse X");
				float y = Input.GetAxis("Mouse Y");

				if(x != 0f)
				{
					position -= rotation * Vector3.right * x * mousefactor_move * slowFactor;
					hasInput = true;
				}

				if(y != 0)
				{
					position -= rotation * Vector3.up * y * mousefactor_move * slowFactor;
					hasInput = true;
				}
			}

			if(hasInput)
			{
				ClampPosition();

				pivot = position + positionToPivot;

				relPosition = Quaternion.Inverse(frameOfReference) * (position - partOfReference.transform.position);
				relPivot = Quaternion.Inverse(frameOfReference) * (pivot - partOfReference.transform.position);
			}
			}

			//////////////////////////////
			// Rotation Around View Axis

			{
			hasInput = false;

			if(Input.GetKey(KeyCode.RightControl)
			&& !Input.GetKey(KeyCode.RightShift))
			{
				if(Input.GetKey(KeyCode.RightArrow))
				{
					rot -= (Mathf.PI / 180f) * inputfactor_roll * slowFactor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.LeftArrow))
				{
					rot += (Mathf.PI / 180f) * inputfactor_roll * slowFactor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.UpArrow))
				{
					rot2 += (Mathf.PI / 180f) * inputfactor_roll * slowFactor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.DownArrow))
				{
					rot2 -= (Mathf.PI / 180f) * inputfactor_roll * slowFactor;
					hasInput = true;
				}

				if(Input.GetMouseButton(1))
				{
					float x = Input.GetAxis("Mouse X");
					float y = Input.GetAxis("Mouse Y");

					if(x != 0f)
					{
						rot -= x * mousefactor_roll * slowFactor;
						hasInput = true;
					}

					if(y != 0f)
					{
						rot2 -= y * mousefactor_roll * slowFactor;
						hasInput = true;
					}
				}
			}

			if(Input.GetMouseButton(4))
			{
				float x = Input.GetAxis("Mouse X");
				float y = Input.GetAxis("Mouse Y");

				if(x != 0f)
				{
					rot -= x * mousefactor_roll * slowFactor;
					hasInput = true;
				}

				if(y != 0f)
				{
					rot2 -= y * mousefactor_roll * slowFactor;
					hasInput = true;
				}
			}

			if(hasInput)
			{
				Vector3 localPivotToPosition = Quaternion.Inverse(rotation) * (position - pivot);

				pivotRotation =
					Quaternion.AngleAxis(rot2 * 57.29578f, rotation * Vector3.right)
					* Quaternion.AngleAxis(rot * 57.29578f, rotation * Vector3.forward)
					* pivotRotation;

				relPivotRotation = Quaternion.Inverse(frameOfReference) * pivotRotation;

				rotation =
					Quaternion.AngleAxis(rot2 * 57.29578f, rotation * Vector3.right)
					* Quaternion.AngleAxis(rot * 57.29578f, rotation * Vector3.forward)
					* rotation;

				relRotation = Quaternion.Inverse(frameOfReference) * rotation;

				position = pivot + rotation * localPivotToPosition;

				ClampPosition();

				relPosition = Quaternion.Inverse(frameOfReference) * (position - partOfReference.transform.position);
			}
			}

			//////////////////////////////
			// Rotation

			{
			hasInput = false;

			if(!Input.GetKey(KeyCode.RightControl) && !Input.GetKey(KeyCode.RightShift))
			{
				if(Input.GetKey(KeyCode.RightArrow))
				{
					hdg -= (Mathf.PI / 180f) * inputfactor_roll * slowFactor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.LeftArrow))
				{
					hdg += (Mathf.PI / 180f) * inputfactor_roll * slowFactor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.UpArrow))
				{
					pitch += (Mathf.PI / 180f) * inputfactor_roll * slowFactor;
					hasInput = true;
				}

				if(Input.GetKey(KeyCode.DownArrow))
				{
					pitch -= (Mathf.PI / 180f) * inputfactor_roll * slowFactor;
					hasInput = true;
				}

				if(Input.GetMouseButton(1))
				{
					float x = Input.GetAxis("Mouse X");
					float y = Input.GetAxis("Mouse Y");

					if(x != 0f)
					{
						hdg += x * mousefactor_roll * slowFactor;
						hasInput = true;
					}

					if(y != 0)
					{
						pitch -= y * mousefactor_roll * slowFactor;
						hasInput = true;
					}
				}
			}

			if(hasInput)
			{
				Vector3 localPivotToPosition = Quaternion.Inverse(rotation) * (position - pivot);

				rotation = rotation * Quaternion.AngleAxis(pitch * 57.29578f, Vector3.right);
				rotation = Quaternion.AngleAxis(hdg * 57.29578f, pivotRotation * Vector3.up) * rotation;

				relRotation = Quaternion.Inverse(frameOfReference) * rotation;

				position = pivot + rotation * localPivotToPosition;

				ClampPosition();

				relPosition = Quaternion.Inverse(frameOfReference) * (position - partOfReference.transform.position);
			}
			}

			if(GameSettings.AXIS_MOUSEWHEEL.GetAxis() != 0f)
			{
				//////////////////////////////
				// Zoom (or Translation)

				float sm = 0.3f + Mathf.Min(scrollMagnitude / 4f, 0.7f);
				float z = sm * GameSettings.AXIS_MOUSEWHEEL.GetAxis();

				z *= (pivot - position).magnitude * 0.067f; // distance factor

				z *= wheelfactor * slowFactor;

				Vector3 positionToPivot = pivot - position;

				position += rotation * Vector3.forward * z;

				ClampPosition();

				if(Input.GetKey(KeyCode.RightShift) || Input.GetMouseButton(3))
				{
					//////////////////////////////
					// Translation

					pivot = position + positionToPivot;
				}

				relPosition = Quaternion.Inverse(frameOfReference) * (position - partOfReference.transform.position);
				relPivot = Quaternion.Inverse(frameOfReference) * (pivot - partOfReference.transform.position);
			}

			//////////////////////////////
			// Zoom

			{
				hasInput = false;

				Vector3 positionToPivot = pivot - position;

				if(GameSettings.ZOOM_IN.GetKey())
				{
					position += rotation * Vector3.forward * inputfactor_move * slowFactor;
					hasInput = true;
				}

				if(GameSettings.ZOOM_OUT.GetKey())
				{
					position -= rotation * Vector3.forward * inputfactor_move * slowFactor;
					hasInput = true;
				}

				if(hasInput)
				{
					ClampPosition();

					pivot = position + positionToPivot;
				}

				relPosition = Quaternion.Inverse(frameOfReference) * (position - partOfReference.transform.position);
				relPivot = Quaternion.Inverse(frameOfReference) * (pivot - partOfReference.transform.position);
			}
		}

		public void LateUpdate()
		{
			if((fsm != null) && fsm.Started)
				fsm.LateUpdateFSM();
		}
	}
}
