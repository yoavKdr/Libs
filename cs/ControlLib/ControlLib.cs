using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using ServiceLib;

namespace ControlLib
{
    public class MouseControler
    {
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out Point lpPoint);
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [DllImport("user32")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;


        static public Point Position
        {
            get
            {
                Point pos;
                GetCursorPos(out pos);
                return pos;
            }
            set
            {
                SetCursorPos(value.X, value.Y);
            }
        }

        public static void MouseClick()
        {
            Point mousePos = Point.Zero;
            try
            {
                mousePos = Position;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while geting position: " + ex.Message);
            }
            mouse_event(MOUSEEVENTF_LEFTDOWN, mousePos.X, mousePos.Y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, mousePos.X, mousePos.Y, 0, 0);
        }
        static public void FromPointToPoint(Point target, int speed_millisecond, int tick_millisecond, MovmentMode mode)
        {
            Point startLocation = Position;
            Point dis = target - startLocation;
            int jumps = speed_millisecond / tick_millisecond;
            float spmuj = (float)tick_millisecond / (float)speed_millisecond;

            for (int i = 0; i < jumps; i++)
            {
                Point AddedDis;
                float x = 0;
                float y = 0;

                if (mode == MovmentMode.Normal)
                {
                    x = (i + 1) * dis.X * spmuj;
                    y = (i + 1) * dis.Y * spmuj;
                }

                AddedDis = new Point((int)x, (int)y);

                try
                {
                    Position = startLocation + AddedDis;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred while jumping: " + ex.Message);
                }

                Thread.Sleep(tick_millisecond);
            }

        }
        static public void FromPointToPoint(Point target, int speed_millisecond)
        {
            FromPointToPoint(target, speed_millisecond, 10, MovmentMode.Normal);
        }
        static public void ClickOnClose(Point target, int speed_millisecond)
        {
            FromPointToPoint(target, speed_millisecond, 10, MovmentMode.Normal);
            MouseClick();
        }
    }

    public struct Point
    {
        private int x, y;

        // builders
        public Point(Point pos)
        {
            x = pos.X;
            y = pos.Y;
        }
        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        // geters + seters
        public int X
        {
            get
            {
                return x;
            }

            set
            {
                x = value;
            }
        }
        public int Y
        {
            get
            {
                return y;
            }

            set
            {
                y = value;
            }
        }

        // operators
        public static Point operator -(Point A, Point B)
        {
            return new Point(A.X - B.X, A.Y - B.Y);
        }
        public static Point operator -(Point A, int B)
        {
            return new Point(A.X - B, A.Y - B);
        }
        public static Point operator -(Point A, float B)
        {
            return new Point((int)(A.X - B), (int)(A.Y - B));
        }
        public static Point operator +(Point A, Point B)
        {
            return new Point(A.X + B.X, A.Y + B.Y);
        }
        public static Point operator +(Point A, int B)
        {
            return new Point(A.X + B, A.Y + B);
        }
        public static Point operator +(Point A, float B)
        {
            return new Point((int)(A.X + B), (int)(A.Y + B));
        }
        public static Point operator /(Point A, Point B)
        {
            if (B.X == 0)
            {
                return new Point(0, A.Y / B.Y);
            }
            if (B.Y == 0)
            {
                return new Point(A.X / B.X, 0);
            }

            return new Point(A.X / B.X, A.Y / B.Y);
        }
        public static Point operator /(Point A, int B)
        {
            if (B == 0)
            {
                return new Point(0, 0);
            }

            return new Point(A.X / B, A.Y / B);
        }
        public static Point operator /(Point A, float B)
        {
            if (B == 0)
            {
                return new Point(0, 0);
            }

            return new Point((int)(A.X - B), (int)(A.Y - B));
        }
        public static Point operator *(Point A, Point B)
        {
            return new Point(A.X * B.X, A.Y * B.Y);
        }
        public static Point operator *(Point A, int B)
        {
            return new Point(A.X * B, A.Y * B);
        }
        public static Point operator *(Point A, float B)
        {
            return new Point((int)(A.X * B), (int)(A.Y * B));
        }

        // locations
        static public Point Close
        {
            get { return new Point(1905, 10); }
        }
        static public Point Zero
        {
            get { return new Point(0, 0); }
        }

    }

    public enum MovmentMode
    {
        Normal = 0,
        Lerp = 1,
    }
}
