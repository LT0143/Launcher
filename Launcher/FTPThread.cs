using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Launcher
{
    public class FTPThread
    {
        #region FTPDirectoryHistory

        private delegate void invoke_FTPDirectoryHistory(object _value);

        private Thread m_thread_FTPDirectoryHistory = null;

        /// <summary>
        /// 获取所有补丁包大小
        /// </summary>
        /// <param name="_parameter"></param>
        public void StartFTPDirectoryHistory(object[] _parameter)
        {
            m_thread_FTPDirectoryHistory = new Thread(new ParameterizedThreadStart(ThreadFTPDirectoryHistory));
            m_thread_FTPDirectoryHistory.SetApartmentState(ApartmentState.STA);
            m_thread_FTPDirectoryHistory.Priority = ThreadPriority.Normal;
            m_thread_FTPDirectoryHistory.Start(_parameter);
        }

        private void ThreadFTPDirectoryHistory(object _value)
        {
            object[] _object = _value as object[];
            String strDirectory = _object[0] as String;
            Step.State state = (Step.State)_object[1];
            FtpProtocol protocol = _object[2] as FtpProtocol;
            string verCurrent = _object[3] as string;
            //string nextVer = _object[4] as string;

            FileHistoryRepository.Reset();
            // protocol.CurrentDirectory = strDirectory;
            if (!FileHistoryRepository.GetFileHistory(protocol, strDirectory, verCurrent))
            {
                return;
            }
            FTPUpdateValue.BGetDownLoadNum = true;

            switch (state)
            {
                //case Step.State._ProcessState_RequiredFileDirectoryHistory:
                //    {
                //        Step.CURRENT_STEP = Step.State._ProcessState_RequiredFTPDownload;
                //    }
                //    break;
                //case Step.State._ProcessState_InstallFileDirectoryHistory:
                //    {
                //        Step.CURRENT_STEP = Step.State._ProcessState_InstallFTPDownload;
                //    }
                    //break;
                case Step.State._ProcessState_GetingPatchFileNum:
                    {
                        //MainForm.NextPathVer = nextVer;
                        Step.CURRENT_STEP = Step.State._ProcessState_PatchDownload;
                    }
                    break;
            }
        }

        #endregion

        #region Download

        private delegate void invoke_Download(object _value);

        public Thread m_thread_Download = null;
        //public Thread m_thread_DownloadZip = null;

        public void StartDownloadRes(object[] _parameter)
        {
            m_thread_Download = new Thread(new ParameterizedThreadStart(ThreadDownload));
            m_thread_Download.SetApartmentState(ApartmentState.STA);
            m_thread_Download.Priority = ThreadPriority.Highest;
            m_thread_Download.Start(_parameter);
        }

        private void ThreadDownload(object _value)
        {
            object[] _object = _value as object[];
            String[] pathArray = _object[0] as String[];
            Step.State state = (Step.State)_object[1];
            FtpProtocol protocol = _object[2] as FtpProtocol;
            string ver = _object[3] as String;
            string curDirectory = _object[4] as String;

            //FTPUpdateValue.maxfileCount = Math.Max(0, FTPUpdateValue.maxfileCount - 1);
            //FTPUpdateValue.downloadIndex =  Math.Max(0, FTPUpdateValue.downloadIndex - 1);
             FTPUpdateValue.CurrentFileDownloadBytes = 0;
            FTPUpdateValue.CurrentFileByte = 0;
            //FTPUpdateValue.maxByte = FTPUpdateValue.maxByte - FTPUpdateValue.CurrentFileByte;
            //FTPUpdateValue.CurrentAccumlatereadByte =Math.Max(0, FTPUpdateValue.CurrentAccumlatereadByte- FTPUpdateValue.CurrentFileDownloadBytes);
            FTPUpdateValue.maxfileCount+= pathArray.Length;

            //TODO 此处开始获取新资源大小
            for (int i = 0; i < pathArray.Length; i++)
            {
                string strLocal = curDirectory + pathArray[i];
                strLocal = strLocal.Replace('\\', '/');
                string strTarget = UpdateHelper.Instance().UpdateFolder + ver + "/" + pathArray[i];
                long sizeI = protocol.GetFileSize(strTarget);
                if (sizeI >= 0)
                {
                    FTPUpdateValue.maxByte += sizeI;
                }
                else
                    return;
            }


            // 此处开始下载资源。更新下载进度
            for (int i=0;i< pathArray.Length;i++)
            {
                string strLocal = curDirectory + pathArray[i];
                strLocal = strLocal.Replace('\\','/');
                string strTarget = UpdateHelper.Instance().UpdateFolder +ver+"/" + pathArray[i];
                FTPUpdateValue.downloadIndex++;
                if ( !protocol.Download(strTarget, strLocal, true))
                {
                    return;
                }
            }
            
            switch (state)
            {
                case Step.State._ProcessState_NewResDownloading:
                    {
                        //开始版本号检测
                        Step.CURRENT_STEP = Step.State._ProcessState_NewResCheck;
                    }
                    break;
            }

        }

        public void StartSingleDownload(object[] _parameter)
        {
            m_thread_Download = new Thread(new ParameterizedThreadStart(ThreadSingleDownload));
            m_thread_Download.SetApartmentState(ApartmentState.STA);
            m_thread_Download.Priority = ThreadPriority.Highest;
            m_thread_Download.Start(_parameter);
        }

        private void ThreadSingleDownload(object _value)
        {
            try
            {
                object[] _object = _value as object[];

                String strLocal = _object[0] as String;
                String strTarget = _object[1] as String;
                Step.State state = (Step.State)_object[2];

                FtpProtocol protocol = _object[3] as FtpProtocol;

                if (!protocol.Download(strTarget, strLocal, true))
                {
                    return;
                }

                //FTPUpdateValue.CurrentFileByte=0;
                //FTPUpdateValue.maxByte = 0;
                switch (state)
                {
                    case Step.State._ProcessState_ResourceMd5Downloading:
                        {
                            Step.CURRENT_STEP = Step.State._ProcessState_ResMd5DownloadFinish;// 旧资源校验到不对上传服务器
                        }
                        break;
                        
                  case Step.State._ProcessState_VerDownloading:
                        {
                            Step.CURRENT_STEP = Step.State.__CheckLauncherMD5;
                        }
                        break;

                    case Step.State._ProcessState_LauncherDownloading:
                        {
                            Step.CURRENT_STEP = Step.State.__MergeLauncher;
                        }
                        break;
                    //case Step.State.__DownloadingConfig:
                    //   {
                    //       Step.CURRENT_STEP = Step.State.___CheckClientComplete;
                    //    }
                    //    break;
                    //case Step.State._ProcessState_InstallFTPDownload:
                    //    {
                    //        Step.CURRENT_STEP = Step.State._ProcessState_PatchVerCheck;
                    //    }
                        //break;
                    case Step.State._ProcessState_PatchDownloading:
                        {
                            Step.CURRENT_STEP = Step.State._ProcessState_PatchDownloadFinish;
                        }
                        break;
                    default:
                        Step.CURRENT_STEP = state;
                        break;
                }
            }
            catch (ThreadAbortException ex)
            {

                return;
            }
          
        }

        #endregion

        #region 新下载


        public void StartSingleDownloadLPL(object[] _parameter)
        {
            m_thread_Download = new Thread(new ParameterizedThreadStart(ThreadSingleDownloadLPL));
            m_thread_Download.SetApartmentState(ApartmentState.STA);
            m_thread_Download.Priority = ThreadPriority.Highest;
            m_thread_Download.Start(_parameter);
        }

        private void ThreadSingleDownloadLPL(object _value)
        {
            try
            {
                object[] _object = _value as object[];

                String strLocal = _object[0] as String;
                String strTarget = _object[1] as String;
                Step.State state = (Step.State)_object[2];
                FtpProtocol protocol = _object[3] as FtpProtocol;

                Step.State failstate = (Step.State)_object[4];

                if (!protocol.DownloadLPL(strLocal, strTarget, true))
                {
                    Step.CURRENT_STEP = failstate;
                    return;
                }

                Step.CURRENT_STEP = state;
            }
            catch (ThreadAbortException ex)
            {
                return;
            }

        }

        //下载zip压缩包 用断点续传

        public void StartSingleDownloadZipLPL(object[] _parameter)
        {
            //m_thread_DownloadZip = new Thread(new ParameterizedThreadStart(ThreadSingleDownloadZipLPL));
            //m_thread_DownloadZip.SetApartmentState(ApartmentState.STA);
            //m_thread_DownloadZip.Priority = ThreadPriority.Highest;
            //m_thread_DownloadZip.Start(_parameter);
        }

        private void ThreadSingleDownloadZipLPL(object _value)
        {
            try
            {
                object[] _object = _value as object[];

                String strLocal = _object[0] as String;
                String strTarget = _object[1] as String;
                Step.State state = (Step.State)_object[2];
                FtpProtocol protocol = _object[3] as FtpProtocol;

                Step.State failstate = (Step.State)_object[4];
                long size0 = (long)_object[5];
                Step.State preState = (Step.State)_object[6];

                if (!protocol.DownloadZipLPL(strLocal, strTarget, true, size0))
                {

                    if (UpdateHelper.Instance().failCount > 0)
                    {
                        UpdateHelper.Instance().failCount--;
                        Step.CURRENT_STEP = preState;
                    }
                    else
                    {
                        Step.CURRENT_STEP = failstate;
                        UpdateHelper.Instance().failCount = UpdateHelper.m_ZipDownTimes;
                        //UpdateHelper.Instance().ShowBox("网络连接状态差,请查看网络是否良好再打开程序");
                        //Step.CURRENT_STEP = Step.State._ProcessState_Exit;
                    }

                    return;
                }

                Step.CURRENT_STEP = state;
            }
            catch (ThreadAbortException ex)
            {
                return;
            }
        }

        public void StartDownloadGameFile(object[] _parameter)
        {
            m_thread_Download = new Thread(new ParameterizedThreadStart(DownloadGameFile));
            m_thread_Download.SetApartmentState(ApartmentState.STA);
            m_thread_Download.Priority = ThreadPriority.Highest;
            m_thread_Download.Start(_parameter);
        }

        private void DownloadGameFile(object _value)
        {
            object[] _object = _value as object[];
            String[] pathArray = _object[0] as String[];
            Step.State state = (Step.State)_object[1];
            FtpProtocol protocol = _object[2] as FtpProtocol;
            string curDirectory = _object[3] as String;
            string finalDirectory = _object[4] as String;

            //FTPUpdateValue.maxfileCount = Math.Max(0, FTPUpdateValue.maxfileCount - 1);
            //FTPUpdateValue.downloadIndex =  Math.Max(0, FTPUpdateValue.downloadIndex - 1);
            UpdateHelper.CurrentFileDownloadBytes = 0;
            UpdateHelper.CurrentFileByte = 0;
            //FTPUpdateValue.maxByte = FTPUpdateValue.maxByte - FTPUpdateValue.CurrentFileByte;
            //FTPUpdateValue.CurrentAccumlatereadByte =Math.Max(0, FTPUpdateValue.CurrentAccumlatereadByte- FTPUpdateValue.CurrentFileDownloadBytes);
            UpdateHelper.maxfileCount += pathArray.Length;

            //TODO 此处开始获取新资源大小
            for (int i = 0; i < pathArray.Length; i++)
            {
                string strTargetprint =  pathArray[i];
                string strTarget = UpdateHelper.Instance().completeFolder + pathArray[i];
                string text = string.Format("当前检查{0}/{1}个文件", i, UpdateHelper.maxfileCount);
                
                UpdateHelper.Instance().ChangeTextInfo(text);
                long sizeI = protocol.GetFileSize(strTarget);
                if (sizeI >= 0)
                {
                    UpdateHelper.maxByte += sizeI;
                }
                else
                    return;
            }
            UpdateHelper.Instance().ChangeTextInfo("");

            //todo 此处开始下载资源。更新下载进度
            for (int i = 0; i < pathArray.Length; i++)
            {
                var filepath = pathArray[i];
                string strLocal = curDirectory + filepath;
                strLocal = strLocal.Replace('\\', '/');
                string strTarget = UpdateHelper.Instance().completeFolder + filepath;
                UpdateHelper.downloadIndex++;
                if (!protocol.DownloadLPLEachFile(strTarget, strLocal, true))
                {
                    return;
                }
                else
                {
                   //每次下载完对比
                    if (File.Exists(strLocal)&&UpdateHelper.Instance()._ServerUpdateStructs.ContainsKey(filepath))
                    {
                        var computeMd5Hash = FileMatchUp.ComputeMD5Hash(strLocal);
                        var serverUpdateStruct = UpdateHelper.Instance()._ServerUpdateStructs[filepath].Md5;
                        if (computeMd5Hash == serverUpdateStruct)
                        {
                            if (UpdateHelper.Instance()._ClientUpdateStructs.ContainsKey(filepath))
                            {

                                UpdateHelper.Instance()._ClientUpdateStructs[filepath].Md5= computeMd5Hash;
                            }
                            else
                            {
                                var myUpdateStruct = new MyUpdateStruct();
                                myUpdateStruct.PathKey = filepath;
                                myUpdateStruct.Md5 = computeMd5Hash;
                                UpdateHelper.Instance()._ClientUpdateStructs.Add(myUpdateStruct.PathKey, myUpdateStruct);
                            }

                            if (File.Exists(finalDirectory + "/" + filepath))
                            {
                                File.Delete(finalDirectory + "/" + filepath);
                            }

                            if (filepath.Contains('/'))
                            {
                                var strings = filepath.Split('/');
                                string curpath = "";
                                for (var i1 = 0; i1 < strings.Length; i1++)
                                {
                                    var path = strings[i1];
                                    if (i1!= strings.Length-1)
                                    {
                                        curpath= curpath+"/"+path;
                                        if (!Directory.Exists(finalDirectory + curpath))
                                        {
                                            Directory.CreateDirectory(finalDirectory + curpath);
                                        }
                                    }
                                }
                            }
                            File.Move(strLocal, finalDirectory + "/" + filepath);

                        }
                    }
                }
            }

             Step.CURRENT_STEP = state;

        }

        #endregion
    }
}
