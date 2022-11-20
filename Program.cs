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
        public static HttpClient HttpClient = new HttpClient();

        [STAThread]
        private static void Main() 
        {
            ServicePointManager.DefaultConnectionLimit = 2 * Environment.ProcessorCount;

            const string Github = "https://github.com/DioExtreme/Tunetoon";
            HttpClient.DefaultRequestHeaders.Add("User-Agent", $"Tunetoon - A multi-toon launcher. ({Github})");

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
            using (StreamWriter sw = File.AppendText(strPath))
            {
                sw.WriteLine("Crash occurred at: " + DateTime.Now);
                sw.WriteLine();
                sw.WriteLine(ex.Message);
                sw.WriteLine(ex.StackTrace);
            }
            Application.Exit();
        }
    }
}
