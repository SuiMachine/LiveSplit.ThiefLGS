using System;
using System.Collections.Generic;
using System.Linq;

namespace SuisCodeInjection
{

    class CodeInjectionMasterContainer
    {
        /// <summary>
        /// A struct for each detour.
        /// </summary>
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

        /// <summary>
        /// A struct for each Variable
        /// </summary>
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

        /// <summary>
        /// Constructor for CodeInjectionMasterContainer object.
        /// </summary>
        public CodeInjectionMasterContainer()
        {
            ByteOpCodes = new List<byte>();
            Variables = new Dictionary<string, VariableStruct>();
            Detours = new Dictionary<string, DetoursStruct>();
        }

        /// <summary>
        /// Gets ByteCode representation.
        /// </summary>
        /// <returns>Returns byte op codes as array.</returns>
        public byte[] GetBytes()
        {
            return ByteOpCodes.ToArray();
        }

        #region VariablesHandling
        /// <summary>
        /// Adds a variable to allocate to CodeInjectionMasterContainer. WARNING: Make sure not to allocate variable inside the injection block!
        /// </summary>
        /// <param name="Name">Name of the variable, by which you can referance it.</param>
        /// <param name="Value">The default value to allocate.</param>
        public void AddVariable(string Name, int Value)
        {
            if(Variables.Keys.Contains(Name))
                throw new Exception("Variable under this name is already specified");

            Variables.Add(Name, new VariableStruct((IntPtr)ByteOpCodes.Count, 4));
            ByteOpCodes.AddRange(BitConverter.GetBytes(Value));
        }

        /// <summary>
        /// Gets the offset variable under specified name.
        /// </summary>
        /// <param name="Name">Name of the variable.</param>
        /// <returns>Returns VariableStruct.</returns>
        public VariableStruct GetVariableOffset(string Name)
        {
            if(Variables.Keys.Contains(Name))
            {
                return Variables[Name];
            }
            else
                throw new Exception("Variable under this name wasn't specified");
        }

        /// <summary>
        /// Adds an instruction to write to the variable.
        /// </summary>
        /// <param name="name">Name of the variable.</param>
        /// <param name="Value">What to write to the variable.</param>
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

        public void AddIncrementValue(string name, byte Value)
        {
            if(!Variables.Keys.Contains(name))
                throw new Exception("No variable specified");
            else
            {
                ByteOpCodes.AddRange(new byte[] { 0x83, 0x05 });  //mov to absolute address, value
                Variables[name].VariableUsage.Add((IntPtr)ByteOpCodes.Count);
                ByteOpCodes.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });  //address
                ByteOpCodes.Add(Value);  //value
            }
        }
        #endregion

        #region DetoursCreationAndCode
        /// <summary>
        /// Adds an injection point.
        /// </summary>
        /// <param name="Name">Name of the injection, by which it can be referenced.</param>
        /// <param name="InjectionPoint">Adress where to create a detour.</param>
        /// <param name="OverridenBytes">Amount of bytes (min 5).</param>
        public void AddInjectionPoint(string Name, IntPtr InjectionPoint, byte OverridenBytes)
        {
            if(Detours.Keys.Contains(Name))
                throw new Exception("Detour under the name " + Name + " already exists!");
            if(OverridenBytes < 5)
                throw new Exception("To create a detour, at least 5 bytes have to be overriden (1 for JMP and 4 for offset).");

            Detours.Add(Name, new DetoursStruct(InjectionPoint, OverridenBytes, (IntPtr)ByteOpCodes.Count));
        }

        /// <summary>
        /// Adds raw byte code.
        /// </summary>
        /// <param name="code">Assembly code as byte OP code.</param>
        public void AddByteCode(byte[] code)
        {
            ByteOpCodes.AddRange(code);
        }

        /// <summary>
        /// Closed the injection point, creating the jump back to a spot from which the injection started.
        /// </summary>
        /// <param name="Name">Name of the injection point.</param>
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
