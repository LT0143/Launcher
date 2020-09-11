using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace Launcher
{
    public class IniManager
    {
        public static String GetINIValue(String strINI, String strSection, String strKey)
        {
            StringBuilder strValue = new StringBuilder(2048);
            int i = GetPrivateProfileString(strSection, strKey, "NOT_FOUND", strValue, 2048, strINI);
            return strValue.ToString();
        }

        public static void SetINIValue(String strINI, String strSection, String strKey, String strValue)
        {
            WritePrivateProfileString(strSection, strKey, strValue, strINI);
        }



        #region INI Read, Write DLL

        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(String section, String key, String def, StringBuilder retVal, int size, string filePath);
        [DllImport("kernel32.dll")]
        private static extern long WritePrivateProfileString(String section, String key, String val, string filePath);

        #endregion
    }
}
