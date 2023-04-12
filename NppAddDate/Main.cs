using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace Kbg.NppPluginNET
{
    class Main
    {
        internal const string PluginName = "Npp Add Date";
        static string iniFilePath = null;
        static string stateFilePath => $"{iniFilePath}.state";

        static bool DoInsertDate = true;
        static bool IsDelayActive = false;
        static string DatetimeFmt = "yyyy.MM.dd HH:mm:ss";
        static string DateTimeDelimiterFmt = "--- yyyy.MM.dd ---";
        static bool AddDateTimeDelimiter = true;
        static string ApplicableExtension = ".wlog";
        static char ToggleAddDateChar = '~';
        static char AddDateKey = '\n'; // LF
        static bool Enabled = true;
        static bool DelayAddDateOnInactivity = true;
        static int InactivityDelayInMs = 8 * 60 * 1000; // 8 min default
        //static DateTime prevDate;
        static Dictionary<string, DateTime> prevDates = new Dictionary<string, DateTime>();
        public static void OnNotification(ScNotification notification)
        {
            if (!Enabled) return;
            var npp = new NotepadPPGateway();
            var path = npp.GetCurrentFilePath();
            if (path.EndsWith(ApplicableExtension))
            {
                if (!prevDates.ContainsKey(path)) prevDates[path] = DateTime.Now;
                if (notification.Header.Code == (uint)SciMsg.SCI_ADDTEXT)
                {
                    CheckInactivity();
                    if (notification.Mmodifiers == ToggleAddDateChar) // toggle adding the date 
                    {
                        DoInsertDate = !DoInsertDate;
                    }
                    if (notification.Mmodifiers == AddDateKey)
                    {
                        if (DoInsertDate)
                        {
                            var scih = PluginBase.GetCurrentScintilla();
                            ScintillaGateway sci = new ScintillaGateway(scih);
                            var nowD = DateTime.Now;
                            string now;
                            try
                            {
                                now = nowD.ToString(DatetimeFmt) + " ";
                            } catch
                            {
                                now = nowD.ToString() + " ";
                            }
                            if (AddDateTimeDelimiter && nowD.ToString("yyyy.MM.dd") != prevDates[path].ToString("yyyy.MM.dd")) // another day
                            {
                                var txt = $"{Environment.NewLine}{nowD.ToString(DateTimeDelimiterFmt)}{Environment.NewLine}";
                                sci.AddText(txt.Length, txt);
                            }
                            lock (idLock)
                            {
                                if (!IsDelayActive || !DelayAddDateOnInactivity) // add only if delay is not active
                                {
                                    sci.AddText(now.Length, now);
                                    if (DelayAddDateOnInactivity) // set delay
                                    {
                                        TurnOnInactivityDelay();
                                        CheckInactivity();
                                    }
                                }
                            }
                            prevDates[path] = nowD;
                            Task.Run(WriteState); // background

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
        private static System.Timers.Timer InactivityTimer = null;
        private static void CheckInactivity()
        {
            lock (idLock)
            {
                if (DelayAddDateOnInactivity && IsDelayActive)
                {
                    InactivityTimer?.Stop(); // stop any existing timer
                    InactivityTimer = new System.Timers.Timer(InactivityDelayInMs);
                    InactivityTimer.Elapsed += OnElapsed;
                    InactivityTimer.AutoReset = false;
                    InactivityTimer.Start();
                }
            }
        }
        private static object idLock = new object();
        private static void OnElapsed(object source, ElapsedEventArgs e)
        {
            lock (idLock)
            {
                IsDelayActive = false;
            }
        }
        private static void TurnOnInactivityDelay()
        {
            lock (idLock)
            {
                IsDelayActive = true;
            }
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
            PluginBase.SetCommand(0, "Edit Config", EditConfig, new ShortcutKey(false, false, false, Keys.None));
        }

        private static void EditConfig()
        {
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DOOPEN, 0, iniFilePath);
        }

        internal static void PluginCleanUp()
        {
            //Win32.WritePrivateProfileString("SomeSection", "SomeKey", someSetting ? "1" : "0", iniFilePath);
        }

        internal static void ShowInfoAndReloadConfig()
        {
            var config = ReadConfig();
            MessageBox.Show($@"On each newline ({(AddDateKey == '\r' ? "CR" : "LF")}) in files with extension {ApplicableExtension} adds a date in format {DatetimeFmt} at the line start
Char {ToggleAddDateChar} ('{ToggleAddDateChar}') toggles adding date.
On startup 'add date' is enabled
Config file: '{Path.GetFullPath(iniFilePath)}'
Config:
{string.Join(Environment.NewLine, config)}
");
        }

        static string[] ReadConfig()
        {
            try
            {
                ReadState();
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
                var addDatetimeDelimeter = ConfigValue(iniContent, "AddDatetimeDelimiter");
                if (!string.IsNullOrWhiteSpace(addDatetimeDelimeter))
                {
                    var trimmedAndLower = addDatetimeDelimeter.Trim().ToLower();
                    AddDateTimeDelimiter = "true" == trimmedAndLower || "1" == trimmedAndLower;
                }
                fmt = ConfigValue(iniContent, "DateTimeDelimiterFmt");
                if (!string.IsNullOrWhiteSpace(fmt))
                {
                    try
                    {
                        var formatted = DateTime.Now.ToString(fmt); // if it is successful
                        DateTimeDelimiterFmt = fmt;
                    }
                    catch
                    {
                        // nothing
                    }
                }
                var delayAddDateOnInactivity = ConfigValue(iniContent, "DelayAddDateOnInactivity");
                if (!string.IsNullOrWhiteSpace(delayAddDateOnInactivity))
                {
                    var trimmedAndLower = delayAddDateOnInactivity.Trim().ToLower();
                    DelayAddDateOnInactivity = "true" == trimmedAndLower || "1" == trimmedAndLower;
                }
                var inactivityDelayInMs = ConfigValue(iniContent, "InactivityDelayInMs");
                int delayInMs = -1;
                if (int.TryParse(inactivityDelayInMs, out delayInMs))
                {
                    if (delayInMs > 0)
                    {
                        InactivityDelayInMs = delayInMs;
                    }
                }
                return iniContent;
            }
            catch
            {
                // nothing
            }
            return new string[0];
        }

        private const char delim = '*';
        private static void ReadState()
        {
            try
            {
                Dictionary<string, DateTime> pd = new Dictionary<string, DateTime>();
                var lines = File.ReadAllLines(stateFilePath);
                foreach (var l in lines)
                {
                    var split = l.Split(delim);
                    if (split.Length > 1)
                    {
                        pd[split[0]] = DateTime.Parse(split[1]);
                    }
                }
                if (pd.Any())
                {
                    prevDates = pd;
                }
            }
            catch // no luck
            {
            }
        }
        private static void WriteState()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (var kv in prevDates)
                {
                    lines.Add($"{kv.Key}{delim}{kv.Value}");
                }
                File.WriteAllLines(stateFilePath, lines);
            } catch
            {
                // nothing
            }
        }

        static string ConfigValue(string[] lines, string key)
        {
            var vLine = lines.Where(l => l.Trim().ToLower().StartsWith($"{key.ToLower()}=")).FirstOrDefault();
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