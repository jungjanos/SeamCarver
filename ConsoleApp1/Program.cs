﻿using SeamCarver;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();
            //using (Image<Rgba32> image = (Image<Rgba32>)Image<Rgba32>.Load(@"Test1Sm.bmp"))
            //using (Image<Rgba32> image = (Image<Rgba32>)Image<Rgba32>.Load(@"Sample.jpg"))
            using (Image<Rgba32> image = (Image<Rgba32>)Image<Rgba32>.Load(@"LeetCode_SpanT.jpg"))
            //using (Image<Rgba32> image = (Image<Rgba32>)Image<Rgba32>.Load(@"Sample.bmp"))
            //using (Image<Rgba32> image3 = (Image<Rgba32>)Image<Rgba32>.Load(@"Sample.png"))
            {
                var height = image.Height;
                var width = image.Width;

                AllocatePixelBuffersForVCarving(width, height, verticalCarving: true, out byte[,] r, out byte[,] g, out byte[,] b, out byte[,] a, out int[,] energyMap, out int[] seamVector);

                TransformToSoaRgba(image, width, height, verticalCarving: true, r, g, b, a);

                RemoveNVerticalSeams(1000, width, height, r, g, b, a, energyMap, seamVector);

                TransformToAosRgba(image, width, height, true, r, g, b, a);

                image.SaveAsBmp(File.OpenWrite("tmp.bmp"));
            }
            sw.Stop();
            Console.WriteLine($"total seconds: {sw.Elapsed.TotalSeconds}");

        }

        /// <summary>
        /// Removes the specified number of seams from the image. All buffers are supplied by the caller. RGBA SoA must be pre-populated
        /// </summary>
        /// <param name="n">number of vertical seams to remove</param>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>

        static void RemoveNVerticalSeams(int n, int width, int height, byte[,] r, byte[,] g, byte[,] b, byte[,] a, int[,] energyMap, int[] seamVector)
        {
            int w = width;
            int h = height;

            for (int i = 0; i < n; i++)
            {
                CalculateEnergyMap(w, h, verticalCarving: true, r, g, b, a, energyMap);

                ConvertEnergyMapToVerticalSeamMap(energyMap, w, h);

                CalculateMinimalVerticalSeam(energyMap, w, h, seamVector);

                RemoveVerticalSeamPixels(seamVector, w, h, r, g, b, a);

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
        static void ConvertEnergyMapToVerticalSeamMap(int[,] energyMap, int width, int height)
        {
            for (int row = 1; row < height; row++)
            {
                energyMap[row, 0] += min3(int.MaxValue, energyMap[row - 1, 0], energyMap[row - 1, 1]);

                for (int col = 1; col < width - 1; col++)
                    energyMap[row, col] += min3(energyMap[row - 1, col - 1], energyMap[row - 1, col], energyMap[row - 1, col + 1]);

                energyMap[row, width - 1] += min3(energyMap[row - 1, width - 2], energyMap[row - 1, width - 1], int.MaxValue);
            }
            int min3(int a, int b, int c) => a < b ? (a < c ? a : c) : (b < c ? b : c); // TODO => check if less branchy implementation exists
        }

        /// <summary>
        /// Calculates the horizontal indexes of a vertical seam
        /// </summary>
        /// <param name="seamMap"> [height,width] map of energy calculated for each pixel </param>        
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


                //for (int row = 1; row < height - 1; row++)
                //{
                //    // accounting for row major layout
                //    for (int col = 1; col < width - 1; col++)
                //    {
                //        var dx2r = (r[row, col + 1] - r[row, col - 1]); dx2r *= dx2r;
                //        var dx2g = (g[row, col + 1] - g[row, col - 1]); dx2g *= dx2g;
                //        var dx2b = (b[row, col + 1] - b[row, col - 1]); dx2b *= dx2b;
                //        var dx2a = (a[row, col + 1] - a[row, col - 1]); dx2a *= dx2a;


                //        var dy2r = (r[row + 1, col] - r[row - 1, col]); dy2r *= dy2r;
                //        var dy2g = (g[row + 1, col] - g[row - 1, col]); dy2g *= dy2g;
                //        var dy2b = (b[row + 1, col] - b[row - 1, col]); dy2b *= dy2b;
                //        var dy2a = (a[row + 1, col] - a[row - 1, col]); dy2a *= dy2a;

                //        energyMap[row, col] = dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;
                //    }
                //}

                for (int row = 1; row < height - 1; row++)
                {
                    eP = ePtr + row * pictureWidth;
                    rP = rPtr + row * pictureWidth;
                    gP = gPtr + row * pictureWidth;
                    bP = bPtr + row * pictureWidth;
                    aP = aPtr + row * pictureWidth;

                    for (int col = 1; col < width - 1; col++)
                    {
                        //var dx2r = (r[row, col + 1] - r[row, col - 1]); dx2r *= dx2r;
                        var dx2r = *(rP + 1) - *(rP - 1); dx2r *= dx2r;
                        Debug.Assert(r[row, col + 1] - r[row, col - 1] == *(rP + 1) - *(rP - 1));


                        //var dx2g = (g[row, col + 1] - g[row, col - 1]); dx2g *= dx2g;
                        var dx2g = *(gP + 1) - *(gP - 1); dx2g *= dx2g;
                        Debug.Assert(g[row, col + 1] - g[row, col - 1] == *(gP + 1) - *(gP - 1));

                        //var dx2b = (b[row, col + 1] - b[row, col - 1]); dx2b *= dx2b;
                        var dx2b = *(bP + 1) - *(bP - 1); dx2b *= dx2b;
                        Debug.Assert(b[row, col + 1] - b[row, col - 1] == *(bP + 1) - *(bP - 1));

                        //var dx2a = (a[row, col + 1] - a[row, col - 1]); dx2a *= dx2a;
                        var dx2a = *(aP + 1) - *(aP - 1); dx2a *= dx2a;
                        Debug.Assert(a[row, col + 1] - a[row, col - 1] == *(aP + 1) - *(aP - 1));

                        //var dy2r = (r[row + 1, col] - r[row - 1, col]); dy2r *= dy2r;
                        var dy2r = *(rP + pictureWidth) - *(rP - pictureWidth); dy2r *= dy2r;
                        Debug.Assert(r[row + 1, col] - r[row - 1, col] == *(rP + pictureWidth) - *(rP - pictureWidth));

                        //var dy2g = (g[row + 1, col] - g[row - 1, col]); dy2g *= dy2g;
                        var dy2g = *(gP + pictureWidth) - *(gP - pictureWidth); dy2g *= dy2g;
                        Debug.Assert(g[row + 1, col] - g[row - 1, col] == *(gP + pictureWidth) - *(gP - pictureWidth));

                        //var dy2b = (b[row + 1, col] - b[row - 1, col]); dy2b *= dy2b;
                        var dy2b = *(bP + pictureWidth) - *(bP - pictureWidth); dy2b *= dy2b;
                        Debug.Assert(b[row + 1, col] - b[row - 1, col] == *(bP + pictureWidth) - *(bP - pictureWidth));

                        //var dy2a = (a[row + 1, col] - a[row - 1, col]); dy2a *= dy2a;
                        var dy2a = *(aP + pictureWidth) - *(aP - pictureWidth); dy2a *= dy2a;
                        Debug.Assert(a[row + 1, col] - a[row - 1, col] == *(aP + pictureWidth) - *(aP - pictureWidth));


                         *eP = dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;
                        //energyMap[row, col]
                        Debug.Assert(eP == ePtr+ row*pictureWidth+col/*-1*/);

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

                energyMap[0, col] = dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;


                dx2r = (r[height - 1, col + 1] - r[height - 1, col - 1]); dx2r *= dx2r;
                dx2g = (g[height - 1, col + 1] - g[height - 1, col - 1]); dx2g *= dx2g;
                dx2b = (b[height - 1, col + 1] - b[height - 1, col - 1]); dx2b *= dx2b;
                dx2a = (a[height - 1, col + 1] - a[height - 1, col - 1]); dx2a *= dx2a;

                dy2r = (r[0, col] - r[height - 2, col]); dy2r *= dy2r;
                dy2g = (g[0, col] - g[height - 2, col]); dy2g *= dy2g;
                dy2b = (b[0, col] - b[height - 2, col]); dy2b *= dy2b;
                dy2a = (a[0, col] - a[height - 2, col]); dy2a *= dy2a;

                energyMap[height - 1, col] = dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;
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

                energyMap[row, 0] = dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;


                dx2r = (r[row, 0] - r[row, width - 2]); dx2r *= dx2r;
                dx2g = (g[row, 0] - g[row, width - 2]); dx2g *= dx2g;
                dx2b = (b[row, 0] - b[row, width - 2]); dx2b *= dx2b;
                dx2a = (a[row, 0] - a[row, width - 2]); dx2a *= dx2a;

                dy2r = (r[row + 1, width - 1] - r[row - 1, width - 1]); dy2r *= dy2r;
                dy2g = (g[row + 1, width - 1] - g[row - 1, width - 1]); dy2g *= dy2g;
                dy2b = (b[row + 1, width - 1] - b[row - 1, width - 1]); dy2b *= dy2b;
                dy2a = (a[row + 1, width - 1] - a[row - 1, width - 1]); dy2a *= dy2a;

                energyMap[row, width - 1] = dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;
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

            energyMap[0, 0] = dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;
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

            energyMap[0, width - 1] = dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;
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

            energyMap[height - 1, 0] = dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;
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

            energyMap[height - 1, width - 1] = dx2r + dx2g + dx2b + dx2a + dy2r + dy2g + dy2b + dy2a;
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
        static void TransformToSoaRgba(Image<Rgba32> image, int width, int height, bool verticalCarving, byte[,] r, byte[,] g, byte[,] b, byte[,] a)
        {
            if (verticalCarving)
            {
                for (int rowIdx = 0; rowIdx < height; rowIdx++)
                {
                    var rowPixels = image.GetPixelRowSpan(rowIdx);

                    for (int col = 0; col < width; col++)
                    {
                        var pixel = rowPixels[col];
                        r[rowIdx, col] = pixel.R;
                        g[rowIdx, col] = pixel.G;
                        b[rowIdx, col] = pixel.B;
                        a[rowIdx, col] = pixel.A;
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
                // every access is an L1$ miss(?!) 
                for (int rowIdx = 0; rowIdx < height; rowIdx++)
                {
                    var rowPixels = image.GetPixelRowSpan(rowIdx);

                    for (int col = 0; col < width; col++)
                    {
                        var pixel = rowPixels[col];
                        r[col, rowIdx] = pixel.R;
                        g[col, rowIdx] = pixel.G;
                        b[col, rowIdx] = pixel.B;
                        a[col, rowIdx] = pixel.A;
                    }
                }
            }
        }

        // TODO Refactor to use Span<int> instead of Image<T> (decouple algorithm from image API)
        /// <summary>
        /// Transform from SoA to AoS representation
        /// </summary>
        /// <param name="image"></param>
        /// <param name="verticalCarving"></param>        
        static unsafe void TransformToAosRgba(Image<Rgba32> image, int width, int height, bool verticalCarving, byte[,] r, byte[,] g, byte[,] b, byte[,] a)
        {
            // TODO make fast, vectorize, make all array access ptr based
            if (verticalCarving)
            {
                uint pix = 0;
                byte* pPix0 = (byte*)&pix;
                byte* pPix = (byte*)&pix;

                for (int row = 0; row < height; row++)
                {
                    var rgbaRow = MemoryMarshal.Cast<Rgba32, uint>(image.GetPixelRowSpan(row));

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
        static void AllocatePixelBuffersForVCarving(int width, int height, bool verticalCarving, out byte[,] r, out byte[,] g, out byte[,] b, out byte[,] a, out int[,] energyMap, out int[] seamPath)
        {
            if (verticalCarving) // y before x
            {
                r = new byte[height, width];
                g = new byte[height, width];
                b = new byte[height, width];
                a = new byte[height, width];
                energyMap = new int[height, width];
                seamPath = new int[height];
            }
            else // x before y
            {
                throw new NotImplementedException("");
                //r = new byte[width, height];
                //g = new byte[width, height];
                //b = new byte[width, height];
                //a = new byte[width, height];
                //energyMap = new int[width, height];
                //seamVector = new int[height];
            }
        }
    }
}
