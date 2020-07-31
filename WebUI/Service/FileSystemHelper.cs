using System;
using System.IO;
using System.Threading.Tasks;

namespace WebUI.Service
{
    public class FileSystemHelper
    {
        public FileSystemHelper(string uploadBasePhysical, string uploadBaseVirtual)
        {
            UploadBasePhysical = uploadBasePhysical;
            UploadBaseVirtual = uploadBaseVirtual;
        }

        public string CreateRandomFilename(string filename)
        {
            var rnd = Guid.NewGuid().ToString("N");
            var extension = Path.GetExtension(filename);
            return $"{Path.GetFileNameWithoutExtension(filename)}_{rnd}{extension}";
        }

        public string PrependPhysicalFolderPath(string filename) => Path.Combine(UploadBasePhysical, filename);

        public async Task<string> SaveUploadFileToRandomFile(string origFileName, Stream uploadStream)
        {
            var rndFilename = CreateRandomFilename(origFileName);
            var fullPath = Path.Join(UploadBasePhysical, rndFilename);

            using (var fs = new FileStream(fullPath, FileMode.OpenOrCreate))
            {
                await uploadStream.CopyToAsync(fs);
            }
            return rndFilename;
        }

        public string UploadBasePhysical { get; }
        public string UploadBaseVirtual { get; }
    }

}
