using System;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.Thief1
{
    public partial class Thief1Settings : UserControl
    {
        public bool AutoRestart { get; set; }
        public bool AutoStart { get; set; }
        public bool L01 { get; set; }
        public bool L02 { get; set; }
        public bool L03 { get; set; }
        public bool L04 { get; set; }
        public bool L05 { get; set; }
        public bool L05b { get; set; }
        public bool L06 { get; set; }
        public bool L07 { get; set; }
        public bool L07b { get; set; }
        public bool L08 { get; set; }
        public bool L08b { get; set; }
        public bool L09 { get; set; }
        public bool L10 { get; set; }
        public bool L11 { get; set; }
        public bool L12 { get; set; }
        public bool L13 { get; set; }

        private const bool DEFAULT_AUTORESET = false;
        private const bool DEFAULT_AUTOSTART = true;
        private const bool DEFAULT_L01 = true;
        private const bool DEFAULT_L02 = true;
        private const bool DEFAULT_L03 = true;
        private const bool DEFAULT_L04 = true;
        private const bool DEFAULT_L05 = true;
        private const bool DEFAULT_L05b = true;
        private const bool DEFAULT_L06 = true;
        private const bool DEFAULT_L07 = true;
        private const bool DEFAULT_L07b = true;
        private const bool DEFAULT_L08 = true;
        private const bool DEFAULT_L08b = true;
        private const bool DEFAULT_L09 = true;
        private const bool DEFAULT_L10 = true;
        private const bool DEFAULT_L11 = true;
        private const bool DEFAULT_L12 = true;
        private const bool DEFAULT_L13 = false;

        public Thief1Settings()
        {
            InitializeComponent();

            this.CB_Autostart.DataBindings.Add("Checked", this, "AutoStart", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_Autorestart.DataBindings.Add("Checked", this, "AutoRestart", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L01.DataBindings.Add("Checked", this, "L01", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L02.DataBindings.Add("Checked", this, "L02", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L03.DataBindings.Add("Checked", this, "L03", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L04.DataBindings.Add("Checked", this, "L04", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L05.DataBindings.Add("Checked", this, "L05", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L05b.DataBindings.Add("Checked", this, "L05b", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L06.DataBindings.Add("Checked", this, "L06", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L07.DataBindings.Add("Checked", this, "L07", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L07b.DataBindings.Add("Checked", this, "L07b", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L08.DataBindings.Add("Checked", this, "L08", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L08b.DataBindings.Add("Checked", this, "L08b", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L09.DataBindings.Add("Checked", this, "L09", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L10.DataBindings.Add("Checked", this, "L10", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L11.DataBindings.Add("Checked", this, "L11", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L12.DataBindings.Add("Checked", this, "L12", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L13.DataBindings.Add("Checked", this, "L13", false, DataSourceUpdateMode.OnPropertyChanged);
            this.CB_L13.Enabled = false;

            // defaults
            this.AutoRestart = DEFAULT_AUTORESET;
            this.AutoStart = DEFAULT_AUTOSTART;
            this.L01 = DEFAULT_L01;
            this.L02 = DEFAULT_L02;
            this.L03 = DEFAULT_L03;
            this.L04 = DEFAULT_L04;
            this.L05 = DEFAULT_L05;
            this.L05b = DEFAULT_L05b;
            this.L06 = DEFAULT_L06;
            this.L07 = DEFAULT_L07;
            this.L07b = DEFAULT_L07b;
            this.L08 = DEFAULT_L08;
            this.L08b = DEFAULT_L08b;
            this.L09 = DEFAULT_L09;
            this.L10 = DEFAULT_L10;
            this.L11 = DEFAULT_L11;
            this.L12 = DEFAULT_L12;
            this.L13 = DEFAULT_L13;
        }

        public XmlNode GetSettings(XmlDocument doc)
        {
            XmlElement settingsNode = doc.CreateElement("Settings");

            settingsNode.AppendChild(ToElement(doc, "Version", Assembly.GetExecutingAssembly().GetName().Version.ToString(3)));

            settingsNode.AppendChild(ToElement(doc, "AutoReset", this.AutoRestart));
            settingsNode.AppendChild(ToElement(doc, "AutoStart", this.AutoStart));
            settingsNode.AppendChild(ToElement(doc, "L01", this.L01));
            settingsNode.AppendChild(ToElement(doc, "L02", this.L02));
            settingsNode.AppendChild(ToElement(doc, "L03", this.L03));
            settingsNode.AppendChild(ToElement(doc, "L04", this.L04));
            settingsNode.AppendChild(ToElement(doc, "L05", this.L05));
            settingsNode.AppendChild(ToElement(doc, "L05b", this.L05b));
            settingsNode.AppendChild(ToElement(doc, "L06", this.L06));
            settingsNode.AppendChild(ToElement(doc, "L07", this.L07));
            settingsNode.AppendChild(ToElement(doc, "L07b", this.L07b));
            settingsNode.AppendChild(ToElement(doc, "L08", this.L08));
            settingsNode.AppendChild(ToElement(doc, "L08b", this.L08b));
            settingsNode.AppendChild(ToElement(doc, "L09", this.L09));
            settingsNode.AppendChild(ToElement(doc, "L10", this.L10));
            settingsNode.AppendChild(ToElement(doc, "L11", this.L11));
            settingsNode.AppendChild(ToElement(doc, "L12", this.L12));
            settingsNode.AppendChild(ToElement(doc, "L13", this.L13));

            return settingsNode;
        }

        public void SetSettings(XmlNode settings)
        {

            this.AutoRestart = ParseBool(settings, "AutoReset", DEFAULT_AUTORESET);
            this.AutoStart = ParseBool(settings, "AutoStart", DEFAULT_AUTOSTART);
            this.L01 = ParseBool(settings, "L01", DEFAULT_L01);
            this.L02 = ParseBool(settings, "L02", DEFAULT_L02);
            this.L03 = ParseBool(settings, "L03", DEFAULT_L03);
            this.L04 = ParseBool(settings, "L04", DEFAULT_L04);
            this.L05 = ParseBool(settings, "L05", DEFAULT_L05);
            this.L05b = ParseBool(settings, "L05b", DEFAULT_L05b);
            this.L06 = ParseBool(settings, "L06", DEFAULT_L06);
            this.L07 = ParseBool(settings, "L07", DEFAULT_L07);
            this.L07b = ParseBool(settings, "L07b", DEFAULT_L07b);
            this.L08 = ParseBool(settings, "L08", DEFAULT_L08);
            this.L08b = ParseBool(settings, "L08b", DEFAULT_L08b);
            this.L09 = ParseBool(settings, "L09", DEFAULT_L09);
            this.L10 = ParseBool(settings, "L10", DEFAULT_L10);
            this.L11 = ParseBool(settings, "L11", DEFAULT_L11);
            this.L12 = ParseBool(settings, "L12", DEFAULT_L12);
            this.L13 = ParseBool(settings, "L13", DEFAULT_L13);
        }

        static bool ParseBool(XmlNode settings, string setting, bool default_ = false)
        {
            bool val;
            return settings[setting] != null ?
                (Boolean.TryParse(settings[setting].InnerText, out val) ? val : default_)
                : default_;
        }

        static XmlElement ToElement<T>(XmlDocument document, string name, T value)
        {
            XmlElement str = document.CreateElement(name);
            str.InnerText = value.ToString();
            return str;
        }
    }
}
