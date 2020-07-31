namespace WebUI.Models
{
    public class ImageViewModel
    {
        public string ContentPath { get; }
        public int? Width { get; }
        public int? Height { get; }
        public string OrigFileName { get; }
        public long? Length { get; }

        public ImageViewModel(string contentPath, int? width = null, int? height = null, string origFileName = null, long? length = null )
        {
            ContentPath = contentPath;
            Width = width;
            Height = height;
            OrigFileName = origFileName;
            Length = length;
        }

        public ImageViewModel()
        {
        }
    }
}
