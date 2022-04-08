using Svg;

namespace SystemTrayImage
{
    public interface IImageManager : IDisposable
    {
        int Width { get; }
        int Height { get; }
        Bitmap GetBitmap(double Scale);
    }
    public class BitmapImageManager : IImageManager
    {
        readonly Image Image;
        public int Width => Image.Width;
        public int Height => Image.Height;
        public BitmapImageManager(Image NormalImg) => Image = NormalImg;
        public Bitmap GetBitmap(double Scale)
        {
            var Size = new Size((Image.Size.Width * Scale).Round(), (Image.Size.Height * Scale).Round());
            var Bitmap = new Bitmap(Size.Width, Size.Height);
            var g = Graphics.FromImage(Bitmap);
            g.DrawImage(Image, 0, 0, Size.Width, Size.Height);
            g.Dispose();
            return Bitmap;
        }
        public void Dispose()
        {
            Image.Dispose();
            GC.SuppressFinalize(this);
        }
    }
    public class SVGImageManager : IImageManager
    {
        readonly SvgDocument SVGImage;
        readonly Size SVGSize;
        
        public int Width => SVGSize.Width;
        public int Height => SVGSize.Height;
        public string SVGXML => SVGImage.GetXML();
        
        public SVGImageManager(SvgDocument SVGImage)
        {
            this.SVGImage = SVGImage;
            SVGSize = GetSVGSize(SVGImage);
        }
        
        public Bitmap GetBitmap(double Scale) => SVGImage.Draw((SVGSize.Width * Scale).Round(), 0);
        public void SetColor(Color C) => SVGImage.ApplyRecursive(x => x.Fill = new SvgColourServer(C));
        public void Dispose() => GC.SuppressFinalize(this);

        public static Size GetSVGSize(SvgDocument SVGImage) => SVGImage.GetDimensions().ToSize();
    }
}
