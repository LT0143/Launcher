using System;
using System.Net;
using System.IO;
using System.Windows.Forms;


namespace Launcher
{
    public class FtpProtocol
    {
        private string m_strHost;
        private string m_strID;
        private string m_strPassword;
        private string m_strCurrentDirectory = "/";
        private string m_strLastDirectory = "";

        public MainWindow mainform;
        //public string m_downloadTemp = "temp";

        public string Host
        {
            get
            {
#if DEBUG
                if (m_strHost.StartsWith("http://"))
#else
                  if (m_strHost.StartsWith("https://"))
#endif
                //if (m_strHost.StartsWith("ftp://"))
                {
                    return m_strHost;
                }
                else
                {
#if DEBUG
                    return "http://" + m_strHost;
#else
                    return "https://" + m_strHost; 
#endif
                    // return "ftp://" + m_strHost;
                }
            }
            set { m_strHost = value; }
        }

        public string ID
        {
            get { return (m_strID == "" ? "anonymous" : m_strID); }
            set { m_strID = value; }
        }

        public string Password
        {
            get { return m_strPassword; }
            set { m_strPassword = value; }
        }

        public string CurrentDirectory
        {
            get { return m_strCurrentDirectory + ((m_strCurrentDirectory.EndsWith("/")) ? "" : "/").ToString(); }
            set
            {
                if (!value.StartsWith("/"))
                {
                    throw (new ApplicationException("Directory should start with /"));
                }
                m_strCurrentDirectory = value;
            }
        }


        public FtpProtocol(string _strHost, string _strUser, string _strPassword,MainWindow form)
        {
            m_strHost = _strHost;
            m_strID = _strUser;
            m_strPassword = _strPassword;
            mainform = form;
        }


        //文件夹详情
        public FTPDirectory ListDirectoryDetail(string strDirectory)
        {
            string _strDirectory = GetDirectory(strDirectory);

            //FtpWebRequest ftpr = GetRequest(_strDirectory);
            HttpWebRequest ftpr = GetRequest(_strDirectory);
            //ftpr.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            string str = GetStringResponse(ftpr);

            //
            if (str.Contains("Fail") == true)
                return null;

            str = str.Replace("\r\n", "\r").TrimEnd('\r');
            return new FTPDirectory(str, m_strLastDirectory);
        }


        // 재귀 호출
        public bool DownloadDir(string strLocalDir, string strTargetDir)
        {
            FTPDirectory directory = ListDirectoryDetail(strTargetDir);

            for (int i = 0; i < directory.Count; i++)
            {
                if (directory[i].FileType == FTPFile.DirectoryEntryTypes.Directory)
                {
                    DownloadDir(strLocalDir, directory[i].FullName + "/");
                }
                else if (directory[i].FileType == FTPFile.DirectoryEntryTypes.File)
                {
                    string strLocalFileName = strLocalDir + directory[i].FullName;

                    // 폴더 만들기
                    System.IO.DirectoryInfo _directoryinfo = new System.IO.DirectoryInfo(strLocalDir + directory[i].Path);
                    if (_directoryinfo.Exists == false)
                        _directoryinfo.Create();

                    Download(directory[i].FullName, strLocalFileName, true);
                }
            }
            return true;
        }

        // 仅文件夹内部文件
        public bool DownloadFolderFile(string strLocalDir, string strTargetDir)
        {
            FTPDirectory directory = ListDirectoryDetail(strTargetDir);

            if (directory == null) return false;

            for (int i = 0; i < directory.Count; i++)
            {
                if (directory[i].FileType == FTPFile.DirectoryEntryTypes.File)
                {
                    string strLocalFileName = strLocalDir + directory[i].FileName;
                    if (strLocalFileName.Contains("."))
                        Download(directory[i].FullName, strLocalFileName, true);
                    else
                    {
                        string name = strLocalFileName + "//";
                        string dir = strTargetDir + "//";
                        DownloadFolderFile(name, dir);
                    }
                }
            }
            return true;
        }

       
        /// <summary>
        /// 从服务器下载文件
        /// </summary>
        /// <param name="strSourceFilePath">文件在服务器的路径</param>
        /// <param name="strLocalFilePath">文件下载到本地的路径</param>
        /// <param name="bPermitOverwrite">是否覆盖原文件</param>
        /// <returns></returns>
        public bool Download(string strSourceFilePath, string strLocalFilePath, bool bPermitOverwrite)
        {
            bool isFinish = false;
            int index = strLocalFilePath.LastIndexOf('/')+1;


            //目录缺少需要创建新目录
            string dir = strLocalFilePath.Substring(0, index) ;

            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(strLocalFilePath))
                {
                    File.Delete(strLocalFilePath);
                }

                FileInfo fileinfo = new FileInfo(strLocalFilePath);
                isFinish = this.Download(strSourceFilePath, fileinfo, bPermitOverwrite);
       
            }
            catch (Exception e)
            {
               MessageBox.Show(e.Message);
            }

            return isFinish;
        }
    

        public bool Download(string strSourceFilename, FileInfo targetfileinfo, bool bPermitOverwrite)
        {
            bool isFinish = false;
            if (targetfileinfo.Exists && !(bPermitOverwrite))
            {
                throw (new ApplicationException("Target file already exists"));
            }

            string target;
            if (strSourceFilename.Trim() == "")
            {
                throw (new ApplicationException("File not specified"));
            }
            else if (strSourceFilename.Contains("/"))
            {
                target = AdjustDir(strSourceFilename);
            }
            else
            {
                target = CurrentDirectory + strSourceFilename;
            }

            long size = GetFileSize(strSourceFilename);
            if (size <= 0)
            {
                return false;
            }

            FTPUpdateValue.CurrentFileByte = size;
            long nReadByte = 0;
            long SPosition = 0;
            FTPUpdateValue.CurrentDownloadFileName = strSourceFilename;

            string URL = (Host + target);
                URL = URL + GetTimeStamp();

            FileStream writeStream = null;
            Stream responseStream = null;
            HttpWebResponse response = null;
            HttpWebRequest request = null;
            try
            {
                writeStream = new FileStream(targetfileinfo.FullName, FileMode.Append, FileAccess.Write,
                    FileShare.ReadWrite);
                request = WebRequest.Create(URL) as HttpWebRequest;
                //request.Timeout = 5000;
                response = request.GetResponse() as HttpWebResponse;
                responseStream = response.GetResponseStream();

                //if (SPosition > 0)
                //    request.AddRange((int)SPosition);
                nReadByte = SPosition;
                FTPUpdateValue.CurrentAccumlatereadByte += SPosition;

                byte[] bArr = new byte[2048];
                int sizeRead = responseStream.Read(bArr, 0, (int) bArr.Length);
                //while (nReadByte < size)
                while (sizeRead > 0)
                {
                    writeStream.Write(bArr, 0, sizeRead);
                    nReadByte += sizeRead;
                    if (strSourceFilename.Contains(mainform.m_clientServerName))
                    {
                        //todo 更新进度条
                        FTPUpdateValue.CurrentFileDownloadBytes = nReadByte;
                        FTPUpdateValue.CurrentAccumlatereadByte += sizeRead;
                    }

                    //mainform.UpdateDownLoadSpeed(nReadByte, sizeRead);
                    sizeRead = responseStream.Read(bArr, 0, bArr.Length);
                }

                //writeStream.Flush();
                writeStream.Close();
                responseStream.Close();
                response.Close();

                if (nReadByte == size)
                {
                    isFinish = true;
                }
                else
                {
                    mainform.ShowErrorMessage("服务器连接超时，请重启启动器！");
                    Step.CURRENT_STEP = Step.State._ProcessState_Exit;
                }
            }

            catch (Exception ex)
            {
                ShowDownloadError(ex, strSourceFilename);
                return false;
            }
            finally
            {
                if (writeStream != null)
                {
                    writeStream.Close();
                }

                if (responseStream != null)
                {
                    responseStream.Close();
                }

                if (response != null)
                {
                    response.Close();
                }
            }

            return isFinish;
        }
            

        public long GetFileSize(string strFilename)
        {
            string path;
            long size = -1;
            if (strFilename.Contains("/"))
            {
                path = AdjustDir(strFilename);
            }
            else
            {
                path = this.CurrentDirectory + strFilename;
            }
            string URI = Host + path+GetTimeStamp();
            HttpWebRequest req = null;
            HttpWebResponse rsp = null;

            try
            {
                //FtpWebRequest ftp = GetRequest(URI);
                //ftp.Method = WebRequestMethods.Ftp.GetFileSize;
                //string tmp = this.GetStringResponse(ftp);
                //size = GetSize(ftp);
                req = (HttpWebRequest)HttpWebRequest.Create(URI); //打开网络
                rsp = (HttpWebResponse)req.GetResponse();

                if (rsp.StatusCode == HttpStatusCode.OK)
                {
                    size = rsp.ContentLength; //从文件头得到远程文件的长度。
                }
                rsp.Close();
            }
            catch(WebException ex)
            {
                MessageBox.Show( "服务器文件获取异常："+ex.ToString());
                return size;
            }
            return size;
        }



        //请求查看文件夹
        //private FtpWebRequest GetRequest(string strURI)
        private HttpWebRequest GetRequest(string strURI)
        {
                //打开网络连接
              HttpWebRequest  result = (HttpWebRequest)HttpWebRequest.Create(strURI);
            //FtpWebRequest result = (FtpWebRequest)FtpWebRequest.Create(strURI);
            //result.UseBinary = true;

            //result.Credentials = GetCredentials();//创建请求

            //result.KeepAlive = false;
            //result.Timeout = 20000;
            //result.UsePassive = false;

            return result;
        }



        private string GetDirectory(string strDirectory)
        {
            string URI;
            if (strDirectory == "")
            {
                URI = Host + this.CurrentDirectory;
                m_strLastDirectory = this.CurrentDirectory;
            }
            else
            {
                if (!strDirectory.StartsWith("/"))
                {
                    throw (new ApplicationException("Directory should start with /"));
                }
                URI = Host + strDirectory;
                m_strLastDirectory = strDirectory;
            }

            //MessageBox.Show(URI);
            //MessageBox.Show(m_strLastDirectory);

            return URI;
        }


   

        //public string GetStringResponse(FtpWebRequest ftp)
        public string GetStringResponse(HttpWebRequest ftp)
        {
            string result = "";
            try
            {
                //using (FtpWebResponse response = (FtpWebResponse)ftp.GetResponse())
                using (HttpWebResponse response = (HttpWebResponse)ftp.GetResponse())
                {
                    long size = response.ContentLength;
                    using (Stream datastream = response.GetResponseStream())
                    {

                        using (StreamReader sr = new StreamReader(datastream))
                        {
                            result = sr.ReadToEnd();
                            sr.Close();
                        }

                        datastream.Close();
                    }

                    response.Close();
                }
            }
            catch (WebException ex)
            {
                ShowDownloadError(ex, "GetStringResponse");
            }
            return result;
        }


      
        private string AdjustDir(string strPath)
        {
            return ((strPath.StartsWith("/")) ? "" : "/").ToString() + strPath;
        }


        public bool DownloadPatch(string strLocalDir, string strTargetDir, string strDepthDir)
        {
            FTPDirectory directory = ListDirectoryDetail(strTargetDir);

            for (int i = 0; i < directory.Count; i++)
            {
                if (directory[i].FullName.Contains(".svn") == true) continue;

                if (directory[i].FileType == FTPFile.DirectoryEntryTypes.File)
                {
                    string strLocalFileName = string.Empty;

                    if (strDepthDir != "")
                    {
                        strLocalFileName = strLocalDir + strDepthDir + "/" + directory[i].FileName;
                        if (Directory.Exists(strLocalDir + strDepthDir) == false)
                        {
                            Directory.CreateDirectory(strLocalDir + strDepthDir);
                        }
                    }
                    else
                    {
                        strLocalFileName = strLocalDir + directory[i].FileName;
                    }

                    //FTPUpdateValue.DOWNLOADINDEX++;

                    Download(directory[i].FullName, strLocalFileName, true);
                }
                else if (directory[i].FileType == FTPFile.DirectoryEntryTypes.Directory)
                {
                    DownloadPatch(strLocalDir, directory[i].FullName + "/", strDepthDir + "/" + directory[i].FileName);
                }
            }

            return true;
        }

  
        private void ShowDownloadError(Exception ex,string fileName, bool isRepair = false)
        {
            string error ="服务连接失败";
            string status = ex.GetType()+ " " + WebExceptionStatus.ConnectFailure;
 

            string name = fileName.Substring(fileName.LastIndexOf("/")+1);
            error = error + status + "-" + Convert.ToInt32(Step.CURRENT_STEP)+ " path:" + name;
            mainform.ShowErrorMessage(error, isRepair);
        }


        /// <summary> 
        /// 获取时间戳 
        /// </summary> 
        /// <returns></returns> 
        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string times = "?" + Convert.ToInt64(ts.TotalSeconds).ToString();
            return times;
        }

    }
}
