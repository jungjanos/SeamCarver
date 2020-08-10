using Common;
using System.Threading;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            SeamCarver.SeamCarver.CarveVertically(imagePath: "LeetCode_SpanT.jpg", columnsToCarve: 1000, savePath: "LeetCode_SpanT.bmp", ImageFormat.bmp, CancellationToken.None, crop: false);
            //SeamCarver.SeamCarver.CarveVertically(imagePath: "Sample.jpg", columnsToCarve: 700, savePath: "SampleM.bmp", ImageFormat.bmp, CancellationToken.None, crop: false);
            //SeamCarver.SeamCarver.CarveVertically(imagePath: "Sample.jpg", columnsToCarve: 700, savePath: "SampleM.jpeg", ImageFormat.jpeg, CancellationToken.None, crop: false);
        }
    }
}
