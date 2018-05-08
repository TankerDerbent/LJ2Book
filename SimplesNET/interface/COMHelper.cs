using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimplesNET
{
	public class COMUtils
	{
		public static void ReleaseComRef<T>(T _ptr)
		{
			if (_ptr != null && !ReferenceEquals(null, _ptr))
				Marshal.ReleaseComObject(_ptr);
		}

		public static void ReleaseComRef<T>(ref T _ptr)
		{
			ReleaseComRef(_ptr);
			_ptr = default(T);
		}
	}
}