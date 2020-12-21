using Microsoft.Win32;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using FloatingImage.Properties;
using System.Threading;
using Timer = System.Windows.Forms.Timer;

namespace FloatingImage {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (Settings.Default.FirstRun) {
                MessageBox.Show(new Form { TopMost = true, Icon = Resources.Image_16x.ToIcon() }, "This program will run every time you turn on your computer.\nIf you don't want the program to run on startup, turn it off in Task Manager.", "Warning!");
                Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).SetValue("FloatingImage", Application.ExecutablePath);
                Application.Run(new Form2() { Icon = Resources.Image_16x.ToIcon() });
            } else Application.Run(new MyCustomApplicationContext());
        }
    }

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
            Timer T = new Timer() { Interval = ms };
            T.Tick += delegate {
                Action();
                T.Stop();
                T.Dispose();
            };
            T.Start();
        }
    }
    public class ImageManager {
        bool SVGMode = false;
        Image Image = null;
        SvgDocument SVG = null;
        Size SVGSize;
        public int Width {
            get {
                if (SVGMode) return 100;
                return Image.Width;
            }
        }
        public int Height {
            get {
                if (SVGMode) return 100;
                return Image.Height;
            }
        }
        public ImageManager(Image NormalImg) {
            Image = NormalImg;
            SVGMode = false;
        }
        public ImageManager(SvgDocument SVGImage) {
            SVG = SVGImage;
            SVGMode = true;
            Bitmap SVGImg = SVGImage.Draw();
            SVGSize = SVGImg.Size;
            SVGImg.Dispose();
        }
        public Bitmap GetBitmap(double Scale) {

            Bitmap Bitmap;
            if (SVGMode) {
                Bitmap = SVG.Draw((SVGSize.Width * Scale).Round(), 0);
            } else {
                var Size = new Size((Image.Size.Width * Scale).Round(), (Image.Size.Height * Scale).Round());
                Bitmap = new Bitmap(Size.Width, Size.Height);
                var g = Graphics.FromImage(Bitmap);
                g.DrawImage(Image, 0, 0, Size.Width, Size.Height);
                g.Dispose();
            }

            return Bitmap;

        }
    }
    public class MyCustomApplicationContext : ApplicationContext {
        private readonly NotifyIcon TrayIcon;
        readonly Form1 Form1;
        public MyCustomApplicationContext() {

            Form1 = new Form1() {

            };
            var PerPixelAlphaForm = new PerPixelAlphaForm() {
                FormBorderStyle = FormBorderStyle.None,
                Owner = Form1,
                Text = "PerPixel",
                TopMost = true,
                //WindowState = FormWindowState.Maximized,
                ShowInTaskbar = false
            };
            PerPixelAlphaForm.Show();

            ImageManager ImgManager = null;
            double Scale = 1, X = 100, Y = 100;
            void ReadImage(object o = null, EventArgs e = null) {
                var Image = Clipboard.GetImage();
                if (Image == null) {
                    ErrorIcon();
                    return;
                }
                ImgManager = new ImageManager(Image);

                Update();
                Form1.Show();
                return;
            }

            TrayIcon = new NotifyIcon() {
                Icon = Resources.Image_16x.ToIcon(),
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("Reset Application", Reset),
                    new MenuItem("Read as Regular Image (Forced)", ReadImage),
                    new MenuItem("Exit", Exit)
                }),
                Visible = true
            };

            Form1.Reset += delegate {
                double Delta = 1 - Scale;
                Scale += Delta;
                X -= ImgManager.Width * Delta / 2;
                Y -= ImgManager.Height * Delta / 2;
                Update();
            };

            Point StartPoint = Point.Empty;
            void Update() {
                var Loca = new Point(X.Round(), Y.Round());
                var Bitmap = ImgManager.GetBitmap(Scale);
                var Size = Bitmap.Size;

                Form1.Location = Loca;
                Form1.Size = Size;

                PerPixelAlphaForm.Location = Loca;
                PerPixelAlphaForm.Size = Size;

                PerPixelAlphaForm.SetBitmap(Bitmap);
                Bitmap.Dispose();
            }

            Form1.MouseDown += delegate {
                StartPoint = Form1.PointToClient(Cursor.Position);
            };

            Form1.MouseMove += (o, e) => {
                switch (e.Button) {
                    case MouseButtons.Left:
                        Point CurrentPoint = Form1.PointToClient(Cursor.Position);
                        X += CurrentPoint.X - StartPoint.X;
                        Y += CurrentPoint.Y - StartPoint.Y;
                        Update();
                        break;
                }
            };

            Form1.MouseWheel += (o, e) => {
                double Delta = e.Delta > 0 ? 0.01 : -0.01;
                Scale += Delta;
                X -= ImgManager.Width * Delta / 2;
                Y -= ImgManager.Height * Delta / 2;
                Update();
            };

            bool Read = false;

            Thread T = new Thread((ThreadStart)delegate {
                while (true) {
                    while (!Read)
                        Thread.Sleep(16);
                    IDataObject DataObject = Clipboard.GetDataObject();
                    if (DataObject == null) { ErrorIcon(); return; }
                    string[] Formats = DataObject.GetFormats();
                    switch (0) {
                        case 0:
                            if (Formats.Contains("PNG")) {

                                ImgManager = new ImageManager(Image.FromStream((MemoryStream)DataObject.GetData("PNG")));
                                goto default;
                            }
                            goto case 1;
                        case 1:
                            if (Formats.Contains("HTML Format")) {
                                string ImageURL = Regex.Match((string)DataObject.GetData("HTML Format"), "<img.+?src=[\"'](.+?)[\"'].+?>", RegexOptions.IgnoreCase).Groups[1].Value;

                                TrayIcon.Icon = Resources.ASX_Download_blue_16x.ToIcon();
                                WebClient client = new WebClient();
                                Stream Stream;
                                try {
                                    Stream = client.OpenRead(ImageURL);
                                } catch {
                                    ErrorIcon(Resources.CloudError_16x.ToIcon());
                                    goto case 1;
                                }
                                TrayIcon.Icon = Resources.Image_16x.ToIcon();
                                if (ImageURL.EndsWith(".svg")) {
                                    try {
                                        var XML = new XmlDocument();

                                        XML.Load(Stream);

                                        var SVG = SvgDocument.Open(XML);

                                        ImgManager = new ImageManager(SVG);
                                    } catch {
                                        ErrorIcon();
                                        goto case 1;
                                    }
                                } else {
                                    try {
                                        ImgManager = new ImageManager(Image.FromStream(Stream));
                                    } catch {
                                        ErrorIcon();
                                        goto case 1;
                                    }
                                }
                                Stream.Dispose();
                                client.Dispose();


                                goto default;
                            }
                            goto case 2;
                        case 2:
                            if (Formats.Contains("FileName")) {

                                var Filename = ((string[])DataObject.GetData("FileName"))[0];
                                if (Filename.EndsWith(".svg")) {
                                    try {
                                        var XML = new XmlDocument();

                                        XML.Load(Filename);

                                        var SVG = SvgDocument.Open(XML);

                                        ImgManager = new ImageManager(SVG);
                                    } catch {
                                        ErrorIcon();
                                        goto case 1;
                                    }
                                } else ImgManager = new ImageManager(Image.FromFile(Filename));
                                goto default;
                            }
                            goto case 3;
                        case 3:
                            Form1.Invoke((Action)delegate {
                                ReadImage();
                            });
                            break;
                        default:
                            Form1.Invoke((Action)delegate {
                                Update();
                                Form1.Show();
                                return;
                            });
                            break;
                    }
                    Read = false;
                }
            });
            T.SetApartmentState(ApartmentState.STA);
            T.Start();

            TrayIcon.Click += delegate {
                Read = true;
            };

            Application.ApplicationExit += delegate {
                TrayIcon.Visible = false;
            };
        }
        void ErrorIcon(Icon NewIcon = null, bool Reset = true) {
            if (Form1.InvokeRequired) Form1.Invoke((Action)delegate {
                NoNewThreadEI(NewIcon, Reset);
            });
            else NoNewThreadEI(NewIcon, Reset);
        }
        void NoNewThreadEI(Icon NewIcon = null, bool Reset = true) {
            TrayIcon.Icon = NewIcon ?? Resources.ASX_Cancel_blue_16x.ToIcon();
            if (Reset) Extension.SetTimeout(500, ResetIcon);
        }
        void ResetIcon() {
            TrayIcon.Icon = Resources.Image_16x.ToIcon();
        }

        void Exit(object sender, EventArgs e) {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            TrayIcon.Visible = false;

            Environment.Exit(0);
        }

        void Reset(object sender, EventArgs e) {
            if (DialogResult.Yes == MessageBox.Show("Are you sure you want to reset everything?", "WARNING?", MessageBoxButtons.YesNo)) {
                Settings.Default.Reset();
                Settings.Default.Save();
                Application.Restart();
            }
        }
    }
}
