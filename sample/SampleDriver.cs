using System;
using UnityEngine;

namespace TrellisSample
{
	// unity calls the host and only the host calls the menu
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
