using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using LiveSplit.ComponentUtil;

namespace SuisCodeInjection
{
    public enum CodeInjectionResult
    {
        Success,
        Failure,
        ProcessNotFound,
        FailedToOpenProcess,
        FailedToGetProcAddress,
        FailedToVirtualAlloc,
        FailedToWriteInstructionToMemory
    }

    class CodeInjection
    {
        #region DLLImports
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern Int32 OpenProcess(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int CloseHandle(Int32 hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern Int32 GetProcAddress(Int32 hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern Int32 GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern Int32 VirtualAllocEx(Int32 hProcess, Int32 lpAddress, Int32 dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int WriteProcessMemory(Int32 hProcess, Int32 lpBaseAddress, byte[] buffer, uint size, int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(Int32 hProcess, Int32 lpBaseAddress, byte[] buffer, uint size, ref int lpNumberOfBytesToRead);
        #endregion

        /// <summary>
        /// Result of code injection
        /// </summary>
        public CodeInjectionResult Result { get; }
        private Process process;
        private IntPtr alocAdress;

        private CodeInjectionMasterContainer Container;

        /// <summary>
        /// Performs a Code Injection.
        /// </summary>
        /// <param name="process">Process you want to inject the code to.</param>
        /// <param name="Container">CodeInjectionMasterContainer to inject. Create the object before the injection!</param>
        public CodeInjection(Process process, CodeInjectionMasterContainer Container)
        {
            this.process = process;
            this.Container = Container;

            Validate();

            Result = Inject();
        }

        /// <summary>
        /// Checks if all injections have been closed.
        /// </summary>
        private void Validate()
        {
            foreach(var element in Container.Detours)
            {
                if(element.Value.MasterContainerJmpBackLocation == (IntPtr)0)
                    throw new Exception("Injection " + element.Key + " not closed!");
            }
        }

        /// <summary>
        /// Used to get address of an allocated memory inside of the process.
        /// </summary>
        /// <returns>Address of allocated memory as Unsigned Int (uint).</returns>
        public uint GetAllocationAddress()
        {
            return (uint)alocAdress;
        }

        private CodeInjectionResult Inject()
        {
            if(process.Id == 0)
            {
                return CodeInjectionResult.ProcessNotFound;
            }

            byte[] fullInjectedCode = Container.GetBytes();

            IntPtr address = ExtensionMethods.AllocateMemory(process, 512);
            if(address == null)
            {
                return CodeInjectionResult.FailedToVirtualAlloc;
            }
            alocAdress = address;

            if(!ExtensionMethods.WriteBytes(process, address, fullInjectedCode))
            {
                return CodeInjectionResult.FailedToWriteInstructionToMemory;

            }

            //Replace temp return addresses with proper ones
            foreach(var Detour in Container.Detours)
            {
                IntPtr LocationOfReturnJump = IntPtr.Add(alocAdress, Detour.Value.MasterContainerJmpBackLocation.ToInt32());
                byte[] backAddy = BitConverter.GetBytes(Detour.Value.InjectionPoint.ToInt32() + Detour.Value.OverridenBytes - (LocationOfReturnJump.ToInt32() + 4));
                if(!ExtensionMethods.WriteBytes(process, LocationOfReturnJump, backAddy))
                {
                    return CodeInjectionResult.FailedToWriteInstructionToMemory;

                }
            }

            //Replace temp addresses with actual position of a variable
            foreach(var variable in Container.Variables)
            {
                foreach(var variableUse in variable.Value.VariableUsage)
                {
                    if(!ExtensionMethods.WriteBytes(process, IntPtr.Add(alocAdress, variableUse.ToInt32()), BitConverter.GetBytes(alocAdress.ToInt32() + variable.Value.VariableLocation.ToInt32())))
                        return CodeInjectionResult.FailedToWriteInstructionToMemory;
                }
            }

            foreach(var Detour in Container.Detours)
            {
                byte[] DetourInstruction = new byte[Detour.Value.OverridenBytes];
                DetourInstruction[0] = 0xE9; //Jmp
                for(int i=5; i<Detour.Value.OverridenBytes; i++)
                {
                    DetourInstruction[i] = 0x90; //Nop
                }

                IntPtr LocationOfDetourJMP = Detour.Value.InjectionPoint;
                byte[] backAddy = BitConverter.GetBytes(alocAdress.ToInt32() + Detour.Value.MasterContainerStartLocation.ToInt32() - (Detour.Value.InjectionPoint.ToInt32() +5));
                backAddy.CopyTo(DetourInstruction, 1);
                if(!ExtensionMethods.WriteBytes(process, LocationOfDetourJMP, DetourInstruction))
                    return CodeInjectionResult.FailedToWriteInstructionToMemory;
            }
            
            return CodeInjectionResult.Success;
        }

        /// <summary>
        /// Calculates and returns the Variable adress.
        /// </summary>
        /// <param name="Name">The name of the variable to return.</param>
        /// <param name="Absolute">Specifies whatever the function should return relative adress or absolute one.</param>
        /// <returns>Adress of the variable in process' memory</returns>
        public IntPtr GetVariableAdress(string Name, bool Absolute = false)
        {
            if(!Container.Variables.Keys.Contains(Name))
                throw new Exception("No variable to access!");

            IntPtr offset = Container.Variables[Name].VariableLocation;
            if(Absolute)
                return IntPtr.Add(alocAdress, offset.ToInt32());
            else
                return IntPtr.Add(alocAdress, offset.ToInt32()) - process.MainModule.BaseAddress.ToInt32();
        }

        #region StaticStuffToHelp
        /// <summary>
        /// Converts bytes as string to byte array.
        /// </summary>
        /// <param name="bytesAsString">Bytes as string you want to convert to an array, they should be provided without "0x". Whitespaces are allowed. Throws an exception, on error.</param>
        /// <returns>Array of bytes.</returns>
        public static byte[] StringBytesToArray(string bytesAsString)
        {
            bytesAsString = bytesAsString.Replace(" ", "");
            
            if (bytesAsString.Length % 2 != 0)
            {
                throw new Exception("Provided string to conversion does not contain proper amount of bits.");
            }

            byte[] outArray = new byte[bytesAsString.Length / 2];
            for (int i = 0; i < outArray.Length; i++)
            {
                byte temp = (byte)(CharToByte(bytesAsString[i * 2])*16 + CharToByte(bytesAsString[i * 2 +1]));
                outArray[i] = temp;
            }
            return outArray;
        }

        private static byte CharToByte(char c)
        {
            c = char.ToLower(c);
            switch (c)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'a': return 10;
                case 'b': return 11;
                case 'c': return 12;
                case 'd': return 13;
                case 'e': return 14;
                case 'f': return 15;
            }
            throw new FormatException("Invalid char in a string");
        }
        #endregion
    }
}
