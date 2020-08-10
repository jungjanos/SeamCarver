using Common;
using Common.ImageSharp;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SeamCarver
{
    public static class SeamCarver
    {
        public static void CarveVertically(string imagePath, int columnsToCarve, string savePath, ImageFormat outputFormat, CancellationToken cancel, bool crop = true)
        {
            cancel.ThrowIfCancellationRequested();

            if (File.Exists(savePath))
                throw new IOException($"there is already a file under the path {savePath}");

            using (var image = ImageWrapper.LoadImageAsWrappedRgba(imagePath))
            using (var outFs = File.OpenWrite(savePath))
            {
                int imageWidth = image.Width;
                int imageHeight = image.Height;

                if (imageWidth < 4)
                    throw new ArgumentOutOfRangeException("Image too small for carving, at least with of 4 is required");

                if (columnsToCarve < 1 || columnsToCarve > imageWidth - 3)
                    throw new ArgumentOutOfRangeException($"Number of columns to carve is out of range: 1 - {imageWidth - 3}");

                AllocatePixelBuffersForVCarving(imageWidth, imageHeight, verticalCarving: true, out byte[,] r, out byte[,] g, out byte[,] b, out byte[,] a, out int[,] energyMap, out int[,] seamMap, out int[] seamVector);

                Utils.TransformToSoaRgba(image, imageWidth, imageHeight, verticalCarving: true, r, g, b, a);

                RemoveNVerticalSeams(columnsToCarve, imageWidth, imageHeight, r, g, b, a, energyMap, seamMap, seamVector, cancel);

                Utils.TransformToAosRgba(image, imageWidth, imageHeight, true, r, g, b, a);

                if (crop)
                    image.CropRightColumns(columnsToCarve);

                image.Save(outFs, outputFormat);
            }
        }

        /// <summary>
        /// The main entry point for the seamcarver algorithm. Receives R,G,B,A arrays, working buffers and gives back the adjusted image in place
        /// </summary>
        /// <param name="n">number of vertical seams to carve</param>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <param name="r">r, g, b, a are SoA representation of pixels, in row major format</param>
        /// <param name="energyMap">working buffer for pixel energy</param>
        /// <param name="seamMap">working buffer for seam map</param>
        /// <param name="seamVector"></param>
        /// <param name="cancel"></param>
        static void RemoveNVerticalSeams(int n, int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap, int[,] seamMap, int[] seamVector, CancellationToken cancel)
        {
            if (n == 0)
                return;

            cancel.ThrowIfCancellationRequested();

            int w = width;
            CalculateEnergyMap(w, height, verticalCarving: true, r, g, b, a, energyMap);

            for (int i = 0; i < n; i++)
            {
                cancel.ThrowIfCancellationRequested();

                ConvertEnergyMapToVerticalSeamMap(energyMap, w, height, seamMap);
                CalculateMinimalVerticalSeam(seamMap, w, height, seamVector);
                RemoveVerticalSeamPixels(seamVector, w, height, r, g, b, a);
                AdjustEnergyMap(seamVector, w, height, r, g, b, a, energyMap);

                w--;
            }
        }

        // TODO make this faster (use vectors or 64bit scalars to copy)
        /// <summary> Removes the specified vertical seam, fills last column with 0xFFFFFFFF</summary>
        /// <param name="seam">Vertical seam to be removed</param>
        /// <param name="width">width of actual working area</param>
        /// <param name="height">height of actual working area</param>   
        unsafe static void RemoveVerticalSeamPixels(int[] seam, int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a)
        {
            fixed (byte* rPtr = &r[0, 0])
            fixed (byte* gPtr = &g[0, 0])
            fixed (byte* bPtr = &b[0, 0])
            fixed (byte* aPtr = &a[0, 0])
            {
                var imageWidth = a.GetLength(1);

                for (int row = 0; row < height; row++)
                {
                    int posToRemove = seam[row];
                    var offset = row * imageWidth + posToRemove;

                    byte* rP = rPtr + offset;
                    byte* gP = gPtr + offset;
                    byte* bP = bPtr + offset;
                    byte* aP = aPtr + offset;

                    for (int j = posToRemove; j < width - 1; j++)
                    {
                        *rP = *(rP + 1); rP++;
                        *gP = *(gP + 1); gP++;
                        *bP = *(bP + 1); bP++;
                        *aP = *(aP + 1); aP++;
                    }

                    *rP = *gP = *bP = *aP = 255;
                }
            }
        }

        /// <summary>
        /// Method removes the pixels of the given seam from the energyMap, this is done by left shifting row suffixes by 1 position to eliminiate the to be deleted pixel.
        /// After this the energy of all pixels who have a new neighbour is recalculated
        /// </summary>
        /// <param name="seam">seam to be removed</param>
        /// <param name="width">starting working width of the energy map, after the left shift this is reduced by one, caller must keep track</param>
        /// <param name="height">height of image</param>
        /// <param name="a">a, r, g, b is the representation of the current (already adjusted) image</param>
        /// <param name="energyMap">energy map to be adjusted</param>
        unsafe static void AdjustEnergyMap(int[] seam, int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap)
        {
            var imageWidth = energyMap.GetLength(1);

            // left shifting the energy map row-suffixes in place of the to be removed position
            fixed (int* ePtr = &energyMap[0, 0])
            {
                for (int row = 0; row < height; row++)
                {
                    int posToRemove = seam[row];
                    var offset = row * imageWidth + posToRemove;
                    int* eP = ePtr + offset;

                    for (int j = posToRemove; j < width - 1; j++)
                    {
                        *eP = *(eP + 1);
                        eP++;
                    }
                    *eP = int.MinValue;
                }
            }

            for (int row = 0; row < height; row++)
            {
                RecalculateEnergyForPixelNeighbours(row, seam[row], width - 1, height, r, g, b, a, energyMap);
            }

            var top = seam[0];
            var bottom = seam[height - 1];

            for (int row = Math.Min(top, bottom); row < Math.Max(top, bottom); row++)
            {
                CalculateEnergyForPixel(0, row, width - 1, height, r, g, b, a, energyMap);
                CalculateEnergyForPixel(height - 1, row, width - 1, height, r, g, b, a, energyMap);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void RecalculateEnergyForPixelNeighbours(int row, int col, int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap)
        {
            // recalculate the energy of the pixel neighbouring the seam pixel from left
            var left = (col - 1 + width) % width;

            var dx2r = r[row, (left + 1) % width] - r[row, (left - 1 + width) % width]; dx2r *= dx2r;
            var dx2g = g[row, (left + 1) % width] - g[row, (left - 1 + width) % width]; dx2g *= dx2g;
            var dx2b = b[row, (left + 1) % width] - b[row, (left - 1 + width) % width]; dx2b *= dx2b;
            var dx2a = a[row, (left + 1) % width] - a[row, (left - 1 + width) % width]; dx2a *= dx2a;

            var dy2r = r[(row + 1) % height, left] - r[(row - 1 + height) % height, left]; dy2r *= dy2r;
            var dy2g = g[(row + 1) % height, left] - g[(row - 1 + height) % height, left]; dy2g *= dy2g;
            var dy2b = b[(row + 1) % height, left] - b[(row - 1 + height) % height, left]; dy2b *= dy2b;
            var dy2a = a[(row + 1) % height, left] - a[(row - 1 + height) % height, left]; dy2a *= dy2a;

            energyMap[row, left] = (int)Math.Round(Math.Sqrt((double)dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a));

            // recalculate the energy of the pixel neighbouring the seam pixel from right
            var right = col % width;

            dx2r = r[row, (right + 1) % width] - r[row, (right - 1 + width) % width]; dx2r *= dx2r;
            dx2g = g[row, (right + 1) % width] - g[row, (right - 1 + width) % width]; dx2g *= dx2g;
            dx2b = b[row, (right + 1) % width] - b[row, (right - 1 + width) % width]; dx2b *= dx2b;
            dx2a = a[row, (right + 1) % width] - a[row, (right - 1 + width) % width]; dx2a *= dx2a;

            dy2r = r[(row + 1) % height, right] - r[(row - 1 + height) % height, right]; dy2r *= dy2r;
            dy2g = g[(row + 1) % height, right] - g[(row - 1 + height) % height, right]; dy2g *= dy2g;
            dy2b = b[(row + 1) % height, right] - b[(row - 1 + height) % height, right]; dy2b *= dy2b;
            dy2a = a[(row + 1) % height, right] - a[(row - 1 + height) % height, right]; dy2a *= dy2a;

            energyMap[row, right] = (int)Math.Round(Math.Sqrt((double)dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a));
        }

        static void CalculateEnergyForPixel(int row, int col, int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap)
        {
            var dx2r = r[row, (col + 1) % width] - r[row, (col - 1 + width) % width]; dx2r *= dx2r;
            var dx2g = g[row, (col + 1) % width] - g[row, (col - 1 + width) % width]; dx2g *= dx2g;
            var dx2b = b[row, (col + 1) % width] - b[row, (col - 1 + width) % width]; dx2b *= dx2b;
            var dx2a = a[row, (col + 1) % width] - a[row, (col - 1 + width) % width]; dx2a *= dx2a;

            var dy2r = r[(row + 1) % height, col] - r[(row - 1 + height) % height, col]; dy2r *= dy2r;
            var dy2g = g[(row + 1) % height, col] - g[(row - 1 + height) % height, col]; dy2g *= dy2g;
            var dy2b = b[(row + 1) % height, col] - b[(row - 1 + height) % height, col]; dy2b *= dy2b;
            var dy2a = a[(row + 1) % height, col] - a[(row - 1 + height) % height, col]; dy2a *= dy2a;

            energyMap[row, col] = (int)Math.Round(Math.Sqrt((double)dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a));
        }


        /// <summary></summary>
        /// <param name="width">width of actual working area</param>
        /// <param name="height">height of actual working area</param>                
        static void CalculateEnergyMap(int width, int height, bool verticalCarving, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap)
        {
            if (verticalCarving)
            {
                for (int col = 0; col < width; col++)
                {
                    CalculateEnergyForPixel(row: 0, col, width, height, r, g, b, a, energyMap);
                    CalculateEnergyForPixel(row: height - 1, col, width, height, r, g, b, a, energyMap);
                }

                for (int row = 1; row < height - 1; row++)
                {
                    CalculateEnergyForPixel(row, 0, width, height, r, g, b, a, energyMap);
                    CalculateEnergyForPixel(row, width - 1, width, height, r, g, b, a, energyMap);
                }

                CalculateNonBorderEnergy(width, height, r, g, b, a, energyMap);

            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary></summary>
        /// <param name="energyMap"> [height,width] map of energy calculated for each pixel </param>        
        /// <param name="width">width of actual working area</param>
        /// <param name="height">height of actual working area</param>
        static unsafe void ConvertEnergyMapToVerticalSeamMap(int[,] energyMap, int width, int height, int[,] seamMap)
        {
            var imageWidth = energyMap.GetLength(1);

            fixed (int* ePtr = &energyMap[0, 0])
            fixed (int* sPtr = &seamMap[0, 0])
            {
                for (int i = 0; i < width; i++)
                    *(sPtr + i) = *(ePtr + i);

                for (int row = 1; row < height; row++)
                {
                    int offset = row * imageWidth;
                    int* eP = ePtr + offset;
                    int* sP = sPtr + offset;

                    *sP = *eP + min3(int.MaxValue, *(sP - imageWidth), *(sP - imageWidth + 1));

                    for (int col = 1; col < width - 1; col++)
                    {
                        eP++; sP++;
                        *sP = *eP + min3(*(sP - imageWidth - 1), *(sP - imageWidth), *(sP - imageWidth + 1));
                    }

                    ++eP; ++sP;

                    *sP = *eP + min3(*(sP - imageWidth - 1), *(sP - imageWidth), int.MaxValue);
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int min3(int a, int b, int c) => a < b ? (a < c ? a : c) : (b < c ? b : c); // TODO => check if less branchy implementation exists

        /// <summary>
        /// Calculates the horizontal indexes of a vertical seam
        /// </summary>
        /// <param name="seamMap"> [height,width] map of minimum seam energy calculated for each pixel </param>        
        /// <param name="width">width of actual working area</param>
        /// <param name="height">height of actual working area</param>
        static void CalculateMinimalVerticalSeam(int[,] seamMap, int width, int height, int[] seamHorizontalIndexes)
        {
            int lastIndex = seamHorizontalIndexes[height - 1] = GetHorizontalIndexOfMinimumSeam();

            for (int row = height - 2; row >= 0; row--)
            {
                var copy = lastIndex;

                if (copy == 0)
                    lastIndex = seamHorizontalIndexes[row] = min3Index(int.MaxValue, -1, seamMap[row, 0], 0, seamMap[row, 1], 1);

                else if (copy == width - 1)
                    lastIndex = seamHorizontalIndexes[row] = min3Index(seamMap[row, width - 2], width - 2, seamMap[row, width - 1], width - 1, int.MaxValue, -1);

                else
                    lastIndex = seamHorizontalIndexes[row] = min3Index(seamMap[row, copy - 1], copy - 1, seamMap[row, copy], copy, seamMap[row, copy + 1], copy + 1);
            }

            // returns the index of the smallest of the three array elements, if two are equal and both minimum, 
            // it returns the index of the later of the two        
            int min3Index(int a, int indexOfA, int b, int indexOfB, int c, int indexOfC)
            {
                int helper;
                int indexHelper;

                if (a < b) { helper = a; indexHelper = indexOfA; }
                else { helper = b; indexHelper = indexOfB; }
                if (helper < c) { return indexHelper; }
                else { return indexOfC; }
            }

            int GetHorizontalIndexOfMinimumSeam()
            {
                int idx = -1;
                int min = int.MaxValue;
                for (int col = 0; col < width; col++)
                {
                    if (seamMap[height - 1, col] < min)
                    {
                        idx = col;
                        min = seamMap[height - 1, col];
                    }
                }
                return idx;
            }
        }

        // TODO 
        // evaluate the possibility for using floats for energy/seam map
        // evaluate possibility for using single precision sqrt with les int-> floating point->int conversion
        // 32bit int=> 64bit double seems very unnecessary here, floats are ok up to +-16M for storing int
        private unsafe static void CalculateNonBorderEnergy(int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap)
        {
            fixed (int* ePtr = &energyMap[0, 1])
            fixed (byte* rPtr = &r[0, 1])
            fixed (byte* gPtr = &g[0, 1])
            fixed (byte* bPtr = &b[0, 1])
            fixed (byte* aPtr = &a[0, 1])
            {
                var pictureWidth = r.GetLength(1);
                int* eP = ePtr;
                byte* rP = rPtr;
                byte* gP = gPtr;
                byte* bP = bPtr;
                byte* aP = aPtr;

                for (int row = 1; row < height - 1; row++)
                {
                    eP = ePtr + row * pictureWidth;
                    rP = rPtr + row * pictureWidth;
                    gP = gPtr + row * pictureWidth;
                    bP = bPtr + row * pictureWidth;
                    aP = aPtr + row * pictureWidth;

                    for (int col = 1; col < width - 1; col++)
                    {
                        var dx2r = *(rP + 1) - *(rP - 1); dx2r *= dx2r;
                        var dx2g = *(gP + 1) - *(gP - 1); dx2g *= dx2g;
                        var dx2b = *(bP + 1) - *(bP - 1); dx2b *= dx2b;
                        var dx2a = *(aP + 1) - *(aP - 1); dx2a *= dx2a;

                        var dy2r = *(rP + pictureWidth) - *(rP - pictureWidth); dy2r *= dy2r;
                        var dy2g = *(gP + pictureWidth) - *(gP - pictureWidth); dy2g *= dy2g;
                        var dy2b = *(bP + pictureWidth) - *(bP - pictureWidth); dy2b *= dy2b;
                        var dy2a = *(aP + pictureWidth) - *(aP - pictureWidth); dy2a *= dy2a;

                        *eP = (int)Math.Round(Math.Sqrt((double)dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a));

                        eP++; rP++; gP++; bP++; aP++;
                    }
                }
            }
        }

        /// <summary>
        /// Allocates pixel buffers in advance
        /// Note: .Net uses row major order for multi dimensional arrays
        /// </summary>
        /// <param name="width">width of image</param>
        /// <param name="height">height of image</param>
        /// <param name="verticalCarving"></param>
        /// <param name="energyMap"></param>
        /// <param name="seamPath">Array to hold the minimum energy path</param>
        static void AllocatePixelBuffersForVCarving(int width, int height, bool verticalCarving, out byte[,] r, out byte[,] g, out byte[,] b, out byte[,] a, out int[,] energyMap, out int[,] seamMap, out int[] seamPath)
        {
            if (verticalCarving) // y before x
            {
                r = new byte[height, width];
                g = new byte[height, width];
                b = new byte[height, width];
                a = new byte[height, width];
                energyMap = new int[height, width];
                seamMap = new int[height, width];
                seamPath = new int[height];
            }
            else // x before y
            {
                throw new NotImplementedException("");
            }
        }
    }
}
