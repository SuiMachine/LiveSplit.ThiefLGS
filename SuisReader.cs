using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ReworkedTrainer32bit
{
    public class SuisReader
    {
        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        [DllImport("kernel32")]
        private static extern int OpenProcess(int AccessType, int InheritHandle, int ProcessId);
        [DllImport("kernel32")]
        private static extern int CloseHandle(int Handle);


        [DllImport("kernel32", EntryPoint = "ReadProcessMemory")]
        private static extern byte ReadProcessMemoryByte(int Handle, int Address, ref byte Value, int Size, ref int BytesRead);
        [DllImport("kernel32", EntryPoint = "ReadProcessMemory")]
        private static extern int ReadProcessMemoryInteger(int Handle, int Address, ref int Value, int Size, ref int BytesRead);
        [DllImport("kernel32", EntryPoint = "ReadProcessMemory")]
        private static extern float ReadProcessMemoryFloat(int Handle, int Address, ref float Value, int Size, ref int BytesRead);
        [DllImport("kernel32", EntryPoint = "ReadProcessMemory")]
        private static extern char ReadProcessMemoryChar(int Handle, int Address, ref char Value, int Size, ref int BytesRead);



        private Process GameProcess { get; set; }
        private int Address { get; set; }
        private int Handle { get; set; }


        public SuisReader(Process GameProcess, int Address, bool AutomaticallyGetHandle = true)
        {
            this.GameProcess = GameProcess;
            this.Address = Address;
            if(GameProcess != null && !GameProcess.HasExited)
            {
                if(AutomaticallyGetHandle)
                    GetHandle();
            }
            else
            {
                GameProcess = null;
                this.Handle = 0;
            }
        }

        public void GetHandle()
        {
            try
            {
                Handle = OpenProcess(PROCESS_ALL_ACCESS, 0, GameProcess.Id);
                if(Handle == 0)
                    return;
                else
                    return;
            }
            catch(Exception e)
            {
                Debug.WriteLine("[Trainer] Exception: " + e.ToString());
                return;
            }
        }

        public void CloseHandle()
        {
            try
            {
                CloseHandle(this.Handle);
                Handle = 0;
            }
            catch(Exception e)
            {
                Debug.WriteLine("[Trainer] Exception: " + e.ToString());
                Handle = 0;
            }
        }

        public bool ReadBool()
        {
            byte Value = 0;
            checked
            {
                try
                {
                    int Bytes = 0;
                    if(Handle != 0)
                    {
                        ReadProcessMemoryByte(Handle, Address, ref Value, 2, ref Bytes);
                    }
                }
                catch
                { }
            }
            return Value != 0;
        }

        public byte ReadByte()
        {
            byte Value = 0;
            checked
            {
                try
                {
                    int Bytes = 0;
                    if(Handle != 0)
                    {
                        ReadProcessMemoryByte(Handle, Address, ref Value, 2, ref Bytes);
                    }
                }
                catch
                { }
            }
            return Value;
        }

        public int ReadInteger()
        {
            int Value = 0;
            checked
            {
                try
                {
                    int Bytes = 0;
                    if(Handle != 0)
                    {
                        ReadProcessMemoryInteger(Handle, Address, ref Value, 4, ref Bytes);
                    }
                }
                catch
                { }
            }
            return Value;
        }

        public float ReadFloat()
        {
            float Value = 0;
            checked
            {
                try
                {
                    int Bytes = 0;
                    if(Handle != 0)
                    {
                        ReadProcessMemoryFloat((int)Handle, Address, ref Value, 4, ref Bytes);
                    }
                }
                catch
                { }
            }
            return Value;
        }

        public string ReadString(int Lenght)
        {
            string StringThatWeRead = "";
            checked
            {
                try
                {
                    for(int i = 0; i < Lenght; i++)
                    {
                        int Bytes = 0;
                        char Character = '\0';
                        ReadProcessMemoryChar(Handle, Address+i, ref Character, 2, ref Bytes);
                        if(Character == '\0')
                            break;
                        StringThatWeRead += Character;
                    }
                }
                catch
                { }
            }
            return StringThatWeRead;
        }
        public static byte ReadPointerByte(string EXENAME, int Pointer, int[] Offset)
        {
            byte Value = 0;
            checked
            {
                try
                {
                    Process[] Proc = Process.GetProcessesByName(EXENAME);
                    if(Proc.Length != 0)
                    {
                        int Bytes = 0;
                        int Handle = OpenProcess(PROCESS_ALL_ACCESS, 0, Proc[0].Id);
                        if(Handle != 0)
                        {
                            foreach(int i in Offset)
                            {
                                ReadProcessMemoryInteger((int)Handle, Pointer, ref Pointer, 4, ref Bytes);
                                Pointer += i;
                            }
                            ReadProcessMemoryByte((int)Handle, Pointer, ref Value, 2, ref Bytes);
                            CloseHandle(Handle);
                        }
                    }
                }
                catch
                { }
            }
            return Value;
        }
        public static int ReadPointerInteger(string EXENAME, int Pointer, int[] Offset)
        {
            int Value = 0;
            checked
            {
                try
                {
                    Process[] Proc = Process.GetProcessesByName(EXENAME);
                    if(Proc.Length != 0)
                    {
                        int Bytes = 0;
                        int Handle = OpenProcess(PROCESS_ALL_ACCESS, 0, Proc[0].Id);
                        if(Handle != 0)
                        {
                            foreach(int i in Offset)
                            {
                                ReadProcessMemoryInteger((int)Handle, Pointer, ref Pointer, 4, ref Bytes);
                                Pointer += i;
                            }
                            ReadProcessMemoryInteger((int)Handle, Pointer, ref Value, 4, ref Bytes);
                            CloseHandle(Handle);
                        }
                    }
                }
                catch
                { }
            }
            return Value;
        }
        public static float ReadPointerFloat(string EXENAME, int Pointer, int[] Offset)
        {
            float Value = 0;
            checked
            {
                try
                {
                    Process[] Proc = Process.GetProcessesByName(EXENAME);
                    if(Proc.Length != 0)
                    {
                        int Bytes = 0;
                        int Handle = OpenProcess(PROCESS_ALL_ACCESS, 0, Proc[0].Id);
                        if(Handle != 0)
                        {
                            foreach(int i in Offset)
                            {
                                ReadProcessMemoryInteger((int)Handle, Pointer, ref Pointer, 4, ref Bytes);
                                Pointer += i;
                            }
                            ReadProcessMemoryFloat((int)Handle, Pointer, ref Value, 4, ref Bytes);
                            CloseHandle(Handle);
                        }
                    }
                }
                catch
                { }
            }
            return Value;
        }
    }
}