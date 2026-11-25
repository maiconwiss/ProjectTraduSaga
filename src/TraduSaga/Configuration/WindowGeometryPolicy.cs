using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;

namespace TraduSaga.Configuration
{
    public static class WindowGeometryPolicy
    {
        private const double MINIMUM_VISIBLE_SIZE = 80;

        private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr deviceContext, IntPtr monitorRect, IntPtr data);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect Monitor;
            public NativeRect Work;
            public uint Flags;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr deviceContext, IntPtr clipRect, MonitorEnumProc callback, IntPtr data);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

        public static RectangleF[] GetMonitorBounds()
        {
            var bounds = new List<RectangleF>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
            {
                var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                if (GetMonitorInfo(monitor, ref info))
                {
                    bounds.Add(new RectangleF(
                        info.Monitor.Left,
                        info.Monitor.Top,
                        info.Monitor.Right - info.Monitor.Left,
                        info.Monitor.Bottom - info.Monitor.Top));
                }

                return true;
            }, IntPtr.Zero);
            return bounds.ToArray();
        }

        public static Rect[] GetMonitorWorkAreas()
        {
            var workAreas = new List<Rect>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
            {
                var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                if (GetMonitorInfo(monitor, ref info))
                {
                    workAreas.Add(new Rect(
                        info.Work.Left,
                        info.Work.Top,
                        info.Work.Right - info.Work.Left,
                        info.Work.Bottom - info.Work.Top));
                }

                return true;
            }, IntPtr.Zero);
            return workAreas.ToArray();
        }

        public static Rect Restore(Rect savedBounds, Rect workArea, double defaultWidth, double defaultHeight)
        {
            double width = IsPositiveFinite(savedBounds.Width) ? savedBounds.Width : defaultWidth;
            double height = IsPositiveFinite(savedBounds.Height) ? savedBounds.Height : defaultHeight;

            double minimumVisibleWidth = Math.Min(MINIMUM_VISIBLE_SIZE, width);
            double minimumVisibleHeight = Math.Min(MINIMUM_VISIBLE_SIZE, height);
            double left = IsFinite(savedBounds.Left) ? savedBounds.Left : workArea.Left;
            double top = IsFinite(savedBounds.Top) ? savedBounds.Top : workArea.Top;

            left = Math.Clamp(left, workArea.Left - width + minimumVisibleWidth, workArea.Right - minimumVisibleWidth);
            top = Math.Clamp(top, workArea.Top - height + minimumVisibleHeight, workArea.Bottom - minimumVisibleHeight);

            return new Rect(left, top, width, height);
        }

        public static Rect Restore(Rect savedBounds, Rect[] workAreas, double defaultWidth, double defaultHeight)
        {
            if (workAreas == null || workAreas.Length == 0)
            {
                return Restore(savedBounds, SystemParameters.WorkArea, defaultWidth, defaultHeight);
            }

            Rect selectedWorkArea = workAreas[0];
            double greatestIntersection = -1;
            foreach (var workArea in workAreas)
            {
                var intersection = Rect.Intersect(savedBounds, workArea);
                double area = intersection.IsEmpty ? 0 : intersection.Width * intersection.Height;
                if (area > greatestIntersection)
                {
                    greatestIntersection = area;
                    selectedWorkArea = workArea;
                }
            }

            return Restore(savedBounds, selectedWorkArea, defaultWidth, defaultHeight);
        }

        public static Rect Restore(double left, double top, double width, double height, Rect[] workAreas, double defaultWidth, double defaultHeight)
        {
            var safeBounds = new Rect(
                IsFinite(left) ? left : 0,
                IsFinite(top) ? top : 0,
                IsFinite(width) && width >= 0 ? width : 0,
                IsFinite(height) && height >= 0 ? height : 0);
            return Restore(safeBounds, workAreas, defaultWidth, defaultHeight);
        }

        public static bool IsCaptureAreaValid(System.Drawing.RectangleF area, System.Drawing.RectangleF[] monitorBounds)
        {
            if (float.IsNaN(area.X) || float.IsInfinity(area.X)
                || float.IsNaN(area.Y) || float.IsInfinity(area.Y)
                || float.IsNaN(area.Width) || float.IsInfinity(area.Width) || area.Width <= 0
                || float.IsNaN(area.Height) || float.IsInfinity(area.Height) || area.Height <= 0)
            {
                return false;
            }

            return monitorBounds != null && Array.Exists(monitorBounds, monitor =>
                area.Left >= monitor.Left && area.Top >= monitor.Top
                && area.Right <= monitor.Right && area.Bottom <= monitor.Bottom);
        }

        private static bool IsPositiveFinite(double value) => IsFinite(value) && value > 0;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
