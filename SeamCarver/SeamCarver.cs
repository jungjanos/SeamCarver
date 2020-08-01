using System;
using System.IO;
using System.Runtime.InteropServices;
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

            using (var image = Utils.LoadImageAsWrappedRgba(imagePath))
            using (var outFs = File.OpenWrite(savePath))
            {
                int imageWidth = image.Width;
                int imageHeight = image.Height;

                if (imageWidth < 4)
                    throw new ArgumentOutOfRangeException("Image too small for carving, at least with of 4 is required");

                if (columnsToCarve < 1 || columnsToCarve > imageWidth - 3)
                    throw new ArgumentOutOfRangeException($"Number of columns to carve is out of range: 1 - {imageWidth - 3}");

                AllocatePixelBuffersForVCarving(imageWidth, imageHeight, verticalCarving: true, out byte[,] r, out byte[,] g, out byte[,] b, out byte[,] a, out int[,] energyMap, out int[,] seamMap, out int[] seamVector);

                TransformToSoaRgba(image, imageWidth, imageHeight, verticalCarving: true, r, g, b, a);

                RemoveNVerticalSeams(columnsToCarve, imageWidth, imageHeight, r, g, b, a, energyMap, seamMap, seamVector, cancel);

                TransformToAosRgba(image, imageWidth, imageHeight, true, r, g, b, a);

                if (crop)
                    image.CropRightColumns(columnsToCarve);

                image.Save(outFs, outputFormat);
            }
        }

        static void RemoveNVerticalSeams(int n, int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap, int[,] seamMap, int[] seamVector, CancellationToken cancel)
        {
            int w = width;

            for (int i = 0; i < n; i++)
            {
                cancel.ThrowIfCancellationRequested();
                CalculateEnergyMap(w, height, verticalCarving: true, r, g, b, a, energyMap);
                ConvertEnergyMapToVerticalSeamMap(energyMap, w, height, seamMap);
                CalculateMinimalVerticalSeam(seamMap, w, height, seamVector);
                RemoveVerticalSeamPixels(seamVector, w, height, r, g, b, a);

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
        /// <summary></summary>
        /// <param name="width">width of actual working area</param>
        /// <param name="height">height of actual working area</param>                
        static void CalculateEnergyMap(int width, int height, bool verticalCarving, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap)
        {
            if (verticalCarving)
            {
                CalculateNonBorderEnergy(width, height, r, g, b, a, energyMap);
                CalculateCornerEnergyTopLeft(width, height, r, g, b, a, energyMap);
                CalculateCornerEnergyTopRight(width, height, r, g, b, a, energyMap);
                CalculateCornerEnergyBottomLeft(width, height, r, g, b, a, energyMap);
                CalculateCornerEnergyBottomRight(width, height, r, g, b, a, energyMap);
                CalculateBorderEnergyVertical(width, height, r, g, b, a, energyMap);
                CalculateBorderEnergyHorizontal(width, height, r, g, b, a, energyMap);
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

            //fixed (int* ePtr = &energyMap[0, 0])
            //{

            //    for (int row = 1; row < height; row++)
            //    {
            //        int* eP = ePtr + row * imageWidth;
            //        *eP += min3(int.MaxValue, *(eP - imageWidth), *(eP - imageWidth + 1));


            //        for (int col = 1; col < width - 1; col++)
            //        {
            //            eP++;
            //            *eP += min3(*(eP - imageWidth - 1), *(eP - imageWidth), *(eP - imageWidth + 1));
            //        }

            //        ++eP;
            //        *eP += min3(*(eP - imageWidth - 1), *(eP - imageWidth), int.MaxValue);
            //    }
            //    int min3(int a, int b, int c) => a < b ? (a < c ? a : c) : (b < c ? b : c); // TODO => check if less branchy implementation exists
            //}

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

                    *sP = *eP + min3(int.MaxValue, *(eP - imageWidth), *(eP - imageWidth + 1));

                    for (int col = 1; col < width - 1; col++)
                    {
                        eP++; sP++;
                        *sP = *eP + min3(*(eP - imageWidth - 1), *(eP - imageWidth), *(eP - imageWidth + 1));
                    }

                    ++eP; ++sP;

                    *sP = *eP + min3(*(eP - imageWidth - 1), *(eP - imageWidth), int.MaxValue);
                }
                int min3(int a, int b, int c) => a < b ? (a < c ? a : c) : (b < c ? b : c); // TODO => check if less branchy implementation exists
            }

        }

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

        // TODO Sqrt must be vectorized (causes huge slowdown, float sqrt wouldnt loose precision)
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

                        //*eP = dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;
                        *eP = (int)Math.Round(Math.Sqrt((double)dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a));

                        eP++; rP++; gP++; bP++; aP++;
                    }
                }
            }
        }

        private static void CalculateBorderEnergyHorizontal(int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap)
        {
            for (int col = 1; col < width - 1; col++)
            {
                var dx2r = (r[0, col + 1] - r[0, col - 1]); dx2r *= dx2r;
                var dx2g = (g[0, col + 1] - g[0, col - 1]); dx2g *= dx2g;
                var dx2b = (b[0, col + 1] - b[0, col - 1]); dx2b *= dx2b;
                var dx2a = (a[0, col + 1] - a[0, col - 1]); dx2a *= dx2a;

                var dy2r = (r[1, col] - r[height - 1, col]); dy2r *= dy2r;
                var dy2g = (g[1, col] - g[height - 1, col]); dy2g *= dy2g;
                var dy2b = (b[1, col] - b[height - 1, col]); dy2b *= dy2b;
                var dy2a = (a[1, col] - a[height - 1, col]); dy2a *= dy2a;

                energyMap[0, col] = /*dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;*/
                    (int)Math.Round(Math.Sqrt((double)dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a));

                dx2r = (r[height - 1, col + 1] - r[height - 1, col - 1]); dx2r *= dx2r;
                dx2g = (g[height - 1, col + 1] - g[height - 1, col - 1]); dx2g *= dx2g;
                dx2b = (b[height - 1, col + 1] - b[height - 1, col - 1]); dx2b *= dx2b;
                dx2a = (a[height - 1, col + 1] - a[height - 1, col - 1]); dx2a *= dx2a;

                dy2r = (r[0, col] - r[height - 2, col]); dy2r *= dy2r;
                dy2g = (g[0, col] - g[height - 2, col]); dy2g *= dy2g;
                dy2b = (b[0, col] - b[height - 2, col]); dy2b *= dy2b;
                dy2a = (a[0, col] - a[height - 2, col]); dy2a *= dy2a;

                energyMap[height - 1, col] = /*dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a*/
                    (int)Math.Round(Math.Sqrt((double)dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a));
            }
        }

        private static void CalculateBorderEnergyVertical(int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap)
        {
            for (int row = 1; row < height - 1; row++)
            {
                var dx2r = (r[row, 1] - r[row, width - 1]); dx2r *= dx2r;
                var dx2g = (g[row, 1] - g[row, width - 1]); dx2g *= dx2g;
                var dx2b = (b[row, 1] - b[row, width - 1]); dx2b *= dx2b;
                var dx2a = (a[row, 1] - a[row, width - 1]); dx2a *= dx2a;

                var dy2r = (r[row + 1, 0] - r[row - 1, 0]); dy2r *= dy2r;
                var dy2g = (g[row + 1, 0] - g[row - 1, 0]); dy2g *= dy2g;
                var dy2b = (b[row + 1, 0] - b[row - 1, 0]); dy2b *= dy2b;
                var dy2a = (a[row + 1, 0] - a[row - 1, 0]); dy2a *= dy2a;

                energyMap[row, 0] = /*dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;*/
                    (int)Math.Round(Math.Sqrt((double)dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a));


                dx2r = (r[row, 0] - r[row, width - 2]); dx2r *= dx2r;
                dx2g = (g[row, 0] - g[row, width - 2]); dx2g *= dx2g;
                dx2b = (b[row, 0] - b[row, width - 2]); dx2b *= dx2b;
                dx2a = (a[row, 0] - a[row, width - 2]); dx2a *= dx2a;

                dy2r = (r[row + 1, width - 1] - r[row - 1, width - 1]); dy2r *= dy2r;
                dy2g = (g[row + 1, width - 1] - g[row - 1, width - 1]); dy2g *= dy2g;
                dy2b = (b[row + 1, width - 1] - b[row - 1, width - 1]); dy2b *= dy2b;
                dy2a = (a[row + 1, width - 1] - a[row - 1, width - 1]); dy2a *= dy2a;

                energyMap[row, width - 1] = /*dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;*/
                    (int)Math.Round(Math.Sqrt((double)dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a));
            }
        }

        private static void CalculateCornerEnergyTopLeft(int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap)
        {
            var dx2r = r[0, 1] - r[0, width - 1]; dx2r *= dx2r;
            var dx2g = g[0, 1] - g[0, width - 1]; dx2g *= dx2g;
            var dx2b = b[0, 1] - b[0, width - 1]; dx2b *= dx2b;
            var dx2a = a[0, 1] - a[0, width - 1]; dx2a *= dx2a;

            var dy2r = (r[1, 0] - r[height - 1, 0]); dy2r *= dy2r;
            var dy2g = (g[1, 0] - g[height - 1, 0]); dy2g *= dy2g;
            var dy2b = (b[1, 0] - b[height - 1, 0]); dy2b *= dy2b;
            var dy2a = (a[1, 0] - a[height - 1, 0]); dy2a *= dy2a;

            energyMap[0, 0] = /*dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;*/
                (int)Math.Round(Math.Sqrt((double)dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a));
        }

        private static void CalculateCornerEnergyTopRight(int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap)
        {
            var dx2r = r[0, 0] - r[0, width - 2]; dx2r *= dx2r;
            var dx2g = g[0, 0] - g[0, width - 2]; dx2g *= dx2g;
            var dx2b = b[0, 0] - b[0, width - 2]; dx2b *= dx2b;
            var dx2a = a[0, 0] - a[0, width - 2]; dx2a *= dx2a;

            var dy2r = (r[1, width - 1] - r[height - 1, width - 1]); dy2r *= dy2r;
            var dy2g = (g[1, width - 1] - g[height - 1, width - 1]); dy2g *= dy2g;
            var dy2b = (b[1, width - 1] - b[height - 1, width - 1]); dy2b *= dy2b;
            var dy2a = (a[1, width - 1] - a[height - 1, width - 1]); dy2a *= dy2a;

            energyMap[0, width - 1] = /*dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;*/
                (int)Math.Round(Math.Sqrt((double)dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a));
        }

        private static void CalculateCornerEnergyBottomLeft(int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap)
        {
            var dx2r = r[height - 1, 1] - r[height - 1, width - 1]; dx2r *= dx2r;
            var dx2g = g[height - 1, 1] - g[height - 1, width - 1]; dx2g *= dx2g;
            var dx2b = b[height - 1, 1] - b[height - 1, width - 1]; dx2b *= dx2b;
            var dx2a = a[height - 1, 1] - a[height - 1, width - 1]; dx2a *= dx2a;

            var dy2r = (r[0, 0] - r[height - 2, 0]); dy2r *= dy2r;
            var dy2g = (g[0, 0] - g[height - 2, 0]); dy2g *= dy2g;
            var dy2b = (b[0, 0] - b[height - 2, 0]); dy2b *= dy2b;
            var dy2a = (a[0, 0] - a[height - 2, 0]); dy2a *= dy2a;

            energyMap[height - 1, 0] = /*dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;*/
                (int)Math.Round(Math.Sqrt((double)dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a));
        }

        private static void CalculateCornerEnergyBottomRight(int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap)
        {
            var dx2r = r[height - 1, 0] - r[height - 1, width - 2]; dx2r *= dx2r;
            var dx2g = g[height - 1, 0] - g[height - 1, width - 2]; dx2g *= dx2g;
            var dx2b = b[height - 1, 0] - b[height - 1, width - 2]; dx2b *= dx2b;
            var dx2a = a[height - 1, 0] - a[height - 1, width - 2]; dx2a *= dx2a;

            var dy2r = (r[0, width - 1] - r[height - 2, width - 1]); dy2r *= dy2r;
            var dy2g = (g[0, width - 1] - g[height - 2, width - 1]); dy2g *= dy2g;
            var dy2b = (b[0, width - 1] - b[height - 2, width - 1]); dy2b *= dy2b;
            var dy2a = (a[0, width - 1] - a[height - 2, width - 1]); dy2a *= dy2a;

            energyMap[height - 1, width - 1] = /*dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;*/
                (int)Math.Round(Math.Sqrt((double)dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a));
        }

        // TODO Refactor to use Span<int> instead of Image<T> (decouple algorithm from image API)
        // TODO make this fast
        /// <summary>
        /// Transforms RGBA representation to SoA arrays
        /// </summary>
        /// <param name="image"></param>
        /// <param name="width">width of picture</param>
        /// <param name="height">height of picture</param>
        /// <param name="verticalCarving">true is indicating vertical carving </param>
        static void TransformToSoaRgba(IImageWrapper image, int width, int height, bool verticalCarving, byte[,] r, byte[,] g, byte[,] b, byte[,] a)
        {
            if (verticalCarving)
            {
                for (int rowIdx = 0; rowIdx < height; rowIdx++)
                {
                    var rowPixels = image.GetRow(rowIdx);

                    for (int col = 0; col < width; col++)
                    {
                        var pixel = rowPixels[col];
                        r[rowIdx, col] = (byte)(pixel);
                        g[rowIdx, col] = (byte)(pixel >> 8);
                        b[rowIdx, col] = (byte)(pixel >> 16);
                        a[rowIdx, col] = (byte)(pixel >> 24);
                    }
                }
            }
            else
                throw new NotImplementedException();
        }

        // TODO Refactor to use Span<int> instead of Image<T> (decouple algorithm from image API)
        /// <summary>
        /// Transform from SoA to AoS representation
        /// </summary>
        /// <param name="image"></param>
        /// <param name="verticalCarving"></param>        
        static unsafe void TransformToAosRgba(IImageWrapper image, int width, int height, bool verticalCarving, byte[,] r, byte[,] g, byte[,] b, byte[,] a)
        {
            // TODO make fast, vectorize, make all array access ptr based
            if (verticalCarving)
            {
                uint pix = 0;
                byte* pPix0 = (byte*)&pix;
                byte* pPix = (byte*)&pix;

                for (int row = 0; row < height; row++)
                {
                    var rgbaRow = image.GetRow(row);

                    for (int col = 0; col < width; col++)
                    {
                        *(pPix++) = r[row, col];
                        *(pPix++) = g[row, col];
                        *(pPix++) = b[row, col];
                        *pPix = a[row, col];
                        rgbaRow[col] = pix;
                        pPix = pPix0;
                    }
                }
            }
            else
                throw new NotImplementedException();
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
