using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Windows.Forms;

namespace Launcher
{
    public class FileMatchUp
    {
        public static String FILENAME = "md5checksum.txt";

        public class RepositoryList
        {
            public List<String> m_list_file = new List<String>();
        }

        public static Dictionary<String, RepositoryList> RepositoryFile
        {
            get { return m_repository_file; }
        }

        public static Dictionary<String, RepositoryList> m_repository_file = new Dictionary<String, RepositoryList>();

        public static int m_nMaxCount = 0;
        public static int m_nCurrentCount = 0;

        public static int MAXCOUNT
        {
            get { return m_nMaxCount; }
        }

        public static int CURRENTCOUNT
        {
            get { return m_nCurrentCount; }
        }

        public static String ComputeMD5Hash(String strFilePath)
        {
            return ComputeHash(strFilePath, new MD5CryptoServiceProvider());
        }

        public static String ComputeHash(String strFilePath, HashAlgorithm Algorithm)
        {

            using (FileStream _FileStream = File.OpenRead(strFilePath))
            {
                try
                {
                    byte[] HashResult = Algorithm.ComputeHash(_FileStream);
                    String ResultString = BitConverter.ToString(HashResult).Replace("-", "");
                    return ResultString;
                }
                finally
                {
                    _FileStream.Close();
                }
            }

        }


        public static bool MakeEncodingList(String strPath)
        {
            RepositoryFile.Clear();

            DirectoryInfo strDirectory = new DirectoryInfo(strPath);

            GetDirectory(strDirectory, RepositoryFile);

            if (RepositoryFile.Count < 1)
            {
                return false;
            }

            m_nMaxCount = 0;
            m_nCurrentCount = 0;

            foreach (KeyValuePair<String, RepositoryList> _pair in RepositoryFile)
            {
                m_nMaxCount += _pair.Value.m_list_file.Count;
            }

            String strMakeFileName = strPath + "/" + FILENAME;
            FileStream fs = null;
            try
            {
                fs = new FileStream(strMakeFileName, FileMode.Create);
                StreamWriter writer = new StreamWriter(fs, Encoding.UTF8);
                writer.BaseStream.Seek(0, SeekOrigin.Begin);

                foreach (KeyValuePair<String, RepositoryList> _pair in RepositoryFile)
                {
                    foreach (String strFile in _pair.Value.m_list_file)
                    {
                        if (FILENAME == strFile) continue;

                        String strInsertPath = _pair.Key;
                        strInsertPath = strInsertPath.Replace(strPath, "");

                        strInsertPath = strInsertPath.Replace('\\', '/');

                        if (strInsertPath.StartsWith("/"))
                        {
                            strInsertPath = strInsertPath.Substring(1, strInsertPath.Length - 1) + "/";
                        }
                        writer.WriteLine(strInsertPath + strFile + "=" + ComputeMD5Hash(_pair.Key + "/" + strFile));
                        m_nCurrentCount++;
                    }
                }

                writer.Flush();
                writer.Close();
            }
            catch { }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }


            return true;
        }

        public static void GetDirectory(DirectoryInfo infoDirectory, Dictionary<String, RepositoryList> repository)
        {
            GetFiles(infoDirectory, repository);

            DirectoryInfo[] dirs = infoDirectory.GetDirectories();

            if (dirs.Length < 1) return;

            for (int i = 0; i < dirs.Length; i++)
            {
                GetDirectory(dirs[i], repository);
            }
        }

        public static void GetFiles(DirectoryInfo infoDirectory, Dictionary<String, RepositoryList> repository)
        {
            FileInfo[] files = infoDirectory.GetFiles();

            if (files.Length < 1) return;

            for (int i = 0; i < files.Length; i++)
            {
                if (repository.ContainsKey(infoDirectory.FullName) == false)
                {
                    RepositoryList _list = new RepositoryList();

                    repository.Add(infoDirectory.FullName, _list);
                }

                repository[infoDirectory.FullName].m_list_file.Add(files[i].Name);
            }
        }

        public enum CheckSumResult
        {
            CheckSumResult_None,
            CheckSumResult_NotCheckSumFile,
            CheckSumResult_FakeFile,
            CheckSumResult_Process,
            CheckSumResult_Success,
        }

        public static CheckSumResult CheckSum()
        {
            // 현재 경로의 체크섬 파일을 찾는다.
            String strCurrentDirectory = Directory.GetCurrentDirectory();
            String strCurrentCheckSumFile = strCurrentDirectory + "/" + FILENAME;

            if (File.Exists(strCurrentCheckSumFile) == false)
            {
                return CheckSumResult.CheckSumResult_NotCheckSumFile;
            }

            // 조사할 파일을 읽는다.
            StreamReader filereader = new StreamReader(strCurrentCheckSumFile, Encoding.UTF8);

            if (filereader == null) return CheckSumResult.CheckSumResult_NotCheckSumFile;

            String line;
            while ((line = filereader.ReadLine()) != null)
            {
                String[] str = line.Split(':');

                String strFile = strCurrentDirectory + str[0];

                String strMD5 = ComputeMD5Hash(strFile);

                if (strMD5 != str[1])
                {
                  return CheckSumResult.CheckSumResult_FakeFile;
                }
            }
            return CheckSumResult.CheckSumResult_Success;
        }

        /// <summary>
        /// 获得目录下所有文件或指定文件类型文件(包含所有子文件夹)
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <param name="extName">扩展名可以多个 例如 .mp3.wma.rm</param>
        /// <returns>List<FileInfo></returns>
        public static void getFile(string path, string extName, List<FileInfo> list)
        {
            if (!Directory.Exists(path))
            {
                return;
            }
            try
            {
                string[] dir = Directory.GetDirectories(path); //文件夹列表   
                DirectoryInfo fdir = new DirectoryInfo(path);
                FileInfo[] file = fdir.GetFiles();
                //FileInfo[] file = Directory.GetFiles(path); //文件列表   
                if (file.Length != 0 || dir.Length != 0) //当前目录文件或文件夹不为空                   
                {
                    foreach (FileInfo f in file) //显示当前目录所有文件   
                    {
                        if (extName.ToLower().IndexOf(f.Extension.ToLower()) >= 0)
                        {
                            list.Add(f);
                        }
                    }

                    foreach (string d in dir)
                    {
                        getFile(d, extName, list); //递归   
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }




        public static bool MakeEncodingList(Dictionary<string, MyUpdateStruct> serverFile, String strPath,string mdfilename)
        {
            RepositoryFile.Clear();

            DirectoryInfo strDirectory = new DirectoryInfo(strPath);

            GetDirectory(strDirectory, RepositoryFile);

            if (RepositoryFile.Count < 1)
            {
                return false;
            }

            m_nMaxCount = serverFile.Count;
            m_nCurrentCount = 0;

            if (m_nMaxCount > 0)
            {
                string strMakeFileName = strPath + "/" + mdfilename;
                FileStream fs = null;
                try
                {
                    fs = new FileStream(strMakeFileName, FileMode.Create);
                    StreamWriter writer = new StreamWriter(fs, Encoding.UTF8);
                    writer.BaseStream.Seek(0, SeekOrigin.Begin);

                    int index = 0;

                    foreach (var myUpdateStruct in serverFile)
                    {
                        var filename = myUpdateStruct.Key;
                        String strInsertPath = strPath + filename;
                        index++;
                        //string text = string.Format("当前本地校验文件第{0}/{1}个", index, m_nMaxCount);

                        if (File.Exists(strInsertPath))
                        {
                            var format = filename + "=" + ComputeMD5Hash(strInsertPath);
                            writer.WriteLine(format);
                        }
                        else
                        {
                            var format = filename + "=null" ;
                            writer.WriteLine(format);
                        }
                        m_nCurrentCount++;
                    }

                    writer.Flush();
                    writer.Close();
                }
                catch (Exception e)
                {
                    // ignored
                    MessageBox.Show("客户端文件列表检查失败," + e.Message, "MD5Error", MessageBoxButtons.OK);
                }
                finally
                {
                    if (fs != null)
                    {
                        fs.Close();
                    }
                }
            }
            else
            {
                return false;
            }
            


            return true;
        }

    }
}
