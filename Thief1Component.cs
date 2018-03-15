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
            _timer.CurrentState.OnStart += Timer_OnStart;

            _gameMemory = new GameMemory(this.Settings);
            _gameMemory.OnLoadStarted += GameMemory_OnLoadStarted;
            _gameMemory.OnLoadFinished += GameMemory_OnLoadFinished;
            _gameMemory.OnPlayerGainedControl += GameMemory_OnPlayerGainedControl;
            _gameMemory.OnSplitCompleted += GameMemory_OnSplitCompleted;

            state.OnStart += State_OnStart;
            _gameMemory.StartMonitoring();
        }

        public override void Dispose()
        {
            this.Disposed = true;

            _state.OnStart -= State_OnStart;
            _timer.CurrentState.OnStart -= Timer_OnStart;

            if (_gameMemory != null)
            {
                _gameMemory.Stop();
            }

        }

        private void State_OnStart(object sender, EventArgs e) => _timer.InitializeGameTime();

        private void Timer_OnStart(object sender, EventArgs e) => _timer.InitializeGameTime();

        private void GameMemory_OnFirstLevelLoading(object sender, EventArgs e)
        {
            if(this.Settings.AutoRestart)
            {
                _timer.Reset();
            }
        }

        private void GameMemory_OnPlayerGainedControl(object sender, EventArgs e)
        {
            if(this.Settings.AutoStart)
            {
                _timer.Start();
            }
        }

        private void GameMemory_OnLoadStarted(object sender, EventArgs e) => _state.IsGameTimePaused = true;

        private void GameMemory_OnLoadFinished(object sender, EventArgs e) => _state.IsGameTimePaused = false;

        private void GameMemory_OnSplitCompleted(object sender, int splitindex, uint frame)
        {
            Debug.WriteLineIf(splitindex != 0, String.Format("[NoLoads] Trying to split {0}, State: {1} - {2}", splitindex, _gameMemory.SplitStates[splitindex], frame));
            if(_state.CurrentPhase == TimerPhase.Running && !_gameMemory.SplitStates[splitindex])
            {
                Trace.WriteLine(String.Format("[NoLoads] {0} Split - {1}", splitindex, frame));
                _timer.Split();
                _gameMemory.SplitStates[splitindex] = true;
            }
        }

        public override XmlNode GetSettings(XmlDocument document) => this.Settings.GetSettings(document);

        public override Control GetSettingsControl(LayoutMode mode) => this.Settings;

        public override void SetSettings(XmlNode settings) => this.Settings.SetSettings(settings);

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) { }
        //public override void RenameComparison(string oldName, string newName) { }
    }
}
