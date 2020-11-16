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
        public long? Length { get; }

        public ImageViewModel(string virtualFolder,  string filename, int? width = null, int? height = null, string origFileName = null, long? length = null )
        {
            VirtualFolder = virtualFolder;
            Filename = filename;
            Width = width;
            Height = height;
            OrigFileName = origFileName;
            Length = length;
        }

        protected ImageViewModel() { }
    }
}
