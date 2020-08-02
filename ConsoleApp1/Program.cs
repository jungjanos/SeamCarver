﻿using SeamCarver;
using System.Threading;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            SeamCarver.SeamCarver.CarveVertically(imagePath: "Sample.jpg", columnsToCarve: 700, savePath: "SampleM.bmp", ImageFormat.bmp, CancellationToken.None, crop: false);
            SeamCarver.SeamCarver.CarveVertically(imagePath: "Sample.jpg", columnsToCarve: 700, savePath: "SampleM.jpeg", ImageFormat.jpeg, CancellationToken.None, crop: false);
        }
    }
}
