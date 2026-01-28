using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MWModle
{
    class Libkernel32
    {
        public const uint INFINITE = 0xFFFFFFFF;

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateEvent(IntPtr lpEventAttributes, int bManualReset, int bInitialState, IntPtr lpName);

        [DllImport("kernel32.dll")]
        public static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll")]
        public static extern UInt32 WaitForMultipleObjects(UInt32 nCount, IntPtr[] hHandles, int bWaitAll, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern int SetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll")]
        public static extern long GetTickCount();

        [DllImport("kernel32.dll")]
        public static extern bool WriteFile(IntPtr hFile, IntPtr lpBuffer, int NumberOfBytesToWrite, out int lpNumberOfBytesWritten, IntPtr lpOverlapped);
    }
}
