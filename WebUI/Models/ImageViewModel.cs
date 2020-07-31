using SixLabors.ImageSharp;

namespace WebUI.Models
{
    public class ImageViewModel
    {
        public static ImageViewModel Empty = new ImageViewModel();

        public string Filename { get; }
        public int? Width { get; }
        public int? Height { get; }
        public string OrigFileName { get; }
        public long? Length { get; }

        public ImageViewModel(string filename, int? width = null, int? height = null, string origFileName = null, long? length = null )
        {
            Filename = filename;
            Width = width;
            Height = height;
            OrigFileName = origFileName;
            Length = length;
        }

        protected ImageViewModel() { }
    }
}
