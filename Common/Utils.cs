using System;
using System.Runtime.CompilerServices;

namespace Common
{
    public static class Utils
    {
        public static void GuardNotNull<T>(T obj, [CallerMemberName] string name = "unknown")
        {
            if (obj == null)
                throw new ArgumentNullException($"Method {name} was passed a null value (parameter type {typeof(T)}) ");
        }

        /// <summary>
        /// Transforms RGBA representation to SoA arrays
        /// </summary>
        /// <param name="image"></param>
        /// <param name="width">width of picture</param>
        /// <param name="height">height of picture</param>
        /// <param name="verticalCarving">true is indicating vertical carving </param>
        public static void TransformToSoaRgba(IImageWrapper image, int width, int height, bool verticalCarving, byte[,] r, byte[,] g, byte[,] b, byte[,] a)
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

        /// <summary>
        /// Transform from SoA to AoS representation
        /// </summary>
        /// <param name="image"></param>
        /// <param name="verticalCarving"></param>        
        public static unsafe void TransformToAosRgba(IImageWrapper image, int width, int height, bool verticalCarving, byte[,] r, byte[,] g, byte[,] b, byte[,] a)
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
    }
}
