using System;
using System.IO;
using System.Threading.Tasks;

namespace WebUI.Service
{
    public class UserFileSystemHelper
    {
        private readonly string userFolderBasePhysical;
        private readonly string userFolderBaseVirtual;

        public UserFileSystemHelper(string userFolderBasePhysical, string userFolderBaseVirtual, string userFolder)
        {
            this.userFolderBasePhysical = userFolderBasePhysical;
            this.userFolderBaseVirtual = userFolderBaseVirtual;
            UserFolder = userFolder;
        }

        public string CreateRandomFilename(string filename)
        {
            var rnd = Guid.NewGuid().ToString("N");
            var extension = Path.GetExtension(filename);
            return $"{Path.GetFileNameWithoutExtension(filename)}_{rnd}{extension}";
        }

        public string PrependPhysicalFolderPath(string filename) => Path.Combine(UserPhysicalFolder, filename);

        public async Task<string> SaveUploadFileToRandomFile(string origFileName, Stream uploadStream)
        {
            var rndFilename = CreateRandomFilename(origFileName);
            var fullPath = Path.Join(UserPhysicalFolder, rndFilename);

            using (var fs = new FileStream(fullPath, FileMode.OpenOrCreate))
            {
                await uploadStream.CopyToAsync(fs);
            }
            return rndFilename;
        }
        
        public string UserPhysicalFolder => Path.Combine(userFolderBasePhysical, UserFolder);
        public string UserFolder { get; }
        public string UserVirtualFolder => Path.Combine(userFolderBaseVirtual, UserFolder).Replace('\\', '/');
    }
}
