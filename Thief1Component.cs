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

        void gameMemory_OnSplitCompleted(object sender, GameMemory.SplitArea split, uint frame)
        {
            Debug.WriteLineIf(split != GameMemory.SplitArea.None, String.Format("[NoLoads] Trying to split {0}, State: {1} - {2}", split, _gameMemory.SplitStates[(int)split], frame));
            if(_state.CurrentPhase == TimerPhase.Running && !_gameMemory.SplitStates[(int)split] &&
                ((split == GameMemory.SplitArea.l01 && this.Settings.L01) ||
                (split == GameMemory.SplitArea.l02 && this.Settings.L02) ||
                (split == GameMemory.SplitArea.l03 && this.Settings.L03) ||
                (split == GameMemory.SplitArea.l04 && this.Settings.L04) ||
                (split == GameMemory.SplitArea.l05 && this.Settings.L05) ||
                (split == GameMemory.SplitArea.l05b && this.Settings.L05b) ||
                (split == GameMemory.SplitArea.l06 && this.Settings.L06) ||
                (split == GameMemory.SplitArea.l07 && this.Settings.L07) ||
                (split == GameMemory.SplitArea.l07b && this.Settings.L07b) ||
                (split == GameMemory.SplitArea.l08 && this.Settings.L08) ||
                (split == GameMemory.SplitArea.l08b && this.Settings.L08b) ||
                (split == GameMemory.SplitArea.l09 && this.Settings.L09) ||
                (split == GameMemory.SplitArea.l10 && this.Settings.L10) ||
                (split == GameMemory.SplitArea.l11 && this.Settings.L11) ||
                (split == GameMemory.SplitArea.l12 && this.Settings.L12) ||
                (split == GameMemory.SplitArea.l13 && this.Settings.L13)
                ))
            {
                Trace.WriteLine(String.Format("[NoLoads] {0} Split - {1}", split, frame));
                _timer.Split();
                _gameMemory.SplitStates[(int)split] = true;
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
