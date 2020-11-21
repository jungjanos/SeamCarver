using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ImageDetails
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public long Size { get; set; }
    }

    public interface IImageDetailService
    {
        Task<ImageDetails> GetDetailsAsync(string path);
    }
}
