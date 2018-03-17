using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ReworkedTrainer32bit;

namespace LiveSplit.ThiefLGS
{
    class GameMemory
    {
        public event EventHandler OnPlayerGainedControl;
        public event EventHandler OnLoadStarted;
        public event EventHandler OnLoadFinished;
        public delegate void SplitCompletedEventHandler(object sender, int SplitIndex, uint frame);
        public event SplitCompletedEventHandler OnSplitCompleted;

        private Task _thread;
        private CancellationTokenSource _cancelSource;
        private SynchronizationContext _uiThread;
        private List<int> _ignorePIDs;
        private ThiefSettings _settings;

        private SuisReader _isLoadingPtr = null;
        private SuisReader _levelCompleteCounter = null;
        private SuisReader _levelName = null;
        private int StringReadLenght = 1;

        private SuisCodeInjection.CodeInjection injection;

        public bool SplitOnLastSplit { get; set; }
        public LevelRow[] Splits { get; set; }
        public bool[] SplitStates { get; set; }


        public GameMemory(ThiefSettings componentSettings)
        {
            _settings = componentSettings;
            _settings.SplitsChanged += _settings_SplitsChanged;
            _ignorePIDs = new List<int>();
        }

        private void _settings_SplitsChanged(object sender, EventArgs e)
        {
            if(_settings.CurrentSplits != null)
            {
                Splits = _settings.CurrentSplits;
                SplitStates = new bool[Splits.Length + 1];
                StringReadLenght = FindLongest(Splits);
                for(int i = 0; i < SplitStates.Length; i++)
                {
                    SplitStates[i] = false;
                }
                SplitOnLastSplit = _settings.SplitOnMissionSuccess;
            }
            Debug.WriteLine(string.Format("[NO LOADS] Splits changed. Updating GameMemory reader (a total of {0} splits and longest name of {1} characters).", Splits != null ? Splits.Length : 0, StringReadLenght));

        }

        private int FindLongest(LevelRow[] splits)
        {
            int longest = 1;
            if(splits == null)
                return 1;
            for(int i=0; i<splits.Length; i++)
            {
                if(splits[i].MapName.Length > longest)
                    longest = splits[i].MapName.Length;
            }
            return longest;
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
        int prevLevelCompletedCounter = 0;
        int LevelCompletedCounter = 0;

        string CurrentMap = "";
        string prevMap = "";

        void MemoryReadThread()
        {
            Debug.WriteLine("[NoLoads] MemoryReadThread");
            _settings_SplitsChanged(null, null);

            while (!_cancelSource.IsCancellationRequested)
            {
                try
                {
                    Debug.WriteLine("[NoLoads] Waiting for thief.exe or thief2.exe...");

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
                        isLoading = _isLoadingPtr.ReadBool();
                        if(_levelCompleteCounter != null)
                            LevelCompletedCounter = _levelCompleteCounter.ReadInteger();
                        string tempRead = _levelName.ReadString(StringReadLenght, SuisReader.StringType.UTF8).ToString();
                        if(tempRead != "")
                            CurrentMap = tempRead.ToLower();

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

                                if(Splits != null && Splits.Length > 0 && CurrentMap == Splits[0].MapName)
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

                        if(CurrentMap != prevMap && CurrentMap != "" || prevLevelCompletedCounter != LevelCompletedCounter)
                        {
                            for(int i=1; i<Splits.Length; i++)
                            {
                                if(Splits[i].Checked && Splits[i].MapName == CurrentMap && Splits[i-1].MapName == prevMap)
                                {
                                    Split(i, frameCounter);
                                    break;
                                }
                            }
                            Debug.WriteLine("[NOLOADS] Map changed from \"" + prevMap + "\" to \"" + CurrentMap + "\"");

                            if(SplitOnLastSplit)
                            {
                                if(Splits.Last().MapName == CurrentMap && LevelCompletedCounter > prevLevelCompletedCounter)
                                {
                                    Split(SplitStates.Length - 1, frameCounter);
                                }
                            }
                        }

                        prevMap = CurrentMap;
                        prevIsLoading = isLoading;
                        prevLevelCompletedCounter = LevelCompletedCounter;
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

        private void Split(int SplitIndex, uint frame)
        {
            Debug.WriteLine(String.Format("[NoLoads] split {0} - {1}", SplitIndex, frame));
            _uiThread.Post(d =>
            {
                if(this.OnSplitCompleted != null)
                {
                    this.OnSplitCompleted(this, SplitIndex, frame);
                }
            }, null);
        }

        Process GetGameProcess()
        {
            Process game = Process.GetProcesses().FirstOrDefault(p => (p.ProcessName.ToLower() == "thief" || p.ProcessName.ToLower() == "thief2") && !p.HasExited && !_ignorePIDs.Contains(p.Id));

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
                var ProductVersion = game.MainModule.FileVersionInfo.ProductVersion;

                if(ProductVersion == "1.25")
                {
                    Debug.WriteLine("[NOLOADS] Detected EXE version 1.25");
                    SuisCodeInjection.CodeInjectionMasterContainer container = new SuisCodeInjection.CodeInjectionMasterContainer();
                    container.AddVariable("IsLoading", 0);
                    container.AddVariable("Counter", 0);
                    container.AddInjectionPoint("LoadStart", game.MainModule.BaseAddress + 0x177A0, 6);
                    container.AddWriteToVariable("IsLoading", 1);
                    container.AddByteCode(new byte[] { 0x81, 0xEC, 0x84, 0x0A, 0x00, 0x00 });
                    container.CloseInjection("LoadStart");
                    container.AddInjectionPoint("LoadEnd", game.MainModule.BaseAddress + 0x18302, 7);
                    container.AddWriteToVariable("IsLoading", 0);
                    container.AddByteCode(new byte[] { 0x8B, 0x8C, 0x24, 0x8C, 0x0A, 0x00, 0x00 });
                    container.CloseInjection("LoadEnd");
                    container.AddInjectionPoint("PlayEndCinematic", game.MainModule.BaseAddress + 0x4D5B7, 6);
                    container.AddIncrementValue("Counter", 1);
                    container.AddByteCode(new byte[] { 0x64, 0xA1, 0x00, 0x00, 0x00, 0x00 });
                    container.CloseInjection("PlayEndCinematic");
                    injection = new SuisCodeInjection.CodeInjection(game, container);

                    if(injection.Result != SuisCodeInjection.CodeInjectionResult.Success)
                    {
                        MessageBox.Show("Failed to inject the code: " + injection.Result);
                        _ignorePIDs.Add(game.Id);
                        return null;
                    }
                    else
                    {
                        _levelName = new ReworkedTrainer32bit.SuisReader(game, (int)game.MainModule.BaseAddress + 0x408900);
                    }
                }
                else if(ProductVersion == "1.22")
                {
                    Debug.WriteLine("[NOLOADS] Detected EXE version 1.22");
                    SuisCodeInjection.CodeInjectionMasterContainer container = new SuisCodeInjection.CodeInjectionMasterContainer();
                    container.AddVariable("IsLoading", 0);
                    container.AddVariable("Counter", 0);
                    container.AddInjectionPoint("LoadStart", game.MainModule.BaseAddress + 0x17306, 6);
                    container.AddWriteToVariable("IsLoading", 1);
                    container.AddByteCode(new byte[] { 0x81, 0xEC, 0x84, 0x09, 0x00, 0x00 });
                    container.CloseInjection("LoadStart");
                    container.AddInjectionPoint("LoadEnd", game.MainModule.BaseAddress + 0x17EF6, 7);
                    container.AddWriteToVariable("IsLoading", 0);
                    container.AddByteCode(new byte[] { 0x8B, 0x8C, 0x24, 0x8C, 0x09, 0x00, 0x00 });
                    container.CloseInjection("LoadEnd");
                    container.AddInjectionPoint("PlayEndCinematic", game.MainModule.BaseAddress + 0x4D507, 6);
                    container.AddIncrementValue("Counter", 1);
                    container.AddByteCode(new byte[] { 0x64, 0xA1, 0x00, 0x00, 0x00, 0x00 });
                    container.CloseInjection("PlayEndCinematic");
                    injection = new SuisCodeInjection.CodeInjection(game, container);

                    if(injection.Result != SuisCodeInjection.CodeInjectionResult.Success)
                    {
                        MessageBox.Show("Failed to inject the code: " + injection.Result);
                        _ignorePIDs.Add(game.Id);
                        return null;
                    }
                    else
                    {
                        _levelName = new SuisReader(game, game.MainModule.BaseAddress.ToInt32() + 0x4030A0);
                    }
                }
                else if(ProductVersion == "1.21")
                {
                    Debug.WriteLine("[NOLOADS] Detected EXE version 1.21");
                    SuisCodeInjection.CodeInjectionMasterContainer container = new SuisCodeInjection.CodeInjectionMasterContainer();
                    container.AddVariable("IsLoading", 0);
                    container.AddVariable("Counter", 0);
                    container.AddInjectionPoint("LoadStart", game.MainModule.BaseAddress + 0x168B0, 6);
                    container.AddWriteToVariable("IsLoading", 1);
                    container.AddByteCode(new byte[] { 0x81, 0xEC, 0x30, 0x04, 0x00, 0x00 });
                    container.CloseInjection("LoadStart");
                    container.AddInjectionPoint("LoadEnd", game.MainModule.BaseAddress + 0x16BD5, 7);
                    container.AddWriteToVariable("IsLoading", 0);
                    container.AddByteCode(new byte[] { 0x8B, 0x8C, 0x24, 0x3C, 0x04, 0x00, 0x00 });
                    container.CloseInjection("LoadEnd");
                    container.AddInjectionPoint("PlayEndCinematic", game.MainModule.BaseAddress + 0x49837, 6);
                    container.AddIncrementValue("Counter", 1);
                    container.AddByteCode(new byte[] { 0x64, 0xA1, 0x00, 0x00, 0x00, 0x00 });
                    container.CloseInjection("PlayEndCinematic");
                    injection = new SuisCodeInjection.CodeInjection(game, container);

                    if(injection.Result != SuisCodeInjection.CodeInjectionResult.Success)
                    {
                        MessageBox.Show("Failed to inject the code: " + injection.Result);
                        _ignorePIDs.Add(game.Id);
                        return null;
                    }
                    else
                    {
                        _levelName = new SuisReader(game, game.MainModule.BaseAddress.ToInt32() + 0x3BFF08);
                    }
                }
                //Old DarkEngine
                else if(ProductVersion == "1.37")
                {
                    Debug.WriteLine("[NOLOADS] Detected EXE version 1.37 (Old Dark)");
                    SuisCodeInjection.CodeInjectionMasterContainer container = new SuisCodeInjection.CodeInjectionMasterContainer();
                    container.AddVariable("IsLoading", 0);
                    container.AddInjectionPoint("LoadStart", game.MainModule.BaseAddress + 0x8A70, 6);
                    container.AddWriteToVariable("IsLoading", 1);
                    container.AddByteCode(new byte[] { 0x81, 0xEC, 0x28, 0x04, 0x00, 0x00 });
                    container.CloseInjection("LoadStart");
                    container.AddInjectionPoint("LoadEnd", game.MainModule.BaseAddress + 0x8D08, 6);
                    container.AddWriteToVariable("IsLoading", 0);
                    container.AddByteCode(new byte[] { 0x81, 0xC4, 0x28, 0x04, 0x00, 0x00 });
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
                        _levelName = new SuisReader(game, game.MainModule.BaseAddress.ToInt32() + 0x2790CC);
                    }
                }
                else if(ProductVersion == "1.18")
                {
                    Debug.WriteLine("[NOLOADS] Detected EXE version 1.18 (Old Dark)");
                    SuisCodeInjection.CodeInjectionMasterContainer container = new SuisCodeInjection.CodeInjectionMasterContainer();
                    container.AddVariable("IsLoading", 0);
                    container.AddInjectionPoint("LoadStart", game.MainModule.BaseAddress + 0xACA0, 6);
                    container.AddWriteToVariable("IsLoading", 1);
                    container.AddByteCode(new byte[] { 0x81, 0xEC, 0x24, 0x04, 0x00, 0x00 });
                    container.CloseInjection("LoadStart");
                    container.AddInjectionPoint("LoadEnd", game.MainModule.BaseAddress + 0xAF06, 5);
                    container.AddWriteToVariable("IsLoading", 0);
                    container.AddByteCode(new byte[] { 0x8B, 0x44, 0x24, 0x28, 0x5F });
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
                        _levelName = new SuisReader(game, game.MainModule.BaseAddress.ToInt32() + 0x3BEB00);
                    }
                }
                else
                {
                    MessageBox.Show("Unrecognized version of EXE. Supported versions are NewDark 1.25, 1.22, 1.21 and OldDark 1.37 and 1.18.");
                    _ignorePIDs.Add(game.Id);
                    return null;
                }

            }

            if(_isLoadingPtr == null && injection != null)
            {
                IntPtr address = injection.GetVariableAdress("IsLoading");
                IntPtr addressCounter = injection.GetVariableAdress("Counter");
                if(address != (IntPtr)0)
                {
                    Debug.WriteLine("[NoLoads] Injected and reading from variable at: 0x" + address.ToInt32().ToString("X4"));
                    _isLoadingPtr = new SuisReader(game, (int)game.MainModule.BaseAddress + address.ToInt32());
                }
                if(addressCounter != (IntPtr)0)
                {
                    Debug.WriteLine("[NoLoads] Injected and reading from variable at: 0x" + addressCounter.ToInt32().ToString("X4"));
                    _levelCompleteCounter = new SuisReader(game, game.MainModule.BaseAddress.ToInt32() + addressCounter.ToInt32());
                }

            }
            return game;
        }

        public void Dispose()
        {
            _settings.SplitsChanged -= _settings_SplitsChanged;
        }
    }
}
