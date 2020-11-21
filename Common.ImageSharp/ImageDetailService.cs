using SixLabors.ImageSharp;
using System.IO;
using System.Threading.Tasks;

namespace Common.ImageSharp
{
    public class ImageDetailService : IImageDetailService
    {
        public async Task<ImageDetails> GetDetailsAsync(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Image file does not exists", path);

            var info = await Image.IdentifyAsync(path);

            return new ImageDetails
            {
                Width = info.Width,
                Height = info.Height,
                Size = (new FileInfo(path).Length)
            };
        }
    }
}
