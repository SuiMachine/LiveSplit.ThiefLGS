using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

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

        static readonly Int32 INTPTR_ZERO = (Int32)0;
        /// <summary>
        /// Result of code injection
        /// </summary>
        public CodeInjectionResult Result { get; }
        private Process process;
        private Int32 alocAdress;

        private CodeInjectionMasterContainer Container;

        public CodeInjection(Process process, CodeInjectionMasterContainer Container)
        {
            this.process = process;
            this.Container = Container;

            Validate();

            Result = Inject();
        }

        private void Validate()
        {
            foreach(var element in Container.Detours)
            {
                if(element.Value.MasterContainerJmpBackLocation == 0)
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

            uint pID = (uint)process.Id;

            Int32 procHandle = OpenProcess((0x2 | 0x8 | 0x10 | 0x20 | 0x400), 1, pID);
            if(procHandle == INTPTR_ZERO)
            {
                return CodeInjectionResult.FailedToOpenProcess;
            }

            Int32 lpLLAddress = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if(lpLLAddress == INTPTR_ZERO)
            {
                return CodeInjectionResult.FailedToGetProcAddress;
            }

            uint lenght = (uint)Container.GetBytes().Length;

            byte[] fullInjectedCode = Container.GetBytes();

            Int32 lpAddress = VirtualAllocEx(procHandle, INTPTR_ZERO, (Int32)512, (0x1000 | 0x2000), 0X40);      //could try taking alloc less memory?
            alocAdress = lpAddress;
            Debug.WriteLine("Allocation address: " + alocAdress.ToString("X4"));

            if (lpAddress == INTPTR_ZERO)
            {
                return CodeInjectionResult.FailedToVirtualAlloc;
            }

            if (WriteProcessMemory(procHandle, lpAddress, fullInjectedCode, (uint)fullInjectedCode.Length, 0) == 0)
            {
                return CodeInjectionResult.FailedToWriteInstructionToMemory;
            }

            //Replace temp return addresses with proper ones
            foreach(var Detour in Container.Detours)
            {
                int LocationOfReturnJump = alocAdress+Detour.Value.MasterContainerJmpBackLocation;
                byte[] backAddy = BitConverter.GetBytes(Detour.Value.InjectionPoint + Detour.Value.OverridenBytes - (LocationOfReturnJump + 4));
                if(WriteProcessMemory(procHandle, LocationOfReturnJump, backAddy, 4, 0) == 0)
                {
                    return CodeInjectionResult.FailedToWriteInstructionToMemory;
                }
            }

            //Replace temp addresses with actual position of a variable
            foreach(var variable in Container.Variables)
            {
                foreach(var variableUse in variable.Value.VariableUsage)
                {
                    if(WriteProcessMemory(procHandle, alocAdress + variableUse, BitConverter.GetBytes(alocAdress + variable.Value.VariableLocation), 4, 0) == 0)
                    {
                        return CodeInjectionResult.FailedToWriteInstructionToMemory;
                    }
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

                int LocationOfDetourJMP = Detour.Value.InjectionPoint;
                byte[] backAddy = BitConverter.GetBytes(alocAdress + Detour.Value.MasterContainerStartLocation - (Detour.Value.InjectionPoint +5));
                backAddy.CopyTo(DetourInstruction, 1);
                if(WriteProcessMemory(procHandle, LocationOfDetourJMP, DetourInstruction, Detour.Value.OverridenBytes, 0) == 0)
                {
                    return CodeInjectionResult.FailedToWriteInstructionToMemory;
                }
            }

            CloseHandle(procHandle);
            
            return CodeInjectionResult.Success;
        }

        public int getVariableAdress(string Name, bool Absolute = false)
        {
            if(!Container.Variables.Keys.Contains(Name))
                throw new Exception("No variable to access!");

            Int32 offset = Container.Variables[Name].VariableLocation;
            if(Absolute)
                return alocAdress + offset;
            else
                return process.MainModule.BaseAddress.ToInt32() - alocAdress + offset;
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
