
using System;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Security.AccessControl;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;


namespace SimplesNet {

	public static class FileHelper
	{
		public static bool GrantAccess(string puth, bool? IsEveryone = true, bool isFullControl = false)
		{
			try
			{
				if (File.Exists(puth))
				{
					FileInfo fInfo = new FileInfo(puth);
					FileSecurity fSecurity = fInfo.GetAccessControl();
					var lRules = fSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
					bool isAddThis = true;
					bool isAddAll = true;
					var worldSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
					foreach (AuthorizationRule rule in lRules)
					{
						if ((!IsEveryone.HasValue || !IsEveryone.Value) && rule.IdentityReference == WindowsIdentity.GetCurrent().User)
						{
							isAddThis = false;
						}
						else if ((!IsEveryone.HasValue || IsEveryone.Value) && rule.IdentityReference == worldSid)
						{
							isAddAll = false;
						}
					}
					if (isAddThis || isAddAll)
					{

						if ((!IsEveryone.HasValue || !IsEveryone.Value) && isAddThis)
						{
							fSecurity.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, (isFullControl ? FileSystemRights.FullControl : FileSystemRights.Modify), InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));
							fInfo.SetAccessControl(fSecurity);
						}
						if ((!IsEveryone.HasValue || IsEveryone.Value) && isAddAll)
							fSecurity.AddAccessRule(new FileSystemAccessRule(worldSid, (isFullControl ? FileSystemRights.FullControl : FileSystemRights.Modify), InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));
						fInfo.SetAccessControl(fSecurity);
					}
				}
				else if (Directory.Exists(puth))
				{
					DirectoryInfo dInfo = new DirectoryInfo(puth);
					DirectorySecurity dSecurity = dInfo.GetAccessControl();
					var lRules = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
					bool isAddThis = true;
					bool isAddAll = true;
					var worldSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
					foreach (AuthorizationRule rule in lRules)
					{
						if ((!IsEveryone.HasValue || !IsEveryone.Value) && rule.IdentityReference == WindowsIdentity.GetCurrent().User)
						{
							isAddThis = false;
						}
						else if ((!IsEveryone.HasValue || IsEveryone.Value) && rule.IdentityReference == worldSid)
						{
							isAddAll = false;
						}
					}
					if (isAddThis || isAddAll)
					{
						if ((!IsEveryone.HasValue || !IsEveryone.Value) && isAddThis)
						{
							dSecurity.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, (isFullControl ? FileSystemRights.FullControl : FileSystemRights.Modify | FileSystemRights.DeleteSubdirectoriesAndFiles), InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
							dInfo.SetAccessControl(dSecurity);
						}

						if ((!IsEveryone.HasValue || IsEveryone.Value) && isAddAll)
							dSecurity.AddAccessRule(new FileSystemAccessRule(worldSid, (isFullControl ? FileSystemRights.FullControl : FileSystemRights.Modify | FileSystemRights.DeleteSubdirectoriesAndFiles), InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
						dInfo.SetAccessControl(dSecurity);
					}
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("GrantAccess {0}", ex.Message);
			}
			return true;
		}
	}

	public static class Privileges
    {
        public static void EnableSeRestorePrivilege()
        {
            IntPtr tokenHandle = IntPtr.Zero;
            try
            {
                var locallyUniqueIdentifier = new NativeMethods.LUID();
                if (NativeMethods.LookupPrivilegeValue(null, "SeRestorePrivilege", ref locallyUniqueIdentifier))
                {
                    var TOKEN_PRIVILEGES = new NativeMethods.TOKEN_PRIVILEGES
                    {
                        PrivilegeCount = 1,
                        Attributes = NativeMethods.SE_PRIVILEGE_ENABLED,
                        Luid = locallyUniqueIdentifier
                    };
                    var currentProcess = Process.GetCurrentProcess().Handle;
                    if (NativeMethods.OpenProcessToken(currentProcess, NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_QUERY, out tokenHandle)
                        && NativeMethods.AdjustTokenPrivileges(tokenHandle, false,
                                                               ref TOKEN_PRIVILEGES,
                                                               1024, IntPtr.Zero, IntPtr.Zero))
                    {
                        var lastError = Marshal.GetLastWin32Error();
                        if (lastError != NativeMethods.ERROR_NOT_ALL_ASSIGNED)
                        {
                            return;
                        }
                    }
                }
                throw new InvalidOperationException();
            }
            finally
            {
                if (tokenHandle != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(tokenHandle);
                }
            }
        }
 
        private static class NativeMethods
        {
            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool LookupPrivilegeValue(string lpsystemname, string lpname, [MarshalAs(UnmanagedType.Struct)] ref LUID lpLuid);
 
            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AdjustTokenPrivileges(IntPtr tokenhandle,
                                     [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
                                     [MarshalAs(UnmanagedType.Struct)]ref TOKEN_PRIVILEGES newstate,
                                     uint bufferlength, IntPtr previousState, IntPtr returnlength);
 
            internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
            internal const int ERROR_NOT_ALL_ASSIGNED = 1300;
            internal const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
            internal const UInt32 TOKEN_QUERY = 0x0008;
 
            [DllImport("Advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool OpenProcessToken(IntPtr processHandle,
                                uint desiredAccesss,
                                out IntPtr tokenHandle);
 
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern Boolean CloseHandle(IntPtr hObject);
 
            [StructLayout(LayoutKind.Sequential)]
            internal struct LUID
            {
                internal Int32 LowPart;
                internal UInt32 HighPart;
            }
 
            [StructLayout(LayoutKind.Sequential)]
            internal struct TOKEN_PRIVILEGES
            {
                internal Int32 PrivilegeCount;
                internal LUID Luid;
                internal Int32 Attributes;
            }
        }
    }
}

