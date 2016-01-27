using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using UsbEject;
using Wox.Plugin;
using Yeylol.UsbEject;

namespace RemoveUSB
{
    public class ReuSet
    {
        // 可移除设备信息
        private List<Reu> _reuList = new List<Reu>();

        /// <summary>
        /// 插件的目录
        /// </summary>
        private string _pluginDirectory = string.Empty;

        public void Load(string pluginDirectory)
        {
            _pluginDirectory = pluginDirectory;
        }

        public List<Result> Query(string query)
        {
            _reuList.Clear();

            // 找到移动硬盘设备
            var UsbNameList = GetAllUsbDriveNames();

            // 获取 U盘的盘符信息
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (var item in allDrives)
            {
                // 如果匹配上就标记出来
                bool bIsUsb = false;
                if (UsbNameList.Contains(item.Name.Substring(0, 2)))
                {
                    bIsUsb = true;
                }

                if (item.DriveType == DriveType.Removable && item.IsReady == true ||
                    item.DriveType == DriveType.Fixed && bIsUsb == true
                    )
                {
                    // 得到更目录的盘符字母
                    string strPath = item.RootDirectory.Root.Name.Replace(":\\", "");
                    USBSerialNumber usbSN = new USBSerialNumber();
                    string strTmp = usbSN.getSerialNumberFromDriveLetter(strPath);

                    _reuList.Add(new Reu()
                        {
                            strUSBSerialNumber = strTmp,
                            drInfo = item
                        });
                }     
            }

            List<Result> results = new List<Result>();

            // 如果没有可移除移动设备的时候，那么就提示没有
            if (_reuList.Count == 0)
            {
                results.Add(new Result
                {
                    Title = "Do not find any USB devices to remove",
                    SubTitle = string.Empty,
                    IcoPath = _pluginDirectory + "\\Images\\" + "usb_stick.png",
                    Action = (c) =>
                    {
                        // 返回false告诉Wox不要隐藏查询窗体，返回true则会自动隐藏Wox查询窗口
                        return true;
                    }
                });

                return results;
            }

            // 所有可移除设备的盘符拼接起来
            string strAllDevice = string.Empty;
            for (int i = 0; i < _reuList.Count; i++)
            {
                string strKK = ",";
                if (i == _reuList.Count - 1)
                {
                    strKK = string.Empty;
                }
                strAllDevice += _reuList[i].drInfo.RootDirectory.Root.Name.Replace(":\\", "") + strKK;
            }

            // 加一个所有移除的选择
            results.Add(new Result
            {
                Title = "All",
                SubTitle = strAllDevice,
                IcoPath = _pluginDirectory + "\\Images\\" + "usb_stick.png",
                Action = (c) =>
                {
                    Thread thread = new Thread(new ThreadStart(() => ProcessAllThread()));
                    thread.Start();

                    // 返回false告诉Wox不要隐藏查询窗体，返回true则会自动隐藏Wox查询窗口
                    return true;
                }
            });

            foreach (var item in _reuList)
            {
                results.Add(new Result
                {
                    Title = item.drInfo.VolumeLabel + "(" + item.drInfo.Name + ")",
                    SubTitle = "TotalFreeSpace : " + FormatBytes(item.drInfo.TotalFreeSpace) + " -- " +
                               "TotalSize" + FormatBytes(item.drInfo.TotalSize),
                    IcoPath = _pluginDirectory + "\\Images\\" + "usb_stick.png",
                    Action = (c) =>
                    {
                        Thread t = new Thread(new ParameterizedThreadStart(ProcessOneThread));
                        t.Start(item.strUSBSerialNumber); 

                        // 返回false告诉Wox不要隐藏查询窗体，返回true则会自动隐藏Wox查询窗口
                        return true;
                    }
                });
            }
            return results.OrderBy(r => r.Title.Length).ToList();
        }

        private static string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        private static void RemoveUSB(string strUSBSerialNumber)
        {
            DiskDeviceClass dc = new DiskDeviceClass();

            foreach (var item in dc.Devices)
            {
                if (item.Path.Contains(strUSBSerialNumber.ToLower()) == true)
                {
                    item.Eject(true);
                }
            }
        }

        private void ProcessAllThread()
        {
            foreach (var item in _reuList)
            {
                RemoveUSB(item.strUSBSerialNumber);
            }
        }

        private void ProcessOneThread(object objStr)
        {
            string strTmp = objStr.ToString();
            RemoveUSB(strTmp);
        }

        private static List<string> GetAllUsbDriveNames()
        {
            var searcher = new ManagementObjectSearcher();
            searcher.Query = new SelectQuery("Win32_DiskDrive", "InterfaceType = \"USB\"");
            var usbDiskDrives = searcher.Get().Cast<ManagementObject>();
            var usBdriveNames = new List<string>();
            foreach (var usbDiskDrive in usbDiskDrives)
            {
                try
                {
                    searcher.Query = new SelectQuery("Win32_DiskDriveToDiskPartition");
	                var diskDriveToDiskPartition = searcher.Get().Cast<ManagementObject>();
	
	                searcher.Query = new SelectQuery("Win32_LogicalDiskToPartition");
	                var logicalDiskToPartition = searcher.Get().Cast<ManagementObject>();
	
	                searcher.Query = new SelectQuery("Win32_LogicalDisk");
	                var logicalDisk = searcher.Get().Cast<ManagementObject>();
	
	                var usbPartition =
	                    diskDriveToDiskPartition.FirstOrDefault(p => p["Antecedent"].ToString() == usbDiskDrive.ToString())[
	                        "Dependent"].ToString();

                    ManagementObject tmp = null;
                    /* 
                         在这里匹配的时候，原来的逻辑是全字符匹配，那么现在遇到一个 DIY U盘
                         在 usbDiskDrive 读取的时候是 \\PC201306031019\root\cimv2:Win32_DiskPartition.DeviceID="Disk #2, Partition #0"
                         然后在下面获取所有硬盘的时候却是

                         {\\PC201306031019\root\cimv2:Win32_LogicalDiskToPartition.Antecedent="\\\\PC201306031019\\root\\cimv2:Win32_DiskPartition.DeviceID=\"Disk #2, Partition #1\"",Dependent="\\\\PC201306031019\\root\\cimv2:Win32_LogicalDisk.DeviceID=\"H:\""}
                         {\\PC201306031019\root\cimv2:Win32_LogicalDiskToPartition.Antecedent="\\\\PC201306031019\\root\\cimv2:Win32_DiskPartition.DeviceID=\"Disk #2, Partition #2\"",Dependent="\\\\PC201306031019\\root\\cimv2:Win32_LogicalDisk.DeviceID=\"I:\""}

                         Partition 从 0 变到了 1 或者 2，当然这个U盘的确分区了2个出来。

                         所以现在需要进行部分匹配，也就是匹配到 Partition 之前的 disk 编号即可
                         取巧的方式，Partition #0 这个一般人不会分到两位数的盘符吧~~所以移除的时候就不需要用正则表达式了，直接移除固定尾部长度的信息即可
                    */
                    // tmp = logicalDiskToPartition.FirstOrDefault(p => p["Antecedent"].ToString() == usbPartition);

                    int indexTmp = usbPartition.LastIndexOf ('P');

                    if (indexTmp == -1)
                    {
                        // 没有找到
                        continue;
                    }

                    string strTmp = usbPartition.Substring(indexTmp, usbPartition.Length - indexTmp);
                    usbPartition = usbPartition.Replace(strTmp, "");

                    tmp = logicalDiskToPartition.FirstOrDefault(p => p["Antecedent"].ToString().Contains(usbPartition) == true);

                    if (tmp != null)
                    {
                        var usbLogicalDisk = tmp["Dependent"].ToString();

                        foreach (ManagementObject disk in logicalDisk)
                        {
                            if (disk.ToString() == usbLogicalDisk)
                            {
                                usBdriveNames.Add(disk["DeviceID"].ToString());
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    throw;
                }
            }
            return usBdriveNames;
        }

//         private static List<DriverBase> GetDrivers(InterfaceType type, string partitionName)
//         {
//             List<DriverBase> drivers = new List<DriverBase>();
//             string strQuery = "select * from Win32_DiskDrive";
//             if (type == InterfaceType.USB)
//                 strQuery += " Where InterfaceType = 'USB'";
//             else if (type == InterfaceType.IDE)
//                 strQuery += " Where InterfaceType = 'IDE'";
// 
//             SelectQuery sq = new SelectQuery(strQuery);
//             ManagementObjectSearcher mos = new ManagementObjectSearcher(sq);
//             foreach (ManagementObject disk in mos.Get())
//             {
//                 string DeviceID = disk["DeviceID"].ToString();
//                 foreach (ManagementObject partition in new ManagementObjectSearcher("ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + DeviceID + "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition").Get())
//                 {
//                     string query = "ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + partition["DeviceID"] + "'} WHERE AssocClass = Win32_LogicalDiskToPartition";
//                     foreach (ManagementObject disk1 in new ManagementObjectSearcher(query).Get())
//                     {
//                         DriverBase b = new DriverBase();
//                         string diviceID = disk1["DeviceID"].ToString().Trim();
//                         if (!IsNTFS(diviceID))
//                         {
//                             continue;
//                         }
//                         string Name = disk1["Name"].ToString().Trim();
//                         string freeSpace = disk1["FreeSpace"].ToString().Trim();
//                         string size = disk1["Size"].ToString().Trim();
//                         if (!string.IsNullOrEmpty(partitionName) && Name.ToUpper() != partitionName.ToUpper())
//                         {
//                             continue;
//                         }
//                         b.DriverName = Helper.DriverHelper.GetVolumnLabel(diviceID);
//                         b.TotalSpace = double.Parse(size);
//                         b.FreeSpace = double.Parse(freeSpace);
//                         b.DeviceID = diviceID;
//                         b.IsOverload = (b.TotalSpace - b.FreeSpace) / b.TotalSpace > 0.7;
//                         b.SerialNum = disk["SerialNumber"] == null ? "" : disk["SerialNumber"].ToString().Trim();
//                         if (string.IsNullOrEmpty(b.SerialNum))
//                         {
//                             string pnpdeviceid = disk["PNPDeviceID"] == null ? "" : disk["PNPDeviceID"].ToString().Trim();
//                             if (!string.IsNullOrEmpty(pnpdeviceid))
//                                 b.SerialNum = parseSerialFromDeviceID(pnpdeviceid);
//                         }
//                         b.VolumeSerialNum = GetVolumeSerialNumber(diviceID);
//                         if (!string.IsNullOrEmpty(b.SerialNum))
//                             drivers.Add(b);
// 
//                     }
//                 }
//             }
//             return drivers;
//         }
    }

    enum InterfaceType
    {
        USB,
        IDE,
    }

    class DriverBase
    {
        public string DeviceID;
        public string DriverName;
        public string VolumeSerialNum;
        public string TotalSpace;
        public string FreeSpace;
        public string IsOverload;
        public string SerialNum;
    }
}
