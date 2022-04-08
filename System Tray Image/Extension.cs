using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SystemTrayImage; 
public static class Extension {
    public static SizeF Multiply(this SizeF Size, float Scale, bool Round = true) {
        if (Round) return new Size((int)Math.Round(Size.Width * Scale), (int)Math.Round(Size.Height * Scale));
        else return new SizeF(Size.Width * Scale, Size.Height * Scale);
    }
    public static SizeF Multiply(this Size Size, float Scale, bool Round = true) {
        if (Round) return new Size((int)Math.Round(Size.Width * Scale), (int)Math.Round(Size.Height * Scale));
        else return new SizeF(Size.Width * Scale, Size.Height * Scale);
    }
    public static int Round(this double Double) {
        return (int)Math.Round(Double);
    }
    public static Icon ToIcon(this Bitmap Image) {
        return Icon.FromHandle(Image.GetHicon());
    }
    public static void SetTimeout(int ms, Action Action) {
        Timer T = new() { Interval = ms };
        T.Tick += delegate {
            Action();
            T.Stop();
            T.Dispose();
        };
        T.Start();
    }
    public static MemoryStream ToUTF8Stream(this string String) {
        MemoryStream ms = new();
        byte[] bytes = Encoding.UTF8.GetBytes(String);
        ms.Write(bytes, 0, bytes.Length);
        return ms;
    }
    public static Icon FromSVGToIcon(this byte[] Bytes, bool ReplaceColor = true, double Scale = 20)
    {
        using var svgmanager = ImageDataObjectReader.GetSVGImageManager(Bytes);
        if (ReplaceColor) svgmanager.SetColor(Color.White);
        return svgmanager.GetBitmap(Scale).ToIcon();
    }
}
