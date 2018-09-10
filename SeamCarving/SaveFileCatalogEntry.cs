using System;
using System.Drawing;
using System.IO;

namespace SeamCarving
{
    class SaveFileCatalogEntry
    {
        public const string EXTENSION = "bmp";
        public DateTime CreationTime { get; }
        public Guid SaveFileGuid { get; }
        public string FilePath { get; }
        public string OriginalFilename { get; }
        public Size OriginalSize { get; }
        public Size SavedImageSize { get; }

        // every finished carving operation will create a temporary save file with the currect state of the image
        // every file will have a catalog entry with complete metadata information
        // the main window should aquire the process name via reflecion and pass to this constructor so that the temporary files can be saved 
        // in an application specific folder

        public SaveFileCatalogEntry(Bitmap bitmap, string customSavePath, string originalFileName, Size originalImageSize, string applicationName)
        {
            OriginalSize = originalImageSize;
            SavedImageSize = bitmap.Size;
            CreationTime = DateTime.Now;
            SaveFileGuid = Guid.NewGuid();
            OriginalFilename = originalFileName;

            if (customSavePath == String.Empty)
            {
                FilePath = Path.Combine(Path.GetTempPath(), applicationName, SaveFileGuid.ToString());                
            }
            else
            {
                FilePath = Path.Combine(customSavePath, applicationName, SaveFileGuid.ToString());

            }
            FilePath = Path.ChangeExtension(FilePath, EXTENSION);

            if (!Directory.Exists(Path.GetDirectoryName(FilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            }

            using (FileStream fs = new FileStream(FilePath, FileMode.Create))
            {
                bitmap.Save(fs, System.Drawing.Imaging.ImageFormat.Bmp);
            }
        }
        public bool DeleteFile()
        {
            bool result = false;

            if (File.Exists(FilePath))
            {
                try
                {
                    File.Delete(FilePath);                    
                }
                catch (IOException ioEx)
                {
                    return result;
                }            
             }
            return result = true;
        }
    }
}
