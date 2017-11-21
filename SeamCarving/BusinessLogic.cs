using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamCarving
{
    public class BusinessLogic
    {
        public Bitmap bitmap;
        public SeamCarverH sH;
        public bool ImageLoaded { set; get; }

        public Size ImageWorkingSize { set; get; }

        public void SetupSeamCarver()
        {
            sH = new SeamCarverH(bitmap);
            ImageLoaded = true;
            ImageWorkingSize = bitmap.Size;
        }

    }
}
