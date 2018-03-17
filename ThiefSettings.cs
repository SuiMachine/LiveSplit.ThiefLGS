using System;
using System.Xml;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using LiveSplit.UI;


namespace LiveSplit.ThiefLGS
{
    public struct LevelRow
    {
        public bool Checked { get; set; }
        public string MapName { get; set; }

        public LevelRow(bool Checked, string MapName)
        {
            this.Checked = Checked;
            this.MapName = MapName;
        }
    }

    enum Presets
    {
        [Description("Thief: Gold")]
        ThiefGold,
        [Description("Thief 2: The Metal Age")]
        Thief2,
        [Description("Custom")]Custom,
    }

    public partial class ThiefSettings : UserControl
    {
        public bool AutoRestart { get; set; }
        public bool AutoStart { get; set; }
        public bool SplitOnMissionSuccess { get; set; }
        public LevelRow[] CurrentSplits { get; set; }

        private const bool DEFAULT_AUTORESET = false;
        private const bool DEFAULT_AUTOSTART = true;
        private const bool DEFAULT_SPLITONMISSIONSUCCESS = true;

        private LevelRow[] thief1Rows = new LevelRow[] {
            new LevelRow(true, "miss1.mis"),
            new LevelRow(true, "miss2.mis"),
            new LevelRow(true, "miss3.mis"),
            new LevelRow(true, "miss4.mis"),
            new LevelRow(true, "miss5.mis"),
            new LevelRow(true, "miss15.mis"),
            new LevelRow(true, "miss6.mis"),
            new LevelRow(true, "miss7.mis"),
            new LevelRow(true, "miss16.mis"),
            new LevelRow(true, "miss9.mis"),
            new LevelRow(true, "miss17.mis"),
            new LevelRow(true, "miss10.mis"),
            new LevelRow(true, "miss11.mis"),
            new LevelRow(true, "miss12.mis"),
            new LevelRow(true, "miss13.mis"),
            new LevelRow(true, "miss14.mis")
        };

        private LevelRow[] thief2Rows = new LevelRow[] {
            new LevelRow(true, "miss1.mis"),
            new LevelRow(true, "miss2.mis"),
            new LevelRow(true, "miss3.mis"),
            new LevelRow(true, "miss4.mis"),
            new LevelRow(true, "miss5.mis"),
            new LevelRow(true, "miss6.mis"),
            new LevelRow(true, "miss7.mis"),
            new LevelRow(true, "miss8.mis"),
            new LevelRow(true, "miss9.mis"),
            new LevelRow(true, "miss10.mis"),
            new LevelRow(true, "miss11.mis"),
            new LevelRow(true, "miss12.mis"),
            new LevelRow(true, "miss13.mis")
        };

        public ThiefSettings()
        {
            InitializeComponent();
            AddComboBoxSources();
            this.ComboBox_SplitPresets.SelectedIndex = (int)Presets.Custom;
            this.ComboBox_SplitPresets.SelectedIndexChanged += new System.EventHandler(this.ComboBox_SplitPresets_SelectedIndexChanged);    //Added later, to make sure we don't get pop up, while Combobox isn't populated.
            this.Dock = DockStyle.Fill;

            this.CB_Autostart.DataBindings.Add("Checked", this, "AutoStart", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_Autorestart.DataBindings.Add("Checked", this, "AutoRestart", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_SplitOnMissionSuccess.DataBindings.Add("Checked", this, "SplitOnMissionSuccess", false, DataSourceUpdateMode.OnPropertyChanged);
            OnSplitsChanged(EventArgs.Empty);
        }

        private void ThiefSettings_HandleDestroyed(object sender, System.EventArgs e)
        {
            if(splitsChanged)
            {
                splitsChanged = false;
                OnSplitsChanged(EventArgs.Empty);
            }
        }

        private bool splitsChanged = true;

        public event EventHandler SplitsChanged;
        protected virtual void OnSplitsChanged(EventArgs e)
        {
            if(SplitsChanged != null)
            {
                UpdateSplitsArray();
                SplitsChanged(this, e);
            }
        }


        public XmlNode GetSettings(XmlDocument doc)
        {
            XmlElement settingsNode = doc.CreateElement("Settings");

            settingsNode.AppendChild(ToElement(doc, "Version", Assembly.GetExecutingAssembly().GetName().Version.ToString(3)));

            settingsNode.AppendChild(ToElement(doc, "AutoReset", this.AutoRestart));
            settingsNode.AppendChild(ToElement(doc, "AutoStart", this.AutoStart));
            settingsNode.AppendChild(ToElement(doc, "SplitOnSuccess", this.SplitOnMissionSuccess));
            settingsNode.AppendChild(ToNodeArray(doc, "SplitsNode", this.CurrentSplits));

            return settingsNode;
        }

        private void UpdateSplitsArray()
        {
            Debug.WriteLine("[NO LOADS] Updating Splits array");
            CurrentSplits = new LevelRow[ChkList_Splits.Items.Count];

            for(int i=0; i<ChkList_Splits.Items.Count; i++)
            {
                string item = ChkList_Splits.Items[i].ToString().ToLower();
                CurrentSplits[i] = new LevelRow(ChkList_Splits.CheckedIndices.Contains(i), item);
            }
            splitsChanged = false;
        }



        public void SetSettings(XmlNode settings)
        {
            this.AutoRestart = ParseBool(settings, "AutoReset", DEFAULT_AUTORESET);
            this.AutoStart = ParseBool(settings, "AutoStart", DEFAULT_AUTOSTART);
            this.SplitOnMissionSuccess = ParseBool(settings, "SplitsNode", DEFAULT_SPLITONMISSIONSUCCESS);
            this.CurrentSplits = ParseSplits(settings, "SplitsNode");
            this.ChkList_Splits.Fill(CurrentSplits);
            splitsChanged = false;
            OnSplitsChanged(EventArgs.Empty);
        }



        static bool ParseBool(XmlNode settings, string setting, bool default_ = false)
        {
            return settings[setting] != null ?
                (Boolean.TryParse(settings[setting].InnerText, out bool val) ? val : default_)
                : default_;
        }

        static int ParseInt(XmlNode settings, string setting, int default_ = 0)
        {
            return settings[setting] != null ?
                (int.TryParse(settings[setting].InnerText, out int val) ? val : default_)
                : default_;
        }

        static LevelRow[] ParseSplits(XmlNode settings, string setting)
        {
            List<LevelRow> tempLst = new List<LevelRow>();
            if(settings[setting] == null)
                return tempLst.ToArray();

            foreach(XmlNode element in settings[setting].ChildNodes)
            {
                string tempMapName = element.Name;
                bool tempCheck = false;
                if(element.Attributes["Checked"] != null)
                    tempCheck = bool.TryParse(element.Attributes["Checked"].InnerText, out bool parseRes) ? parseRes : false;
                tempLst.Add(new LevelRow(tempCheck, tempMapName));
            }
            return tempLst.ToArray();
        }

        static XmlElement ToElement<T>(XmlDocument document, string name, T value)
        {
            XmlElement str = document.CreateElement(name);
            str.InnerText = value.ToString();
            return str;
        }

        static XmlElement ToNodeArray(XmlDocument document, string name, LevelRow[] value)
        {
            XmlElement str = document.CreateElement(name);
            foreach(var elementOfArray in value)
            {
                if(elementOfArray.MapName != "")
                {
                    XmlNode child = document.CreateElement(elementOfArray.MapName);
                    var Checked = document.CreateAttribute("Checked");
                    Checked.InnerText = elementOfArray.Checked.ToString();
                    child.Attributes.Append(Checked);
                    str.AppendChild(child);
                }
            }
            return str;
        }

        private void AddComboBoxSources()
        {
            ComboBox_SplitPresets.DisplayMember = "Description";
            ComboBox_SplitPresets.ValueMember = "value";

            this.ComboBox_SplitPresets.DataSource = Enum.GetValues(typeof(Presets)).Cast<Enum>().Select(value =>
                new {
                    (Attribute.GetCustomAttribute(value.GetType().GetField(value.ToString()),
                    typeof(DescriptionAttribute)) as DescriptionAttribute).Description,
                    value
                }).OrderBy(item => item.value).ToList();
        }

        private void ComboBox_SplitPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(ComboBox_SplitPresets.SelectedIndex != (int)Presets.Custom)
            {
                DialogResult res = MessageBox.Show("Your current list of splits and settings will you overriden. Are you sure, you want to apply the preset?", "Do you want to override?", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
                if(res == DialogResult.Yes)
                {
                    switch(ComboBox_SplitPresets.SelectedIndex)
                    {
                        case (int)Presets.ThiefGold:
                            {
                                ChkList_Splits.Fill(thief1Rows);
                                break;
                            }
                        case (int)Presets.Thief2:
                            {
                                ChkList_Splits.Fill(thief2Rows);
                                break;
                            }
                        default:
                        break;
                    }
                }
            }
            else
            {
                ComboBox_SplitPresets.SelectedIndex = (int)Presets.Custom;
            }

        }

        #region ListButtonsEvents
        private void B_SplitAdd_Click(object sender, EventArgs e)
        {
            string tempMap = "";
            DialogResult result = InputBox.Show("Name of the map", "Please provide name of the map:", ref tempMap);
            if(result == DialogResult.OK && tempMap.Length > 0)
            {
                ChkList_Splits.Items.Add(tempMap.ToLower(), true);
                ComboBox_SplitPresets.SelectedIndex = (int)Presets.Custom;
            }
            splitsChanged = true;
        }

        private void B_SplitEdit_Click(object sender, EventArgs e)
        {
            if(ChkList_Splits.Items.Count < 0)
                MessageBox.Show("Map list is empty!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
            {
                int selectedElement = ChkList_Splits.SelectedIndex;
                if(selectedElement == -1)
                {
                    MessageBox.Show("No element was selected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string tempMap = "";
                    DialogResult result = InputBox.Show("Name of the map", "Please provide name of the map:", ref tempMap);
                    if(result == DialogResult.OK && tempMap.Length > 0)
                    {
                        ChkList_Splits.Items[selectedElement] = tempMap;
                    }
                }
            }
            splitsChanged = true;
        }

        private void B_SplitRemove_Click(object sender, EventArgs e)
        {
            if(ChkList_Splits.Items.Count < 0)
                MessageBox.Show("Map list is empty!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
            {
                int selectedElement = ChkList_Splits.SelectedIndex;
                if(selectedElement == -1)
                {
                    MessageBox.Show("No element was selected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    ChkList_Splits.Items.RemoveAt(selectedElement);
                }
            }
            splitsChanged = true;
        }

        private void B_SplitMoveUp_Click(object sender, EventArgs e)
        {
            if(ChkList_Splits.Items.Count < 0)
                MessageBox.Show("Map list is empty!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
            {
                int selectedElementIndex = ChkList_Splits.SelectedIndex;
                if(selectedElementIndex == -1)
                {
                    MessageBox.Show("No element was selected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if(selectedElementIndex != 0)
                {

                    object elementBefore = ChkList_Splits.Items[selectedElementIndex - 1];
                    object elementCurrent = ChkList_Splits.Items[selectedElementIndex];
                    ChkList_Splits.Items[selectedElementIndex - 1] = elementCurrent;
                    ChkList_Splits.Items[selectedElementIndex] = elementBefore;
                    ChkList_Splits.SelectedIndex = selectedElementIndex - 1;
                }
            }
            splitsChanged = true;
        }

        private void B_SplitMoveDown_Click(object sender, EventArgs e)
        {
            if(ChkList_Splits.Items.Count < 0)
                MessageBox.Show("Map list is empty!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
            {
                int selectedElementIndex = ChkList_Splits.SelectedIndex;
                int totalElements = ChkList_Splits.Items.Count;
                if(selectedElementIndex == -1)
                {
                    MessageBox.Show("No element was selected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if(selectedElementIndex < totalElements - 1)
                {
                    object elementAfter = ChkList_Splits.Items[selectedElementIndex + 1];
                    object elementCurrent = ChkList_Splits.Items[selectedElementIndex];
                    ChkList_Splits.Items[selectedElementIndex + 1] = elementCurrent;
                    ChkList_Splits.Items[selectedElementIndex] = elementAfter;
                    ChkList_Splits.SelectedIndex = selectedElementIndex + 1;
                }
            }
            splitsChanged = true;
        }
        #endregion

        #region ChkListAdditionalEvents
        private void ChkList_Splits_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            splitsChanged = true;
        }
        #endregion
    }


    public static class SuisExtensions
    {
        public static void Fill(this CheckedListBox lBox, LevelRow[] elements)
        {
            lBox.Items.Clear();
            foreach(var element in elements)
            {
                lBox.Items.Add(element.MapName.ToLower(), element.Checked);
            }
        }
    }
}
