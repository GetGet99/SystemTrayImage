using System.IO;

namespace SystemTrayImage.Forms; 
public partial class ImageForm : PerPixelAlphaForm {
    public event Action? Reset;
    public IImageManager? ImgManager { get; set; }
    public double ImgScale { get; set; }
    //TaskbarIcon TaskbarIcon;
    public ImageForm() {

        //TaskbarIcon = new TaskbarIconWithForm(this) { Icon = AppContext.IconImage };

        Cursor = Cursors.SizeAll;

        FormClosing += (o, e) => {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); }
        };
        FormClosed += (o, e) => {
            Environment.Exit(0);
        };

        var ContextMenuStrip = new ThemeContextMenu()
        {
            ShowCheckMargin = true,
            ShowImageMargin = false
        };
        var AOT = new ThemeMenuItem() {
            CheckOnClick = true,
            Checked = true,
            Text = "Always on Top",
        };
        
        AOT.CheckedChanged += delegate
        {
            //ShowInTaskbar = !AOT.Checked;
            //TaskbarIcon.Visible = true;
            ShowInTaskbar = !AOT.Checked;
            TopMost = AOT.Checked;
            if (ImgManager is not null) SetBitmap(ImgManager.GetBitmap(ImgScale));
        };

        var Re = new ThemeMenuItem() {
            Text = "Reset Size"
        };
        Re.Click += delegate
        {
            Reset?.Invoke();
        };

        var Hi = new ThemeMenuItem() {
            Text = "Hide"
        };
        Hi.Click += (_, _) => Hide();

        var Copy = new ThemeMenuItem() {
            Text = "Copy (Original Scale)"
        };
        Copy.Click += (_, _) => CopyImage(1);

        var Copy2 = new ThemeMenuItem() {
            Text = "Copy (Current Scale)"
        };
        Copy2.Click += (_, _) => CopyImage(ImgScale);

        ContextMenuStrip.Items.Add(AOT);
        ContextMenuStrip.Items.Add(Re);
        ContextMenuStrip.Items.Add(Hi);
        ContextMenuStrip.Items.Add(Copy);
        ContextMenuStrip.Items.Add(Copy2);

        ContextMenu = ContextMenuStrip;

    }

    public void CopyImage(double scale) {
        DataObject dataObject = new ();
        if (ImgManager is null) throw new NullReferenceException();
        if (ImgManager is SVGImageManager SVGImageManager) {
            dataObject.SetData("image/svg+xml", SVGImageManager.SVGXML.ToUTF8Stream());
        }

        Bitmap img = ImgManager.GetBitmap(scale);

        MemoryStream ms = new();
        img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        dataObject.SetData("PNG", ms);

        dataObject.SetImage(img);

        Clipboard.SetDataObject(dataObject);
    }
}
class MyRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected) base.OnRenderMenuItemBackground(e);
        else
        {
            Rectangle rc = new (Point.Empty, e.Item.Size);
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255,255,255,255 / 10)), rc);
        }
    }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = Color.White;
        base.OnRenderItemText(e);
    }
}

