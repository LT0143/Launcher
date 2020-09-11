using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        string currentPath = string.Empty;
        private string m_curDirectory = string.Empty;

       public  string m_serverVerName = "ServerVer.ini";
        public static  string m_localServerVerPath = string.Empty;
        string m_clientmd5Txt = "md5checksum.txt";
        string m_luncherOld = ".exe.old";
        string m_ftpPath = "VRLauncher/";        //FTP上的存放目录
        string m_downloadPath = "DownloadTemp";     //更新文件下载目录
        public string m_luancherPath = string.Empty;
        string launcherMd5 = string.Empty;
        FtpProtocol m_ftpProtocol = null;
        private FTPThread m_ftpThread = null;
        public bool isChangeOnceInfo = false;
                            
        RegistryKey reg;

        public const string FTPHost = "127.0.0.1"; //update.dindanw.com 
        public const string FTPID = "clientId"; //   
        public const string FTPPassword = "clientPs"; // 


        private Thread m_thread_MakeEncodingList = null;
        private Thread m_thread_MakeEncoding = null;


        public static bool UpdateOnce = false;
        public string changeOnceInfostr = "";

        DispatcherTimer StepTimer = new DispatcherTimer();
        

        public MainWindow()
        {
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
            Step.CURRENT_STEP = Step.State._ProcessState_VerDownload; 
        }

        /// <summary>
        /// 初始化窗口设置
        /// </summary>
        private void Init()
        {
            currentPath = Application.ExecutablePath.Replace('\\', '/');
            int len = currentPath.LastIndexOf(@"/");
            m_curDirectory = currentPath.Substring(0, len + 1);

            m_luncherOld = m_curDirectory + App.m_lancherName + m_luncherOld;
            m_ftpThread = new FTPThread();
            m_ftpProtocol = new FtpProtocol(FTPHost, FTPID, FTPPassword, this);
            
            m_downloadPath = m_curDirectory  + m_downloadPath;
            m_localServerVerPath = m_curDirectory + m_serverVerName;
            m_luancherPath = m_curDirectory + App.m_lancherName + ".exe";
            string clientmd5File = m_curDirectory + m_clientmd5Txt;

            //服务端文件、md5文件、旧启动器都要删除
            string[] deleteFiles = { m_localServerVerPath, clientmd5File , m_luncherOld };
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
                             m_localServerVerPath, FTPPath,Step.CURRENT_STEP,
                             m_ftpProtocol
                         });
                    }
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
                case Step.State._ProcessState_CreateMD5List:
                    label_File_Info_String.Content = "本地资源校验中";
                    UpdateHelper.Instance().GetClientMd5List(null);
                    break;

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
            UpdateProgressBarOnce();
            UpdateLoadingForm();

        }

        public void DownLoadLauncher()
        {
            label_File_Info_String.Content = "下载最新启动器版本中";
            Step.CURRENT_STEP = Step.State._ProcessState_LauncherDownloading;
            string ftpPath = m_ftpPath + App.m_lancherName + ".exe";
            string downloadPath = m_downloadPath +"/"+ App.m_lancherName+".exe";
            ResetDownLoadShow();
            m_ftpThread.StartSingleDownload(new object[]
            {
                downloadPath,ftpPath,Step.CURRENT_STEP, m_ftpProtocol
            });
        }

        public void MergeLauncher()
        {
            label_File_Info_String.Content = "合并启动器中";
            Step.CURRENT_STEP = Step.State.___waittt;

            try
            {
                string tempLauncherPath = m_downloadPath + "/" + m_ftpProtocol.m_downloadTemp + App.m_lancherName + ".exe";
                //var launcherMd5name = UpdateHelper.Instance().m_launcherMD5;

                if (File.Exists(m_luancherPath) && File.Exists(tempLauncherPath))
                {
                    var downloadlaunchermd5 = FileMatchUp.ComputeMD5Hash(tempLauncherPath);
                    if (downloadlaunchermd5 != launcherMd5)
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
                      
                        //this.Close();
                        //Owner.Close();
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


        }


        public void CheckMD5()
        {
            label_File_Info_String.Content = "";
            //label_Total_Info_string.Content = "检查当前启动器版本中";
            Step.CURRENT_STEP = Step.State.___waittt;
            //读取serverVer.ini的启动器版本号
            launcherMd5 = IniManager.GetINIValue(m_localServerVerPath, "LauncherMd5", "md5");
            //本地启动器的md5
            
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
            //TODO 启动器下载
            Step.CURRENT_STEP = Step.State.__DownLoadLauncher;
        }


        //更新进度条
        public void UpdateProgressBarOnce()
        {
            if (Step.CURRENT_STEP == Step.State._ProcessState_PatchDownloading || Step.CURRENT_STEP == Step.State._ProcessState_PatchDownloadFinish)
            {

                //if (UpdateOnce)
                //{
                //    UpdateOnce = false;
                //    UpdatePgbUI();
                //}
            }
        }

   

        /// <summary>
        /// 下载服务器资源列表md5
        /// </summary>
        public void DownLoadServerMD5FromLPL()
        {
            label_File_Info_String.Content = "下载最新文件列表";
            Step.CURRENT_STEP = Step.State.__LPLDownLoading;
            var clientPathFile = UpdateHelper.Instance().m_TempDownClientPath;
        
            var filename = UpdateHelper.Instance().m_serverclientMD5file;
            var filepathinserver = UpdateHelper.Instance().completeFolder;

            //ResetDownLoadShow();
            m_ftpThread.StartSingleDownloadLPL(new object[] { filepathinserver + filename
                , clientPathFile+ filename, Step.State.__ClientCreateMD5List, m_ftpProtocol,Step.State.__DownLoadServerMD5ListFail });
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

        //TODO 后期做重试功能重置下载显示流程
        public void ResetDownLoadShow()
        {
            UpdateHelper.CurrentFileByte = 0;
            UpdateHelper.maxByte = 0;
            UpdateHelper.downloadIndex = 0;
            UpdateHelper.maxfileCount = 0;
            UpdateHelper.CurrentFileDownloadBytes = 0;
            UpdateHelper.CurrentAccumlatereadByte = 0;
        }

        #endregion

        private void button_Click(object sender, RoutedEventArgs e)
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

