using System;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace TraduSaga.Services
{
    internal sealed class WindowsScreenColorSampler : IDisposable
    {
        private IntPtr _desktopDeviceContext;

        public bool IsAvailable => _desktopDeviceContext != IntPtr.Zero;

        public WindowsScreenColorSampler()
        {
            _desktopDeviceContext = GetDC(IntPtr.Zero);
        }

        public bool TrySample(int x, int y, out Color color)
        {
            if (!IsAvailable)
            {
                color = default;
                return false;
            }

            return TryConvertColorRef(GetPixel(_desktopDeviceContext, x, y), out color);
        }

        public void Dispose()
        {
            if (IsAvailable)
            {
                ReleaseDC(IntPtr.Zero, _desktopDeviceContext);
                _desktopDeviceContext = IntPtr.Zero;
            }
        }

        internal static bool TryConvertColorRef(uint colorRef, out Color color)
        {
            if (colorRef == 0xFFFFFFFF)
            {
                color = default;
                return false;
            }

            color = Color.FromRgb(
                (byte)(colorRef & 0xFF),
                (byte)((colorRef >> 8) & 0xFF),
                (byte)((colorRef >> 16) & 0xFF));
            return true;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr windowHandle);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr windowHandle, IntPtr deviceContext);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr deviceContext, int x, int y);
    }
}
