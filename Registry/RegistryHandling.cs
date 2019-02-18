using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Utilities.Registry
{
    /// <summary>
    /// A whole class copied from http://www.codeproject.com/Articles/3389/Read-write-and-delete-from-registry-with-C which operates with registry
    /// </summary>

    class RegistryHandling
    {
        // TODO: Not working Registry.CurrentUser missing
        RegistryKey baseRegistryKey = null;
        string subKey = "SOFTWARE\\" + Application.ProductName;
        bool showError = false;

        public bool ShowError
        {
            get { return showError; }
            set { showError = value; }
        }

        public string SubKey
        {
            get { return subKey; }
            set { subKey = value; }
        }
        public RegistryKey BaseRegistryKey
        {
            get { return baseRegistryKey; }
            set { baseRegistryKey = value; }
        }

        public string Read(string KeyName)
        {
            // Opening the registry key
            RegistryKey rk = baseRegistryKey;
            // Open a subKey as read-only
            RegistryKey sk1 = rk.OpenSubKey(subKey);
            // If the RegistrySubKey doesn't exist -> (null)
            if (sk1 == null)
            {
                return null;
            }
            else
            {
                try
                {
                    // If the RegistryKey exists I get its value
                    // or null is returned.
                    return (string)sk1.GetValue(KeyName.ToUpper());
                }
                catch (Exception e)
                {
                    // AAAAAAAAAAARGH, an error!
                    ShowErrorMessage(e, "Reading registry " + KeyName.ToUpper());
                    return null;
                }
            }
        }

        public bool Write(string KeyName, object Value)
        {
            try
            {
                // Setting
                RegistryKey rk = baseRegistryKey;
                // I have to use CreateSubKey 
                // (create or open it if already exits), 
                // 'cause OpenSubKey open a subKey as read-only
                RegistryKey sk1 = rk.CreateSubKey(subKey);
                // Save the value
                sk1.SetValue(KeyName.ToUpper(), Value);

                return true;
            }
            catch (Exception e)
            {
                // AAAAAAAAAAARGH, an error!
                ShowErrorMessage(e, "Writing registry " + KeyName.ToUpper());
                return false;
            }
        }

        public int ReadIntegrer(string KeyName)
        {

            // Opening the registry key
            Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser;
            // Open a subKey as read-only
            Microsoft.Win32.RegistryKey sk1 = rk.OpenSubKey(@"Software\Cidra\Wam");
            // If the RegistrySubKey doesn't exist -> (null)
            if (sk1 == null)
            {
                throw new NullReferenceException(subKey + " key doesn't exist");
            }
            else
            {
                try
                {
                    // If the RegistryKey exists I get its value
                    // or null is returned.
                    return int.Parse(sk1.GetValue(KeyName.ToUpper()).ToString());
                }
                catch (Exception ex)
                {
                    // AAAAAAAAAAARGH, an error!
                    throw new NullReferenceException(ex.Message + "Reading registry ", ex);
                }
            }
        }

        public bool WriteIntegrer(string KeyName, object value)
        {
            try
            {
                // Setting
                RegistryKey rk = Microsoft.Win32.Registry.CurrentUser;
                // I have to use CreateSubKey 
                // (create or open it if already exits), 
                // 'cause OpenSubKey open a subKey as read-only
                RegistryKey sk1 = rk.CreateSubKey(subKey);
                // Save the value
                sk1.SetValue(KeyName.ToUpper(), value);

                return true;
            }
            catch (Exception ex)
            {
                // AAAAAAAAAAARGH, an error!
                throw new Exception(ex.Message, ex);
            }
        }

        public bool DeleteKey(string KeyName)
        {
            try
            {
                // Setting
                RegistryKey rk = baseRegistryKey;
                RegistryKey sk1 = rk.CreateSubKey(subKey);
                // If the RegistrySubKey doesn't exists -> (true)
                if (sk1 == null)
                    return true;
                else
                    sk1.DeleteValue(KeyName);

                return true;
            }
            catch (Exception e)
            {
                // AAAAAAAAAAARGH, an error!
                ShowErrorMessage(e, "Deleting SubKey " + subKey);
                return false;
            }
        }

        public bool DeleteSubKeyTree()
        {
            try
            {
                // Setting
                RegistryKey rk = baseRegistryKey;
                RegistryKey sk1 = rk.OpenSubKey(subKey);
                // If the RegistryKey exists, I delete it
                if (sk1 != null)
                    rk.DeleteSubKeyTree(subKey);

                return true;
            }
            catch (Exception e)
            {
                // AAAAAAAAAAARGH, an error!
                ShowErrorMessage(e, "Deleting SubKey " + subKey);
                return false;
            }
        }

        public int SubKeyCount()
        {
            try
            {
                // Setting
                RegistryKey rk = baseRegistryKey;
                RegistryKey sk1 = rk.OpenSubKey(subKey);
                // If the RegistryKey exists...
                if (sk1 != null)
                    return sk1.SubKeyCount;
                else
                    return 0;
            }
            catch (Exception e)
            {
                // AAAAAAAAAAARGH, an error!
                ShowErrorMessage(e, "Retriving subkeys of " + subKey);
                return 0;
            }
        }

        public int ValueCount()
        {
            try
            {
                // Setting
                RegistryKey rk = baseRegistryKey;
                RegistryKey sk1 = rk.OpenSubKey(subKey);
                // If the RegistryKey exists...
                if (sk1 != null)
                    return sk1.ValueCount;
                else
                    return 0;
            }
            catch (Exception e)
            {
                // AAAAAAAAAAARGH, an error!
                ShowErrorMessage(e, "Retriving keys of " + subKey);
                return 0;
            }
        }

        private void ShowErrorMessage(Exception e, string Title)
        {
            if (showError == true)
                MessageBox.Show(e.Message,
                        Title
                        , MessageBoxButtons.OK
                        , MessageBoxIcon.Error);
        }

        public string[] AllValues()
        {
            // Opening the registry key
            RegistryKey rk = baseRegistryKey;
            // Open a subKey as read-only
            RegistryKey sk1 = rk.OpenSubKey(subKey);
            // If the RegistrySubKey doesn't exist -> (null)
            if (sk1 == null)
            {
                return null;
            }
            else
                return sk1.GetValueNames();
        }
    }
}
