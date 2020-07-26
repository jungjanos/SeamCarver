using SeamCarver;
using System.Threading;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            SeamCarver.SeamCarver.CarveVertically(imagePath: "Sample.jpg", columnsToCarve: 600, savePath: "SampleM.jpeg", ImageFormat.jpeg, false, CancellationToken.None);
        }
    }
}
