using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuisCodeInjection
{

    class CodeInjectionMasterContainer
    {
        public struct DetoursStruct
        {
            public IntPtr InjectionPoint { get; private set; }
            public byte OverridenBytes { get; set; }
            public IntPtr MasterContainerStartLocation { get; private set; }
            public IntPtr MasterContainerJmpBackLocation { get; set; }

            public DetoursStruct(IntPtr InjectionPoint, byte OverridenBytes, IntPtr MasterContainerStartLocation)
            {
                this.InjectionPoint = InjectionPoint;
                this.OverridenBytes = OverridenBytes;
                this.MasterContainerStartLocation = MasterContainerStartLocation;
                this.MasterContainerJmpBackLocation = (IntPtr)0;
            }


            public DetoursStruct(IntPtr InjectionPoint, byte OverridenBytes, IntPtr MasterContainerStartLocation, IntPtr MasterContainerJmpBackLocation)
            {
                this.InjectionPoint = InjectionPoint;
                this.OverridenBytes = OverridenBytes;
                this.MasterContainerStartLocation = MasterContainerStartLocation;
                this.MasterContainerJmpBackLocation = MasterContainerJmpBackLocation;
            }
        }

        public struct VariableStruct
        {
            public IntPtr VariableLocation { get; private set; }
            public int VariableLenght { get; private set; }
            public List<IntPtr> VariableUsage { get; private set; }

            public VariableStruct(IntPtr VariableLocation, int VariableLenght)
            {
                this.VariableLocation = VariableLocation;
                this.VariableLenght = VariableLenght;
                VariableUsage = new List<IntPtr>();
            }
        }

        private List<byte> ByteOpCodes { get; set; }
        public Dictionary<string, VariableStruct> Variables { get; private set; }
        public Dictionary<string, DetoursStruct> Detours { get; private set; }

        public CodeInjectionMasterContainer()
        {
            ByteOpCodes = new List<byte>();
            Variables = new Dictionary<string, VariableStruct>();
            Detours = new Dictionary<string, DetoursStruct>();
        }

        public byte[] GetBytes()
        {
            return ByteOpCodes.ToArray();
        }

        #region VariablesHandling
        public void AddVariable(string Name, int Value)
        {
            if(Variables.Keys.Contains(Name))
                throw new Exception("Variable under this name is already specified");

            Variables.Add(Name, new VariableStruct((IntPtr)ByteOpCodes.Count, 4));
            ByteOpCodes.AddRange(BitConverter.GetBytes(Value));
        }

        public VariableStruct GetVariableOffset(string Name)
        {
            if(Variables.Keys.Contains(Name))
            {
                return Variables[Name];
            }
            else
                throw new Exception("Variable under this name wasn't specified");
        }

        public void AddWriteToVariable(string name, int Value)
        {
            if(!Variables.Keys.Contains(name))
                throw new Exception("No variable specified");
            else
            {
                ByteOpCodes.AddRange(new byte[] { 0xC7, 0x05 });  //mov to absolute address, value
                Variables[name].VariableUsage.Add((IntPtr)ByteOpCodes.Count);
                ByteOpCodes.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });  //address
                ByteOpCodes.AddRange(BitConverter.GetBytes(Value));  //value
            }
        }
        #endregion

        #region DetoursCreationAndCode
        public void AddInjectionPoint(string Name, IntPtr InjectionPoint, byte OverridenBytes)
        {
            if(Detours.Keys.Contains(Name))
                throw new Exception("Detour under the name " + Name + " already exists!");


            Detours.Add(Name, new DetoursStruct(InjectionPoint, OverridenBytes, (IntPtr)ByteOpCodes.Count));
        }

        public void AddByteCode(byte[] code)
        {
            ByteOpCodes.AddRange(code);
        }

        public void CloseInjection(string Name)
        {
            if(Detours.Keys.Contains(Name))
            {
                var oldDetour = Detours[Name];
                Detours[Name] = new DetoursStruct(oldDetour.InjectionPoint, oldDetour.OverridenBytes, oldDetour.MasterContainerStartLocation, (IntPtr)ByteOpCodes.Count +1);
                ByteOpCodes.Add(0xE9);
                ByteOpCodes.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            }
            else
                throw new Exception("No detour available to close under this name: " + Name);
        }
        #endregion
    }
}
