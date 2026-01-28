using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MWModle
{
    public class MWFOURCC
    {
        static public UInt32 MWFOURCC_UNK = MK_MWFOURCC('U', 'N', 'K', 'N');

        // 8bits grey
        static public UInt32 MWFOURCC_GREY = MK_MWFOURCC('G', 'R', 'E', 'Y');					// Y0, Y1, Y2, ...
        static public UInt32 MWFOURCC_Y800 = MK_MWFOURCC('Y', '8', '0', '0');					// = GREY
        static public UInt32 MWFOURCC_Y8 = MK_MWFOURCC('Y', '8', ' ', ' ');					// = GREY

        // 16bits grey
        static public UInt32 MWFOURCC_Y16 = MK_MWFOURCC('Y', '1', '6', ' ');					// Y0, Y1, Y2

        // RGB 15-32bits
        static public UInt32 MWFOURCC_RGB15 = MK_MWFOURCC('R', 'G', 'B', '5');					// R0, G0, B0, A0, ...
        static public UInt32 MWFOURCC_RGB16 = MK_MWFOURCC('R', 'G', 'B', '6');					// R0, G0, B0, R1, ...
        static public UInt32 MWFOURCC_RGB24 = MK_MWFOURCC('R', 'G', 'B', ' ');					// R0, G0, B0, R1, ...
        static public UInt32 MWFOURCC_RGBA = MK_MWFOURCC('R', 'G', 'B', 'A');					// R0, G0, B0, A0, R1, ...
        static public UInt32 MWFOURCC_ARGB = MK_MWFOURCC('A', 'R', 'G', 'B');					// A0, R0, G0, B0, A1, ...

        static public UInt32 MWFOURCC_BGR15 = MK_MWFOURCC('B', 'G', 'R', '5');					// B0, G0, R0, A0, ...
        static public UInt32 MWFOURCC_BGR16 = MK_MWFOURCC('B', 'G', 'R', '6');					// B0, G0, R0, B1, ...
        static public UInt32 MWFOURCC_BGR24 = MK_MWFOURCC('B', 'G', 'R', ' ');					// B0, G0, R0, B1, ...
        static public UInt32 MWFOURCC_BGRA = MK_MWFOURCC('B', 'G', 'R', 'A');					// B0, G0, R0, A0, B1, ...
        static public UInt32 MWFOURCC_ABGR = MK_MWFOURCC('A', 'B', 'G', 'R');					// A0, B0, G0, R0, A1, ...

        // Packed YUV 16bits
        static public UInt32 MWFOURCC_YUY2 = MK_MWFOURCC('Y', 'U', 'Y', '2');					// Y0, U01, Y1, V01, ...
        static public UInt32 MWFOURCC_YUYV = MK_MWFOURCC('Y', 'U', 'Y', 'V');					// = YUY2
        static public UInt32 MWFOURCC_UYVY = MK_MWFOURCC('U', 'Y', 'V', 'Y');					// U01, Y0, V01, Y1, ...

        static public UInt32 MWFOURCC_YVYU = MK_MWFOURCC('Y', 'V', 'Y', 'U');					// Y0, V01, Y1, U01, ...
        static public UInt32 MWFOURCC_VYUY = MK_MWFOURCC('V', 'Y', 'U', 'Y');					// V01, Y0, U01, Y1, ...

        // Planar YUV 12bits
        static public UInt32 MWFOURCC_I420 = MK_MWFOURCC('I', '4', '2', '0');					// = IYUV (Y, U, V)
        static public UInt32 MWFOURCC_IYUV = MK_MWFOURCC('I', 'Y', 'U', 'V');					// = I420 (Y, U, V)
        static public UInt32 MWFOURCC_NV12 = MK_MWFOURCC('N', 'V', '1', '2');					// Y Plane, UV Plane

        static public UInt32 MWFOURCC_YV12 = MK_MWFOURCC('Y', 'V', '1', '2');					// Y Plane, V Plane, U Plane
        static public UInt32 MWFOURCC_NV21 = MK_MWFOURCC('N', 'V', '2', '1');					// Y Plane, VU Plane

        // Packed YUV 24bits
        static public UInt32 MWFOURCC_IYU2 = MK_MWFOURCC('I', 'Y', 'U', '2');					// U0, Y0, V0, U1, Y1, V1, ...
        static public UInt32 MWFOURCC_V308 = MK_MWFOURCC('v', '3', '0', '8');					// V0, Y0, U0, V1, Y1, U1, ...

        // Packed YUV 32bits
        static public UInt32 MWFOURCC_AYUV = MK_MWFOURCC('A', 'Y', 'U', 'V');					// A0, Y0, U0, V0, ...
        static public UInt32 MWFOURCC_UYVA = MK_MWFOURCC('U', 'Y', 'V', 'A');					// U0, Y0, V0, A0, U1, Y1, ...
        static public UInt32 MWFOURCC_V408 = MK_MWFOURCC('v', '4', '0', '8');					// = MWFOURCC_UYVA
        static public UInt32 MWFOURCC_VYUA = MK_MWFOURCC('V', 'Y', 'U', 'A');					// V0, Y0, U0, A0, V1, Y1, ...

        // Packed YUV 30bits deep color
        static public UInt32 MWFOURCC_Y410 = MK_MWFOURCC('Y', '4', '1', '0');					// U0, Y0, V0, A0, ...
        static public UInt32 MWFOURCC_V410 = MK_MWFOURCC('v', '4', '1', '0');					// A0, U0, Y0, V0, ...

        // Packed RGB 30bits deep color
        static public UInt32 MWFOURCC_RGB10 = MK_MWFOURCC('R', 'G', '1', '0');					// R0, G0, B0, A0, ...
        static public UInt32 MWFOURCC_BGR10 = MK_MWFOURCC('B', 'G', '1', '0');					// B0, G0, R0, A0, ...




        static private UInt32 MK_MWFOURCC(char ch0, char ch1, char ch2, char ch3)
        {

            return (UInt32)ch0 | ((UInt32)ch1 << 8) | ((UInt32)ch2 << 16) | ((UInt32)ch3 << 24);
        }

        static public bool FOURCC_IsRGB(UInt32 dwFourcc)
        {

            if (dwFourcc == MWFOURCC_RGB15 ||
                    dwFourcc == MWFOURCC_BGR15 ||
                    dwFourcc == MWFOURCC_RGB16 ||
                    dwFourcc == MWFOURCC_BGR16 ||
                    dwFourcc == MWFOURCC_RGB24 ||
                    dwFourcc == MWFOURCC_BGR24 ||
                    dwFourcc == MWFOURCC_RGBA ||
                    dwFourcc == MWFOURCC_BGRA ||
                    dwFourcc == MWFOURCC_ARGB ||
                    dwFourcc == MWFOURCC_ABGR ||
                    dwFourcc == MWFOURCC_RGB10 ||
                    dwFourcc == MWFOURCC_BGR10)
                return true;


            return false;

        }


        static public bool FOURCC_IsPacked(UInt32 dwFourcc)
        {
            if (dwFourcc == MWFOURCC_NV12 ||
                dwFourcc == MWFOURCC_NV21 ||
                dwFourcc == MWFOURCC_YV12 ||
                dwFourcc == MWFOURCC_IYUV ||
                dwFourcc == MWFOURCC_I420)
                return false;

            return true;

        }


        static public int FOURCC_GetBpp(UInt32 dwFourcc)
        {

            if (dwFourcc == MWFOURCC_GREY ||
                dwFourcc == MWFOURCC_Y800 ||
                dwFourcc == MWFOURCC_Y8)
                return 8;
            else if (dwFourcc == MWFOURCC_I420 ||
                     dwFourcc == MWFOURCC_IYUV ||
                     dwFourcc == MWFOURCC_YV12 ||
                     dwFourcc == MWFOURCC_NV12 ||
                     dwFourcc == MWFOURCC_NV21)
                return 12;
            else if (dwFourcc == MWFOURCC_Y16 ||
                    dwFourcc == MWFOURCC_RGB15 ||
                    dwFourcc == MWFOURCC_BGR15 ||
                    dwFourcc == MWFOURCC_RGB16 ||
                    dwFourcc == MWFOURCC_BGR16 ||
                    dwFourcc == MWFOURCC_YUY2 ||
                    dwFourcc == MWFOURCC_YUYV ||
                    dwFourcc == MWFOURCC_UYVY ||
                    dwFourcc == MWFOURCC_YVYU ||
                    dwFourcc == MWFOURCC_VYUY)
                return 16;
            else if (dwFourcc == MWFOURCC_IYU2 ||
                    dwFourcc == MWFOURCC_V308 ||
                    dwFourcc == MWFOURCC_RGB24 ||
                    dwFourcc == MWFOURCC_BGR24)
                return 24;
            else if (dwFourcc == MWFOURCC_AYUV ||
                    dwFourcc == MWFOURCC_UYVA ||
                    dwFourcc == MWFOURCC_V408 ||
                    dwFourcc == MWFOURCC_VYUA ||
                    dwFourcc == MWFOURCC_RGBA ||
                    dwFourcc == MWFOURCC_BGRA ||
                    dwFourcc == MWFOURCC_ARGB ||
                    dwFourcc == MWFOURCC_ABGR ||
                    dwFourcc == MWFOURCC_Y410 ||
                    dwFourcc == MWFOURCC_V410 ||
                    dwFourcc == MWFOURCC_RGB10 ||
                    dwFourcc == MWFOURCC_BGR10)
                return 32;
            else
                return 0;

        }

        static public UInt32 FOURCC_CalcMinStride(UInt32 dwFOURCC, int cx, UInt32 dwAlign)
        {
            bool bPacked = FOURCC_IsPacked(dwFOURCC);

            UInt32 cbLine;

            if (bPacked)
            {
                int nBpp = FOURCC_GetBpp(dwFOURCC);
                cbLine = (UInt32)(cx * nBpp) / 8;
            }
            else
                cbLine = (UInt32)cx;

            return (cbLine + dwAlign - 1) & ~(dwAlign - 1);
        }

        static public UInt32 FOURCC_CalcImageSize(UInt32 dwFOURCC, int cx, int cy, UInt32 cbStride)
        {
            bool bPacked = FOURCC_IsPacked(dwFOURCC);

            if (bPacked)
            {
                int nBpp = FOURCC_GetBpp(dwFOURCC);
                UInt32 cbLine = (UInt32)(cx * nBpp) / 8;
                if (cbStride < cbLine)
                    return 0;

                return cbStride * (UInt32)cy;
            }
            else
            {
                if (cbStride < (UInt32)cx)
                    return 0;

                if (dwFOURCC == MWFOURCC_NV12 ||
                    dwFOURCC == MWFOURCC_NV21 ||
                    dwFOURCC == MWFOURCC_YV12 ||
                    dwFOURCC == MWFOURCC_IYUV ||
                    dwFOURCC == MWFOURCC_I420)
                {
                    if ((cbStride & 0x01) == 1 || (cy & 0x01) == 1)
                        return 0;
                    return cbStride * (UInt32)cy * 3 / 2;

                }
                else
                    return 0;

            }


        }
    }
}
