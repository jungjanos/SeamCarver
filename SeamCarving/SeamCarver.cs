using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamCarving
{
    public class SeamCarverH
    {
        private Bitmap bitmap;
        private List<List<Color>> pixelList;
        private List<List<int>> valueMap;
        private int height;
        private int width;
        private Int16[] sqrtLookup;
        private int[] sqr;


        public SeamCarverH(Bitmap bitmap)
        {
            this.bitmap = bitmap;
            height = this.bitmap.Height;
            width = this.bitmap.Width;


            //Setting up square root lookup table
            sqrtLookup = new Int16[520201];
            for (int i = 0; i < 520201; ++i)
            {
                sqrtLookup[i] = (Int16)Math.Round(Math.Sqrt(i)); 
            }

            sqr = new int[256];
            for (int i=0; i< 256; ++i)
            {
                sqr[i] = i * i;
            }


            // Setting up a multi dimensional pixel list for holding Color values of the pixels
            // pixelList[x][y] == Color value for the pixel with at (x,y) coordinates
            pixelList = new List<List<Color>>(width);
            for (int i =0; i < width; ++i)
            {
                pixelList.Add(new List<Color>(height));
            }

            // filling the multi dimension list with Color values from the BMP file source
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    pixelList[x].Add (bitmap.GetPixel(x, y));
                }
            }

            valueMap = new List<List<int>>(width);
            for (int x = 0; x<width; ++x)
            {
                valueMap.Add(new List<int>(height));
            }

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    valueMap[x].Add(-1);
                }
            }

            //calculating the value of non-border pixels (dual gradient function)
            for (int x = 1; x < width - 1; ++x)
            {
                for (int y = 1; y < height - 1; ++y)
                {
                    valueMap[x][y] = sqrtLookup[
                        sqr[Math.Abs((pixelList[x + 1][y].R - pixelList[x - 1][y].R))]
                        + sqr[Math.Abs((pixelList[x + 1][y].G - pixelList[x - 1][y].G))]
                        + sqr[Math.Abs((pixelList[x + 1][y].B - pixelList[x - 1][y].B))]

                        + sqr[Math.Abs((pixelList[x][y + 1].R - pixelList[x][y - 1].R))]
                        + sqr[Math.Abs((pixelList[x][y + 1].G - pixelList[x][y - 1].G))]
                        + sqr[Math.Abs((pixelList[x][y + 1].B - pixelList[x][y - 1].B))]
                        ];
                }
            }


        }
        //public SeamCarver(string fullFilePath) { this.bitmap = new Bitmap(fullFilePath); }



        //public int EnergySQR(int x, int y) { }
        //public List<int> FindHorizontalSeam() { }
        //public List<int> FindVerticalSeam() { }
        public void RemoveHorizontalSeam(List<int> seam) { }
        public void RemoveVerticalSeam(List<int> seam) { }        

    }
}
