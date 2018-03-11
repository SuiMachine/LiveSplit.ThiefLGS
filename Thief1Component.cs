using LiveSplit.Model;
using LiveSplit.TimeFormatters;
using LiveSplit.UI.Components;
using LiveSplit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using System.Windows.Forms;
using System.Diagnostics;

namespace LiveSplit.Thief1
{
    class Thief1Component : LogicComponent
    {
        public override string ComponentName
        {
            get { return "Thief1"; }
        }

        public Thief1Settings Settings { get; set; }

        public bool Disposed { get; private set; }
        public bool IsLayoutComponent { get; private set; }

        private TimerModel _timer;
        private GameMemory _gameMemory;
        private LiveSplitState _state;

        public Thief1Component(LiveSplitState state, bool isLayoutComponent)
        {
            _state = state;
            this.IsLayoutComponent = isLayoutComponent;

            this.Settings = new Thief1Settings();

            _timer = new TimerModel { CurrentState = state };
            _timer.CurrentState.OnStart += timer_OnStart;

            _gameMemory = new GameMemory(this.Settings);
            _gameMemory.OnLoadStarted += gameMemory_OnLoadStarted;
            _gameMemory.OnLoadFinished += gameMemory_OnLoadFinished;
            _gameMemory.OnPlayerGainedControl += gameMemory_OnPlayerGainedControl;
            _gameMemory.OnSplitCompleted += gameMemory_OnSplitCompleted;

            state.OnStart += State_OnStart;
            _gameMemory.StartMonitoring();
        }

        public override void Dispose()
        {
            this.Disposed = true;

            _state.OnStart -= State_OnStart;
            _timer.CurrentState.OnStart -= timer_OnStart;

            if (_gameMemory != null)
            {
                _gameMemory.Stop();
            }

        }

        void State_OnStart(object sender, EventArgs e)
        {
            _timer.InitializeGameTime();

        }

        void timer_OnStart(object sender, EventArgs e)
        {
            _timer.InitializeGameTime();
        }

        void gameMemory_OnFirstLevelLoading(object sender, EventArgs e)
        {
            if(this.Settings.AutoRestart)
            {
                _timer.Reset();
            }
        }

        void gameMemory_OnPlayerGainedControl(object sender, EventArgs e)
        {
            if(this.Settings.AutoStart)
            {
                _timer.Start();
            }
        }

        void gameMemory_OnLoadStarted(object sender, EventArgs e)
        {
            _state.IsGameTimePaused = true;
        }

        void gameMemory_OnLoadFinished(object sender, EventArgs e)
        {
            _state.IsGameTimePaused = false;
        }

        void gameMemory_OnSplitCompleted(object sender, int splitindex, uint frame)
        {
            Debug.WriteLineIf(splitindex != 0, String.Format("[NoLoads] Trying to split {0}, State: {1} - {2}", splitindex, _gameMemory.SplitStates[splitindex], frame));
            if(_state.CurrentPhase == TimerPhase.Running && !_gameMemory.SplitStates[splitindex])
            {
                Trace.WriteLine(String.Format("[NoLoads] {0} Split - {1}", splitindex, frame));
                _timer.Split();
                _gameMemory.SplitStates[splitindex] = true;
            }
        }

        public override XmlNode GetSettings(XmlDocument document)
        {
            return this.Settings.GetSettings(document);
        }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            return this.Settings;
        }

        public override void SetSettings(XmlNode settings)
        {
            this.Settings.SetSettings(settings);
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) { }
        //public override void RenameComparison(string oldName, string newName) { }
    }
}
