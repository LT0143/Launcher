using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Launcher
{
    public class FTPFile
    {
        public string FullName
        {
            get { return Path + FileName; }
        }

        public string FileName
        {
            get { return m_strFileName; }
        }

        public string Path
        {
            get { return m_strPath; }
        }

        public DirectoryEntryTypes FileType
        {
            get { return m_nFileType; }
        }

        public long Size
        {
            get { return m_nSize; }
        }

        public DateTime FileDateTime
        {
            get { return m_FileDateTime; }
        }

        public string Permission
        {
            get { return m_strPermission; }
        }

        public string Extension
        {
            get
            {
                int i = this.FileName.LastIndexOf(".");
                if (i >= 0 && i < (this.FileName.Length - 1))
                {
                    return this.FileName.Substring(i + 1);
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// 去掉文件的后缀名
        /// </summary>
        public string NameOnly
        {
            get
            {
                int i = this.FileName.LastIndexOf(".");
                if (i > 0)
                {
                    return this.FileName.Substring(0, i);
                }
                else
                {
                    return this.FileName;
                }
            }
        }
        private string m_strFileName;
        private string m_strPath;
        private DirectoryEntryTypes m_nFileType;
        private long m_nSize;
        private DateTime m_FileDateTime = DateTime.Today;
        private string m_strPermission;

        public enum DirectoryEntryTypes
        {
            File,
            Directory
        }

        /// <summary>
        /// 解析FTPstrPath下所有文件
        /// </summary>
        /// <param name="strLine"></param>
        /// <param name="strPath"></param>
        public FTPFile(string strLine, string strPath)
        {
            Match m = GetMatchingRegex(strLine);
            if (m == null)
            {
                throw (new ApplicationException("Unable to parse line: " + strLine));
            }
            else
            {
                m_strFileName = m.Groups["name"].Value;
                m_strPath = strPath;

                Int64.TryParse(m.Groups["size"].Value, out m_nSize);

                m_strPermission = m.Groups["permission"].Value;
                string _dir = m.Groups["dir"].Value;
                if (_dir != "" && _dir != "-")
                {
                    m_nFileType = DirectoryEntryTypes.Directory;
                }
                else
                {
                    m_nFileType = DirectoryEntryTypes.File;
                }

                try
                {
                    String strValue = m.Groups["timestamp"].Value;

                    m_FileDateTime = DateTime.Parse(strValue);
                }
                catch (Exception)
                {
                    m_FileDateTime = Convert.ToDateTime(null);
                }
            }
        }

        /// <summary>
        /// 获取匹配的正则表达式。
        /// </summary>
        /// <param name="strLine"></param>
        /// <returns></returns>
        private Match GetMatchingRegex(string strLine)
        {
            Regex rx;
            Match m;
            for (int i = 0; i <= _ParseFormats.Length - 1; i++)
            {
                rx = new Regex(_ParseFormats[i]);
                m = rx.Match(strLine);
                if (m.Success)
                {
                    return m;
                }
            }
            return null;
        }

        //解析从FTP LIST命令响应（语法变化）
        private static string[] _ParseFormats = new string[] { 
            "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\w+\\s+\\w+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{4})\\s+(?<name>.+)", 
            "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\d+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{4})\\s+(?<name>.+)", 
            "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\d+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{1,2}:\\d{2})\\s+(?<name>.+)", 
            "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\w+\\s+\\w+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{1,2}:\\d{2})\\s+(?<name>.+)", 
            "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})(\\s+)(?<size>(\\d+))(\\s+)(?<ctbit>(\\w+\\s\\w+))(\\s+)(?<size2>(\\d+))\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{2}:\\d{2})\\s+(?<name>.+)", 
            "(?<timestamp>\\d{2}\\-\\d{2}\\-\\d{2}\\s+\\d{2}:\\d{2}[Aa|Pp][mM])\\s+(?<dir>\\<\\w+\\>){0,1}(?<size>\\d+){0,1}\\s+(?<name>.+)" };
    }

    public class FTPDirectory : List<FTPFile>
    {
        public FTPDirectory()
        {

        }

        public FTPDirectory(string strDir, string strPath)
        {
            foreach (string line in strDir.Replace("\n", "").Split(System.Convert.ToChar('\r')))
            {
                if (line != "")
                {
                    this.Add(new FTPFile(line, strPath));
                }
            }
        }

        public FTPDirectory GetFiles(string strExt)
        {
            return this.GetFileOrDir(FTPFile.DirectoryEntryTypes.File, strExt);
        }

        public FTPDirectory GetDirectories()
        {
            return this.GetFileOrDir(FTPFile.DirectoryEntryTypes.Directory, "");
        }

        private FTPDirectory GetFileOrDir(FTPFile.DirectoryEntryTypes type, string strExt)
        {
            FTPDirectory result = new FTPDirectory();
            foreach (FTPFile fi in this)
            {
                if (fi.FileType == type)
                {
                    if (strExt == "")
                    {
                        result.Add(fi);
                    }
                    else if (strExt == fi.Extension)
                    {
                        result.Add(fi);
                    }
                }
            }
            return result;

        }

        public bool FileExists(string strFilename)
        {
            foreach (FTPFile ftpfile in this)
            {
                if (ftpfile.FileName == strFilename)
                {
                    return true;
                }
            }
            return false;
        }

        private const char slash = '/';

        public static string GetParentDirectory(string dir)
        {
            string tmp = dir.TrimEnd(slash);
            int i = tmp.LastIndexOf(slash);
            if (i > 0)
            {
                return tmp.Substring(0, i - 1);
            }
            else
            {
                throw (new ApplicationException("No parent for root"));
            }
        }
    }





}
