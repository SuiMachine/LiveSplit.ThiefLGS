using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiveSplit.ComponentUtil;

namespace LiveSplit.Thief1
{
    class GameMemory
    {

        public enum SplitArea : int
        {
            None,
            l01,
            l02,
            l03,
            l04,
            l05,
            l05b,
            l06,
            l07,
            l07b,
            l08,
            l08b,
            l09,
            l10,
            l11,
            l12,
            l13,
        }

        public event EventHandler OnPlayerGainedControl;
        public event EventHandler OnLoadStarted;
        public event EventHandler OnLoadFinished;
        public delegate void SplitCompletedEventHandler(object sender, SplitArea type, uint frame);
        public event SplitCompletedEventHandler OnSplitCompleted;


        private Task _thread;
        private CancellationTokenSource _cancelSource;
        private SynchronizationContext _uiThread;
        private List<int> _ignorePIDs;
        private Thief1Settings _settings;

        private DeepPointer _isLoadingPtr = null;
        private DeepPointer _levelName = null;

        private SuisCodeInjection.CodeInjection injection;

        public bool[] SplitStates { get; set; }

        public void ResetSplitStates()
        {
            for(int i = 0; i <= (int)SplitArea.l13; i++)
            {
                SplitStates[i] = false;
            }
        }

        public static class MapNames
        {
            public static string mission01_A_Keepers_Training = "MISS1";
            public static string mission02_Lord_Baffords_Manor = "MISS2";
            public static string mission03_Break_From_Cragscleft_Prison = "MISS3";
            public static string mission04_Down_In_The_Bonehoard = "MISS4";
            public static string mission05_Assassins = "MISS5";
            public static string mission05Gold_ThievesGuild = "MISS15";
            public static string mission06_TheSword = "MISS6";
            public static string mission07_The_Haunted_Cathedral = "MISS7";
            public static string mission07Gold_MagesTowers = "MISS16";
            public static string mission08_TheLostCity = "MISS9";
            public static string mission08Gold_Song_Of_The_Caverns = "MISS17";
            public static string mission09_Undercover = "MISS10";
            public static string mission10_Return_To_The_Cathedral = "MISS11";
            public static string mission11_Escape = "MISS12";
            public static string mission12_Strange_Bedfellows = "MISS13";
            public static string mission13_Into_the_Maw_of_Chaos = "MISS14";
            //public static string mission99_Bloopers = "MISS18";
        }

        public GameMemory(Thief1Settings componentSettings)
        {
            _settings = componentSettings;
            SplitStates = new bool[(int)SplitArea.l13 + 1];
            ResetSplitStates();

            _ignorePIDs = new List<int>();
        }

        public void StartMonitoring()
        {
            if (_thread != null && _thread.Status == TaskStatus.Running)
            {
                throw new InvalidOperationException();
            }
            if (!(SynchronizationContext.Current is WindowsFormsSynchronizationContext))
            {
                throw new InvalidOperationException("SynchronizationContext.Current is not a UI thread.");
            }

            _uiThread = SynchronizationContext.Current;
            _cancelSource = new CancellationTokenSource();
            _thread = Task.Factory.StartNew(MemoryReadThread);
        }

        public void Stop()
        {
            if (_cancelSource == null || _thread == null || _thread.Status != TaskStatus.Running)
            {
                return;
            }

            _cancelSource.Cancel();
            _thread.Wait();
        }

        bool isLoading = false;
        bool prevIsLoading = false;
        bool loadingStarted = false;
        string CurrentMap = "";
        string prevMap = "";

        void MemoryReadThread()
        {
            Debug.WriteLine("[NoLoads] MemoryReadThread");

            while (!_cancelSource.IsCancellationRequested)
            {
                try
                {
                    Debug.WriteLine("[NoLoads] Waiting for thief.exe...");

                    Process game;
                    while ((game = GetGameProcess()) == null)
                    {
                        if (_cancelSource.IsCancellationRequested)
                        {
                            return;
                        }
                    }

                    Debug.WriteLine("[NoLoads] Got games process!");

                    uint frameCounter = 0;

                    while (!game.HasExited)
                    {
                        _isLoadingPtr.Deref(game, out isLoading);
                        string tempMap = _levelName.DerefString(game, 6, "");

                        //Since it changes to String.Empty during loads
                        if(tempMap != "")
                        {
                            CurrentMap = tempMap.ToUpper();
                            if(CurrentMap.StartsWith("MISS0"))
                                CurrentMap = CurrentMap.Remove(4, 1); //Because of course these map names had to differ
                            else if(CurrentMap.EndsWith("."))
                                CurrentMap = CurrentMap.Remove(5, 1);
                        }



                        if(isLoading != prevIsLoading)
                        {
                            if(isLoading)
                            {
                                Debug.WriteLine(String.Format("[NoLoads] Load Start - {0}", frameCounter));

                                loadingStarted = true;

                                // pause game timer
                                _uiThread.Post(d =>
                                {
                                    if(this.OnLoadStarted != null)
                                    {
                                        this.OnLoadStarted(this, EventArgs.Empty);
                                    }
                                }, null);
                            }
                            else
                            {
                                Debug.WriteLine(String.Format("[NoLoads] Load End - {0}", frameCounter));

                                if(loadingStarted)
                                {
                                    loadingStarted = false;

                                    // unpause game timer
                                    _uiThread.Post(d =>
                                    {
                                        if(this.OnLoadFinished != null)
                                        {
                                            this.OnLoadFinished(this, EventArgs.Empty);
                                        }
                                    }, null);
                                }

                                if(CurrentMap == MapNames.mission01_A_Keepers_Training || CurrentMap == MapNames.mission02_Lord_Baffords_Manor)
                                {
                                    // StartTimer
                                    _uiThread.Post(d =>
                                    {
                                        if(this.OnPlayerGainedControl != null)
                                        {
                                            this.OnPlayerGainedControl(this, EventArgs.Empty);
                                        }
                                    }, null);
                                }
                            }
                        }

                        if(CurrentMap != prevMap && CurrentMap != "")
                        {
                            Debug.WriteLine("[NOLOADS] Map changed from \"" + prevMap + "\" to \"" + CurrentMap + "\"");

                            if(prevMap == MapNames.mission01_A_Keepers_Training && CurrentMap == MapNames.mission02_Lord_Baffords_Manor)
                            {
                                Split(SplitArea.l01, frameCounter);
                            }
                            else if(prevMap == MapNames.mission02_Lord_Baffords_Manor && CurrentMap == MapNames.mission03_Break_From_Cragscleft_Prison)
                            {
                                Split(SplitArea.l02, frameCounter);
                            }
                            else if(prevMap == MapNames.mission03_Break_From_Cragscleft_Prison && CurrentMap == MapNames.mission04_Down_In_The_Bonehoard)
                            {
                                Split(SplitArea.l03, frameCounter);
                            }
                            else if(prevMap == MapNames.mission04_Down_In_The_Bonehoard && CurrentMap == MapNames.mission05_Assassins)
                            {
                                Split(SplitArea.l04, frameCounter);
                            }
                            else if(prevMap == MapNames.mission05_Assassins && (CurrentMap == MapNames.mission05Gold_ThievesGuild || CurrentMap == MapNames.mission06_TheSword))
                            {
                                Split(SplitArea.l05, frameCounter);
                            }
                            else if(prevMap == MapNames.mission05Gold_ThievesGuild && CurrentMap == MapNames.mission06_TheSword)
                            {
                                Split(SplitArea.l05b, frameCounter);
                            }
                            else if(prevMap == MapNames.mission06_TheSword && CurrentMap == MapNames.mission07_The_Haunted_Cathedral)
                            {
                                Split(SplitArea.l06, frameCounter);
                            }
                            else if(prevMap == MapNames.mission07_The_Haunted_Cathedral && (CurrentMap == MapNames.mission08_TheLostCity || CurrentMap == MapNames.mission07Gold_MagesTowers))
                            {
                                Split(SplitArea.l07, frameCounter);
                            }
                            else if(prevMap == MapNames.mission07Gold_MagesTowers && CurrentMap == MapNames.mission08_TheLostCity)
                            {
                                Split(SplitArea.l07b, frameCounter);
                            }
                            else if(prevMap == MapNames.mission08_TheLostCity && (CurrentMap == MapNames.mission09_Undercover || CurrentMap == MapNames.mission08Gold_Song_Of_The_Caverns))
                            {
                                Split(SplitArea.l08, frameCounter);
                            }
                            else if(prevMap == MapNames.mission08Gold_Song_Of_The_Caverns && CurrentMap == MapNames.mission09_Undercover)
                            {
                                Split(SplitArea.l08b, frameCounter);
                            }
                            else if(prevMap == MapNames.mission09_Undercover && CurrentMap == MapNames.mission10_Return_To_The_Cathedral)
                            {
                                Split(SplitArea.l09, frameCounter);
                            }
                            else if(prevMap == MapNames.mission10_Return_To_The_Cathedral && CurrentMap == MapNames.mission11_Escape)
                            {
                                Split(SplitArea.l10, frameCounter);
                            }
                            else if(prevMap == MapNames.mission11_Escape && CurrentMap == MapNames.mission12_Strange_Bedfellows)
                            {
                                Split(SplitArea.l11, frameCounter);
                            }
                            else if(prevMap == MapNames.mission12_Strange_Bedfellows && CurrentMap == MapNames.mission13_Into_the_Maw_of_Chaos)
                            {
                                Split(SplitArea.l12, frameCounter);
                            }
                            /*
                            else if(prevMap == MapNames.mission13_Into_the_Maw_of_Chaos && CurrentMap == MapNames.mission11_Escape)
                            {
                                Split(SplitArea.l13, frameCounter);
                            }*/


                        }

                        prevMap = CurrentMap;
                        prevIsLoading = isLoading;
                        frameCounter++;

                        Thread.Sleep(15);

                        if(_cancelSource.IsCancellationRequested)
                        {
                            return;
                        }

                    }

                    // pause game timer on exit or crash
                    _uiThread.Post(d =>
                    {
                        if (this.OnLoadStarted != null)
                        {
                            this.OnLoadStarted(this, EventArgs.Empty);
                        }
                    }, null);
                    isLoading = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    Thread.Sleep(1000);
                }
            }
        }

        private void Split(SplitArea split, uint frame)
        {
            Debug.WriteLine(String.Format("[NoLoads] split {0} - {1}", split, frame));
            _uiThread.Post(d =>
            {
                if(this.OnSplitCompleted != null)
                {
                    this.OnSplitCompleted(this, split, frame);
                }
            }, null);
        }



        Process GetGameProcess()
        {
            Process game = Process.GetProcesses().FirstOrDefault(p => (p.ProcessName.ToLower() == "thief") && !p.HasExited && !_ignorePIDs.Contains(p.Id));


            if (game == null)
            {
                _isLoadingPtr = null;
                injection = null;
                _levelName = null;
                return null;
            }
            else if(injection == null)
            {
                Thread.Sleep(500);
                var ExeVersion = game.MainModule.FileVersionInfo.ProductVersion;

                if(ExeVersion == "1.25")
                {
                    Debug.WriteLine("[NOLOADS] Detected EXE version 1.25");
                    SuisCodeInjection.CodeInjectionMasterContainer container = new SuisCodeInjection.CodeInjectionMasterContainer();
                    container.AddVariable("IsLoading", 0);
                    container.AddInjectionPoint("LoadStart", game.MainModuleWow64Safe().BaseAddress + 0x177A0, 6);
                    container.AddWriteToVariable("IsLoading", 1);
                    container.AddByteCode(new byte[] { 0x81, 0xEC, 0x84, 0x0A, 0x00, 0x00 });
                    container.CloseInjection("LoadStart");
                    container.AddInjectionPoint("LoadEnd", game.MainModuleWow64Safe().BaseAddress + 0x18302, 7);
                    container.AddWriteToVariable("IsLoading", 0);
                    container.AddByteCode(new byte[] { 0x8B, 0x8C, 0x24, 0x8C, 0x0A, 0x00, 0x00 });
                    container.CloseInjection("LoadEnd");
                    injection = new SuisCodeInjection.CodeInjection(game, container);

                    if(injection.Result != SuisCodeInjection.CodeInjectionResult.Success)
                    {
                        MessageBox.Show("Failed to inject the code: " + injection.Result);
                        _ignorePIDs.Add(game.Id);
                        return null;
                    }
                    else
                    {
                        _levelName = new DeepPointer(0x408900);
                    }
                }
                else if(ExeVersion == "1.22")
                {
                    Debug.WriteLine("[NOLOADS] Detected EXE version 1.22");
                    SuisCodeInjection.CodeInjectionMasterContainer container = new SuisCodeInjection.CodeInjectionMasterContainer();
                    container.AddVariable("IsLoading", 0);
                    container.AddInjectionPoint("LoadStart", game.MainModuleWow64Safe().BaseAddress + 0x17306, 6);
                    container.AddWriteToVariable("IsLoading", 1);
                    container.AddByteCode(new byte[] { 0x81, 0xEC, 0x84, 0x09, 0x00, 0x00 });
                    container.CloseInjection("LoadStart");
                    container.AddInjectionPoint("LoadEnd", game.MainModuleWow64Safe().BaseAddress + 0x17EF6, 7);
                    container.AddWriteToVariable("IsLoading", 0);
                    container.AddByteCode(new byte[] { 0x8B, 0x8C, 0x24, 0x8C, 0x09, 0x00, 0x00 });
                    container.CloseInjection("LoadEnd");
                    injection = new SuisCodeInjection.CodeInjection(game, container);

                    if(injection.Result != SuisCodeInjection.CodeInjectionResult.Success)
                    {
                        MessageBox.Show("Failed to inject the code: " + injection.Result);
                        _ignorePIDs.Add(game.Id);
                        return null;
                    }
                    else
                    {
                        _levelName = new DeepPointer(0x4030A0);
                    }
                }
                else
                {
                    MessageBox.Show("Unrecognized version of EXE. Supported versions are NewDark 1.22 (TFix 1.20) and 1.25 (TFix 1.25d)");
                    _ignorePIDs.Add(game.Id);
                    return null;
                }

            }

            if(_isLoadingPtr == null && injection != null)
            {
                IntPtr address = injection.GetVariableAdress("IsLoading");
                if(address != (IntPtr)0)
                {
                    Debug.WriteLine("[NoLoads] Injected and reading from variable at: 0x" + address.ToString("X4"));
                    _isLoadingPtr = new DeepPointer(address.ToInt32());
                }

            }
            return game;

        }
    }
}
