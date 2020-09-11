using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Launcher
{
    public static class FTPUpdateValue
    {
        /// <summary>
        /// 下载第几个文件
        /// </summary>
        public static int downloadIndex
        {
            set { m_nDownloadNumbering = value; }
            get { return m_nDownloadNumbering; }
        }

        /// <summary>
        /// 下载第几个文件
        /// </summary>
        public static List<long> RtpSizeList = new List<long>();


        /// <summary>
        /// 最大下载字节
        /// </summary>
        public static long maxByte
        {
            set { m_nMaxByte = value; }
            get { return m_nMaxByte; }
        }

        /// <summary>
        /// todo 最大字节显示字符串
        /// </summary>
        public static String MaxBtye_string
        {
            get { return FormatBytes(m_nMaxByte); }
        }
        
        /// <summary>
        /// 当前总的已经下载的字节数
        /// </summary>
        public static long CurrentAccumlatereadByte
        {
            set { m_nCurrentAccumulateReadByte = value; }
            get { return m_nCurrentAccumulateReadByte; }
        }

        /// <summary>
        /// 当前要下载的文件大小
        /// </summary>
        public static long CurrentFileByte
        {
            set { m_nCurrentFileByte = value; }
            get { return m_nCurrentFileByte; }
        }

        /// <summary>
        /// 当前需下载的文件已下载部分
        /// </summary>
        public static long CurrentFileDownloadBytes
        {
            set { m_nCurrentFileDownloadByte = value; }
            get { return m_nCurrentFileDownloadByte; }
        }

        /// <summary>
        /// 当前已经下载的字节数显示字符串
        /// </summary>
        public static String CurretnAccumulateReadByte_String
        {
            get { return FormatBytes(m_nCurrentAccumulateReadByte); }
        }
        
        /// <summary>
        /// 当前下载的文件名字
        /// </summary>
        public static String CurrentDownloadFileName
        {
            set { m_strCurrentDowloadFileName = value; }
            get { return m_strCurrentDowloadFileName; }
        }
        public static bool BGetDownLoadNum
        {
            get { return bGetDownLoadNum; }
            set { bGetDownLoadNum = value; }
        }

        /// <summary>
        /// 最大下载的文件个数
        /// </summary>
        public static int maxfileCount
        {
            set { m_nMaxFileCount = value; }
            get { return m_nMaxFileCount; }
        }

        private static int m_nDownloadNumbering;
        private static int m_nMaxFileCount;
        private static long m_nMaxByte;
        private static long m_nCurrentAccumulateReadByte;
        private static long m_nCurrentFileByte;
        private static long m_nCurrentFileDownloadByte;
        private static bool bGetDownLoadNum = false;


        private static String m_strCurrentDowloadFileName;

        public static string FormatBytes(long bytes)
        {
            string[] Suffix = { "Byte", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = bytes;
            if (bytes > 1024)
                for (i = 0; (bytes / 1024) > 0; i++, bytes /= 1024)
                    dblSByte = bytes / 1024.0;
            return String.Format("{0:0.##}{1}", dblSByte, Suffix[i]);
        }
    }
}
