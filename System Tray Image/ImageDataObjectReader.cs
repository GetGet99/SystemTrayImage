using Svg;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml;
using Image = System.Drawing.Image;

namespace SystemTrayImage;

static class ImageDataObjectReader {
    public delegate void StatusUpdateHandler(Status S);
    public static event StatusUpdateHandler? StatusUpdated;
    public enum Status {
        EverythingFails,
        Success,
        ErrorIncorrectFormat,
        Downloading, //TrayIcon.Icon = Resources.ASX_Download_blue_16x.ToIcon();
        ErrorDownloadFailed, //ErrorIcon(Resources.CloudError_16x.ToIcon());
        DownloadComplete //TrayIcon.Icon = Resources.Image_16x.ToIcon();
    }
    public static async Task<IImageManager?> ReadImage(this IDataObject DataObject) {
        Status ErrorCode = Status.Success;
        IImageManager? ToReturn = null;

        // Nothing in clipboard
        if (DataObject == null) {
            goto Error;
        }

        string[] Formats = DataObject.GetFormats();

        // Svg
        if (Formats.Contains("image/svg+xml")) {
            ToReturn = DataObject.GetStream("image/svg+xml").GetSVGImageManager();
            goto Success;
        }

        // HTML
        if (Formats.Contains("HTML Format")) {
            string HTMLData = DataObject.GetString("HTML Format");
            string ImageURL = Regex.Match(HTMLData, "<img.+?src=[\"'](.+?)[\"'].+?>", RegexOptions.IgnoreCase).Groups[1].Value;

            using HttpClient client = new();
            byte[] ImageBytes;

            StatusUpdated?.Invoke(Status.Downloading);
            try {
                ImageBytes = await client.GetByteArrayAsync(ImageURL);
            } catch {
                StatusUpdated?.Invoke(Status.ErrorDownloadFailed);
                ErrorCode = Status.ErrorDownloadFailed;
                goto PNG;
            }
            StatusUpdated?.Invoke(Status.DownloadComplete);

            try {
                if (ImageURL.EndsWith(".svg")) ToReturn = ImageBytes.GetSVGImageManager();
                else ToReturn = ImageBytes.GetBitmapImageManager();
                goto Success;
            } catch {
                StatusUpdated?.Invoke(Status.ErrorIncorrectFormat);
                goto PNG;
            }
        }
    PNG:
        // PNG
        if (Formats.Contains("PNG")) {
            ToReturn = DataObject.GetStream("PNG").GetBitmapImageManager();
            goto Success;
        }
        // FileName
        if (Formats.Contains("FileName")) {
            var FileName = DataObject.GetAs<string[]>("FileName")[0];
            try {
                if (FileName.EndsWith(".svg")) ToReturn = GetSVGImageManager(FileName: FileName);
                else ToReturn = new BitmapImageManager(Image.FromFile(FileName));
            } catch {
                goto Error;
            }
            goto Success;
        }
        // Anything else goes wrong
        ErrorCode = Status.EverythingFails;
        goto Finally;
    Error:
        if (ErrorCode == Status.Success) ErrorCode = Status.ErrorIncorrectFormat;
        goto Finally;
    Success:
        ErrorCode = Status.Success;
        goto Finally;
    Finally:
        StatusUpdated?.Invoke(ErrorCode);
        return ToReturn;
    }
    public static T GetAs<T>(this IDataObject dataObject, string format) => (T)dataObject.GetData(format);
    public static MemoryStream GetStream(this IDataObject dataObject, string format) => dataObject.GetAs<MemoryStream>(format);
    public static string GetString(this IDataObject dataObject, string format) => dataObject.GetAs<string>(format);
    public static SVGImageManager GetSVGImageManager(this Stream Stream) {
        var XML = new XmlDocument();

        XML.Load(Stream);

        return XML.GetSVGImageManager();
    }
    public static SVGImageManager GetSVGImageManager(this byte[] Bytes)
    {
        using var ms = new MemoryStream(Bytes);
        return ms.GetSVGImageManager();
    }
    public static SVGImageManager GetSVGImageManager(string FileName) {
        var XML = new XmlDocument();

        XML.Load(FileName);

        return XML.GetSVGImageManager();
    }
    public static SVGImageManager GetSVGImageManager(this XmlDocument XML) {
        var SVG = SvgDocument.Open(XML);

        return new SVGImageManager(SVG);
    }

    public static BitmapImageManager GetBitmapImageManager(this Stream Stream) {
        return new BitmapImageManager(Image.FromStream(Stream));
    }
    public static BitmapImageManager GetBitmapImageManager(this byte[] Bytes)
    {
        using var ms = new MemoryStream(Bytes);
        return ms.GetBitmapImageManager();
    }

}
/*
    void ShowImageFromDataObject(IDataObject DataObject)
    {
        if (DataObject == null) { ErrorIcon(); return; }
        string[] Formats = DataObject.GetFormats();
        // Svg
        if (Formats.Contains("image/svg+xml"))
        {
            Stream s = (MemoryStream)DataObject.GetData("image/svg+xml");
            SVGSource = (null, null);
            SVGXML = s;

            var XML = new XmlDocument();

            XML.Load(s);

            var SVG = SvgDocument.Open(XML);

            ImgManager = new SVGImageManager(SVG);
            goto Update;
        }

        // HTML
        if (Formats.Contains("HTML Format"))
        {
            string HTMLData = (string)DataObject.GetData("HTML Format");
            string ImageURL = Regex.Match(HTMLData, "<img.+?src=[\"'](.+?)[\"'].+?>", RegexOptions.IgnoreCase).Groups[1].Value;

            TrayIcon.Icon = Resources.ASX_Download_blue_16x.ToIcon();
            WebClient client = new WebClient();
            Stream Stream;
            try
            {
                Stream = client.OpenRead(ImageURL);
            }
            catch
            {
                ErrorIcon(Resources.CloudError_16x.ToIcon());
                goto Fallback;
            }
            TrayIcon.Icon = Resources.Image_16x.ToIcon();
            if (ImageURL.EndsWith(".svg"))
            {
                try
                {
                    SVGSource = ("HTML Format", HTMLData);

                    var XML = new XmlDocument();

                    //Duplicate(ref Stream);

                    Stream.Position = 0;

                    XML.Load(Stream);

                    Stream.Position = 0;

                    SVGXML = Stream;

                    var SVG = SvgDocument.Open(XML);

                    ImgManager = new SVGImageManager(SVG);
                }
                catch
                {
                    ErrorIcon();
                    goto Fallback;
                }
            }
            else
            {
                try
                {
                    ImgManager = new BitmapImageManager(Image.FromStream(Stream));
                }
                catch
                {
                    ErrorIcon();
                    goto Fallback;
                }
            }
            Stream.Dispose();
            client.Dispose();


            goto Update;
        }
        // PNG
        if (Formats.Contains("PNG"))
        {

            ImgManager = new BitmapImageManager(Image.FromStream((MemoryStream)DataObject.GetData("PNG")));
            goto Update;
        }
        // FileName
        if (Formats.Contains("FileName"))
        {

            var Filename = ((string[])DataObject.GetData("FileName"))[0];
            if (Filename.EndsWith(".svg"))
            {
                try
                {
                    SVGSource = ("FileName", Filename);
                    MemoryStream ms = new MemoryStream();
                    byte[] xml = File.ReadAllBytes(Filename);
                    ms.Write(xml, 0, xml.Length);
                    SVGXML = ms;

                    var XML = new XmlDocument();

                    XML.Load(Filename);

                    var SVG = SvgDocument.Open(XML);

                    ImgManager = new SVGImageManager(SVG);
                }
                catch
                {
                    ErrorIcon();
                    goto Fallback;
                }
            }
            else ImgManager = new BitmapImageManager(Image.FromFile(Filename));
            goto Update;
        }
        // Anything else goes wrong
        Form1.Invoke((Action)delegate
        {
            ReadImage();
        });
        goto Done;
    Update:
        Form1.Invoke((Action)delegate
        {
            Update();
            Form1.Show();
            return;
        });
        goto Done;
    Done:
    Fallback:
        return;
    }
*/