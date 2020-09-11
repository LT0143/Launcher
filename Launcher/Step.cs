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
            _ProcessState_FormLoadEnd,

            _ProcessState_CreateMD5List,
            _ProcessState_CheckVer,

            __MergeLauncher,//合并启动器并重启启动器
            __MergingLauncher,//合并启动器并重启启动器

            //__ProcessState_DownLoadServerVer,

            _ProcessState_VerDownload,
            _ProcessState_VerDownloading,
            _ProcessState_VerDownloadFail,

            _ProcessState_LauncherVerCheck,
            _ProcessState_LauncherGetMd5,
            _ProcessState_LauncherGetMd5Finish,
            _ProcessState_LauncherDownload,
            _ProcessState_LauncherDownloading,
            _ProcessState_LauncherDownloadFail,
            _ProcessState_LuncherDownload_Ready,   //10

            _ProcessState_ResourceMd5Download,
            _ProcessState_ResourceMd5Downloading,
            _ProcessState_ResMd5DownloadFail,
            _ProcessState_ResMd5DownloadFinish,

            _ProcessState_OldResourceMd5Check,
            _ProcessState_PatchFileNum,
            _ProcessState_GetingPatchFileNum,
            _ProcessState_GetPatchFileNumFail,

            _ProcessState_PatchDownload,
            _ProcessState_PatchDownloading,      //20
            _ProcessState_PatchDownloadFail,
            _ProcessState_PatchDownloadRestart,
            _ProcessState_PatchDownloadFinish,

            _ProcessState_PatchRTPCheck,
            _ProcessState_PatchRTPCheckFail,    //补丁md5不对

            _ProcessState_PatchMerge,

            _ProcessState_MergeFile,
            _ProcessState_NewResDownload,
            _ProcessState_NewResDownloading,
            _ProcessState_NewResDownloadFail,    //30
            _ProcessState_NewResCheck,
            _ProcessState_NewResChecking,
            _ProcessState_NewResDownloadFinish,

            _ProcessState_DeleteRTPFail,
            _ProcessState_DeleteRTP,

            _ProcessState_Ready,
            _ProcessState_NoNetToReady,
            _ProcessState_CallClient,

            _ProcessState_Wait,
            _ProcessState_WaitMerge,
            _ProcessState_Exit,         //40
            _max,


            //下载启动器配置文件
            //__DownloadConfig,
            //__DownloadingConfig,
            //__DownloadConfigFail,

            //启动器自我更新
            __GetLauncherMd5, //得到启动器MD5 
            __GetingLauncherMd5, //得到启动器MD5 
            __GetLauncherMd5Fail, //下载启动器MD5失败 
            __CheckLauncherMD5, //检查当前启动器是否是最新
            __DownLoadLauncher,//下载最新启动器
            __DownLoadLauncherFail,//下载最新启动器失败

            //下载大包客户端
            ___CheckClientComplete, //检查客户端是否存在
            ___DownLoadClientCompleteMD5,//下载完成客户端压缩包MD5
            ___DownLoadClientCompleteMD5Fail,//下载完成客户端压缩包MD5失败
            ___DownLoadClientCompleteZip, //下载客户端压缩包         50
            ___DownLoadClientCompleteZipFail, //下载客户端压缩包
            __UnZipClientComplete,//解压客户端
            __UnZipClientCompleting,//解压客户端中               
            __DownLoadZipReConnecting,//发生错误重新连接

            //检查完整性
            __DownLoadServerMD5List, //服务器完整客户端列表
            __DownLoadServerMD5ListFail, //服务器完整客户端列表失败
            __ClientCreateMD5List, //客户端生成MD5list
            __CheckNeedDownLoadMD5List, //检查需要下载的MD5列表
            __StartNeedDownLoadFile, //开始下载文件
            __StartNeedDownLoadMD5Listing, //下载中
            __StartNeedDownLoadMD5LisEndAndCheckMD5, //下载中

            __LPLDownLoading, //下载中
            __LPLZipDownLoading, //下载中
            __LPLZipDownLoadPause, //下载暂停               //60

            __initOldUpdate,//todo LT更新入口 1 
            __initOldUpdating,//下载ServerVer.ini
            __initOldUpdateFial,
            __CheckClientVersion,//todo LT更新入口2 ver.ini版本号检查
            __CheckClientVersionFail,//todo LT更新入口2
            ___waittt,
        }

        public static State CURRENT_STEP = State.___waittt;
//        public static State CURRENT_STEP = State._ProcessState_FormLoading;

    }
}
