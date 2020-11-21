using Common;
using Common.ImageSharp;
using System;
using System.IO;
using System.Threading;

namespace SeamCarver
{
    public static class SeamCarverWrapper
    {
        public static void CarveVertically(string imagePath, int columnsToCarve, string savePath, ImageFormat outputFormat, CancellationToken cancel, bool crop = true)
        {
            cancel.ThrowIfCancellationRequested();

            if (File.Exists(savePath))
                throw new IOException($"there is already a file under the path {savePath}");

            using (var image = ImageWrapper.Create(imagePath))            
            using (var outFs = File.OpenWrite(savePath))
            {
                int imageWidth = image.Width;
                int imageHeight = image.Height;

                if (imageWidth < 4)
                    throw new ArgumentOutOfRangeException("Image too small for carving, at least width of 4 is required");

                if (columnsToCarve < 1 || columnsToCarve > imageWidth - 3)
                    throw new ArgumentOutOfRangeException($"Number of columns to carve is out of range: 1 - {imageWidth - 3}");

                SeamCarverAlgorithm.AllocatePixelBuffersForVCarving(imageWidth, imageHeight, verticalCarving: true, out byte[,] r, out byte[,] g, out byte[,] b, out byte[,] a, out int[,] energyMap, out int[,] seamMap, out int[] seamVector);

                Utils.TransformToSoaRgba(image, imageWidth, imageHeight, verticalCarving: true, r, g, b, a);

                SeamCarverAlgorithm.RemoveNVerticalSeams(columnsToCarve, imageWidth, imageHeight, r, g, b, a, energyMap, seamMap, seamVector, cancel);

                Utils.TransformToAosRgba(image, imageWidth, imageHeight, true, r, g, b, a);

                if (crop)
                    image.CropRightColumns(columnsToCarve);

                image.SaveTo(outFs, outputFormat);
            }
        }
    }
}
