﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimplesNet.Helper
{
	public static class KeyBoardHook
	{
		private const int WH_KEYBOARD_LL = 13;
		private const int WM_KEYDOWN = 0x0100;
		private const int WM_KEYDOWNAlt = 0x0104;
		private static LowLevelKeyboardProc _proc = HookCallback;
		private static IntPtr _hookID = IntPtr.Zero;
		public static event EventHandler KeyBoardAction = delegate { };

		public static void Start()
		{
			_hookID = SetHook(_proc);
		}

		public static void stop()
		{
			UnhookWindowsHookEx(_hookID);
		}

		private static IntPtr SetHook(LowLevelKeyboardProc proc)
		{
			using (Process curProcess = Process.GetCurrentProcess())
			using (ProcessModule curModule = curProcess.MainModule)
			{
				return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
					GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

		private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYDOWNAlt))
			{
				KeyBoardAction(null, new EventArgs());
			}

			return CallNextHookEx(_hookID, nCode, wParam, lParam);
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
	}
}
