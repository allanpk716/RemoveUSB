using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RemoveUSB
{
    public class Reu
    {
        /// <summary>
        ///  USB 的序列号
        /// </summary>
        public string strUSBSerialNumber { get; set; }

        /// <summary>
        /// 设备信息
        /// </summary>
        public DriveInfo drInfo { get; set; }
    }
}
