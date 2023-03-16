using System;
using System.IO;
using System.Linq;
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
        static string DatetimeFmt = "yyyy.MM.dd HH:mm:ss";
        static string ApplicableExtension = ".wlog";
        static char ToggleAddDateChar = '~';
        static char AddDateKey = '\n'; // LF
        static bool Enabled = true;
        public static void OnNotification(ScNotification notification)
        {
            if (!Enabled) return;
            if (notification.Header.Code == (uint)SciMsg.SCI_ADDTEXT)
            {
                if (notification.Mmodifiers == ToggleAddDateChar) // toggle adding the date 
                {
                    DoInsertDate = !DoInsertDate;
                } 
                if (notification.Mmodifiers == AddDateKey) 
                {
                    if (DoInsertDate)
                    {
                        var npp = new NotepadPPGateway();
                        var path = npp.GetCurrentFilePath();
                        if (path.EndsWith(ApplicableExtension))
                        {
                            var scih = PluginBase.GetCurrentScintilla();
                            ScintillaGateway sci = new ScintillaGateway(scih);
                            string now;
                            try
                            {
                                now = DateTime.Now.ToString(DatetimeFmt) + " ";
                            } catch
                            {
                                now = DateTime.Now.ToString() + " ";
                            }
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
            ReadConfig();

            PluginBase.SetCommand(0, "Info and Reload Config", ShowInfoAndReloadConfig, new ShortcutKey(false, false, false, Keys.None));
        }

        internal static void PluginCleanUp()
        {
            //Win32.WritePrivateProfileString("SomeSection", "SomeKey", someSetting ? "1" : "0", iniFilePath);
        }

        internal static void ShowInfoAndReloadConfig()
        {
            ReadConfig();
            MessageBox.Show($@"On each newline ({(AddDateKey == '\r' ? "CR" : "LF")}) in files with extension {ApplicableExtension} adds a date in format {DatetimeFmt} at the line start
Char {ToggleAddDateChar} ('{ToggleAddDateChar}') toggles adding date.
On startup 'add date' is enabled
Config file: '{Path.GetFullPath(iniFilePath)}'
Is Date currently being added? {DoInsertDate}
Is currently enabled? {Enabled}
");
        }

        static void ReadConfig()
        {
            try
            {
                var iniContent = File.ReadAllLines(iniFilePath);
                var ext = ConfigValue(iniContent, "ApplicableExtension");
                if (!string.IsNullOrWhiteSpace(ext))
                {
                    ApplicableExtension = ext.Trim();
                }
                var fmt = ConfigValue(iniContent, "DateTimeFmt");
                if (!string.IsNullOrWhiteSpace(fmt))
                {
                    try
                    {
                        var formatted = DateTime.Now.ToString(fmt); // if it is successful
                        DatetimeFmt = fmt;
                    }
                    catch
                    {
                        // nothing
                    }
                }
                var toggleC = ConfigValue(iniContent, "ToggleAddDateChar");
                if (!string.IsNullOrWhiteSpace(toggleC))
                {
                    var chars = toggleC.Trim().ToCharArray();
                    ToggleAddDateChar = chars[0];
                }
                var enabledS = ConfigValue(iniContent, "Enabled");
                if (!string.IsNullOrWhiteSpace(enabledS))
                {
                    var trimmedAndLower = enabledS.Trim().ToLower();
                    Enabled = "true" == trimmedAndLower || "1" == trimmedAndLower;
                }
                var AddDateKeyS = ConfigValue(iniContent, "AddDateKey");
                if (!string.IsNullOrWhiteSpace(AddDateKeyS))
                {
                    AddDateKeyS = AddDateKeyS.Trim().Replace("\\r", "\r").Replace("\\n", "\n");
                    var chars = AddDateKeyS.ToCharArray().Where(c => c == '\r' || c == '\n');
                    if (chars.Any())
                    {
                        AddDateKey = chars.First();
                    }
                }
            } catch
            {
                // nothing
            }
        }
        static string ConfigValue(string[] lines, string key)
        {
            var vLine = lines.Where(l => l.Trim().StartsWith($"{key}=")).FirstOrDefault();
            if (vLine != null)
            {
                var val = vLine.Trim().Substring($"{key}=".Length).TrimStart();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    return val;
                }
            }
            return null;
        }

    }
}