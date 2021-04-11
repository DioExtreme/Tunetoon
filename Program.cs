using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows.Forms;
using Tunetoon.Forms;

namespace Tunetoon 
{
    internal static class Program 
    {
        public static HttpClient httpClient = new HttpClient();

        [STAThread]
        private static void Main() 
        {
            ServicePointManager.DefaultConnectionLimit = 2 * Environment.ProcessorCount;
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Tunetoon - A multi-toon launcher.");

            AppDomain.CurrentDomain.UnhandledException += App_UnhandledException;
            Directory.SetCurrentDirectory(Application.StartupPath);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Launcher());
        }

        private static void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            string strPath = "Crash" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
            if (!File.Exists(strPath))
            {
                File.Create(strPath).Dispose();
            }
            using (StreamWriter sw = File.AppendText(strPath))
            {
                sw.WriteLine("Crash occured at: " + DateTime.Now);
                sw.WriteLine();
                sw.WriteLine(ex.Message);
                sw.WriteLine(ex.StackTrace);
            }
            Application.Exit();
        }
    }
}
