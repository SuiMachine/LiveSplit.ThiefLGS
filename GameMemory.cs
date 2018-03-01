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

        public event EventHandler OnLoadStarted;
        public event EventHandler OnLoadFinished;

        private Task _thread;
        private CancellationTokenSource _cancelSource;
        private SynchronizationContext _uiThread;
        private List<int> _ignorePIDs;
        private Thief1Settings _settings;

        private DeepPointer _isLoadingPtr = null;

        private SuisCodeInjection.CodeInjection injection;


        public bool[] splitStates { get; set; }

        public GameMemory(Thief1Settings componentSettings)
        {
            _settings = componentSettings;

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

                        //Debug.WriteLine("Read from memory: " + isLoading);


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
                            }
                        }

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


        Process GetGameProcess()
        {
            Process game = Process.GetProcesses().FirstOrDefault(p => (p.ProcessName.ToLower() == "thief") && !p.HasExited && !_ignorePIDs.Contains(p.Id));


            if (game == null)
            {
                _isLoadingPtr = null;
                injection = null;
                return null;
            }
            else if(injection == null)
            {
                Thread.Sleep(500);
                var ExeVersion = game.MainModule.FileVersionInfo.ProductVersion;

                if(ExeVersion == "1.25")
                {
                    SuisCodeInjection.CodeInjectionMasterContainer container = new SuisCodeInjection.CodeInjectionMasterContainer();
                    container.AddVariable("IsLoading", 0);
                    container.AddInjectionPoint("LoadStart", game.MainModuleWow64Safe().BaseAddress.ToInt32() + 0x177A0, 6);
                    container.AddWriteToVariable("IsLoading", 1);
                    container.AddByteCode(new byte[] { 0x81, 0xEC, 0x84, 0x0A, 0x00, 0x00 });
                    container.CloseInjection("LoadStart");
                    container.AddInjectionPoint("LoadEnd", game.MainModuleWow64Safe().BaseAddress.ToInt32() + 0x18302, 7);
                    container.AddWriteToVariable("IsLoading", 0);
                    container.AddByteCode(new byte[] { 0x8B, 0x8C, 0x24, 0x8C, 0x0A, 0x00, 0x00 });
                    container.CloseInjection("LoadEnd");
                    injection = new SuisCodeInjection.CodeInjection(game, container);
                    if(injection.Result != SuisCodeInjection.CodeInjectionResult.Success)
                        MessageBox.Show("Failed to inject the code: " + injection.Result);
                }
                else if(ExeVersion == "1.22")
                {
                    SuisCodeInjection.CodeInjectionMasterContainer container = new SuisCodeInjection.CodeInjectionMasterContainer();
                    container.AddVariable("IsLoading", 0);
                    container.AddInjectionPoint("LoadStart", game.MainModuleWow64Safe().BaseAddress.ToInt32() + 0x17306, 6);
                    container.AddWriteToVariable("IsLoading", 1);
                    container.AddByteCode(new byte[] { 0x81, 0xEC, 0x84, 0x09, 0x00, 0x00 });
                    container.CloseInjection("LoadStart");
                    container.AddInjectionPoint("LoadEnd", game.MainModuleWow64Safe().BaseAddress.ToInt32() + 0x17EF6, 7);
                    container.AddWriteToVariable("IsLoading", 0);
                    container.AddByteCode(new byte[] { 0x8B, 0x8C, 0x24, 0x8C, 0x09, 0x00, 0x00 });
                    container.CloseInjection("LoadEnd");
                    injection = new SuisCodeInjection.CodeInjection(game, container);
                    if(injection.Result != SuisCodeInjection.CodeInjectionResult.Success)
                        MessageBox.Show("Failed to inject the code: " + injection.Result);
                }
                else
                {
                    MessageBox.Show("Unrecognized version of EXE. Supporter versions are NewDark 1.22 (TFix 1.20) and 1.25 (TFix 1.25d)");
                    _ignorePIDs.Add(game.Id);
                    return null;
                }

            }

            if(_isLoadingPtr == null && injection != null)
            {
                int address = injection.getVariableAdress("IsLoading");
                if(address != 0)
                {
                    Debug.WriteLine("[NoLoads] Injected and reading from variable at: 0x" + address.ToString("X4"));
                    _isLoadingPtr = new DeepPointer(address);

                }

            }
            return game;

        }
    }
}
