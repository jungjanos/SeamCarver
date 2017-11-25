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
        private List<List<int>> seamMap;
        private int height;
        private int width;
        private Int16[] sqrtLookup;
        private int[] sqr;
        public bool SeamMapSetUp { set; get; }


        public SeamCarverH(Bitmap bitmap)
        {
            SeamMapSetUp = false;
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
                        //+ sqr[Math.Abs((pixelList[x + 1][y].A - pixelList[x - 1][y].A))]


                        + sqr[Math.Abs((pixelList[x][y + 1].R - pixelList[x][y - 1].R))]
                        + sqr[Math.Abs((pixelList[x][y + 1].G - pixelList[x][y - 1].G))]
                        + sqr[Math.Abs((pixelList[x][y + 1].B - pixelList[x][y - 1].B))]
                        //+ sqr[Math.Abs((pixelList[x][y + 1].A - pixelList[x][y - 1].A))]
                        ];
                }
            }

            //filling horizontal border pixel values with MAX numbers (to avoid carving the borders)
            for (int x = 0; x < width; ++x)
            {
                valueMap[x][0] = sqrtLookup[sqr[255] * 6];
                valueMap[x][height-1] = sqrtLookup[sqr[255] * 6];
            }

            //filling vertical border pixel values with MAX numbers (to avoid carving the borders)
            for (int y = 1; y < height-1; ++y)
            {
                valueMap[0][y] = sqrtLookup[sqr[255] * 6];
                valueMap[width-1][y] = sqrtLookup[sqr[255] * 6];
            }
            seamMapSetUp();
        }
        //public SeamCarver(string fullFilePath) { this.bitmap = new Bitmap(fullFilePath); }
        

        public int PixelValue(int x, int y)
        {
            return valueMap[x][y];
        }



        //returns the smallest of its arguments, if two are equal and both minimum, 
        //it returns the later of the two
        private int min3(int a, int b, int c)
        {
            int helper;
            if (a < b){helper = a;}
            else { helper = b;}
            if (helper < c){return helper;}
            else { return c;}
        }

        // returns the index of the smallest array element, if two are equal and both minimum, 
        //it returns the index of the later of the two        
        private int min3Index(int A, int indexOfA, int B, int indexOfB, int C, int indexOfC)
        {
            int helper;
            int indexHelper;

            if (A < B) { helper = A; indexHelper = indexOfA; }
            else { helper = B; indexHelper = indexOfB; }
            if (helper < C) { return indexHelper; }
            else { return indexOfC; }
        }

        private void seamMapSetUp()
        {
            if (SeamMapSetUp) return;
            else
            {
                // allocating seamMap and initializing it with dummy values
                seamMap = new List<List<int>>(width);
                for (int x = 0; x < width; ++x)
                {
                    seamMap.Add(new List<int>(height));
                    for (int y = 0; y < height; ++y)
                    {
                        seamMap[x].Add(-1);
                    }
                }
                //building the seamMap according to the valueMap

                //first column comes directly from the valueMap
                for (int y = 0; y < height; ++y)
                {
                    seamMap[0][y] = valueMap[0][y];
                }

                for (int x = 1; x < width; ++x)
                {
                    // x, y=0 pixel is an "edge case", int.MaxValue is a placeholder for min3(...)
                    seamMap[x][0] = valueMap[x][0] + min3(seamMap[x - 1][1], seamMap[x - 1][0], int.MaxValue);

                    for (int y = 1; y < height-1; ++y)
                    {
                        seamMap[x][y] = valueMap[x][y] + min3(seamMap[x - 1][y - 1], seamMap[x - 1][y + 1], seamMap[x - 1][y]);
                    }

                    // x, y=height-1 pixel is an "edge case", int.MaxValue is a placeholder for min3(...)
                    seamMap[x][height-1] = valueMap[x][height-1] + min3(seamMap[x - 1][height-2], seamMap[x - 1][height-1], int.MaxValue);
                }
                SeamMapSetUp = true;
            }
        }

        public int[] FindHorizontalSeam()
        {
            //will store the to be carved horizontal seam
            //indexes of the array are the x coordinates
            //stored elements of the array are the corresponding y coordinates
            int[] horizontalSeam = new int [width];
            int helper;
            seamMapSetUp();


            horizontalSeam[width - 1] = seamMap[width - 1].FindIndex(a => a.Equals(seamMap[width - 1].Min()));
            
            for (int x = width -2; x >= 0; --x)
            {
                helper = horizontalSeam[x + 1];


                if (helper == 0)
                {
                    horizontalSeam[x] = min3Index(
                                         //Edge case, not applicable: seamMap[x][helper - 1], helper - 1, 
                                         //next line is filler for min3Index(...)
                                         int.MaxValue, -1, //  <---- this cant happen and also causes exception
                                         seamMap[x][helper + 1], helper + 1,
                                         seamMap[x][helper], helper
                                                 );
                }
                else
                {
                    if (helper == height - 1)
                    {
                        horizontalSeam[x] = min3Index(
                                                seamMap[x][helper - 1], helper - 1,
                                                //Edge case, not applicable: seamMap[x][helper + 1], helper + 1,
                                                //next line is filler for min3Index(...)
                                                int.MaxValue, -1, //  <---- this cant happen and also causes exception
                                                seamMap[x][helper], helper
                                            );
                    }
                    else
                    {
                        horizontalSeam[x] = min3Index(
                                                seamMap[x][helper - 1], helper - 1,
                                                seamMap[x][helper + 1], helper + 1,
                                                seamMap[x][helper], helper
                                                    );
                    }
                }
            }
            return horizontalSeam;
        }

        //public List<int> FindVerticalSeam() { }


        
        //private int getPixelValue(int x, int y)
        //{
        //    if (x == 0)
        //    {

        //    }
        //}

        public void RemoveHorizontalSeam(int[] seam)
        {
            
            //removes the seam from the pixelList, making each column shorter by one element : height -> height-1
            for (int x = 0; x < width; ++x)
            {
                pixelList[x].RemoveAt(seam[x]);
            }

            throw new Exception("Not implemented");
            


        }

        public void RemoveVerticalSeam(int[] seam) { }        

    }
}
