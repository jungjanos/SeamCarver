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
        private List<ResultInfoItem> messageList;
        private string fileName;


        public BusinessLogic() { }

        public BusinessLogic (List<ResultInfoItem> messageList, string fileName) : this()
        {
            this.messageList = messageList;
            this.fileName = fileName;
            this.bitmap = new Bitmap(this.fileName);

            SetupSeamCarver();
        }

        public Size ImageWorkingSize { set; get; }

        public void SetupSeamCarver()
        {
            sH = new SeamCarverH(bitmap, this.messageList);
            ImageLoaded = true;
            ImageWorkingSize = bitmap.Size;
        }

    }
}
