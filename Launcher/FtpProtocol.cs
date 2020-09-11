//#define Debug

using System.Collections.Generic;
using System;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Globalization;
using System.Reflection;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Threading;

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
        public string m_downloadTemp = "temp";

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

        public List<string> ListDirectory(string strDirectory)
        {
            List<string> result = new List<string>();

            try
            {
                //FtpWebRequest ftp = GetRequest(GetDirectory(strDirectory));
                HttpWebRequest ftp = GetRequest(GetDirectory(strDirectory));
                //ftp.Method = WebRequestMethods.Ftp.ListDirectory;

                string str = GetStringResponse(ftp);
                str = str.Replace("\r\n", "\r").TrimEnd('\r');

                result.AddRange(str.Split('\r'));
            }
            catch(WebException ex) {
                ShowDownloadError(ex, strDirectory);
            }
            return result;

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


        public bool Upload(string strLocalFileName, string strTargetFileName)
        {
            if (!File.Exists(strLocalFileName))
            {
                throw (new ApplicationException("File " + strLocalFileName + " not found"));
            }

            FileInfo fileinfo = new FileInfo(strLocalFileName);
            return Upload(fileinfo, strTargetFileName);
        }

        public bool Upload(FileInfo fileinfo, string strTargetFileName)
        {
            string strTarget;
            if (strTargetFileName.Trim() == "")
            {
                strTarget = this.CurrentDirectory + fileinfo.Name;
            }
            else if (strTargetFileName.Contains("/"))
            {
                strTarget = AdjustDir(strTargetFileName);
            }
            else
            {
                strTarget = CurrentDirectory + strTargetFileName;
            }

            string URI = Host + strTarget;
            //FtpWebRequest ftp = GetRequest(URI);
            HttpWebRequest ftp = GetRequest(URI);

            //ftp.Method = WebRequestMethods.Ftp.UploadFile;
            //http上传？？？此处要修改
            //ftp.UseBinary = true;

            ftp.ContentLength = fileinfo.Length;

            const int BufferSize = 2048;
            byte[] content = new byte[BufferSize - 1 + 1];
            int dataRead;

            using (FileStream fs = fileinfo.OpenRead())
            {
                try
                {
                    using (Stream rs = ftp.GetRequestStream())
                    {
                        do
                        {
                            dataRead = fs.Read(content, 0, BufferSize);
                            rs.Write(content, 0, dataRead);
                        } while (!(dataRead < BufferSize));
                        rs.Close();
                    }
                }
                catch (WebException ex)
                {
                    ShowDownloadError(ex, strTargetFileName);
                }
                finally
                {
                    fs.Close();
                }
            }

            ftp = null;
            return true;
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

        public bool DownloadLPL(string strSourceFilePath, string strLocalFilePath, bool bPermitOverwrite)
        {
            bool isFinish = false;
            int index = strLocalFilePath.LastIndexOf('/') + 1;
            try
            {
                if (File.Exists(strLocalFilePath))
                {
                    File.Delete(strLocalFilePath);
                }
                //目录缺少需要创建新目录
                string dir = strLocalFilePath.Substring(0, index);

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            FileInfo fileinfo = new FileInfo(strLocalFilePath);
            isFinish = this.DownloadLPL(strSourceFilePath, fileinfo, bPermitOverwrite);
          
            return isFinish;
        }
        public bool DownloadZipLPL(string strSourceFilePath, string strLocalFilePath, bool bPermitOverwrite,long size0)
        {
             
            bool isFinish = false;
            int index = strLocalFilePath.LastIndexOf('/') + 1;

//            if (File.Exists(strLocalFilePath))
//            {
//                File.Delete(strLocalFilePath);
//            }
            //目录缺少需要创建新目录
            string dir = strLocalFilePath.Substring(0, index);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            FileInfo fileinfo = new FileInfo(strLocalFilePath);
            isFinish = this.DownloadZipLPL(strSourceFilePath, fileinfo, bPermitOverwrite, size0);
            if (isFinish)
            {

            }
            else
            {
//                string s = "下载失败： " + strLocalFilePath;
//                MessageBox.Show(s);    
            }
            return isFinish;
        }

        public bool Download(string strSourceFilePath, string strLocalFilePath, bool bPermitOverwrite)
        {
            bool isFinish = false;
            int index = strLocalFilePath.LastIndexOf('/')+1;
            string tempName = strLocalFilePath.Insert(index, m_downloadTemp);

            string downloadPath = tempName;
            string oldName = strLocalFilePath.Insert(index, "old");

            //目录缺少需要创建新目录
            string dir = strLocalFilePath.Substring(0, index) ;

            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(tempName))
                {
                    File.Delete(tempName);
                }

                FileInfo fileinfo = new FileInfo(downloadPath);
                isFinish = this.Download(strSourceFilePath, fileinfo, bPermitOverwrite);

                if (isFinish)
                {
                    
                    
                    //if (strLocalFilePath.Contains(mainform.m_luancherPath))
                    //{
                    //    //File.Move(strLocalFilePath, oldName);
                    //    //File.Move(tempName, strLocalFilePath);
                    //    //将启动器下载到 下载专用文件夹
                    //}
                    //else
                    if(strLocalFilePath.Contains(mainform.m_serverVerName))
                    {
                        if (File.Exists(strLocalFilePath))
                        {
                            File.Delete(strLocalFilePath);
                        }
                        File.Move(tempName, strLocalFilePath);
                    }
                }
                else
                {
                    string s = "下载失败： " + strLocalFilePath;
                    //MessageBox.Show(s);    
                    if (File.Exists(tempName))
                    {
                        File.Delete(tempName);
                    }
                }
            }
            catch (Exception e)
            {
               MessageBox.Show(e.Message);
            }

            return isFinish;
        }

        //多文件
        public bool DownloadLPLEachFile(string strSourceFilePath, string strLocalFilePath, bool bPermitOverwrite)
        {

            bool isFinish = false;
            int index = strLocalFilePath.LastIndexOf('/') + 1;
            //int end = strSourceFilePath.Length - index ;
            //string filename = strSourceFilePath.Substring(index,end);
            string tempName = strLocalFilePath.Insert(index, "temp");
            //"temp" + filename;

            string downloadPath = tempName;
            string oldName = strLocalFilePath.Insert(index, "old");

            //目录缺少需要创建新目录
            string dir = strLocalFilePath.Substring(0, index);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(tempName))
            {
                File.Delete(tempName);
            }

            FileInfo fileinfo = new FileInfo(downloadPath);
            isFinish = this.DownloadForFolderLPL(strSourceFilePath, fileinfo, bPermitOverwrite);

            if (isFinish)
            {
                if (File.Exists(strLocalFilePath))
                {
                    File.Delete(strLocalFilePath);
                }
                File.Move(tempName, strLocalFilePath);
            }
            else
            {
                string s = "下载失败： " + strLocalFilePath;
                //MessageBox.Show(s);    
                if (File.Exists(tempName))
                {
                    File.Delete(tempName);
                }
            }
            return isFinish;
        }


        public bool Download(FTPFile file, string strLocalFilename, bool bPermitOverwrite)
        {
            return this.Download(file.FullName, strLocalFilename, bPermitOverwrite);
        }

        public bool Download(FTPFile file, FileInfo localfileinfo, bool bPermitOverwrite)
        {
            return this.Download(file.FullName, localfileinfo, bPermitOverwrite);
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
                    if (!strSourceFilename.Contains(".ini"))
                    {
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
                    if(Step.CURRENT_STEP == Step.State._ProcessState_PatchDownloading)
                    MainWindow.UpdateOnce = true;
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
        public bool DownloadLPL(string strSourceFilename, FileInfo targetfileinfo, bool bPermitOverwrite)
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

            UpdateHelper.CurrentFileByte = size;
            UpdateHelper.maxByte = size;
            UpdateHelper.downloadIndex++;
            UpdateHelper.maxfileCount++;

            long nReadByte = 0;
            long SPosition = 0;
            UpdateHelper.CurrentDownloadFileName = strSourceFilename;

            string URL = (Host + target);
            URL = URL +GetTimeStamp();

            FileStream writeStream = null;
            Stream responseStream = null;
            HttpWebResponse response = null;
            HttpWebRequest request = null;
            try
            {
                writeStream = new FileStream(targetfileinfo.FullName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                request = WebRequest.Create(URL) as HttpWebRequest;
                //request.Timeout = 5000;
                response = request.GetResponse() as HttpWebResponse;
                responseStream = response.GetResponseStream();

                //if (SPosition > 0)
                //    request.AddRange((int)SPosition);
                nReadByte = SPosition;
                UpdateHelper.CurrentAccumlatereadByte += SPosition;

                byte[] bArr = new byte[2048];
                int sizeRead = responseStream.Read(bArr, 0, (int)bArr.Length);
                //while (nReadByte < size)
                while (sizeRead > 0)
                {
                    writeStream.Write(bArr, 0, sizeRead);
                    nReadByte += sizeRead;

                    UpdateHelper.CurrentFileDownloadBytes = nReadByte;
                    UpdateHelper.CurrentAccumlatereadByte += sizeRead;
                    sizeRead = responseStream.Read(bArr, 0, bArr.Length);
                }

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

        //断点续传
        public bool DownloadZipLPL(string strSourceFilename, FileInfo targetfileinfo, bool bPermitOverwrite,long size0)
        {
            bool isFinish = false;

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
//            long size = GetFileSizeFromOutSide(strSourceFilename);
            if (size <= 0)
            {
                return false;
            }
            //UpdateHelper.Instance().ShowBox("服务器上压缩包大小 " + FTPUpdateValue.FormatBytes(size) + "   " + size);

            UpdateHelper.CurrentFileByte = size;
            UpdateHelper.maxByte = size;
            UpdateHelper.downloadIndex++;
            UpdateHelper.maxfileCount++;

            long nReadByte = 0;
            long SPosition = 0;
            UpdateHelper.CurrentDownloadFileName = strSourceFilename;
//            string URL = ("https://sw2.dindanw.com:8314" + target);
            string URL = (Host + target);
//            URL = URL + "?" + GetTimeStamp();

            FileStream writeStream = null;
            Stream responseStream = null;
            HttpWebResponse response = null;
            HttpWebRequest request = null;
            FtpWebResponse response3 = null;
            try
            {
                #region 断点续传

                if (targetfileinfo.Exists && size0 > 0)
                {
                    //UpdateHelper.Instance().ShowBox("本地压缩包大小 " + FTPUpdateValue.FormatBytes(size0) + "   " + size0);

                    writeStream = File.OpenWrite(targetfileinfo.FullName);
                    //获取已经下载的长度
                    SPosition = writeStream.Length;
                    string pos = FTPUpdateValue.FormatBytes(SPosition);
                    //UpdateHelper.Instance().ShowBox("断点续传 当前下载起始位置 "+ pos + "   " + SPosition);
                    if (SPosition == size)
                    {
                        isFinish = true;
                        UpdateHelper.Instance().ShowBox("断点续传完成");
                        return true;
                    }
                    else if (SPosition >= size)
                    {
                        SPosition = 0;
                        if (File.Exists(targetfileinfo.FullName))
                        {
                            File.Delete(targetfileinfo.FullName);
                        }
                        writeStream = new FileStream(targetfileinfo.FullName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        UpdateHelper.Instance().ShowBox("断点续传 当前下载位置大于需要下载的总大小");
                        string abc = "请求偏移本地文件大小:" + SPosition + "原文件长度:" + size + "本地文件总长度" +
                                     size0;
                    }
                    else
                    {
                        UpdateHelper.Instance().ShowBox("断点续传 继续");
                        writeStream.Seek(SPosition, SeekOrigin.Current);
                    }
                }
                else
                {
                    writeStream = new FileStream(targetfileinfo.FullName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                }

                request = WebRequest.Create(URL) as HttpWebRequest;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

                //request.Timeout = 5000;
                if (SPosition > 0)
                {
//                    request.AddRange(SPosition);
                    request.AddRange(SPosition, size);
                }
                response = request.GetResponse() as HttpWebResponse;
                var responseContentLength = response.ContentLength;
                var totalsizez = responseContentLength + SPosition;

                //UpdateHelper.Instance().ShowBox("断点续传---当前response返回长度" + FTPUpdateValue.FormatBytes(responseContentLength) +"   "+ responseContentLength);

                //                var responseHeader = response.Headers["Content-Range"];
                //                if (responseHeader == null)
                //                {
                //                    UpdateHelper.Instance().ShowBox("如果返回的response头中Content-Range值为空，说明服务器不支持Range属性，不支持断点续传,返回的是所有数据");
                //                }

                if (totalsizez != size)
                {
                    string abc = "请求长度：" + responseContentLength + "请求偏移:" + SPosition + "原文件长度:" + size + "本地文件总长度" +
                                 size0;
                    UpdateHelper.Instance().ShowBox("断点续传---当前response返回长度加上已经下载长度不等于总文件大小\n" + abc);

                    //writeStream.Flush();
                    if (writeStream != null)
                    {
                        writeStream.Close();
                        writeStream = null;
                    }
                    if (responseStream != null)
                    {
                        responseStream.Close();
                        responseStream = null;
                    }
                    if (response != null)
                    {
                        response.Close();
                        response = null;
                    }
                    Step.CURRENT_STEP = Step.State._ProcessState_Exit;
                    return false;
                }
                responseStream = response.GetResponseStream();

                nReadByte = SPosition;
                UpdateHelper.CurrentAccumlatereadByte += SPosition;

                byte[] bArr = new byte[2048];
                int sizeRead = responseStream.Read(bArr, 0, (int)bArr.Length);

                while (sizeRead > 0)
                {
                    writeStream.Write(bArr, 0, sizeRead);
                    nReadByte += sizeRead;

                    UpdateHelper.CurrentFileDownloadBytes = nReadByte;
                    UpdateHelper.CurrentAccumlatereadByte += sizeRead;
                    sizeRead = responseStream.Read(bArr, 0, bArr.Length);
                }
                //writeStream.Flush();
                if (writeStream != null)
                {
                    writeStream.Close();
                    writeStream = null;
                }
                if (responseStream != null)
                {
                    responseStream.Close();
                    responseStream = null;
                }
                if (response != null)
                {
                    response.Close();
                    response = null;
                }

                //如果返回的response头中Content-Range值为空，说明服务器不支持Range属性，不支持断点续传,返回的是所有数据

                if (nReadByte == size)
                {
                    isFinish = true;
                }
                else if (nReadByte < size)
                {
                    UpdateHelper.Instance().ShowBox("连接服务器失败 自动重试，当前下载位置小于总大小");
                    //Step.CURRENT_STEP = Step.State._ProcessState_Exit;
                }
                else
                {
                    UpdateHelper.Instance().ShowBox("连接服务器失败 自动重试，当前下载位置大于总大小");
                    Step.CURRENT_STEP = Step.State._ProcessState_Exit;
                }
                MainWindow.UpdateOnce=true;

                #endregion
                #region FTP断点续传

                //                if (targetfileinfo.Exists && size0 > 0)
                //                {
                //                    writeStream = File.OpenWrite(targetfileinfo.FullName);
                //                    //获取已经下载的长度
                //                    SPosition = writeStream.Length;
                //
                //                    if (SPosition == size)
                //                    {
                //                        isFinish = true;
                //                        UpdateHelper.Instance().ShowBox("断点续传完成");
                //                        return true;
                //                    }
                //                    else if (SPosition >= size)
                //                    {
                //                        SPosition = 0;
                //                        if (File.Exists(targetfileinfo.FullName))
                //                        {
                //                            File.Delete(targetfileinfo.FullName);
                //                        }
                //                        writeStream = new FileStream(targetfileinfo.FullName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                //                        UpdateHelper.Instance().ShowBox("断点续传 当前下载位置大于需要下载的总大小");
                //                    }
                //                    else
                //                    {
                //                        UpdateHelper.Instance().ShowBox("断点续传 继续");
                //                        writeStream.Seek(SPosition, SeekOrigin.Current);
                //                    }
                //                }
                //                else
                //                {
                //                    writeStream = new FileStream(targetfileinfo.FullName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                //                }
                //
                //
                //
                //
                //                FtpWebRequest reqFTP, ftpsize;
                //                Uri uri = new Uri("ftp://192.168.4.141/SoulOfWar/ClientZip/Test.SW");
                //                ftpsize = (FtpWebRequest)FtpWebRequest.Create(uri);
                //                ftpsize.UseBinary = true;
                //                ftpsize.ContentOffset = SPosition;
                //
                //                reqFTP = (FtpWebRequest)FtpWebRequest.Create(uri);
                //                reqFTP.UseBinary = true;
                //                reqFTP.KeepAlive = false;
                //                reqFTP.ContentOffset = SPosition;
                //                ftpsize.Credentials = new NetworkCredential("ftp02", "123456"); //new NetworkCredential(FtpUserID, FtpPassword);
                //                reqFTP.Credentials = new NetworkCredential("ftp02", "123456"); // new NetworkCredential(FtpUserID, FtpPassword);
                //                ftpsize.Method = WebRequestMethods.Ftp.GetFileSize;
                //                FtpWebResponse re = (FtpWebResponse)ftpsize.GetResponse();
                //                var totalBytes = re.ContentLength;
                //
                //                re.Close();
                //                if (SPosition == totalBytes)
                //                {
                //
                //                    return true;
                //                }
                //
                //                reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                //                response3 = (FtpWebResponse)reqFTP.GetResponse();
                //                responseStream = response3.GetResponseStream();
                //                nReadByte = SPosition;
                //                UpdateHelper.CurrentAccumlatereadByte += SPosition;
                //
                //                byte[] bArr = new byte[2048];
                //                int sizeRead = responseStream.Read(bArr, 0, (int)bArr.Length);
                //
                //
                //                while (sizeRead > 0)
                //                {
                //                    writeStream.Write(bArr, 0, sizeRead);
                //                    nReadByte += sizeRead;
                //
                //                    UpdateHelper.CurrentFileDownloadBytes = nReadByte;
                //                    UpdateHelper.CurrentAccumlatereadByte += sizeRead;
                //                    sizeRead = responseStream.Read(bArr, 0, bArr.Length);
                //                }
                //
                //
                //
                //                if (nReadByte == size)
                //                {
                //                    isFinish = true;
                //                }
                //                else if (nReadByte < size)
                //                {
                //                    UpdateHelper.Instance().ShowBox("连接服务器失败 自动重试，当前下载位置小于总大小");
                //
                //                    Step.CURRENT_STEP = Step.State._ProcessState_Exit;
                //                }
                //                else
                //                {
                //                    UpdateHelper.Instance().ShowBox("连接服务器失败 自动重试，当前下载位置大大大于总大小");
                //                    Step.CURRENT_STEP = Step.State._ProcessState_Exit;
                //                }
                //
                //                //writeStream.Flush();
                //                if (writeStream != null)
                //                {
                //                    writeStream.Close();
                //                }
                //                if (responseStream != null)
                //                {
                //                    responseStream.Close();
                //                }
                //                if (response != null)
                //                {
                //                    response.Close();
                //                }
                //                if (response3 != null)
                //                {
                //                    response3.Close();
                //                }

                #endregion
            }
            //catch (WebException ex)
            //{
            //    ShowDownloadError(ex, strSourceFilename);
            //    if (writeStream != null) writeStream.Close();
            //    //Step.CURRENT_STEP = Step.State._ProcessState_Exit;
            //    return false;
            //}
            //catch (SocketException ex)
            //{
            //    UpdateHelper.Instance().ShowBox("SocketException "+ ex.Message);
            //    //UpdateHelper.Instance().ShowBox("连接断开，自动重连中");
            //    if (writeStream != null)
            //    {
            //        writeStream.Close();
            //    }
            //    if (responseStream != null)
            //    {
            //        responseStream.Close();
            //    }
            //    if (response != null)
            //    {
            //        response.Close();
            //    }
            //    if (response3 != null)
            //    {
            //        response3.Close();
            //    }
            //    return false;
            //}
            //catch (IOException e)
            //{
            //    ShowDownloadError(e,"IOException " + e.Message);

            //    //UpdateHelper.Instance().ShowBox("IOException " + e.Message);
            //    if (writeStream != null)
            //    {
            //        writeStream.Close();
            //    }
            //    if (responseStream != null)
            //    {
            //        responseStream.Close();
            //    }
            //    if (response != null)
            //    {
            //        response.Close();
            //    }
            //    if (response3 != null)
            //    {
            //        response3.Close();
            //    }

            //    return false;
            //}
            catch (Exception ex)
            {
                if(UpdateHelper.Instance().failCount==0)
                ShowDownloadError(ex, "IOException " + ex.Message);

                //UpdateHelper.Instance().ShowBox("IOException " + e.Message);
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
                if (response3 != null)
                {
                    response3.Close();
                }

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
                if (response3!=null)
                {
                    response3.Close();
                }
            }
            return isFinish;
        }


        public bool DownloadForFolderLPL(string strSourceFilename, FileInfo targetfileinfo, bool bPermitOverwrite)
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

            UpdateHelper.CurrentFileByte = size;

            long nReadByte = 0;
            long SPosition = 0;
            UpdateHelper.CurrentDownloadFileName = strSourceFilename;

            string URL = (Host + target);
            URL = URL + GetTimeStamp();

            FileStream writeStream = null;
            Stream responseStream = null;
            HttpWebResponse response = null;
            HttpWebRequest request = null;
            try
            {
                writeStream = new FileStream(targetfileinfo.FullName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                request = WebRequest.Create(URL) as HttpWebRequest;
                //request.Timeout = 5000;
                response = request.GetResponse() as HttpWebResponse;
                responseStream = response.GetResponseStream();

                //if (SPosition > 0)
                //    request.AddRange((int)SPosition);
                nReadByte = SPosition;
                UpdateHelper.CurrentAccumlatereadByte += SPosition;

                byte[] bArr = new byte[2048];
                int sizeRead = responseStream.Read(bArr, 0, (int)bArr.Length);
                //while (nReadByte < size)
                while (sizeRead > 0)
                {
                    writeStream.Write(bArr, 0, sizeRead);
                    nReadByte += sizeRead;
                    UpdateHelper.CurrentFileDownloadBytes = nReadByte;
                    UpdateHelper.CurrentAccumlatereadByte += sizeRead;
                    sizeRead = responseStream.Read(bArr, 0, bArr.Length);
                }

                //writeStream.Flush();
                writeStream.Close();
                responseStream.Close();
                response.Close();

                if (nReadByte == size)
                    isFinish = true;
                else
                {
                    mainform.ShowErrorMessage("服务器连接超时，请重启启动器！");
                }
            }

            catch (Exception ex)
            {
                ShowDownloadError(ex, strSourceFilename,true);
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
//                UpdateHelper.Instance()._m_MainForm.UpdateOnce=true;
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
                return size;
            }
            return size;
        }


        public long GetFileSizeFromOutSide(string strFilename)
        {
            string path;
            long size = 0;
            if (strFilename.Contains("/"))
            {
                path = AdjustDir(strFilename);
            }
            else
            {
                path = this.CurrentDirectory + strFilename;
            }
            string URI = "https://sw2.dindanw.com:8314" + path + GetTimeStamp();
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
            catch (WebException ex)
            {
                ShowDownloadError(ex, strFilename);
                return size;
            }
            return size;
        }
        //private long GetSize(FtpWebRequest ftp)
        private long GetSize(HttpWebRequest ftp)
        {
            long size = 0;

            try
            {
                //using (FtpWebResponse response = (FtpWebResponse)ftp.GetResponse())
                using (HttpWebResponse response = (HttpWebResponse)ftp.GetResponse())
                {
                    size = response.ContentLength;
                    response.Close();
                }
            }
            catch (WebException ex)
            {
                ShowDownloadError(ex, "GetSize");
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


        private ICredentials GetCredentials()
        {
            return new NetworkCredential(ID, Password);
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


        private string GetFullPath(string strFile)
        {
            if (strFile.Contains("/"))
            {
                return AdjustDir(strFile);
            }
            else
            {
                return this.CurrentDirectory + strFile;
            }
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

        public void SingleDownload(String strLocal, String strTarget)
        {
            //FtpWebRequest regFtp = (FtpWebRequest)WebRequest.Create(m_strHost);
            //regFtp.Method = WebRequestMethods.Ftp.GetFileSize;
            //regFtp.Credentials = new NetworkCredential(m_strID, m_strPassword);

            Uri ftpUrl = new Uri("ftp://" + m_strHost + strTarget);

            using (WebClient request = new WebClient())
            {
                request.Credentials = new NetworkCredential(m_strID, m_strPassword);

                request.DownloadProgressChanged += request_DowloadProgressChanged;
                request.DownloadFileCompleted += request_DownloadFileCompleted;

                request.DownloadFileAsync(ftpUrl, strLocal);
            }
        }

        public void request_DowloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {

        }

        public void request_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {

        }

        private void ShowDownloadError(Exception ex,string fileName, bool isRepair = false)
        {
            string error ="服务连接失败";
            string status = ex.GetType()+ " " + WebExceptionStatus.ConnectFailure;

            switch (Step.CURRENT_STEP)
            {
                case Step.State.__initOldUpdating:
                case Step.State.__GetingLauncherMd5:
                {
                    string m_clientPath = UpdateHelper.Instance().GetClientPathFile();
                    if (UpdateHelper.Instance().CheckClientIsEx())
                    {
                        DialogResult dialogResult = mainform.ShowErrorMessage("版本号配置文件下载失败，是否跳过此次更新检查进入游戏？", false,
                            "连接出错",
                            MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                        if (dialogResult == DialogResult.OK)
                        {
                            Step.CURRENT_STEP = Step.State._ProcessState_NoNetToReady;
                        }
                        else
                        {
                            Step.CURRENT_STEP = Step.State.__GetLauncherMd5Fail;
                        }
                        return;
                    }
                    else
                    {
                        Step.CURRENT_STEP = Step.State.__GetLauncherMd5Fail;

                    }
                    break;
                }
                //case Step.State.__DownloadingConfig:
                //    Step.CURRENT_STEP = Step.State.__DownloadConfigFail;
                //    break;
                case Step.State._ProcessState_LauncherDownloading:
                    Step.CURRENT_STEP = Step.State._ProcessState_LauncherDownloadFail;
                    break;
                case Step.State._ProcessState_ResourceMd5Downloading:
                    Step.CURRENT_STEP = Step.State._ProcessState_ResMd5DownloadFail;
                    break;
                case Step.State._ProcessState_GetingPatchFileNum:
                    Step.CURRENT_STEP = Step.State._ProcessState_GetPatchFileNumFail;
                    break;
                case Step.State._ProcessState_PatchDownloading:
                    Step.CURRENT_STEP = Step.State._ProcessState_PatchDownloadFail;
                    FTPUpdateValue.downloadIndex--;
                    break;
                case Step.State._ProcessState_NewResDownloading:
                    Step.CURRENT_STEP = Step.State._ProcessState_NewResDownloadFail;
                    break;
                default:
                    break;
            }

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
