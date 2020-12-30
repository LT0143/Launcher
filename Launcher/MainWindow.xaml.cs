using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.Forms.MessageBox;
using Microsoft.Win32;
using System.Threading;
using System.Diagnostics;

namespace Launcher
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //文件名
        public string m_currentPath = string.Empty;
        public string m_curDirectory = string.Empty;

        public string m_clientServerDirectory = "Client/";
        public string m_clientServerName = "Client";

        public string m_serverVerName = "ServerVer.ini";
        public string m_clientVerName = "Ver.ini";
        public static  string m_localServerVerPath = string.Empty;
        public static  string m_verPath = string.Empty;
        public  string m_clientmd5Txt = "md5checksum.txt";
        string m_luncherOld = ".exe.old";
        public string m_ftpPath = "VRLauncher/";        //FTP上的存放目录
        public string m_downloadPath = "DownloadTemp";     //更新文件下载目录
        public string m_luancherPath = string.Empty;
        string launcherMd5 = string.Empty;
        FtpProtocol m_ftpProtocol = null;
        private FTPThread m_ftpThread = null;
        public bool isChangeOnceInfo = false;
                            
        RegistryKey reg;

#if DEBUG
        public const string FTPHost = "127.0.0.1"; //update.dindanw.com   
#else
        public const string FTPHost = "www.ainankang.com/vr";
#endif

        public const string FTPID = "clientId"; //   
        public const string FTPPassword = "clientPs"; // 


        private Thread m_thread_MakeEncodingList = null;
        private Thread m_thread_MakeEncoding = null;


        public static bool UpdateOnce = false;
        public string changeOnceInfostr = "";

        DispatcherTimer StepTimer = new DispatcherTimer();
        String strServerVer = string.Empty;
        private int newResDownloadNum = 0; //新资源下载失败后的下载次数


        public MainWindow()
        {
            UpdateHelper.Instance().m_MainForm = this;
            InitializeComponent();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //启动更新
            Console.WriteLine("=====Window_Loaded=====");
            Init();

            //启动计时器
            InitTimer();

            // 下载服务器版本文件
            //Step.CURRENT_STEP = Step.State.___waittt;
            Step.CURRENT_STEP = Step.State._ProcessState_VerDownload;
            //Step.CURRENT_STEP = Step.State._ProcessState_CheckVer;

        }

        /// <summary>
        /// 初始化窗口设置
        /// </summary>
        private void Init()
        {
            m_currentPath = Application.ExecutablePath.Replace('\\', '/');
            int len = m_currentPath.LastIndexOf(@"/");
            m_curDirectory = m_currentPath.Substring(0, len + 1);

            m_luncherOld = m_curDirectory + App.m_lancherName + m_luncherOld;
            m_ftpThread = new FTPThread();
            m_ftpProtocol = new FtpProtocol(FTPHost, FTPID, FTPPassword, this);
            
            m_downloadPath = m_curDirectory  + m_downloadPath;
            m_localServerVerPath = m_curDirectory + m_serverVerName;
            m_verPath = m_curDirectory + m_clientVerName;

            m_luancherPath = m_curDirectory + App.m_lancherName + ".exe";
            string localMd5 = m_curDirectory+ m_clientmd5Txt; 
            m_clientServerDirectory = m_ftpPath + m_clientServerDirectory;
            //服务端文件、md5文件、旧启动器都要删除
            string[] deleteFiles = { m_localServerVerPath , m_luncherOld, localMd5 };
            foreach(string Path in deleteFiles)
            {
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }
            }
            if(Directory.Exists(m_downloadPath))
            {
                Directory.Delete(m_downloadPath,true);
            }
            CheckFIPSRegistValue();

            Label_Ver.Content = IniManager.GetINIValue(m_verPath, "Client", "ver");

        }

        /// <summary>
        ///Todo 检测更新状态。
        /// </summary>
        public void UpdateLoadingForm()
        {
            switch (Step.CURRENT_STEP)
            {
                case Step.State._ProcessState_VerDownload:
                    {
                        //下载ServerVer.ini
                        Step.CURRENT_STEP = Step.State._ProcessState_VerDownloading;
                        string FTPPath = m_ftpPath+ m_serverVerName ;
                        m_ftpThread.StartSingleDownload(new object[]
                        {
                             m_localServerVerPath, FTPPath,Step.State.__CheckLauncherMD5,
                             m_ftpProtocol,Step.State._ProcessState_VerDownloadFail
                         });
                    }
                    break;
                case Step.State._ProcessState_CheckVer:
                    //todo验证客户端的版本号
                    CheckClientVer(); 
                    break;
                case Step.State.__CheckLauncherMD5:
                        CheckMD5();
                        break;
                case Step.State.__DownLoadLauncher:
                    DownLoadLauncher();
                    break;
                case Step.State.__MergeLauncher:
                    MergeLauncher();
                    break;
                //从服务器下载客户端完整MD5
                case Step.State.__DownLoadServerMD5List:
                    DownLoadServerMD5FromLPL();
                    break;
                case Step.State.__ClientCreateMD5List:
                    label_File_Info_String.Content = "本地资源校验中...";
                    UpdateHelper.Instance().GetClientMd5List(null);
                    break;
                case Step.State.__StartNeedDownLoadFile:
                    DownLoadClientFile();
                    break;
                case Step.State._ProcessState_Ready:
                    //todo 版本号更新为serverini的版本号
                    Ready();
                    break;
                case Step.State._ProcessState_VerDownloadFail:
                case Step.State.__DownLoadServerMD5ListFail:
                case Step.State.__MergeLauncherFail:
                    DownloadFaild();
                    break;
                case Step.State._ProcessState_Exit:
                    AppEnd();
                    break;
            }
        }

     
        /// <summary>
        /// 再次检查补丁文件的版本号
        /// </summary>
        private void CheckClientVer()
        {
            strServerVer = IniManager.GetINIValue(m_localServerVerPath, "Client", "ver");
            string CurrentVer = Label_Ver.Content.ToString();

           //版本号不同则更新版本
            if (!strServerVer.Equals(CurrentVer))
            {
                Step.CURRENT_STEP = Step.State.__DownLoadServerMD5List;
            }
            else
            {
                Step.CURRENT_STEP = Step.State._ProcessState_Ready;
            }
        }

        //初始化计时器
        private void InitTimer()
        {
            StepTimer.IsEnabled = true;
            StepTimer.Interval = new TimeSpan(0, 0, 1);
            StepTimer.Tick += new EventHandler(timer_UpdateDownloadSpeed);
            StepTimer.Start();
        }

        //每秒更新一次进度条和更新一次界面
        private void timer_UpdateDownloadSpeed(object sender, EventArgs e)
        {
            UpdateProgressBar();
            UpdateLoadingForm();
        }

        public void DownLoadLauncher()
        {
            label_File_Info_String.Content = "下载最新启动器版本中...";
            Step.CURRENT_STEP = Step.State._ProcessState_LauncherDownloading;
            string ftpPath = m_ftpPath + App.m_lancherName + ".exe";
            string downloadPath = m_downloadPath +"/"+ App.m_lancherName+".exe";
            m_ftpThread.StartSingleDownload(new object[]
            {
                downloadPath,ftpPath,Step.State.__MergeLauncher, m_ftpProtocol,Step.State.__MergeLauncherFail
            });
        }

        public void MergeLauncher()
        {
            label_File_Info_String.Content = "合并启动器中...";
            Step.CURRENT_STEP = Step.State.___waittt;

#if DEBUG
            Step.CURRENT_STEP = Step.State._ProcessState_CheckVer;
#else
 try
            {
                string tempLauncherPath = m_downloadPath + "/" + App.m_lancherName + ".exe";
                //var launcherMd5name = UpdateHelper.Instance().m_launcherMD5;

                if (File.Exists(m_luancherPath) && File.Exists(tempLauncherPath))
                {
                    var downloadlaunchermd5 = FileMatchUp.ComputeMD5Hash(tempLauncherPath);
                    if (downloadlaunchermd5 == launcherMd5)
                    {
                        FileInfo fi = new FileInfo(m_luancherPath);
                        //将当前启动器改为旧的
                        fi.MoveTo(m_luancherPath + ".old");
                        //temp文件夹里的移动到当前文件夹下
                        File.Move(tempLauncherPath, m_luancherPath);
                        //Process.Start(destFileName, "new");

                        ProcessStartInfo psi = new ProcessStartInfo("cmd.exe",
                            "/C ping 127.0.0.1 -n " + 3 + " -w 1000 > Nul & " + m_luancherPath);
                        psi.WindowStyle = ProcessWindowStyle.Hidden;
                        psi.CreateNoWindow = true;
                        Process.Start(psi);
           
                        AppEnd();
                    }
                    else
                    {
                        MessageBox.Show("下载的启动器与最新版本的启动器不匹配，请待服务器资源同步完成后再启动！", "LMD5Error", MessageBoxButtons.OK);
                    }
                }
                else
                {
                    MessageBox.Show("启动器替换失败");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("启动自我升级异常原因如下： "+ex.Message );
                AppEnd();
            }
#endif


        }

        /// <summary>
        ///下载需要的游戏文件
        /// </summary>
        public void DownLoadClientFile()
        {
            label_File_Info_String.Content = "下载最新文件中...";
            Step.CURRENT_STEP = Step.State.__StartCheckDownLoadList;

            if (UpdateHelper.Instance()._DownLoadList.Count > 0)
            {
                newResDownloadNum++;
                if (newResDownloadNum > 3)
                {
                    string text = "资源多次下载出现错误(错误码：" + Convert.ToInt32(Step.CURRENT_STEP) + ")。";
                    MessageBox.Show(text, "SoulOfWarLauncher", MessageBoxButtons.OK);
                    Step.CURRENT_STEP = Step.State._ProcessState_Exit;
                    return;
                }
                //ResetDownLoadShow();
                //todo 下载新资源，然后进行版本校验，多个资源的情况下
                m_ftpThread.StartDownloadGameFile(new object[] { UpdateHelper.Instance()._DownLoadList.Keys.ToArray()
                    , Step.State._ProcessState_Ready, m_ftpProtocol, m_downloadPath+"/", m_clientServerDirectory }); ;
            }
            else
            {
                label_File_Info_String.Content = "资源已经是最新的了";
                Step.CURRENT_STEP = Step.State._ProcessState_Ready;
            }
        }

        /// <summary>
        /// 更新失败后直接跳过更新启动客户端
        /// </summary>
        private void DownloadFaild()
        {
            MessageBox.Show("更新失败： " + Step.CURRENT_STEP.ToString());
            CallClientEXE();
        }


        private void Ready()
        {

            Step.CURRENT_STEP = Step.State._ProcessState_CallClient;

            Label_Ver.Content = strServerVer;
            IniManager.SetINIValue(m_verPath, "Client", "ver", strServerVer);
            if (progressBar_Total.Value != 100)
            {
                progressBar_Total.Value = 100;
            }

            label_File_Info_String.Content = "更新完成";

            label_Speed.Content = "";
            //MessageBox.Show("更新成功！");

            try
            {
#if DEBUG
                MessageBox.Show("成功启动应用 ");

#else
                CallClientEXE();
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show("应用启动失败： " + ex.Message);
            }
        }


        private void CallClientEXE()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = m_curDirectory + App.m_clientName;
            Process.Start(psi);
            AppEnd();
        }

        public void CheckMD5()
        {
            //label_Total_Info_string.Content = "检查当前启动器版本中";
            Step.CURRENT_STEP = Step.State.___waittt;
            //读取serverVer.ini的启动器版本号
            launcherMd5 = IniManager.GetINIValue(m_localServerVerPath, "LauncherMd5", "md5");
            
            if (File.Exists(m_luancherPath))   //存在启动器则检查
            {
                string computeMd5Hash = FileMatchUp.ComputeMD5Hash(m_luancherPath);
                //Todo 启动器md5相同的话就启动版本号检测是否要更新客户端
                //不相同的话就更新启动器
                if (computeMd5Hash == launcherMd5)
                {
                    Step.CURRENT_STEP = Step.State._ProcessState_CheckVer; 
                    return;
                }
            }
            //  启动器下载
            Step.CURRENT_STEP = Step.State.__DownLoadLauncher;
        }

        long m_lastLoadByte = 0;
        int time = 0;

        /// <summary>
        /// 更新进度条
        /// </summary>
        public void UpdateProgressBar()
        {
            if ( FTPUpdateValue.maxByte <= 0 || FTPUpdateValue.downloadIndex > FTPUpdateValue.maxfileCount)
            {
                return;
            }

            if (Step.CURRENT_STEP == Step.State.__StartNeedDownLoadListing)
            {
                if(!label_Speed.IsVisible)
                {
                    label_Speed.Visibility = Visibility.Visible;
                }

                float valueTotal = FTPUpdateValue.CurrentAccumlatereadByte * 100f / FTPUpdateValue.maxByte;
                valueTotal = valueTotal < 0 ? 0 : valueTotal;
                valueTotal = valueTotal > 100 ? 100 : valueTotal;
                //string textFile = FTPUpdateValue.FormatTime(100);

                //string textTotal = FTPUpdateValue.CurretnAccumulateReadByte_String + " / " + FTPUpdateValue.MaxBtye_string;
                long remenber = FTPUpdateValue.maxByte - FTPUpdateValue.CurrentAccumlatereadByte;

                //label_File_Info_String.Content = textTotal;
                if (valueTotal == 100)
                    label_File_Info_String.Content = "";
                progressBar_Total.Value = (int)valueTotal; 


                //todo 显示下载速度，第几/几个文件
                long speed = FTPUpdateValue.CurrentAccumlatereadByte - m_lastLoadByte;
                speed = Math.Max(0, speed);
                if(speed>0)
                {
                    time =  (int)(remenber / speed);
                }

                m_lastLoadByte = FTPUpdateValue.CurrentAccumlatereadByte;
                string textFile = FTPUpdateValue.FormatBytes(speed) + "/s";
                label_Speed.Content = FTPUpdateValue.FormatTime(time) + "("+textFile+")";

            }
        }

 

        /// <summary>
        /// 下载服务器资源列表md5
        /// </summary>
        public void DownLoadServerMD5FromLPL()
        {
            label_File_Info_String.Content = "下载最新文件列表中...";
            Step.CURRENT_STEP = Step.State._DownLoadingServerMD5List;
            var clientPathFile = m_downloadPath + "/" + m_clientmd5Txt;
            var filepathinserver =  m_clientServerDirectory + m_clientmd5Txt;

            m_ftpThread.StartSingleDownload(new object[] { clientPathFile, filepathinserver, Step.State.__ClientCreateMD5List, m_ftpProtocol,Step.State.__DownLoadServerMD5ListFail });
        }

#region 显示消息窗在主线程


        public DialogResult ShowErrorMessage(string text, bool isRepair = false, string caption = "", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            var dr = new DialogResult();
            try
            {
                var tmp = this.Dispatcher.Invoke(new MessageBoxShow_Error(ErrorMessageBoxShow), new object[] { text, isRepair, caption, buttons, icon });
                if (tmp != null)
                {
                    dr = (DialogResult)tmp;
                }
            }
            catch
            {

            }
            return dr;
        }

        delegate DialogResult MessageBoxShow_Error(string text, bool isRepair, string caption, MessageBoxButtons buttons, MessageBoxIcon icon);
        DialogResult ErrorMessageBoxShow(string text, bool isRepair, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            DialogResult dr = MessageBox.Show(text, caption, buttons, icon);
            //if (isRepair)
            //{
            //    //CheckCompleteButton.IsEnabled = true;
            //    m_bCanComplete = true;
            //}
           
            return dr;
        }
#endregion

        public void AppEnd()
        {
            //m_bExit = true;
            Step.CURRENT_STEP = Step.State._ProcessState_Wait;

            if (m_thread_MakeEncoding != null && m_thread_MakeEncoding.IsAlive)
            {
                m_thread_MakeEncoding.Abort();
            }

            if (m_thread_MakeEncodingList != null && m_thread_MakeEncodingList.IsAlive)
            {
                m_thread_MakeEncodingList.Abort();
            }

            if (m_ftpThread != null && m_ftpThread.m_thread_Download != null && m_ftpThread.m_thread_Download.IsAlive)
            {
                m_ftpThread.m_thread_Download.Abort();
            }


            if (StepTimer.IsEnabled)
                StepTimer.Stop();

            this.Close();
            System.Windows.Application.Current.Shutdown();

            Environment.Exit(0);
        }
#region 工具类

        /// <summary>
        /// 检查MD5算法有没有开启支持
        /// </summary>
        public void CheckFIPSRegistValue()
        {
            try
            {
                reg = Registry.ClassesRoot;
                string value = GetFIPSRegistData("Enabled");
                if (value != "0")
                {
                    //MessageBox.Show("Enabled" + value);
                    SetRegistData("Enabled", "0");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("CheckRegistValue error:" + e.Message);
            }
        }


        private string GetFIPSRegistData(string name)
        {
            string registData;
            RegistryKey hklm = Registry.LocalMachine;
            RegistryKey software = hklm.OpenSubKey("SYSTEM", true);
            RegistryKey aimdir = software.OpenSubKey("CurrentControlSet", true).OpenSubKey("Control", true)
                .OpenSubKey("Lsa", true);
            RegistryKey fips = aimdir.OpenSubKey("FipsAlgorithmPolicy", true);

            registData = fips.GetValue(name).ToString();
            return registData;
        }

        private void SetRegistData(string name, string value)
        {
            RegistryKey hkml = Registry.LocalMachine;
            RegistryKey software = hkml.OpenSubKey("SYSTEM", true);
            RegistryKey aimdir = software.OpenSubKey("CurrentControlSet", true).OpenSubKey("Control", true)
                .OpenSubKey("Lsa", true);
            RegistryKey fips = aimdir.OpenSubKey("FipsAlgorithmPolicy", true);

            fips.SetValue(name, value);
        }
 

#endregion

        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            if ( Step.CURRENT_STEP == Step.State.__MergingLauncher)
            {
                System.Windows.Forms.MessageBox.Show("当前正在更新启动器，请解更新完毕后再关闭！", "CError", MessageBoxButtons.OK);
            }
            else
            {
                if (System.Windows.Forms.MessageBox.Show(("是否要退出？"), "更新器", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    AppEnd();
                }
            }
        }
    }

 

}

