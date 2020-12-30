using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Launcher;
using File = System.IO.File;

public class MyUpdateStruct
{
    public MyUpdateStruct() { }
    public string PathKey;
    public string Md5;
}


public class UpdateHelper
{
    public MainWindow m_MainForm;

    public Dictionary<string, MyUpdateStruct> _ClientUpdateStructs = new Dictionary<string, MyUpdateStruct>();
    public Dictionary<string, MyUpdateStruct> _ServerUpdateStructs = new Dictionary<string, MyUpdateStruct>();

    public Dictionary<string, MyUpdateStruct> _DownLoadList = new Dictionary<string, MyUpdateStruct>();

    public int failCount = 0;

    private static UpdateHelper m_instance = null;

    public static UpdateHelper Instance()
    {
        if (m_instance == null)
        {
            m_instance = new UpdateHelper();
        }
        return m_instance;
    }



    private void ThreadGetClientMD5(object _value)
    {
        //先从服务器拉下整个客户端的md5list 
        //临时文件路径,获取md5列表
        try
        {
            var tempPath = m_MainForm.m_downloadPath + "/" + m_MainForm.m_clientmd5Txt;
            _ServerUpdateStructs = new Dictionary<string, MyUpdateStruct>();
            StreamReader sR = File.OpenText(tempPath);

            string nextLine;
            while ((nextLine = sR.ReadLine()) != null)
            {
                var strings = nextLine.Split('=');
                var key = strings[0];
                var value = strings[1];
                var myUpdateStruct = new MyUpdateStruct();
                myUpdateStruct.PathKey = key;
                myUpdateStruct.Md5 = value;
                //启动器之前校验过，此处不作校验。
                _ServerUpdateStructs.Add(key, myUpdateStruct);
            }
            sR.Close();

            //本地MD5
            if (FileMatchUp.MakeEncodingList(_ServerUpdateStructs, m_MainForm.m_curDirectory, m_MainForm.m_clientmd5Txt) == true)
            {
                _ClientUpdateStructs.Clear();
                var cilentFloder = m_MainForm.m_curDirectory + "/" + m_MainForm.m_clientmd5Txt;
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

                //md5列表创建完成 比对需要下载的MD文件
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

                //下载列表中不一致的文件
                Step.CURRENT_STEP = Step.State.__StartNeedDownLoadFile;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("文件对比失败：" + ex.Message);
        }

    }

    private Thread m_thread_GetClientMD5List = null;
    // 客户端生成MD5list
    public void GetClientMd5List(object[] _parameter)
    {
        Step.CURRENT_STEP = Step.State.___waittt;
        m_thread_GetClientMD5List = new Thread(new ParameterizedThreadStart(ThreadGetClientMD5));
        m_thread_GetClientMD5List.SetApartmentState(ApartmentState.STA);
        m_thread_GetClientMD5List.Priority = ThreadPriority.Normal;
        m_thread_GetClientMD5List.Start(_parameter);
    }

}