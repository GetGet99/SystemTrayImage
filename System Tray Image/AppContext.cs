using SystemTrayImage.Properties;
using SystemTrayImage.Forms;
using System.IO;

namespace SystemTrayImage;

public class AppContext : ApplicationContext
{
    static string Imgsdir { get; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/SystemTrayImage/Images";
    public static Icon IconImage { get; } = Resources.Image.FromSVGToIcon();
    static Icon IconDownloading { get; } = Resources.Download.FromSVGToIcon();
    static Icon IconCloudError { get; } = Resources.CloudError.FromSVGToIcon();
    static Icon IconGeneralError { get; } = Resources.ClipboardError.FromSVGToIcon();
    NotifyIcon TrayIcon { get; } = new NotifyIcon()
    {
        Text = "System Tray Image",
        Icon = IconImage,
        Visible = true
    };

    ImageForm ControlForm { get; } = new ImageForm()
    {
        FormBorderStyle = FormBorderStyle.None,
        Icon = IconImage,
        Text = "System Tray Image",
        TopMost = true,
        ShowInTaskbar = false
    };

    double Scale = 1, X = 100, Y = 100;
    IImageManager? ImageManager = null;

    public AppContext()
    {

        Directory.CreateDirectory(Imgsdir);

        ControlForm.Show();
        ControlForm.Hide();

        RefreshContextMenu();
        SetControlFormEvent();
        SetThreadLoopAndMouseDown();

        Application.ApplicationExit += delegate
        {
            TrayIcon.Visible = false;
        };
        ImageDataObjectReader.StatusUpdated += status =>
        {
            switch (status)
            {
                case ImageDataObjectReader.Status.Downloading:
                    TrayIcon.Icon = IconDownloading;
                    break;
                case ImageDataObjectReader.Status.ErrorDownloadFailed:
                    ErrorIcon(IconCloudError);
                    break;
                case ImageDataObjectReader.Status.DownloadComplete:
                    ResetIcon();
                    break;
                case ImageDataObjectReader.Status.ErrorIncorrectFormat:
                case ImageDataObjectReader.Status.EverythingFails:
                    ErrorIcon(IconGeneralError);
                    break;
                default:
                    break;
            }
        };
    }

    void RefreshContextMenu(object? sender = null, EventArgs? e = null)
    {
        var ContextMenu = new ThemeContextMenu()
        {
            ShowCheckMargin = false,
            ShowImageMargin = false
        };
        foreach (string dir in Directory.GetFiles(Imgsdir))
            ContextMenu.Items.Add(new ThemeMenuItem(Path.GetFileName(dir), image: null, (o, E) =>
            {
                if ((o as ThemeMenuItem)?.Name is not string FileName) return;

                if (FileName.EndsWith(".svg")) ImageManager = ImageDataObjectReader.GetSVGImageManager(FileName: FileName);
                else ImageManager = new BitmapImageManager(Image.FromFile(FileName));

                Update();
                ControlForm.Show();
                return;
            })
            { Name = dir });
        ContextMenu.Items.AddRange(new ToolStripItem[] {
            new ToolStripSeparator(),
            new ThemeMenuItem("Open Image Template Folder", image: null, delegate {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
                    FileName = Imgsdir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }),
            new ThemeMenuItem("Refresh Image List", image: null, RefreshContextMenu),
            new ToolStripSeparator(),
            new ThemeMenuItem("Hard Reset", image: null, (o, E) => {
                if (DialogResult.Yes == MessageBox.Show("Are you sure you want to reset everything?", "WARNING?", MessageBoxButtons.YesNo)) {
                    Settings.Default.Reset();
                    Settings.Default.Save();
                    Restart();
                }
            }),
            new ToolStripSeparator(),
            new ThemeMenuItem("Unhide Image", image: null, (o, E) => ControlForm.Show()),
            new ThemeMenuItem("Forced Read as Regular Image", image: null, ReadImage),
            new ToolStripSeparator(),
            new ThemeMenuItem("Reload the program (Fix Bugs)", image: null, (o, E) => Restart()),
            new ThemeMenuItem("About", image: null, (o, E) => {
                MessageBox.Show(
                    caption: "About",
                    text: @"
System Tray Image

Developer: Get0457
Copyright © 2022 by Get0457

Some parts of this software use the code from (https://www.codeproject.com/Articles/1822/Per-Pixel-Alpha-Blend-in-C) which I need to give credit
Portions Copyright © 2002-2004 Rui Godinho Lopes
".Trim()
                );
            }),
            new ThemeMenuItem("Exit", image: null, (o, E) => {
                TrayIcon.Visible = false;
                Environment.Exit(0);
            })
        });
        TrayIcon.ContextMenuStrip = ContextMenu;
    }
    void SetControlFormEvent()
    {
        Point StartPoint = Point.Empty;

        //ControlForm.MouseDown += delegate {
        //    StartPoint = ControlForm.PointToClient(Cursor.Position);
        //};

        //ControlForm.MouseMove += (o, e) => {
        //    switch (e.Button) {
        //        case MouseButtons.Left:
        //            Point CurrentPoint = ControlForm.PointToClient(Cursor.Position);
        //            X += CurrentPoint.X - StartPoint.X;
        //            Y += CurrentPoint.Y - StartPoint.Y;
        //            Update();
        //            break;
        //    }
        //};
        ControlForm.LocationChanged += (o, e) =>
        {
            X = ControlForm.Location.X;
            Y = ControlForm.Location.Y;
        };

        ControlForm.MouseWheel += (o, e) =>
        {
            if (ImageManager == null) throw new ArgumentException();
            double Delta = e.Delta > 0 ? 0.01 : -0.01;
            Scale += Delta;
            X -= ImageManager.Width * Delta / 2;
            Y -= ImageManager.Height * Delta / 2;
            Update();
        };

        ControlForm.Reset += delegate
        {
            if (ImageManager == null) throw new ArgumentException();
            double Delta = 1 - Scale;
            Scale += Delta;
            X -= ImageManager.Width * Delta / 2;
            Y -= ImageManager.Height * Delta / 2;
            Update();
        };
    }
    void SetThreadLoopAndMouseDown()
    {
        bool Read = false;

        TrayIcon.MouseDown += (o, e) =>
        {
            if (e.Button == MouseButtons.Left)
                Read = true;
        };

        Thread T = new((ThreadStart)delegate
        {
            while (true)
            {
                while (!Read)
                    Thread.Sleep(16);
                IDataObject DataObject = Clipboard.GetDataObject();
                var t = DataObject.ReadImage();
                t.Wait();
                IImageManager? img = t.Result;
                if (img != null)
                {
                    ImageManager = img;
                    ControlForm.Invoke(delegate
                    {
                        Update();
                        ControlForm.Show();
                    });
                }
                Read = false;
            }
        });
        T.SetApartmentState(ApartmentState.STA);
        T.Start();
    }
    void ErrorIcon(Icon? NewIcon = null, bool Reset = true)
    {
        if (ControlForm.InvokeRequired) ControlForm.Invoke(delegate
        {
            NoNewThreadEI(NewIcon, Reset);
        });
        else NoNewThreadEI(NewIcon, Reset);
    }
    void NoNewThreadEI(Icon? NewIcon = null, bool Reset = true)
    {
        TrayIcon.Icon = NewIcon ?? IconGeneralError;
        if (Reset) Extension.SetTimeout(500, ResetIcon);
    }
    void ResetIcon()
    {
        TrayIcon.Icon = IconImage;
    }
    void ReadImage(object? o = null, EventArgs? e = null)
    {
        var Image = Clipboard.GetImage();
        if (Image == null)
        {
            ErrorIcon();
            return;
        }
        ImageManager = new BitmapImageManager(Image);

        Update();
        ControlForm.Show();
        return;
    }
    void Update()
    {
        if (ImageManager == null) throw new NullReferenceException();
        var Loca = new Point(X.Round(), Y.Round());
        var Bitmap = ImageManager.GetBitmap(Scale);

        var Size = Bitmap.Size;

        ControlForm.Location = Loca;
        ControlForm.Size = Size;
        ControlForm.ImgScale = Scale;
        ControlForm.ImgManager = ImageManager;

        ControlForm.SetBitmap(Bitmap);
        Bitmap.Dispose();
    }
    public void Restart()
    {
        TrayIcon.Visible = false;
        Application.Restart();
        Environment.Exit(0);
    }
}
