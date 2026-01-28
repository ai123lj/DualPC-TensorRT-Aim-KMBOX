using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MWModle
{
    public class LibMWMedia
    {
        //////////////////////////////////////////////////////////////////////////////////////////
// video renderer
#if DEBUG
        [DllImport("LibMWMediad.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWMedia.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWCreateD3DRenderer(int cx, int cy, UInt32 dwFourcc,bool bReverse, bool bMirror, IntPtr hWnd);


#if DEBUG
        [DllImport("LibMWMediad.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWMedia.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern void MWDestroyD3DRenderer(IntPtr hRenderer);


#if DEBUG
        [DllImport("LibMWMediad.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWMedia.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern int MWD3DRendererPushFrame(IntPtr hRenderer, byte[] pbyBuffer, Int32 cbStride);


        //////////////////////////////////////////////////////////////////////////////////////////
        // audio renderer
#if DEBUG
        [DllImport("LibMWMediad.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWMedia.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWCreateDSoundRenderer(int nSamplesPerSec, int nChannels, int nSamplesPerFrame, int nBufferFrameCount);

#if DEBUG
        [DllImport("LibMWMediad.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWMedia.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWDestroyDSoundRenderer(IntPtr hRenderer);

#if DEBUG
        [DllImport("LibMWMediad.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWMedia.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern int MWDSoundRendererPushFrame(IntPtr hRenderer, byte[] pbFrame, UInt32 cbFram);

    }
}
