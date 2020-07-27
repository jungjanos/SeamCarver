using SeamCarver;
using System.Threading;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            SeamCarver.SeamCarver.CarveVertically(imagePath: "Sample.jpg", columnsToCarve: 300, savePath: "SampleM.jpeg", ImageFormat.jpeg, crop: true, CancellationToken.None);
        }
    }
}
