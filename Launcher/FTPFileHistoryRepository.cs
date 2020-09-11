using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Launcher
{
    /************************************************************************/
     /* 其历史是文件的大小、文件上的编号编号（版本号？）)将其检入到中。*/
    /************************************************************************/
    public enum FileHistoryOrder
    {
        _Name = 0,
        _Path,
        _Version,
        _Size,

        _Max,
    }

    public class Reposityory
    {
        private String m_strFileName;              
        private String m_strPath;                   
        private int m_nVersion;                    
        private long m_nFileSize;                   

        public Reposityory()
        {
            m_strFileName = "";
            m_strPath = "";
            m_nVersion = 0;
            m_nFileSize = 0;
        }

        public Reposityory(Reposityory _reposityory)
        {
            m_strFileName = _reposityory.Name;
            m_strPath = _reposityory.Path;
            m_nVersion = _reposityory.Version;
            m_nFileSize = _reposityory.Size;
        }

        public Reposityory(String _name, String _path, int _version, long _size)
        {
            m_strFileName = _name;
            m_strPath = _path;
            m_nVersion = _version;
            m_nFileSize = _size;
        }

        public String Name
        {
            get { return m_strFileName; }
            set { m_strFileName = value; }
        }

        public String Path
        {
            get { return m_strPath; }
            set { m_strPath = value; }
        }

        public int Version
        {
            get { return m_nVersion; }
            set { m_nVersion = value; }
        }

        public long Size
        {
            get { return m_nFileSize; }
            set { m_nFileSize = value; }
        }
    }

    public class FileHistoryRepository
    {
        private static List<Reposityory> ms_list_file_server = new List<Reposityory>();
        private static long m_nMaxFileSize = 0;

        
        public static List<Reposityory> ListServerFile
        {
            get { return ms_list_file_server; }
        }

        public static long MAXFILESIZE
        {
            get { return m_nMaxFileSize; }
            set { m_nMaxFileSize = value; }
        }

        public static void Reset()
        {
            if (ListServerFile.Count != 0)
            {
                MAXFILESIZE = 0;
                ListServerFile.Clear();
            }
        }

        public static long GetFileSize(String strFileName)
        {
            foreach (Reposityory reposityory in ms_list_file_server)
            {
                if (reposityory.Name.Contains(strFileName) == true)
                    return reposityory.Size;
            }

            return 0;
        }

        /// <summary>
        ///获取RTP文件夹下文件数量大小。
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="strSourceDir"></param>
        public static bool GetFileHistory(FtpProtocol protocol, string strDirectory, string currentVer)
        {
            bool successful = true;
            String list = IniManager.GetINIValue(MainWindow.m_localServerVerPath, "RtpList", "list");
            string[] array = list.Split('-');
            int num = 0;

            for (int i = 0; i < array.Length; i++)
            {
                //if (RTPVersion.CompareVersion(currentVer, array[i]) == CompareVersionResult.CompareVersionResult_GreaterRight)
                //{
                    string RtpVer = array[i].Replace('.', '_');
                    string name = strDirectory + RtpVer + ".RTP";
                    long sizeI = protocol.GetFileSize(name);
                    if (sizeI != 0)
                    {
                        FileHistoryRepository.MAXFILESIZE += sizeI;
                        FTPUpdateValue.maxByte += sizeI;
                        num++;
                        FTPUpdateValue.RtpSizeList.Add(sizeI);
                    }
                    else
                        successful = false;

                //}
            }
            FTPUpdateValue.maxfileCount += num;
            return successful;
        }
    }
}
