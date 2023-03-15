using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace Kbg.NppPluginNET
{
    class Main
    {
        internal const string PluginName = "Npp Add Date";
        static string iniFilePath = null;

        static bool DoInsertDate = true;

        public static void OnNotification(ScNotification notification)
        {  
            if (notification.Header.Code == (uint)SciMsg.SCI_ADDTEXT)
            {
                if (notification.Mmodifiers == 126) // "~" - toggle adding the date 
                {
                    DoInsertDate = !DoInsertDate;
                } 
                if (notification.Mmodifiers == 10) // LF
                {
                    if (DoInsertDate)
                    {
                        var npp = new NotepadPPGateway();
                        var path = npp.GetCurrentFilePath();
                        if (path.EndsWith(".wlog"))
                        {
                            var scih = PluginBase.GetCurrentScintilla();
                            ScintillaGateway sci = new ScintillaGateway(scih);
                            var now = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + " ";
                            sci.AddText(now.Length, now);
                        }
                    }
                }
            }
            // This method is invoked whenever something is happening in notepad++
            // use eg. as
            // if (notification.Header.Code == (uint)NppMsg.NPPN_xxx)
            // { ... }
            // or
            //
            // if (notification.Header.Code == (uint)SciMsg.SCNxxx)
            // { ... }
        }

        internal static void CommandMenuInit()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(iniFilePath)) Directory.CreateDirectory(iniFilePath);
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");
            //someSetting = (Win32.GetPrivateProfileInt("SomeSection", "SomeKey", 0, iniFilePath) != 0);

            PluginBase.SetCommand(0, "Info", ShowInfo, new ShortcutKey(false, false, false, Keys.None));
        }

        internal static void PluginCleanUp()
        {
            //Win32.WritePrivateProfileString("SomeSection", "SomeKey", someSetting ? "1" : "0", iniFilePath);
        }

        internal static void ShowInfo()
        {
            MessageBox.Show(@"On each newline (LF) in files with extension .wlog adds a date in format yyyy.MM.dd HH:mm:ss at the line start
Tilda ('~') toggles adding date.
On startup 'add date' is enabled
");
        }
    }
}