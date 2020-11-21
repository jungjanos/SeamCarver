namespace WebUI.Models
{
    public class ImageViewModel
    {
        public static ImageViewModel Empty = new ImageViewModel();
        public string VirtualFolder { get; }
        public string Filename { get; }
        public int? Width { get; }
        public int? Height { get; }
        public string OrigFileName { get; }
        public long? Size { get; }

        public ImageViewModel(string virtualFolder,  string filename, int? width = null, int? height = null, long? size = null, string origFileName = null)
        {
            VirtualFolder = virtualFolder;
            Filename = filename;
            Width = width;
            Height = height;
            OrigFileName = origFileName;
            Size = size;
        }

        protected ImageViewModel() { }
    }
}
