using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamCarving
{
    public class ResultInfoItem
    {
        private DateTime timestamp;
        private string message;
        public string TimeStamp
        {
            get
            {
                return timestamp.ToLongTimeString();
            }
        }
        public string Message
        {
            set
            {
                message = value;
                timestamp = DateTime.Now;
            }
            get
            {
                return message;
            }
        }
         
        
    }
}
