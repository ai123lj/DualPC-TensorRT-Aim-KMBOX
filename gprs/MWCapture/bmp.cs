using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace MWModle
{
    class CBmp
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct BITMAPFILEHEADER_T
        {
            public UInt16 bfType;
            public UInt32 bfSize;
            public UInt16 bfReserved1;
            public UInt16 bfReserved2;
            public UInt32 bfOffBits;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct BITMAPINFOHEADER_T
        {
            public UInt32 biSize;
            public UInt32 biWidth;
            public UInt32 biHeight;
            public UInt16 biPlanes;
            public UInt16 biBitCount;
            public UInt32 biCompression;
            public UInt32 biSizeImage;
            public UInt32 biXPelsPerMeter;
            public UInt32 biYPelsPerMeter;
            public UInt32 biClrUsed;
            public UInt32 biClrImportant;
        }
        static public Boolean write_bmp_head(FileStream file, UInt32 len, int width, int height)
        {
            BITMAPFILEHEADER_T file_head = new BITMAPFILEHEADER_T();
            file_head.bfType = 0x4D42;
            file_head.bfSize = (UInt32)len + 54;
            file_head.bfReserved1 = 0;
            file_head.bfReserved2 = 0;
            file_head.bfOffBits = 54;

            BITMAPINFOHEADER_T info_head = new BITMAPINFOHEADER_T();

            info_head.biSize = 40;
            info_head.biWidth = (UInt32)width;
            info_head.biHeight = (UInt32)height;
            info_head.biPlanes = 1;
            info_head.biBitCount = 24;
            info_head.biCompression = 0;
            info_head.biSizeImage = 0;
            info_head.biXPelsPerMeter = 0;
            info_head.biYPelsPerMeter = 0;
            info_head.biClrUsed = 0;
            info_head.biClrImportant = 0;



            int written;
            IntPtr p_file_head = GCHandle.Alloc(file_head, GCHandleType.Pinned).AddrOfPinnedObject();
            Libkernel32.WriteFile(file.Handle, p_file_head, 14, out written, IntPtr.Zero);
            IntPtr p_info_head = GCHandle.Alloc(info_head, GCHandleType.Pinned).AddrOfPinnedObject();
            Libkernel32.WriteFile(file.Handle, p_info_head, 40, out written, IntPtr.Zero);
            return true;
        }
        static public Boolean create_bmp_byte(string file_name, Byte[] frame, UInt32 len, int width, int height)
        {
            FileStream file = new FileStream(file_name, FileMode.Create, FileAccess.Write);
            write_bmp_head(file, len, width, height);
            file.Write(frame, 0, (int)len);
            return true;
        }
        static public Boolean create_bmp_ptr(string file_name, IntPtr frame, UInt32 len, int width, int height)
        {
            FileStream file = new FileStream(file_name, FileMode.Create, FileAccess.Write);
            write_bmp_head(file, len, width, height);
            int written;
            Libkernel32.WriteFile(file.Handle, frame, (Int32)len, out written, IntPtr.Zero);
            return true;
        }

    }
}