using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Launcher
{
    public static class Step
    {
        public enum State
        {
            _ProcessState_FormLoading = 0,
            //_ProcessState_FormLoadEnd,

            _ProcessState_CheckVer,     //检查版本号

            _ProcessState_VerDownload,
            _ProcessState_VerDownloading,
            _ProcessState_VerDownloadFail,

            __CheckLauncherMD5, //检查当前启动器是否是最新
            __DownLoadLauncher,//下载最新启动器
            _ProcessState_LauncherDownloading,

            __MergeLauncher,        //合并启动器并重启启动器
            __MergeLauncherFail,    
            __MergingLauncher,      //合并启动器并重启启动器中

            _ProcessState_CreateMD5List,
            __StartNeedDownLoadFile, //开始下载文件
            __StartCheckDownLoadList, //下载中
            __StartNeedDownLoadListing, //下载中

            __DownLoadServerMD5List, //服务器完整客户端列表
            __DownLoadServerMD5ListFail, //服务器完整客户端列表失败
            __ClientCreateMD5List, //客户端生成MD5list
            _DownLoadingServerMD5List, //下载中

            _ProcessState_CallClient,
            _ProcessState_Ready,

            _ProcessState_Wait,
            _ProcessState_Exit,         //40

            ___waittt,
        }

        public static State CURRENT_STEP = State.___waittt;
    }
}
