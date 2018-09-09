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
        public SeamCarverH sH;
        public bool ImageLoaded { set; get; }
        private List<ResultInfoItem> messageList;        


        public BusinessLogic()
        {            
        }

        public BusinessLogic (List<ResultInfoItem> messageList) : this()
        {
            this.messageList = messageList;           
        }

        public Size ImageWorkingSize { set; get; }        

        public void SetupSeamCarver(Bitmap bitmap)
        {
            sH = new SeamCarverH(bitmap, this.messageList, this);
            ImageLoaded = true;            
        }

    }
}
