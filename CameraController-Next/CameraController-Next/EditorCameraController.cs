using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace CameraController_Next
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class EditorCameraController : MonoBehaviour
	{
		public virtual String AddonName { get; set; }

		protected static EditorCameraController ControllerInstance;

		public static EditorCameraController Instance { get { return ControllerInstance; } }

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

		public void Update()
		{
			if(Input.GetKeyDown(KeyCode.KeypadDivide))
			{
				active = !active;

				if(active)
				{
				}

				ScreenMessages.PostScreenMessage(active ? "CameraController active" : "normal camera", 3, ScreenMessageStyle.UPPER_CENTER);
			}

			if(!active)
			{
			}
		}
	}
}
