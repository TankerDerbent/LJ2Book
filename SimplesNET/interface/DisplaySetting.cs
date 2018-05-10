using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimplesNet
{
    class DisplaySetting
    {
        public enum DPI
        {
            _100_ = 96,
            _125_ = 120,
            _150_ = 144,
            _175_ = 168,
            _200_ = 192,
            _225_ = 216,
            _250_ = 240,
            _300_ = 288,
            _350_ = 336
        }

        const int DEFAULT_DPI = 96;

		public static bool isDefaultDPI()
		{
			return DpiX == DEFAULT_DPI;
		}

        public static int ScaleValue(int value)
        {
            return ScaleValueX(value);
        }

        public static float ScaleValueX(float value)
        {
            return unchecked((Math.BigMul((int)value, DpiX) / DEFAULT_DPI));
        }

        public static int ScaleValueX(int value)
        {
            return unchecked((int)(Math.BigMul(value, DpiX) / DEFAULT_DPI));
        }

        public static int ScaleValueY(int value)
        {
            return unchecked((int)(Math.BigMul(value, DpiY) / DEFAULT_DPI));
        }

        public static void ScaleRect(ref Rectangle rect)
        {
            rect.Width = ScaleValueX(rect.Width);
            rect.Height = ScaleValueY(rect.Height);
        }

        public static void ScaleSize(ref Size size)
        {
            size.Width = ScaleValueX(size.Width);
            size.Height = ScaleValueY(size.Height);
        }

        public static Bitmap ScaleBitmap(Bitmap bitmap)
        {
            Size size = bitmap.Size;
            DisplaySetting.ScaleSize(ref size);
            return new Bitmap(bitmap, size);
        }

        public static int DpiX
        {
            get
            {
                int answer = DEFAULT_DPI;
                try
                {
                    answer = (int)Graphics.FromHwnd(IntPtr.Zero).DpiX;
                }
                catch { }
                return answer;
            }
        }

        private static int DpiY
        {
            get
            {
                int answer = DEFAULT_DPI;
                try
                {
                    answer = (int)Graphics.FromHwnd(IntPtr.Zero).DpiY;
                }
                catch { }
                return answer;
            }
        }
    }
}
