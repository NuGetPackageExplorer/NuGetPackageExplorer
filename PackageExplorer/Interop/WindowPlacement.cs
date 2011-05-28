using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace PackageExplorer {
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPlacement {
        public int length;
        public int flags;
        public int showCmd;

        public Point minPosition;
        public Point maxPosition;
        public Rect normalPosition;

        public override string ToString() {
            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1}|{2}|{3}|{4}|{5}",
                length,
                flags,
                showCmd,
                minPosition,
                maxPosition,
                normalPosition);
        }

        public static WindowPlacement Parse(string value) {
            string[] parts = value.Split('|');

            if (parts.Length != 6) {
                return new WindowPlacement();
            }

            int flength = int.Parse(parts[0], CultureInfo.InvariantCulture);
            int fflags = int.Parse(parts[1], CultureInfo.InvariantCulture);
            int fshowCmd = int.Parse(parts[2], CultureInfo.InvariantCulture);
            Point fminPosition = Point.Parse(parts[3]);
            Point fmaxPosition = Point.Parse(parts[4]);
            Rect fnormalPosition = Rect.Parse(parts[5]);

            return new WindowPlacement {
                length = flength,
                flags = fflags,
                showCmd = fshowCmd,
                minPosition = fminPosition,
                maxPosition = fmaxPosition,
                normalPosition = fnormalPosition};
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Width;
        public int Height;

        public Rect(int left, int top, int width, int height)
        {
            this.Left = left;
            this.Top = top;
            this.Width = width;
            this.Height = height;
        }

        public override string ToString() {
            return String.Format(CultureInfo.InvariantCulture, "{0};{1};{2};{3}", Left, Top, Width, Height);
        }

        public static Rect Parse(string value) {
            int[] ss = Array.ConvertAll<string, int>(value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries), v => int.Parse(v, CultureInfo.InvariantCulture));
            return ss.Length == 4 ? new Rect(ss[0], ss[1], ss[2], ss[3]) : new Rect();
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public override string ToString() {
            return String.Format(CultureInfo.InvariantCulture, "{0};{1}", X, Y);
        }

        public static Point Parse(string value) {
            int[] ss = Array.ConvertAll<string, int>(value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries), v => int.Parse(v, CultureInfo.InvariantCulture));
            return ss.Length == 2 ? new Point(ss[0], ss[1]) : new Point();
        }
    }
}