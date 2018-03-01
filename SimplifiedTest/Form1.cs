using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using SuisCodeInjection;

namespace SimplifiedTest
{
    public partial class Form1 : Form
    {
        Process gameProcess;
        CodeInjection injection = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            gameProcess = Process.GetProcessesByName("thief").FirstOrDefault();
            if(gameProcess == null)
            {
                injection = null;
            }
            else if(injection == null)
            {
                CodeInjectionMasterContainer container = new CodeInjectionMasterContainer();
                container.AddVariable("IsLoading", 0);
                container.AddInjectionPoint("LoadStart", gameProcess.MainModule.BaseAddress.ToInt32() + 0x177A0, 6);
                container.AddWriteToVariable("IsLoading", 1);
                container.AddByteCode(new byte[] { 0x81, 0xEC, 0x84, 0x0A, 0x00, 0x00 });
                container.CloseInjection("LoadStart");
                container.AddInjectionPoint("LoadEnd", gameProcess.MainModule.BaseAddress.ToInt32() + 0x18302, 7);
                container.AddWriteToVariable("IsLoading", 0);
                container.AddByteCode(new byte[] { 0x8B, 0x8C, 0x24, 0x8C, 0x0A, 0x00, 0x00 });
                container.CloseInjection("LoadEnd");
                injection = new CodeInjection(gameProcess, container);
            }
        }
    }
}
