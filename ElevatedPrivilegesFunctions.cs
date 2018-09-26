using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    class ElevatedPrivilegesFunctions
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public void BringToFrontByTime(string processName, DateTime startTime)
        {
            Process[] procs = Process.GetProcessesByName(processName);
            for (int i = 0; i < procs.Length; i++)
            {
                if (DateTime.Compare(procs[i].StartTime, startTime) == 0)
                    if (procs[i].MainWindowHandle != IntPtr.Zero)
                        SetForegroundWindow(procs[i].MainWindowHandle);
            }
        }

        public static bool IsUserAdministrator()
        {
            //bool value to hold our return value
            bool isAdmin;
            try
            {
                //get the currently logged in user
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException)
            {
                isAdmin = false;
            }
            catch (Exception)
            {
                isAdmin = false;
            }
            return isAdmin;
        }
    }
}
