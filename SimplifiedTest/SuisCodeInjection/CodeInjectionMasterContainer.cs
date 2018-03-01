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
            public Int32 InjectionPoint { get; private set; }
            public byte OverridenBytes { get; set; }
            public Int32 MasterContainerStartLocation { get; private set; }
            public Int32 MasterContainerJmpBackLocation { get; set; }

            public DetoursStruct(Int32 InjectionPoint, byte OverridenBytes, Int32 MasterContainerStartLocation)
            {
                this.InjectionPoint = InjectionPoint;
                this.OverridenBytes = OverridenBytes;
                this.MasterContainerStartLocation = MasterContainerStartLocation;
                this.MasterContainerJmpBackLocation = 0;
            }


            public DetoursStruct(Int32 InjectionPoint, byte OverridenBytes, Int32 MasterContainerStartLocation, Int32 MasterContainerJmpBackLocation)
            {
                this.InjectionPoint = InjectionPoint;
                this.OverridenBytes = OverridenBytes;
                this.MasterContainerStartLocation = MasterContainerStartLocation;
                this.MasterContainerJmpBackLocation = MasterContainerJmpBackLocation;
            }
        }

        public struct VariableStruct
        {
            public Int32 VariableLocation { get; private set; }
            public int VariableLenght { get; private set; }
            public List<Int32> VariableUsage { get; private set; }

            public VariableStruct(Int32 VariableLocation, int VariableLenght)
            {
                this.VariableLocation = VariableLocation;
                this.VariableLenght = VariableLenght;
                VariableUsage = new List<Int32>();
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

            Variables.Add(Name, new VariableStruct(ByteOpCodes.Count, 4));
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
                Variables[name].VariableUsage.Add(ByteOpCodes.Count);
                ByteOpCodes.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });  //address
                ByteOpCodes.AddRange(BitConverter.GetBytes(Value));  //value
            }
        }
        #endregion

        #region DetoursCreationAndCode
        public void AddInjectionPoint(string Name, Int32 InjectionPoint, byte OverridenBytes)
        {
            if(Detours.Keys.Contains(Name))
                throw new Exception("Detour under the name " + Name + " already exists!");


            Detours.Add(Name, new DetoursStruct(InjectionPoint, OverridenBytes, ByteOpCodes.Count));
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
                Detours[Name] = new DetoursStruct(oldDetour.InjectionPoint, oldDetour.OverridenBytes, oldDetour.MasterContainerStartLocation, ByteOpCodes.Count +1);
                ByteOpCodes.Add(0xE9);
                ByteOpCodes.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            }
            else
                throw new Exception("No detour available to close under this name: " + Name);
        }
        #endregion
    }
}
