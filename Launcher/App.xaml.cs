using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Launcher
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static string m_lancherName = "Launcher";
        //TODO 定应用程序的名字
        public static string m_clientName = "LHBLZX_VR";

        public App()
        {
            bool bExit = false;

            Process[] processesLancher = Process.GetProcessesByName(m_lancherName);
            Process[] processesClient = Process.GetProcessesByName(m_clientName);

            if (processesClient.Length > 0)
            {
                string text = "已经启动了客户端：" + m_clientName + ".exe，请先关闭客户端后再启动！";
                System.Windows.Forms.MessageBox.Show(text, "MRError", MessageBoxButtons.OK);
                bExit = true;
            }

            else if (processesLancher.Length > 1)
            {
                string text = "已经启动了游戏下载程序：" + m_lancherName + ".exe！";
                System.Windows.Forms.MessageBox.Show(text, "MRError", MessageBoxButtons.OK);
                bExit = true;
            }
            if (bExit == true)
            {
                //Step.CURRENT_STEP = Step.State._ProcessState_Exit;
                Current.Shutdown();
                //Environment.Exit(0);
                return;
            }
        }
    }
}
