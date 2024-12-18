using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HackOpenGL
{
    public class OpenGLVendorSelector
    {
        // 常量定义
        public const string VENDOR_AMD = "PCI\\VEN_1002&";
        public const string VENDOR_NVIDIA = "PCI\\VEN_10DE&";
        public const string VENDOR_INTEL = "PCI\\VEN_8086&";

        // DISPLAY_DEVICE 结构体
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct DISPLAY_DEVICE
        {
            public uint cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        // PIXELFORMATDESCRIPTOR 结构体
        [StructLayout(LayoutKind.Sequential)]
        struct PIXELFORMATDESCRIPTOR
        {
            public ushort nSize;
            public ushort nVersion;
            public uint dwFlags;
            public byte iPixelType;
            public byte cColorBits;
            public byte cRedBits;
            public byte cRedShift;
            public byte cGreenBits;
            public byte cGreenShift;
            public byte cBlueBits;
            public byte cBlueShift;
            public byte cAlphaBits;
            public byte cAlphaShift;
            public byte cAccumBits;
            public byte cAccumRedBits;
            public byte cAccumGreenBits;
            public byte cAccumBlueBits;
            public byte cAccumAlphaBits;
            public byte cDepthBits;
            public byte cStencilBits;
            public byte cAuxBuffers;
            public byte iLayerType;
            public byte bReserved;
            public uint dwLayerMask;
            public uint dwVisibleMask;
            public uint dwDamageMask;
        }

        // Windows API 函数声明
        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("gdi32.dll", CharSet = CharSet.Ansi)]
        static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll")]
        static extern int ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR ppfd);

        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);

        public static void ChooseOpenGLVendor(string vendorId)
        {
            DISPLAY_DEVICE dd = new DISPLAY_DEVICE();
            dd.cb = (uint)Marshal.SizeOf(dd);

            uint idx = 0;

            // 枚举显示设备
            while (EnumDisplayDevices(null, idx, ref dd, 0))
            {
                if (dd.DeviceID.Contains(vendorId))
                {
                    Console.WriteLine($"Found device: {dd.DeviceName} ({vendorId})");
                    break;
                }
                idx++;
            }

            if (string.IsNullOrEmpty(dd.DeviceName))
            {
                Console.WriteLine("Vendor not found!");
                return;
            }

            // 创建设备上下文
            IntPtr hdc = CreateDC(null, dd.DeviceName, null, IntPtr.Zero);
            if (hdc == IntPtr.Zero)
            {
                Console.WriteLine("Failed to create device context.");
                return;
            }

            // 设置像素格式描述符
            PIXELFORMATDESCRIPTOR pfd = new PIXELFORMATDESCRIPTOR
            {
                nSize = (ushort)Marshal.SizeOf(typeof(PIXELFORMATDESCRIPTOR)),
                nVersion = 1,
                dwFlags = 0x4 | 0x20 | 0x1, // PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER
                iPixelType = 0, // PFD_TYPE_RGBA
                cColorBits = 24
            };

            int pixelFormat = ChoosePixelFormat(hdc, ref pfd);
            if (pixelFormat == 0)
            {
                Console.WriteLine("Failed to choose pixel format.");
            }
            else
            {
                Console.WriteLine("Pixel format successfully chosen.");
            }

            // 删除设备上下文
            if (!DeleteDC(hdc))
            {
                Console.WriteLine("Failed to delete device context.");
            }
            else
            {
                Console.WriteLine("Device context deleted successfully.");
            }
        }
    }

}
