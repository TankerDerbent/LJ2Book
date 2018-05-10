using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using System.Globalization;

namespace SimplesNet
{
	public static class HelperProcess
	{
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(IntPtr hwnd);

		public static bool IsRunedAnyProcess(string MCPath, IEnumerable<string> lignore = null)
		{
			Process[] procList = Process.GetProcesses();
			List<string> forDumpsProcesses = get_process_list_for_dumps(MCPath);
			return procList.Any(pl =>
				{
					try
					{
						var result = forDumpsProcesses.Contains(pl.MainModule.FileName.ToLowerInvariant());
						if (result && lignore != null)
						{
							result = !lignore.Any(i => pl.MainModule.FileName.ToLowerInvariant().Contains(i.ToLowerInvariant()));
						}
						return result;
					}
					catch (System.Exception)
					{
						return false;
					}
				});	
		}

		public static Process GetMineProcess()
		{
			Process[] procList = Process.GetProcesses();
			return procList.SingleOrDefault(pl =>
				{
					try
					{
						return pl.MainModule.FileName.ToLower() == Application.ExecutablePath.ToLower() && pl.MainWindowHandle != Process.GetCurrentProcess().MainWindowHandle;
					}
					catch (System.Exception)
					{
						return false;
					}
				});	
		}

		public static void ActivateProcess(Process process)
		{
			if (process != null)
			{
				SetForegroundWindow(process.MainWindowHandle);
			}
		}

		private static List<string> get_process_list_for_dumps(string path)
		{
			List<String> processList = new List<String>();
			processList.AddRange(Directory.GetFiles(path, "*.exe", SearchOption.AllDirectories));
			List<string> result = processList.ConvertAll(val => val.ToLower());
			string mine = Application.ExecutablePath.ToLower();
			if (result.Contains(mine))
				result.Remove(mine);

			return result;
		}

		public static Process CreateElevatedProcessRunAs(string fileName, string arguments, out string error)
		{
			try
			{
				Process p = new Process();
				p.StartInfo.FileName = fileName;
				p.StartInfo.Arguments = arguments;
				p.StartInfo.UseShellExecute = true;
				p.StartInfo.Verb = "runas";
				p.Start();
				error = string.Empty;
				return p;
			}
			catch (System.Exception ex)
			{
				error = ex.Message;
				return null;
			}
		}

		public static Process CreateUnElevatedProcessRunAs(string fileName, string arguments, out string error)
		{
			try
			{
				Process p = new Process();
				p.StartInfo.FileName = "runas.exe";
				p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
				p.StartInfo.Arguments = string.Format("/trustlevel:0x20000 \"{0}\"", fileName);
				p.Start();
				error = string.Empty;
				return p;
			}
			catch (System.Exception ex)
			{
				error = ex.Message;
				return null;
			}
		}

		public static Process CreateUnElevatedProcess(string fileName, string arguments, out string error)
		{
			Process newProcess = null;
			IntPtr hWnd = IntPtr.Zero;
			uint id = default(uint);
			SafeTokenHandle hShell = default(SafeTokenHandle);
			SafeTokenHandle hShellToken = default(SafeTokenHandle);
			SafeTokenHandle hDupedToken = default(SafeTokenHandle);
			PROCESS_INFORMATION pi = default(PROCESS_INFORMATION);
			STARTUPINFO si = default(STARTUPINFO);
			string path = Path.GetFullPath(fileName);
			string directory = Path.GetDirectoryName(path);

			try
			{
				PrivilegesHelperProcess.SetPrivilege(SECURITY_ENTITY.SE_INCREASE_QUOTA_NAME, true);
				hWnd = NativeMethodsHelperProcess.GetShellWindow();
				if (hWnd != IntPtr.Zero)
				{
					NativeMethodsHelperProcess.GetWindowThreadProcessId(hWnd, out id);
					if (id != 0)
					{
						hShell = NativeMethodsHelperProcess.OpenProcess(ProcessAccessFlags.QueryInformation, false, (int)id);
						if (hShell != default(SafeTokenHandle) && NativeMethodsHelperProcess.OpenProcessToken(hShell, TOKEN_ACCESS_OBJECT.TOKEN_DUPLICATE, out hShellToken))
						{
							TOKEN_ACCESS_OBJECT desired = (TOKEN_ACCESS_OBJECT.TOKEN_QUERY | TOKEN_ACCESS_OBJECT.TOKEN_ASSIGN_PRIMARY |
								TOKEN_ACCESS_OBJECT.TOKEN_DUPLICATE | TOKEN_ACCESS_OBJECT.TOKEN_ADJUST_DEFAULT | TOKEN_ACCESS_OBJECT.TOKEN_ADJUST_SESSIONID);

							if (NativeMethodsHelperProcess.DuplicateTokenEx(hShellToken, desired, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
								TOKEN_TYPE.TokenPrimary, out hDupedToken))
							{
								si.cb = Marshal.SizeOf(si);
								si.lpDesktop = string.Empty;
								si.dwFlags = (int)StartFlags.STARTF_USESTDHANDLES;

								if (!NativeMethodsHelperProcess.CreateProcessWithTokenW(hDupedToken, LogonFlags.None, path,
									string.Format("\"{0}\" {1}", fileName.Replace("\"", "\"\""), arguments), 0, IntPtr.Zero, directory, ref si, ref pi))
									throw new Win32Exception(Marshal.GetLastWin32Error());
								else
									newProcess = Process.GetProcessById(pi.dwProcessId);
								error = string.Empty;
							}
							else
								throw new Win32Exception(Marshal.GetLastWin32Error());
						}
						else
							throw new Win32Exception(Marshal.GetLastWin32Error());
					}
					else
						throw new Win32Exception(Marshal.GetLastWin32Error());
				}
				else
					throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			catch (Exception ex)
			{
				error = ex.Message;
			}
			finally
			{
				if (hShellToken != default(SafeTokenHandle))
					hShellToken.Close();

				if (hDupedToken != default(SafeTokenHandle))
					hDupedToken.Close();

				if (hShell != default(SafeTokenHandle))
					hShell.Close();

				if (pi.hProcess != IntPtr.Zero)
					NativeMethodsHelperProcess.CloseHandle(pi.hProcess);

				if (pi.hThread != IntPtr.Zero)
					NativeMethodsHelperProcess.CloseHandle(pi.hThread);
			}
			return newProcess;
		}
	}

	#region Classes
	public static class PrivilegesHelperProcess
	{
		/// <summary>Sets the privilege.</summary>
		/// <param name="se">The se.</param>
		/// <param name="enable">if set to <c>true</c> [enable].</param>
		/// <exception cref="InvalidEnumArgumentException">se</exception>
		/// <exception cref="InvalidOperationException">
		/// AdjustTokenPrivileges failed.
		/// </exception>
		public static void SetPrivilege(SECURITY_ENTITY se, bool enable)
		{
			if (!Enum.IsDefined(typeof(SECURITY_ENTITY), se))
				throw new InvalidEnumArgumentException("se", (int)se, typeof(SECURITY_ENTITY));

			var entity = GetSecurityEntityValue(se);
			try
			{
				var luid = new LUID();

				if (NativeMethodsHelperProcess.LookupPrivilegeValue(null, entity, out luid))
				{
					var tp = new TOKEN_PRIVILEGES() { PrivilegeCount = 1, Privileges = new LUID_AND_ATTRIBUTES[1] };
					tp.Privileges[0].Attributes = (uint)(enable ? 2 : 0);
					tp.Privileges[0].Luid = luid;

					var hToken = default(SafeTokenHandle);
					try
					{
						var process = NativeMethodsHelperProcess.GetCurrentProcess();
						var previous = default(TOKEN_PRIVILEGES);
						var result = IntPtr.Zero;
						if (NativeMethodsHelperProcess.OpenProcessToken(process, TOKEN_ACCESS_OBJECT.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_OBJECT.TOKEN_QUERY, out hToken))
						{
							if (NativeMethodsHelperProcess.AdjustTokenPrivileges(hToken, false, ref tp, (uint)Marshal.SizeOf(tp), ref previous, ref result))
							{
								var lastError = Marshal.GetLastWin32Error();
								if (lastError == NativeMethodsHelperProcess.ERROR_NOT_ALL_ASSIGNED)
								{
									var ex = new Win32Exception();
									throw new InvalidOperationException("AdjustTokenPrivileges failed.", ex);
								}
							}
							else
							{
								var ex = new Win32Exception();
								throw new InvalidOperationException("AdjustTokenPrivileges failed.", ex);
							}
						}
						else
						{
							var ex = new Win32Exception();
							var message = string.Format(CultureInfo.InvariantCulture, "OpenProcessToken failed. CurrentProcess: {0}", process.ToInt32());
							throw new InvalidOperationException(message, ex);
						}
					}
					finally
					{
						if (hToken != default(SafeTokenHandle))
							hToken.Close();
					}
				}
				else
				{
					var win32Exception = new Win32Exception();
					var exceptionMessage = string.Format(CultureInfo.InvariantCulture, "LookupPrivilegeValue failed. SecurityEntityValue: {0}", entity);
					throw new InvalidOperationException(exceptionMessage, win32Exception);
				}
			}
			catch (Exception e)
			{
				var exceptionMessage = string.Format(CultureInfo.InvariantCulture, "EnablePrivilege failed. SecurityEntity: {0}", se);
				throw new InvalidOperationException(exceptionMessage, e);
			}
		}

		/// <summary>Gets the security entity value.</summary>
		/// <param name="entity">The security entity.</param>
		/// <returns>System.String.</returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		private static string GetSecurityEntityValue(SECURITY_ENTITY entity)
		{
			switch (entity)
			{
				case SECURITY_ENTITY.SE_ASSIGNPRIMARYTOKEN_NAME:
					return "SeAssignPrimaryTokenPrivilege";
				case SECURITY_ENTITY.SE_AUDIT_NAME:
					return "SeAuditPrivilege";
				case SECURITY_ENTITY.SE_BACKUP_NAME:
					return "SeBackupPrivilege";
				case SECURITY_ENTITY.SE_CHANGE_NOTIFY_NAME:
					return "SeChangeNotifyPrivilege";
				case SECURITY_ENTITY.SE_CREATE_GLOBAL_NAME:
					return "SeCreateGlobalPrivilege";
				case SECURITY_ENTITY.SE_CREATE_PAGEFILE_NAME:
					return "SeCreatePagefilePrivilege";
				case SECURITY_ENTITY.SE_CREATE_PERMANENT_NAME:
					return "SeCreatePermanentPrivilege";
				case SECURITY_ENTITY.SE_CREATE_SYMBOLIC_LINK_NAME:
					return "SeCreateSymbolicLinkPrivilege";
				case SECURITY_ENTITY.SE_CREATE_TOKEN_NAME:
					return "SeCreateTokenPrivilege";
				case SECURITY_ENTITY.SE_DEBUG_NAME:
					return "SeDebugPrivilege";
				case SECURITY_ENTITY.SE_ENABLE_DELEGATION_NAME:
					return "SeEnableDelegationPrivilege";
				case SECURITY_ENTITY.SE_IMPERSONATE_NAME:
					return "SeImpersonatePrivilege";
				case SECURITY_ENTITY.SE_INC_BASE_PRIORITY_NAME:
					return "SeIncreaseBasePriorityPrivilege";
				case SECURITY_ENTITY.SE_INCREASE_QUOTA_NAME:
					return "SeIncreaseQuotaPrivilege";
				case SECURITY_ENTITY.SE_INC_WORKING_SET_NAME:
					return "SeIncreaseWorkingSetPrivilege";
				case SECURITY_ENTITY.SE_LOAD_DRIVER_NAME:
					return "SeLoadDriverPrivilege";
				case SECURITY_ENTITY.SE_LOCK_MEMORY_NAME:
					return "SeLockMemoryPrivilege";
				case SECURITY_ENTITY.SE_MACHINE_ACCOUNT_NAME:
					return "SeMachineAccountPrivilege";
				case SECURITY_ENTITY.SE_MANAGE_VOLUME_NAME:
					return "SeManageVolumePrivilege";
				case SECURITY_ENTITY.SE_PROF_SINGLE_PROCESS_NAME:
					return "SeProfileSingleProcessPrivilege";
				case SECURITY_ENTITY.SE_RELABEL_NAME:
					return "SeRelabelPrivilege";
				case SECURITY_ENTITY.SE_REMOTE_SHUTDOWN_NAME:
					return "SeRemoteShutdownPrivilege";
				case SECURITY_ENTITY.SE_RESTORE_NAME:
					return "SeRestorePrivilege";
				case SECURITY_ENTITY.SE_SECURITY_NAME:
					return "SeSecurityPrivilege";
				case SECURITY_ENTITY.SE_SHUTDOWN_NAME:
					return "SeShutdownPrivilege";
				case SECURITY_ENTITY.SE_SYNC_AGENT_NAME:
					return "SeSyncAgentPrivilege";
				case SECURITY_ENTITY.SE_SYSTEM_ENVIRONMENT_NAME:
					return "SeSystemEnvironmentPrivilege";
				case SECURITY_ENTITY.SE_SYSTEM_PROFILE_NAME:
					return "SeSystemProfilePrivilege";
				case SECURITY_ENTITY.SE_SYSTEMTIME_NAME:
					return "SeSystemtimePrivilege";
				case SECURITY_ENTITY.SE_TAKE_OWNERSHIP_NAME:
					return "SeTakeOwnershipPrivilege";
				case SECURITY_ENTITY.SE_TCB_NAME:
					return "SeTcbPrivilege";
				case SECURITY_ENTITY.SE_TIME_ZONE_NAME:
					return "SeTimeZonePrivilege";
				case SECURITY_ENTITY.SE_TRUSTED_CREDMAN_ACCESS_NAME:
					return "SeTrustedCredManAccessPrivilege";
				case SECURITY_ENTITY.SE_UNDOCK_NAME:
					return "SeUndockPrivilege";
				default:
					throw new ArgumentOutOfRangeException(typeof(SECURITY_ENTITY).Name);
			}
		}
	}

	public class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		private SafeTokenHandle() : base(true) { ;}

		internal SafeTokenHandle(IntPtr handle) : base(true) { base.SetHandle(handle); }

		protected override bool ReleaseHandle()
		{
			return NativeMethodsHelperProcess.CloseHandle(base.handle);
		}
	}
	#endregion

	#region Enumerations
	[Flags]
	public enum CreationFlags : uint
	{
		DefaultErrorMode = 0x04000000,
		NewConsole = 0x00000010,
		NewProcessGroup = 0x00000200,
		SeparateWOWVDM = 0x00000800,
		Suspended = 0x00000004,
		UnicodeEnvironment = 0x00000400,
		ExtendedStartupInfoPresent = 0x00080000
	}

	public enum LogonFlags
	{
		None = 0,
		/// <summary>
		/// Log on, then load the user's profile in the HKEY_USERS registry key. The function
		/// returns after the profile has been loaded. Loading the profile can be time-consuming,
		/// so it is best to use this value only if you must access the information in the 
		/// HKEY_CURRENT_USER registry key. 
		/// NOTE: Windows Server 2003: The profile is unloaded after the new process has been
		/// terminated, regardless of whether it has created child processes.
		/// </summary>
		/// <remarks>See LOGON_WITH_PROFILE</remarks>
		WithProfile = 1,
		/// <summary>
		/// Log on, but use the specified credentials on the network only. The new process uses the
		/// same token as the caller, but the system creates a new logon session within LSA, and
		/// the process uses the specified credentials as the default credentials.
		/// This value can be used to create a process that uses a different set of credentials
		/// locally than it does remotely. This is useful in inter-domain scenarios where there is
		/// no trust relationship.
		/// The system does not validate the specified credentials. Therefore, the process can start,
		/// but it may not have access to network resources.
		/// </summary>
		/// <remarks>See LOGON_NETCREDENTIALS_ONLY</remarks>
		NetCredentialsOnly
	}

	[Flags]
	public enum ProcessAccessFlags : uint
	{
		All = 0x001F0FFF,
		Terminate = 0x00000001,
		CreateThread = 0x00000002,
		VirtualMemoryOperation = 0x00000008,
		VirtualMemoryRead = 0x00000010,
		VirtualMemoryWrite = 0x00000020,
		DuplicateHandle = 0x00000040,
		CreateProcess = 0x000000080,
		SetQuota = 0x00000100,
		SetInformation = 0x00000200,
		QueryInformation = 0x00000400,
		QueryLimitedInformation = 0x00001000,
		Synchronize = 0x00100000
	}

	public enum SECURITY_ENTITY : int
	{
		SE_CREATE_TOKEN_NAME,
		SE_ASSIGNPRIMARYTOKEN_NAME,
		SE_LOCK_MEMORY_NAME,
		SE_INCREASE_QUOTA_NAME,
		SE_UNSOLICITED_INPUT_NAME,
		SE_MACHINE_ACCOUNT_NAME,
		SE_TCB_NAME,
		SE_SECURITY_NAME,
		SE_TAKE_OWNERSHIP_NAME,
		SE_LOAD_DRIVER_NAME,
		SE_SYSTEM_PROFILE_NAME,
		SE_SYSTEMTIME_NAME,
		SE_PROF_SINGLE_PROCESS_NAME,
		SE_INC_BASE_PRIORITY_NAME,
		SE_CREATE_PAGEFILE_NAME,
		SE_CREATE_PERMANENT_NAME,
		SE_BACKUP_NAME,
		SE_RESTORE_NAME,
		SE_SHUTDOWN_NAME,
		SE_DEBUG_NAME,
		SE_AUDIT_NAME,
		SE_SYSTEM_ENVIRONMENT_NAME,
		SE_CHANGE_NOTIFY_NAME,
		SE_REMOTE_SHUTDOWN_NAME,
		SE_UNDOCK_NAME,
		SE_SYNC_AGENT_NAME,
		SE_ENABLE_DELEGATION_NAME,
		SE_MANAGE_VOLUME_NAME,
		SE_IMPERSONATE_NAME,
		SE_CREATE_GLOBAL_NAME,
		SE_CREATE_SYMBOLIC_LINK_NAME,
		SE_INC_WORKING_SET_NAME,
		SE_RELABEL_NAME,
		SE_TIME_ZONE_NAME,
		SE_TRUSTED_CREDMAN_ACCESS_NAME
	}

	public enum SECURITY_IMPERSONATION_LEVEL : int
	{
		SecurityAnonymous,
		SecurityIdentification,
		SecurityImpersonation,
		SecurityDelegation
	}

	[Flags]
	public enum TOKEN_ACCESS_OBJECT : uint
	{
		TOKEN_ASSIGN_PRIMARY = 0x0001,
		TOKEN_DUPLICATE = 0x0002,
		TOKEN_IMPERSONATE = 0x0004,
		TOKEN_QUERY = 0x0008,
		TOKEN_QUERY_SOURCE = 0x0010,
		TOKEN_ADJUST_PRIVILEGES = 0x0020,
		TOKEN_ADJUST_GROUPS = 0x0040,
		TOKEN_ADJUST_DEFAULT = 0x0080,
		TOKEN_ADJUST_SESSIONID = 0x0100,
		STANDARD_RIGHTS_READ = 0x00020000,
		STANDARD_RIGHTS_REQUIRED = 0x000F0000,
		TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY),
		TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
			TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID)
	}

	public enum TOKEN_TYPE : int
	{
		TokenPrimary = 1,
		TokenImpersonation
	}

	[Flags]
	public enum StartFlags : int
	{
		STARTF_USESHOWWINDOW = 0x00000001,
		STARTF_USESIZE = 0x00000002,
		STARTF_USEPOSITION = 0x00000004,
		STARTF_USECOUNTCHARS = 0x00000008,
		STARTF_USEFILLATTRIBUTE = 0x00000010,
		STARTF_RUNFULLSCREEN = 0x00000020,  // ignored for non-x86 platforms
		STARTF_FORCEONFEEDBACK = 0x00000040,
		STARTF_FORCEOFFFEEDBACK = 0x00000080,
		STARTF_USESTDHANDLES = 0x00000100,
	}
	#endregion

	#region Structures
	[StructLayout(LayoutKind.Sequential)]
	public struct LUID
	{
		public uint LowPart;
		public int HighPart;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct LUID_AND_ATTRIBUTES
	{
		public LUID Luid;
		public uint Attributes;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct PROCESS_INFORMATION
	{
		public IntPtr hProcess;
		public IntPtr hThread;
		public int dwProcessId;
		public int dwThreadId;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct STARTUPINFO
	{
		public Int32 cb;
		public string lpReserved;
		public string lpDesktop;
		public string lpTitle;
		public Int32 dwX;
		public Int32 dwY;
		public Int32 dwXSize;
		public Int32 dwYSize;
		public Int32 dwXCountChars;
		public Int32 dwYCountChars;
		public Int32 dwFillAttribute;
		public Int32 dwFlags;
		public Int16 wShowWindow;
		public Int16 cbReserved2;
		public IntPtr lpReserved2;
		public IntPtr hStdInput;
		public IntPtr hStdOutput;
		public IntPtr hStdError;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct TOKEN_PRIVILEGES
	{
		public uint PrivilegeCount;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = UnmanagedType.Struct)]
		public LUID_AND_ATTRIBUTES[] Privileges;
	}
	#endregion

	#region Native Methods
	public static class NativeMethodsHelperProcess
	{
		public const int ERROR_NOT_ALL_ASSIGNED = 1300;

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AdjustTokenPrivileges(SafeTokenHandle TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
			[MarshalAs(UnmanagedType.Struct)]ref TOKEN_PRIVILEGES Newstate, uint BufferLength,
			[MarshalAs(UnmanagedType.Struct)]ref TOKEN_PRIVILEGES PreviousState, ref IntPtr ReturnLength);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr hObject);

		[DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool CreateProcessWithTokenW(SafeTokenHandle hToken, LogonFlags dwLogonFlags, string lpApplicationName, string lpCommandLine,
			CreationFlags dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo,
			ref PROCESS_INFORMATION lpProcessInformation);

		[DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DuplicateTokenEx(SafeTokenHandle hExistingToken, TOKEN_ACCESS_OBJECT desiredAccess, IntPtr pTokenAttributes,
			SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType, out SafeTokenHandle hNewToken);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetCurrentProcess();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetShellWindow();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern SafeTokenHandle OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

		[DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool OpenProcessToken(IntPtr hProcess, TOKEN_ACCESS_OBJECT desiredAccess, out SafeTokenHandle hToken);

		[DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool OpenProcessToken(SafeTokenHandle hProcess, TOKEN_ACCESS_OBJECT desiredAccess, out SafeTokenHandle hToken);
	}
	#endregion
}
