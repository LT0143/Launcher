using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using SoulOfWarLauncher;
using ZTO.PicTest.Utilities;
using File = System.IO.File;

public class MyUpdateStruct
{
    public MyUpdateStruct() { }
    public string PathKey;
    public string Md5;
}

public enum ButtonStateLPL
{
    _WaitCheckClient,   //等待检查客户端状态
    _CheckClient,   //检查客户端状态
    _WaitClickDownLoadZip,   //等待点击下载完整客户端
    _WaitClickUpdate,   //等待更新状态
    _Updateing,   //更新状态
    _WaitStartGame,   //等待开始游戏装填
    _WaitUnZipGame,   //等待解压游戏

    _StartDownLoad,
    _Wait,
    _WaitToClickStartDownLoad,
    _DownLoadEachFileing,//检查完整性的下载中
    _RetryDownload,   //下载重试
    _DownLoading,  //文件下载中

    _DownLoadZiping,   //下载完整客户端状态
    _PauseDownLoadZip,   //暂停下载完整客户端中

}
/*public class UpdateHelpe*/r
{
    public WindowHttps _m_MainForm;

    public Dictionary<string, MyUpdateStruct> _ClientUpdateStructs = new Dictionary<string, MyUpdateStruct>();
    public Dictionary<string, MyUpdateStruct> _ServerUpdateStructs = new Dictionary<string, MyUpdateStruct>();

    public Dictionary<string, MyUpdateStruct> _DownLoadList = new Dictionary<string, MyUpdateStruct>();

    //启动器所在文件夹
    public string m_curPath = "";
    //todo 客户端所在路径
    private string m_clientPath = "";
    private string m_tempPath = "/TempFolder/";

    //启动器下载临时路径
    public string m_TempDownLoadPath
    {
        get { return m_curPath + m_tempPath; }
    }

    //客户端下载临时路径
    public string m_TempDownClientPath
    {
        get { return GetClientPathFile() + m_tempPath; }
    }

    public string serverVerName = "ServerVer.ini";   //服务器上的serverVer.ini
    public string m_ServerVerIniPath
    {
        get { return VersionFolder + serverVerName; }
    }

    public string m_TempVerIniPath
    {
        get { return m_TempDownClientPath + serverVerName; }
    }

    //todo 本地md5列表
    private string m_clientmd5Txt = "md5checksum.txt";
    public string m_launcherMD5 = "LauncherMD5.txt";
    private string m_saveClientfile = "/clientsavefile.sowt";

    //todo 服务器md5列表 
    public string m_serverclientMD5file = "/md5checksum.txt";       //完整客户端内资源md5

    //todo LT更新流程改变目录结构依赖的目录
#if DEBUG
    public string ServerFolder = "SoulOfWar/";
#else
    public string ServerFolder = "";
#endif
    public string UpdateFolder = "MyUpdate/";
    public string serverlauncherFolder = "Launcher/";  //todo 启动器的服务器下载路径 包括MD5和EXE
    public string serverZipFolder = "ClientZip/";//todo 完整客户端zip的路径 包括MD5 和zip
    //public string localZipmd5Path = "";// 本地压缩包的md5的
    //todo 检查完整性的下载地址
    public string completeFolder = "Client/";//todo 检查完整性需要下载文件的地址 这个全是零散文件
    public string VersionFolder = "Version/";//todo 检查完整性需要下载文件的地址 这个全是零散文件
    public string resMd5ini = "ResMd5.ini";
    public string clienExtName = ".sow";
    public string clientName = "SoulOfWar";
    public string clientNameCN = "灵魂战纪";
    public string configIniName = "config.ini";

    //todo 完整客户端 zip 和md5 txt
    public string m_completeZip = "";   //要下载的压缩包的名字
    //public string m_completeZipMD5 = "ClientZipMD5.txt";        //客户端压缩包的md5
    public string m_unzipPassWord = "12345";
    public const int m_ZipDownTimes = 3;
    public string m_LauncherName = "SoulOfWarLauncher.exe";
    public int failCount = 0;
    public string m_useAgree = "Agree.txt";

#if DEBUG
    //    public string m_serverIp = "192.168.4.141";  // LPL
    public string m_serverIp = "192.168.4.244";  // LT
    //public string m_serverIp = "192.168.3.185";  // LYL
#else
    public string m_serverIp = "sowdown.playgate8.com:30008";  //todo 自己ID IP sw2.dindanw.com:8314  192.168.4.244
#endif
    //    public string m_serverIp = "192.168.4.141";  //todo 自己FTP        FTP !!!!!!!!!!!! IP
    //public string m_serverIni = "/ServerVer.ini";

    public string m_upzipflag = "/swft.flag";
    public string m_clientini = "/Ver.ini";
    public string m_generalini = "/General.ini";
    public string m_clientexe = "/SoulOfWar.exe";
    public string m_loadingexe = "/SWLoading.exe";
    public string m_dumpexe = "/DumpReport.exe";

    private string m_datasFolder = "/datas";
    private string m_gameetcFolder = "/GameEtc";
#region 下载显示

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

    public static string FormatBytesSpeed(long bytes)
    {
        string[] Suffix = { "Byte", "KB", "MB", "GB", "TB" };
        int i = 0;
        double dblSByte = bytes;
        if (bytes > 1024)
            for (i = 0; (bytes / 1024) > 0; i++, bytes /= 1024)
                dblSByte = bytes / 1024.0;
        return String.Format("{0:0.##}{1}", dblSByte, Suffix[i]);
    }
#endregion


    public static ButtonStateLPL CurButtonState = ButtonStateLPL._WaitToClickStartDownLoad;
    UpdateHelper()
    {
        m_curPath = Application.ExecutablePath;

        int len = m_curPath.LastIndexOf(@"\");
        m_curPath = m_curPath.Substring(0, len + 1);

        UpdateFolder = ServerFolder + UpdateFolder;
        completeFolder = ServerFolder + completeFolder;
        serverZipFolder = ServerFolder + serverZipFolder;
        serverlauncherFolder = ServerFolder + serverlauncherFolder;
        VersionFolder = ServerFolder + VersionFolder;
        //serverVerIniPath = VersionFolder + serverVerIniPath;
        failCount = m_ZipDownTimes;

    }

    private static UpdateHelper m_instance = null;
    public static UpdateHelper Instance()
    {
        if (m_instance == null)
        {
            m_instance = new UpdateHelper();
        }
        return m_instance;
    }

    public string GetClientPathFile()
    {
        if (File.Exists(m_curPath + m_saveClientfile))
        {
            m_clientPath = File.ReadAllText(m_curPath + m_saveClientfile);
            string lastStr = m_clientPath.Substring(m_clientPath.Length - 1);
            if (lastStr != "//" && lastStr != "\\")
            {
                m_clientPath += "\\";
            }
            return m_clientPath;
        }
        return null;
    }


    /// <summary>
    /// Todo 判断客户端是否存在
    /// todo 通过判断客户端位置配置文件以及重要的几个客户端文件
    /// </summary>
    /// <returns></returns>
    public void CopyWritePathSaveFile()
    {
        File.WriteAllText(GetClientPathFile() + m_saveClientfile, GetClientPathFile());
    }
    public void CopyLauncherInClientHaveSaveFile()
    {

        bool _isExist = File.Exists(m_curPath + m_saveClientfile);
        if (_isExist)
        {
            var patha = GetClientPathFile() + m_saveClientfile;
            var exists = File.Exists(patha);
            if (!exists)
            {
                File.Copy(m_curPath + m_saveClientfile, patha);
            }
            else
            {
                UpdateHelper.Instance().ShowBox("当前更新器路径在客户端内");
            }
        }
        else
        {
            UpdateHelper.Instance().ShowBox("客户端路径遭到破坏");
        }
    }
    public bool CheckClientExist()
    {
        bool _isExist = File.Exists(m_curPath+m_saveClientfile);
        if (_isExist)
        {
//            m_clientPath = GetClientPathFile();
//            if (File.Exists(m_clientPath + m_upzipflag))
//            {
//                File.Delete(m_clientPath + m_upzipflag);
//                UpdateHelper.Instance().ShowBox("更新流程没有走完");
//                
//                return false;
//            }

            //判断6个重要文件 来判断是否有客户端        这里还判断了是否解压完成
            if (CheckClientIsEx())
            {
                return true;
            }
            else
            {
                UpdateHelper.Instance().ShowBox("当前选择目录里么有客户端~~~");
            }
        }
        else
        {
            UpdateHelper.Instance().ShowBox("请指定客户端目录");
        }
        return false;
    }

    public bool CheckClientIsEx()
    {
        if (m_clientPath != null &&
            File.Exists(m_clientPath + m_clientexe)
            && File.Exists(m_clientPath + m_clientini)
            && File.Exists(m_clientPath + m_generalini)
            && File.Exists(m_clientPath + m_loadingexe)
            && File.Exists(m_clientPath + m_dumpexe)
            && Directory.Exists(m_clientPath + m_datasFolder)
            && Directory.Exists(m_clientPath + m_gameetcFolder))
        {
            return true;
        }

        return false;
    }

    public void SaveClientPathFile(string path)
    {
        File.WriteAllText(m_curPath + m_saveClientfile, path, Encoding.UTF8);
    }
    public void ClearClientPathFile()
    {
        File.Delete(m_curPath + m_saveClientfile);
    }
   

    /// <summary>
    /// 初始化客户端路径，先检查当前文件夹下有没有客户端，有的话检查下当前目录是不是m_clientPath
    /// </summary>
    public void InitClientPath()
    {
        if (!CheckClientIsEx())
        {
            string curPath = Application.StartupPath;
            //string curPath = programPath.Substring(0, programPath.LastIndexOf('\\'));

            if (curPath != null && File.Exists(curPath + m_clientexe)
    && File.Exists(curPath + m_clientini) && File.Exists(curPath + m_generalini)
    && File.Exists(curPath + m_loadingexe) && File.Exists(curPath + m_dumpexe)
    && Directory.Exists(curPath + m_datasFolder) && Directory.Exists(curPath + m_gameetcFolder))
            {
                //当前文件夹下存在客户端

                SaveClientPathFile(curPath);
                //return true;
            }
        }
      
    }

    private void ThreadGetClientMD5(object _value)
    {
        //先从服务器拉下整个客户端的md5list 
        //临时文件夹
        var tempFloder = m_TempDownClientPath + m_serverclientMD5file;
        _ServerUpdateStructs = new Dictionary<string, MyUpdateStruct>();
        StreamReader sR = File.OpenText(@tempFloder);
        string nextLine;
        while ((nextLine = sR.ReadLine()) != null)
        {
            var strings = nextLine.Split('=');
            var key = strings[0];
            var value = strings[1];
            var myUpdateStruct = new MyUpdateStruct();
            myUpdateStruct.PathKey = key;
            myUpdateStruct.Md5 = value;
            _ServerUpdateStructs.Add(key, myUpdateStruct);
        }
        sR.Close();
        var clientPathFile = GetClientPathFile();
        if (clientPathFile != null)
        {
            //本地MD5
            if (FileMatchUp.MakeEncodingList(_ServerUpdateStructs,clientPathFile, m_clientmd5Txt) == true)
            {
                _ClientUpdateStructs.Clear();
                var cilentFloder = m_clientPath + "/" + m_clientmd5Txt;
                StreamReader sRclient = File.OpenText(cilentFloder);
                string nextLineclient;
                while ((nextLineclient = sRclient.ReadLine()) != null)
                {
                    var strings = nextLineclient.Split('=');
                    var key = strings[0];
                    var value = strings[1];
                    var myUpdateStruct = new MyUpdateStruct();
                    myUpdateStruct.PathKey = key;
                    myUpdateStruct.Md5 = value;
                    _ClientUpdateStructs.Add(key, myUpdateStruct);
                }
                sRclient.Close();
                _m_MainForm.UpdateProgressBarLPLOnce();


                _DownLoadList.Clear();
                foreach (var serverUpdateStruct in _ServerUpdateStructs)
                {
                    var key = serverUpdateStruct.Key;
                    var serverMd5 = serverUpdateStruct.Value.Md5;
                    if (_ClientUpdateStructs.ContainsKey(key))
                    {
                        var clientmd5 = _ClientUpdateStructs[key].Md5;
                        if (serverMd5 != clientmd5)
                        {
                            var myUpdateStruct = new MyUpdateStruct();
                            myUpdateStruct.PathKey = key;
                            myUpdateStruct.Md5 = serverMd5;
                            _DownLoadList.Add(myUpdateStruct.PathKey, myUpdateStruct);
                        }
                    }
                    else
                    {
                        var myUpdateStruct = new MyUpdateStruct();
                        myUpdateStruct.PathKey = key;
                        myUpdateStruct.Md5 = serverMd5;
                        _DownLoadList.Add(myUpdateStruct.PathKey, myUpdateStruct);
                    }
                }

                //md5列表创建完成 比对需要下载的MD文件
                Step.CURRENT_STEP = Step.State.__CheckNeedDownLoadMD5List;
            }
            else
            {
                _DownLoadList.Clear();
                foreach (var serverUpdateStruct in _ServerUpdateStructs)
                {
                    var key = serverUpdateStruct.Key;
                    var serverMd5 = serverUpdateStruct.Value.Md5;
                    if (_ClientUpdateStructs.ContainsKey(key))
                    {
                        var clientmd5 = _ClientUpdateStructs[key].Md5;
                        if (serverMd5 != clientmd5)
                        {
                            var myUpdateStruct = new MyUpdateStruct();
                            myUpdateStruct.PathKey = key;
                            myUpdateStruct.Md5 = serverMd5;
                            _DownLoadList.Add(myUpdateStruct.PathKey, myUpdateStruct);
                        }
                    }
                    else
                    {
                        var myUpdateStruct = new MyUpdateStruct();
                        myUpdateStruct.PathKey = key;
                        myUpdateStruct.Md5 = serverMd5;
                        _DownLoadList.Add(myUpdateStruct.PathKey, myUpdateStruct);
                    }
                }

                _m_MainForm.UpdateProgressBarLPLOnce();
                Step.CURRENT_STEP = Step.State.__CheckNeedDownLoadMD5List;
            }
        }
        else
        {
            UpdateHelper.Instance().ShowBox("客户端路径不存在");
        }



        




    }

    private Thread m_thread_GetClientMD5List = null;
    //todo 客户端生成MD5list
    public void GetClientMd5List(object[] _parameter)
    {
        Step.CURRENT_STEP = Step.State.___waittt;
        m_thread_GetClientMD5List = new Thread(new ParameterizedThreadStart(ThreadGetClientMD5));
        m_thread_GetClientMD5List.SetApartmentState(ApartmentState.STA);
        m_thread_GetClientMD5List.Priority = ThreadPriority.Normal;
        m_thread_GetClientMD5List.Start(_parameter);
    }
    /// <summary>
    /// todo 检索出所有需要下载的文件列表
    /// </summary>
    public void CheckClientComplete()
    {
        //比对完成开始下载 游戏文件 
        Step.CURRENT_STEP = Step.State.__StartNeedDownLoadFile;
    }

    //private Thread m_thread_HistoryMergeMd5;

    //开启完整大包客户端下载  建立flag文件
    public void CreateZipFlag()
    {
//        if (!File.Exists(m_clientPath + m_upzipflag))
//        {
//            File.Create(m_clientPath + m_upzipflag);
//        }
    }
    public void DeleteZipFlag()
    {
//        if (File.Exists(m_clientPath + m_upzipflag))
//        {
//            File.Delete(m_clientPath + m_upzipflag);
//        }
    }


    //需要引入IWshRuntimeLibrary，搜索Windows Script Host Object Model

    /// <summary>
    /// 创建快捷方式
    /// </summary>
    /// <param name="directory">快捷方式所处的文件夹</param>
    /// <param name="shortcutName">快捷方式名称</param>
    /// <param name="targetPath">目标路径</param>
    /// <param name="description">描述</param>
    /// <param name="iconLocation">图标路径，格式为"可执行文件或DLL路径, 图标编号"，
    /// 例如System.Environment.SystemDirectory + "\\" + "shell32.dll, 165"</param>
    /// <remarks></remarks>
    public static void CreateShortcut(string directory, string shortcutName, string targetPath,
        string description = null, string iconLocation = null)
    {
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        string shortcutPath = Path.Combine(directory, string.Format("{0}.lnk", shortcutName));
        WshShell shell = new WshShell();
        IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);//创建快捷方式对象
        shortcut.TargetPath = targetPath;//指定目标路径
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);//设置起始位置
        shortcut.WindowStyle = 1;//设置运行方式，默认为常规窗口
        shortcut.Description = description;//设置备注
        shortcut.IconLocation = string.IsNullOrWhiteSpace(iconLocation) ? targetPath : iconLocation;//设置图标路径
        shortcut.Save();//保存快捷方式
    }

    /// <summary>
    /// 创建桌面快捷方式
    /// </summary>
    /// <param name="shortcutName">快捷方式名称</param>
    /// <param name="targetPath">目标路径</param>
    /// <param name="description">描述</param>
    /// <param name="iconLocation">图标路径，格式为"可执行文件或DLL路径, 图标编号"</param>
    /// <remarks></remarks>
    public static void CreateShortcutOnDesktop(string shortcutName, string targetPath,
        string description = null, string iconLocation = null)
    {
        try
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);//获取桌面文件夹路径
            CreateShortcut(desktop, shortcutName, targetPath, description, iconLocation);

            string commonPro= Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
            CreateShortcut(commonPro, shortcutName, targetPath, description, iconLocation);

            

        }
        catch (Exception e)
        {
            MessageBox.Show( e.Message);
        }
    }

    public void CreateShortcutPrograme(string shortcutName, string targetPath, string description = null, string iconLocation = null)
    {
        try
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms) ;//获取开始程序菜单栏文件夹路径
            folderPath = folderPath+"\\" + clientNameCN;
            if(Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }
            Directory.CreateDirectory(folderPath);
            CreateShortcut(folderPath, shortcutName, targetPath, description, iconLocation);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message);
        }
    }

  
    public void ChangeTextInfo(string info)
    {
        _m_MainForm.isChangeOnceInfo = true;
        _m_MainForm.changeOnceInfostr = info;
    }


    /// <summary>  

    /// 获取指定驱动器的剩余空间总大小(单位为GB)  

    /// </summary>  

    /// <param name=”str_HardDiskName”>只需输入代表驱动器的字母即可 </param>  

    /// <returns> </returns>  

    public static bool GetHardDiskFreeSpace()
    {
        string programPath = UpdateHelper.Instance().GetClientPathFile();
        string str_HardDiskName = programPath.Substring(0, programPath.IndexOf(':'));
        long freeSpace = new long();

        str_HardDiskName = str_HardDiskName + ":\\";

        System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();

        foreach (System.IO.DriveInfo drive in drives)
        {
            if (drive.Name == str_HardDiskName)
            {
                freeSpace = drive.TotalFreeSpace / (1024 * 1024 * 1024);
            }
        }

        if (freeSpace>20)
        {
            return true;
        }

        return false;
    }

    public static long GetHardDiskFreeSpaceSize(string str_HardDiskName)
    {
        long freeSpace = 0;
        str_HardDiskName = str_HardDiskName + ":\\";
        System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();

        foreach (System.IO.DriveInfo drive in drives)
        {
            if (drive.Name == str_HardDiskName)
            {
                if(drive.IsReady)
                    freeSpace = drive.TotalFreeSpace / (1024 * 1024 * 1024);
                //else
                //{
                //    MessageBox.Show(drive.Name + " drive.IsReady == false ");
                //}

                //MessageBox.Show(drive.Name);
            }
        }
        return freeSpace;
    }



    public static string GetMaxDisk()
    {
        String[] drives = Environment.GetLogicalDrives();
        DriveInfo[] drives2 = DriveInfo.GetDrives();
        long temp = 0;
        string tempdrives = "";
        int i = 0;

        try
        {
            for (; i < drives.Length; i++)
            {
                var disk = drives[i];
                disk = disk.Replace(":\\", "");
                var hardDiskFreeSpaceSize = GetHardDiskFreeSpaceSize(disk);
                if (hardDiskFreeSpaceSize > temp)
                {
                    temp = hardDiskFreeSpaceSize;
                    tempdrives = disk;
                }
            }
        }
        catch (Exception e)
        {
//            MessageBox.Show(drives[i] + "盘：" + e.Message);
        }

        if (string.IsNullOrEmpty(tempdrives))
        {
            //获取失败了，需要选中默认盘
            string path = Application.ExecutablePath;
            path = path.Substring(0, path.IndexOf(":"));
            tempdrives = path;
        }

        return tempdrives;
    }

    public void AbortThread()
    {
        if (m_thread_GetClientMD5List != null&&m_thread_GetClientMD5List.IsAlive )
        {
            m_thread_GetClientMD5List.Abort();
        }
        //if (m_thread_HistoryMergeMd5 != null&&m_thread_HistoryMergeMd5.IsAlive)
        //{
        //    m_thread_HistoryMergeMd5.Abort();
        //}
    }

    //public bool CheckDownLoadZipMd5()
    //{
    //    var filenamemd5 = m_completeZipMD5;
    //    var tempmd5txt = File.ReadAllText(m_TempDownClientPath + filenamemd5 + "Temp");
    //    var strings1 = tempmd5txt.Split('=');
    //    var tempname = strings1[0];
    //    var tempmd5 = strings1[1];


    //    var servermd5txt = File.ReadAllText(m_TempDownClientPath + filenamemd5);
    //    var strings2 = servermd5txt.Split('=');
    //    var sername = strings2[0];
    //    var sermd5 = strings2[1];

    //    if (tempname== sername&&tempmd5==sermd5)
    //    {
    //        return true;
    //    }

    //    return false;
    //}

    private static bool isShowBox = false;
    public  void ShowBox(string message)
    {
        if (isShowBox)
        {
           _m_MainForm.ShowErrorMessage("LPL  " + message);
            //MessageBox.Show("LPL  "+message, "", MessageBoxButtons.OK,
            //    MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
        }
    }

}