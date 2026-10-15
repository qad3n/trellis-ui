using System;
using UnityEngine;

namespace TrellisSample
{
	// unity calls into the host and the host calls into the menu, nothing else drives it
	public class SampleDriver : MonoBehaviour
	{
		public SampleDriver(IntPtr pointer) : base(pointer)
		{
		}

		private void Update()
		{
			if (Input.GetKeyDown(SampleMenu.kToggleKey))
				MenuHost.toggle();

			MenuHost.update(true);
		}

		private void OnGUI()
		{
			MenuHost.cacheInput();
			MenuHost.onGUI();
		}
	}
}
