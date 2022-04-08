// This file (class PerPixelAlphaForm) were taken and modified from https://www.codeproject.com/Articles/1822/Per-Pixel-Alpha-Blend-in-C

using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PInvoke;

namespace SystemTrayImage;

public class PerPixelAlphaForm : Form
{
    [StructLayout(LayoutKind.Sequential)]
    struct SIZE
    {
        public int cx;
        public int cy;

        public SIZE(int cx, int cy)
        {
            this.cx = cx;
            this.cy = cy;
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }


    const int ULW_ALPHA = 0x00000002;
    
    const byte AC_SRC_OVER = 0x00;
    const byte AC_SRC_ALPHA = 0x01;


    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pprSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

    public const int HT_CAPTION = 0x2;
    public ContextMenuStrip? ContextMenu { get; set; }
    public PerPixelAlphaForm()
    {
        Text = "PerPixelAlphaForm";
        FormBorderStyle = FormBorderStyle.None;
        MouseDown += (o, e) => {
            if (e.Button == MouseButtons.Right)
            {
                if (ContextMenu is null) return;
                ContextMenu.Show(Cursor.Position);
            }
            else if (e.Button == MouseButtons.Left)
            {
                User32.ReleaseCapture();
                User32.SendMessage(Handle, User32.WindowMessage.WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        };
    }

    public void SetBitmap(Bitmap bitmap)
    {
        SetBitmap(bitmap, 255);
    }

    public void SetBitmap(Bitmap bitmap, byte opacity)
    {
        if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            throw new ApplicationException("The bitmap must be 32ppp with alpha-channel.");


        using var screenDc = User32.GetDC(IntPtr.Zero);
        using var memDc = Gdi32.CreateCompatibleDC(screenDc);
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr oldBitmap = IntPtr.Zero;

        try
        {
            hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));  // grab a GDI handle from this GDI+ bitmap
            oldBitmap = Gdi32.SelectObject(memDc, hBitmap);

            var size = new SIZE(Width, Height);
            var pointSource = new POINT() { x = 0, y = 0};
            var topPos = new POINT() { x = Left, y = Top };
            var blend = new BLENDFUNCTION
            {
                BlendOp = AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = opacity,
                AlphaFormat = AC_SRC_ALPHA
            };

            UpdateLayeredWindow(Handle, screenDc.DangerousGetHandle(), ref topPos, ref size, memDc.DangerousGetHandle(), ref pointSource, 0, ref blend, ULW_ALPHA);
        }
        finally
        {
            if (hBitmap != IntPtr.Zero)
            {
                Gdi32.SelectObject(memDc, oldBitmap);
                Gdi32.DeleteObject(hBitmap);
            }
        }
    }   


    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= (int)User32.WindowStylesEx.WS_EX_LAYERED; // This form has to have the WS_EX_LAYERED extended style
            cp.ExStyle |= (int)User32.WindowStylesEx.WS_EX_TOOLWINDOW; // Turn on WS_EX_TOOLWINDOW style bit
            return cp;
        }
    }
    public new double Opacity { get => base.Opacity; set => base.Opacity = value; }

    
}

