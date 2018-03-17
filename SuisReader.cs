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
        private const int PROCESS_WM_READ = 0x0010;
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
        [DllImport("kernel32", EntryPoint = "ReadProcessMemory")]
        private static extern bool ReadProcessMemory(int Handle, int Address, byte[] lpBuffer, int Size, ref int BytesRead);

        private Process GameProcess { get; set; }
        private int Address { get; set; }
        private int Handle { get; set; }

        public enum StringType
        {
            UTF8,
            UTF16,
            Unicode,
            ASCII
        }

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
                Handle = OpenProcess(PROCESS_WM_READ, 0, GameProcess.Id);
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

        public string ReadString(int Lenght, StringType stringType)
        {
            byte[] StringThatWeRead = new byte[Lenght];
            checked
            {
                try
                {
                    int Bytes = 0;
                    ReadProcessMemory(Handle, Address, StringThatWeRead, Lenght, ref Bytes);
                    string ToReturn = "";
                    switch(stringType)
                    {
                        case (StringType.UTF8): ToReturn = Encoding.UTF8.GetString(StringThatWeRead); break;
                        case (StringType.Unicode): ToReturn = Encoding.Unicode.GetString(StringThatWeRead); break;
                        case (StringType.UTF16): ToReturn = Encoding.UTF32.GetString(StringThatWeRead); break;
                        case (StringType.ASCII): ToReturn = Encoding.ASCII.GetString(StringThatWeRead); break;
                    }

                    if(ToReturn.StartsWith("\0"))
                        return "";
                    else
                        return ToReturn.TrimEnd('\0');

                }
                catch
                {
                    return "";
                }
            }
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