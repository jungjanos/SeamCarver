using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamCarving
{
    public class SeamCarverH
    {
        private Bitmap bitmap;
        private LockBitmap lockBitmap;
        private List<List<Color>> pixelList;
        private List<List<int>> valueMap;
        private List<List<int>> seamMap;
        private int height;
        private int width;
        private Int16[] sqrtLookup;
        private int[] sqr;
        public bool SeamMapSetUp { set; get; }

        private List<ResultInfoItem> messageList;
        private object messageListLOCK;


        private BusinessLogic parent;
        private string fileName;
        private Stopwatch stopwatch;


        private void loadBitmap (string fileName)
        {
            Stopwatch stopwatch =Stopwatch.StartNew();
            this.bitmap = new Bitmap(this.fileName);
            height = this.bitmap.Height;
            width = this.bitmap.Width;
            parent.ImageWorkingSize = bitmap.Size;


            lockBitmap = new LockBitmap(bitmap);
            lockBitmap.LockBits();

            stopwatch.Stop();
            ResultInfoItem resultInfoItem = new ResultInfoItem
            {
                Message = stopwatch.ElapsedMilliseconds
                                             .ToString() + "ms" + " - image loaded as " +
                                             "file from: " + fileName
            };
            lock (messageListLOCK)
            {
                this.messageList.Add(resultInfoItem);
            }
        }

        public SeamCarverH(string fileName, List<ResultInfoItem> messageList, BusinessLogic parent)
        {
            SeamMapSetUp = false;
            this.messageList = messageList;
            this.parent = parent;
            this.fileName = fileName;

            messageListLOCK = new object();
            stopwatch = new Stopwatch();           

            Task loadBitmapTask = Task.Factory.StartNew(() => loadBitmap(this.fileName));

                       

            //Setting up square root lookup table
            //setting up square lookup table
            {
                stopwatch.Start();                
                sqrtLookup = new Int16[520201];
                for (int i = 0; i < 520201; ++i)
                {
                    sqrtLookup[i] = (Int16)Math.Round(Math.Sqrt(i));
                }
                
                sqr = new int[256];
                for (int i = 0; i < 256; ++i)
                {
                    sqr[i] = i * i;
                }

                stopwatch.Stop();
                this.messageList.Add(new ResultInfoItem { Message =
                                    stopwatch.ElapsedMilliseconds.ToString() +
                                    "ms" + " - set up square root and square lookup tables "});
                stopwatch.Reset();
            }
            loadBitmapTask.Wait();
            // Setting up a multi dimensional pixel list for holding Color values of the pixels
            // pixelList[x][y] == Color value for the pixel with at (x,y) coordinates
            {
                stopwatch.Start();
                pixelList = new List<List<Color>>(width);
                for (int i = 0; i < width; ++i)
                {
                    pixelList.Add(new List<Color>(height));
                }
                stopwatch.Stop();
                this.messageList.Add(new ResultInfoItem
                {
                    Message = stopwatch.ElapsedMilliseconds.ToString() +
                                    "ms" + " - set up " + width + " X " + height + 
                                    " size List<List<Color>> matrix for holding pixel information"});
                stopwatch.Reset();
            }
            // filling the multi dimension list with Color values from the BMP file source
            {
                stopwatch.Start();
                for (int x = 0; x < width; ++x)
                {
                    for (int y = 0; y < height; ++y)
                    {
                        pixelList[x].Add(lockBitmap.GetPixel(x, y));
                    }
                }
                stopwatch.Stop();
                this.messageList.Add(new ResultInfoItem
                {
                    Message = stopwatch.ElapsedMilliseconds.ToString() +
                                    "ms" + " - filling the List<List<Color>> matrix width Color values, method: LockBitmap.GetPixel(int x, int y)"});
                stopwatch.Reset();
            }

            //setting up multi-dimensional list for holding pixel value information, it is the so called "energy value"
            {
                stopwatch.Start();
                valueMap = new List<List<int>>(width);
                for (int x = 0; x < width; ++x)
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
                stopwatch.Stop();
                this.messageList.Add(new ResultInfoItem
                {
                    Message = stopwatch.ElapsedMilliseconds.ToString() +
                                    "ms" + " - set up " + width + " X " + height +
                                    " size List<List<int>> matrix for holding pixel value information"});
                stopwatch.Reset();

            }

            //Calculating the value "energy" of each pixel
            {
                stopwatch.Start();
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
                //filling up corner pixels
                {
                    //filling corner pixels: top left corner
                    valueMap[0][0] = sqrtLookup[
                            sqr[Math.Abs((pixelList[1][0].R - pixelList[width - 1][0].R))]
                            + sqr[Math.Abs((pixelList[1][0].G - pixelList[width - 1][0].G))]
                            + sqr[Math.Abs((pixelList[1][0].B - pixelList[width - 1][0].B))]
                            //+ sqr[Math.Abs((pixelList[1][0].A - pixelList[width - 1][0].A))]

                            + sqr[Math.Abs((pixelList[0][1].R - pixelList[0][height - 1].R))]
                            + sqr[Math.Abs((pixelList[0][1].G - pixelList[0][height - 1].G))]
                            + sqr[Math.Abs((pixelList[0][1].B - pixelList[0][height - 1].B))]
                            //+ sqr[Math.Abs((pixelList[0][0].A - pixelList[0][height -1].A))]
                            ];

                    //filling corner pixels: top right corner
                    valueMap[width - 1][0] = sqrtLookup[
                            sqr[Math.Abs((pixelList[0][0].R - pixelList[width - 2][0].R))]
                            + sqr[Math.Abs((pixelList[0][0].G - pixelList[width - 2][0].G))]
                            + sqr[Math.Abs((pixelList[0][0].B - pixelList[width - 2][0].B))]
                            //+ sqr[Math.Abs((pixelList[0][0].A - pixelList[width - 2][0].A))]

                            + sqr[Math.Abs((pixelList[width - 1][1].R - pixelList[width - 1][height - 1].R))]
                            + sqr[Math.Abs((pixelList[width - 1][1].G - pixelList[width - 1][height - 1].G))]
                            + sqr[Math.Abs((pixelList[width - 1][1].B - pixelList[width - 1][height - 1].B))]
                            //+ sqr[Math.Abs((pixelList[width -1][0].A - pixelList[width -1][height -1].A))]
                            ];

                    //filling corner pixels: bottom left corner
                    valueMap[0][height - 1] = sqrtLookup[
                            sqr[Math.Abs((pixelList[1][height - 1].R - pixelList[width - 1][height - 1].R))]
                            + sqr[Math.Abs((pixelList[1][height - 1].G - pixelList[width - 1][height - 1].G))]
                            + sqr[Math.Abs((pixelList[1][height - 1].B - pixelList[width - 1][height - 1].B))]
                            //+ sqr[Math.Abs((pixelList[1][height -1].A - pixelList[width - 1][height -1].A))]

                            + sqr[Math.Abs((pixelList[0][0].R - pixelList[0][height - 2].R))]
                            + sqr[Math.Abs((pixelList[0][0].G - pixelList[0][height - 2].G))]
                            + sqr[Math.Abs((pixelList[0][0].B - pixelList[0][height - 2].B))]
                            //+ sqr[Math.Abs((pixelList[0][0].A - pixelList[0][height -2].A))]
                            ];

                    //filling corner pixels: bottom right corner
                    valueMap[width - 1][height - 1] = sqrtLookup[
                            sqr[Math.Abs((pixelList[0][height - 1].R - pixelList[width - 2][height - 1].R))]
                            + sqr[Math.Abs((pixelList[0][height - 1].G - pixelList[width - 2][height - 1].G))]
                            + sqr[Math.Abs((pixelList[0][height - 1].B - pixelList[width - 2][height - 1].B))]
                            //+ sqr[Math.Abs((pixelList[0][height -1].A - pixelList[width - 2][height -1].A))]

                            + sqr[Math.Abs((pixelList[width - 1][0].R - pixelList[width - 1][height - 2].R))]
                            + sqr[Math.Abs((pixelList[width - 1][0].G - pixelList[width - 1][height - 2].G))]
                            + sqr[Math.Abs((pixelList[width - 1][0].B - pixelList[width - 1][height - 2].B))]
                            //+ sqr[Math.Abs((pixelList[width - 1][0].A - pixelList[width - 1][height -2].A))]
                            ];
                }


                //filling horizontal border pixel values 
                for (int x = 1; x < width - 1; ++x)
                {
                    //filling the top row
                    valueMap[x][0] = sqrtLookup[
                            sqr[Math.Abs((pixelList[x + 1][0].R - pixelList[x - 1][0].R))]
                            + sqr[Math.Abs((pixelList[x + 1][0].G - pixelList[x - 1][0].G))]
                            + sqr[Math.Abs((pixelList[x + 1][0].B - pixelList[x - 1][0].B))]
                            //+ sqr[Math.Abs((pixelList[x + 1][0].A - pixelList[x - 1][0].A))]


                            + sqr[Math.Abs((pixelList[x][height - 1].R - pixelList[x][1].R))]
                            + sqr[Math.Abs((pixelList[x][height - 1].G - pixelList[x][1].G))]
                            + sqr[Math.Abs((pixelList[x][height - 1].B - pixelList[x][1].B))]
                            //+ sqr[Math.Abs((pixelList[x][height].A - pixelList[x][1].A))]
                            ];
                    //filling the bottom row
                    valueMap[x][height - 1] = sqrtLookup[
                            sqr[Math.Abs((pixelList[x + 1][height - 1].R - pixelList[x - 1][height - 1].R))]
                            + sqr[Math.Abs((pixelList[x + 1][height - 1].G - pixelList[x - 1][height - 1].G))]
                            + sqr[Math.Abs((pixelList[x + 1][height - 1].B - pixelList[x - 1][height - 1].B))]
                            //+ sqr[Math.Abs((pixelList[x + 1][height-1].A - pixelList[x - 1][height-1].A))]


                            + sqr[Math.Abs((pixelList[x][height - 2].R - pixelList[x][0].R))]
                            + sqr[Math.Abs((pixelList[x][height - 2].G - pixelList[x][0].G))]
                            + sqr[Math.Abs((pixelList[x][height - 2].B - pixelList[x][0].B))]
                            //+ sqr[Math.Abs((pixelList[x][height - 2].A - pixelList[x][0].A))]
                            ];
                }


                //filling vertical border pixel values
                for (int y = 1; y < height - 1; ++y)
                {
                    //filling the leftmost column
                    valueMap[0][y] = sqrtLookup[
                            sqr[Math.Abs((pixelList[1][y].R - pixelList[width - 1][y].R))]
                            + sqr[Math.Abs((pixelList[1][y].G - pixelList[width - 1][y].G))]
                            + sqr[Math.Abs((pixelList[1][y].B - pixelList[width - 1][y].B))]
                            //+ sqr[Math.Abs((pixelList[1][y].A - pixelList[width - 1][y].A))]


                            + sqr[Math.Abs((pixelList[0][y + 1].R - pixelList[0][y - 1].R))]
                            + sqr[Math.Abs((pixelList[0][y + 1].G - pixelList[0][y - 1].G))]
                            + sqr[Math.Abs((pixelList[0][y + 1].B - pixelList[0][y - 1].B))]
                            //+ sqr[Math.Abs((pixelList[0][y + 1].A - pixelList[0][y - 1].A))]
                            ];


                    //filling the rightmost row
                    valueMap[width - 1][y] = sqrtLookup[
                            sqr[Math.Abs((pixelList[0][y].R - pixelList[width - 2][y].R))]
                            + sqr[Math.Abs((pixelList[0][y].G - pixelList[width - 2][y].G))]
                            + sqr[Math.Abs((pixelList[0][y].B - pixelList[width - 2][y].B))]
                            //+ sqr[Math.Abs((pixelList[0][y].A - pixelList[width - 2][y].A))]


                            + sqr[Math.Abs((pixelList[width - 1][y + 1].R - pixelList[width - 1][y - 1].R))]
                            + sqr[Math.Abs((pixelList[width - 1][y + 1].G - pixelList[width - 1][y - 1].G))]
                            + sqr[Math.Abs((pixelList[width - 1][y + 1].B - pixelList[width - 1][y - 1].B))]
                            //+ sqr[Math.Abs((pixelList[width - 1][y + 1].A - pixelList[width - 1][y - 1].A))]
                            ];
                }
                stopwatch.Stop();
                this.messageList.Add(new ResultInfoItem
                {
                    Message = stopwatch.ElapsedMilliseconds.ToString() +
                    "ms" + " - calculated the value map for the whole image"});
                stopwatch.Reset();
            }
            //seamMapSetUp();
        }      
        

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
                stopwatch.Start();
                seamMap = new List<List<int>>(width);
                for (int x = 0; x < width; ++x)
                {
                    seamMap.Add(new List<int>(height));
                    for (int y = 0; y < height; ++y)
                    {
                        seamMap[x].Add(-1);
                    }
                }
                SeamMapSetUp = true;
                stopwatch.Stop();
                this.messageList.Add(new ResultInfoItem
                {
                    Message = stopwatch.ElapsedMilliseconds.ToString() +
                              "ms" + " - initial setup of the " 
                                + width + " X " +height + " dimension seam map, " +
                                "seam map was filled with dummy numbers (-1)"});
                stopwatch.Reset();
            }
        }

        private void calculateSeamMap()
        {
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

                for (int y = 1; y < height - 1; ++y)
                {
                    seamMap[x][y] = valueMap[x][y] + min3(seamMap[x - 1][y - 1], seamMap[x - 1][y + 1], seamMap[x - 1][y]);
                }
                // x, y=height-1 pixel is an "edge case", int.MaxValue is a placeholder for min3(...)
                seamMap[x][height - 1] = valueMap[x][height - 1] + min3(seamMap[x - 1][height - 2], seamMap[x - 1][height - 1], int.MaxValue);
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
            calculateSeamMap();

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

        // modKadder() is used to increment/decrement the X or Y coordinate by one in modK arithmetic 
        // the implementation is generic. The concreate usage is: 
        // coordinate: value of X or X ccordinate
        // add: -1 or +1 : ---> so the function evaluates to X or Y coordinate of the logical neighbour
        // modK: will be current image width height
        private int modKadder(int coordinate, int add, int modK)
        {
            if (coordinate+add >= 0) return ((coordinate + add) % modK);
            else return (((coordinate + add) % modK) + modK);
        }

        private void updatePixelValue(int x, int y)
        {
                valueMap[x][y] = sqrtLookup[
                        sqr[Math.Abs((pixelList[modKadder(x, 1,width)][y].R - pixelList[modKadder(x, -1, width)][y].R))]
                        + sqr[Math.Abs((pixelList[modKadder(x, 1, width)][y].G - pixelList[modKadder(x, -1, width)][y].G))]
                        + sqr[Math.Abs((pixelList[modKadder(x, 1, width)][y].B - pixelList[modKadder(x, -1, width)][y].B))]
                        //+ sqr[Math.Abs((pixelList[modKadder(x,1,width)][y].A - pixelList[modKadder(x,-1,width)][y].A))]


                        + sqr[Math.Abs((pixelList[x][modKadder(y, 1, height)].R - pixelList[x][modKadder(y, -1, height)].R))]
                        + sqr[Math.Abs((pixelList[x][modKadder(y, 1, height)].G - pixelList[x][modKadder(y, -1, height)].G))]
                        + sqr[Math.Abs((pixelList[x][modKadder(y, 1, height)].B - pixelList[x][modKadder(y, -1, height)].B))]
                        //+ sqr[Math.Abs((pixelList[x][modKadder(y, 1, height)].A - pixelList[x][modKadder(y, -1, height)].A))]
                        ];
        }

        public void RemoveHorizontalSeam(int[] seam)
        {

            //removes the seam from the pixelList, making each column shorter by one element : height -> height-1
            int helper;
            for (int x = 0; x < width; ++x)
            {
                helper = seam[x];
                pixelList[x].RemoveAt(helper);
                valueMap[x].RemoveAt(helper);
                
                //making saemMap one row shorter calculation cost: width X O(1)
                seamMap[x].RemoveAt(height - 1);
            }
            --height;
            for (int x =0; x < width; ++x)
            {
                updatePixelValue(x, modKadder(seam[x], -1, height));
                updatePixelValue(x, modKadder(seam[x], 0, height));
            }         
        }

        //This function removes N horizontal seams. At least 4 rows need to remain. If height-N < 4, then
        //height-4 rows are removed
        public void RemoveNHorizontalSeams(int n)
        {
            int rowsToRemove = Math.Min(n, height - 4);
            while (rowsToRemove > 0)
            {
                --rowsToRemove;

                RemoveHorizontalSeam(FindHorizontalSeam());
            }
            SaveBitmap();
        }

        public void SaveBitmap()
        {
           stopwatch.Start();
            Bitmap bitmap = new Bitmap(width, height);
            LockBitmap lockBitmap = new LockBitmap(bitmap);
            lockBitmap.LockBits();

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    lockBitmap.SetPixel(x, y, pixelList[x][y]);
                }                
            }
            lockBitmap.UnlockBits();
            bitmap.Save(@"c:\TMP\mod.bmp");
            stopwatch.Stop();
            this.messageList.Add(new ResultInfoItem
            {
                Message = stopwatch.ElapsedMilliseconds.ToString() +
                "ms" + " - created and saved to disk the carved out "
                + width + " X " + height + "image, used methods LockBitmap.SetPixel(int x, int y, Color c)"
            });
            stopwatch.Reset();
        }

        public void RemoveVerticalSeam(int[] seam) { }       

    }
}
