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
        #region Download

        private delegate void invoke_Download(object _value);

        public Thread m_thread_Download = null;

        public void StartSingleDownload(object[] _parameter)
        {
            m_thread_Download = new Thread(new ParameterizedThreadStart(ThreadSingleDownload));
            m_thread_Download.SetApartmentState(ApartmentState.STA);
            m_thread_Download.Priority = ThreadPriority.Highest;
            m_thread_Download.Start(_parameter);
        }

        private void ThreadSingleDownload(object _value)
        {
           
                object[] _object = _value as object[];

                String strLocal = _object[0] as String;
                String strTarget = _object[1] as String;
                Step.State state = (Step.State)_object[2];

                FtpProtocol protocol = _object[3] as FtpProtocol;
                Step.State failState = (Step.State)_object[4];

                if (!protocol.Download(strTarget, strLocal, true))
                {
                    Step.CURRENT_STEP = failState;
                    return;
                }
                Step.CURRENT_STEP = state;
        }

        #endregion

        #region 新下载


   
        /// <summary>
        /// 下载客户端文件
        /// </summary>
        /// <param name="_parameter"></param>
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
            Step.State nextState = (Step.State)_object[1];
            FtpProtocol protocol = _object[2] as FtpProtocol;
            string downloadLocalDirectory = _object[3] as String;   //本地的保存地址
            string sourceDirectory = _object[4] as String;          //服务端的下载地址

            List<string> failFile = new List<string>();         //更新过程中出现更新失败的文件

            FTPUpdateValue.CurrentFileDownloadBytes = 0;
            FTPUpdateValue.CurrentFileByte = 0;
            //FTPUpdateValue.maxByte = FTPUpdateValue.maxByte - FTPUpdateValue.CurrentFileByte;
            //FTPUpdateValue.CurrentAccumlatereadByte =Math.Max(0, FTPUpdateValue.CurrentAccumlatereadByte- FTPUpdateValue.CurrentFileDownloadBytes);
            FTPUpdateValue.maxfileCount += pathArray.Length;

            //TODO 此处开始获取新资源大小
            for (int i = 0; i < pathArray.Length; i++)
            {
                string strTarget = protocol.mainform.m_clientServerDirectory + pathArray[i];
                string text = string.Format("当前更新{0}/{1}个文件", i, FTPUpdateValue.maxfileCount);
                
                long sizeI = protocol.GetFileSize(strTarget);
                if (sizeI >= 0)
                {
                    FTPUpdateValue.maxByte += sizeI;
                }
                else
                {
                    //todo 获取文件大小的文件暂时不处理
                    continue;
                }
            }

            Step.CURRENT_STEP = Step.State.__StartNeedDownLoadListing;

            //todo 此处开始下载资源。更新下载进度
            for (int i = 0; i < pathArray.Length; i++)
            {
                var filepath = pathArray[i];
                string strLocal = downloadLocalDirectory + filepath;
                strLocal = strLocal.Replace('\\', '/');
                string strSource = sourceDirectory + filepath;
                FTPUpdateValue.downloadIndex++;
                if (!protocol.Download(strSource, strLocal, true))
                {
                    //todo下载失败
                    failFile.Add(filepath);
                    continue;
                }
                else
                {
                    //每次下载完校验md5后移动到本地的最终位置
                    bool isLocalEx = File.Exists(strLocal);
                    bool isServer = UpdateHelper.Instance()._ServerUpdateStructs.ContainsKey(filepath);
                    if (isLocalEx && isServer)
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
                            string tarPath = protocol.mainform.m_curDirectory + filepath;
                           
                            if (File.Exists(tarPath  ))
                            {
                                File.Delete(tarPath  );
                            }

                            if (filepath.Contains('/'))
                            {
                                var strings = filepath.Split('/');
                                string curpath = "";

                                for (var i1 = 0; i1 < strings.Length; i1++)
                                {
                                    if (i1!= strings.Length-1)
                                    {
                                        curpath = curpath + "/" + strings[i1];

                                        if (!Directory.Exists(protocol.mainform.m_curDirectory + curpath))
                                        {
                                            Directory.CreateDirectory(protocol.mainform.m_curDirectory + curpath);
                                        }
                                    }
                                }
                            }
                            // 将下载好的文件挪出去
                            File.Move(strLocal, tarPath  );
                        }
                    }
                }
            }
            if(failFile.Count>0)
            {
                string text = "更新失败的文件有：";
                foreach(string var  in  failFile)
                {
                    text = text+ var+" , ";
                }

            }
             Step.CURRENT_STEP = nextState;
        }

        #endregion
    }
}
