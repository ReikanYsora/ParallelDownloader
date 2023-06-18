using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace FastDownloader
{
    internal class Program
    {
        #region ATTRIBUTES
        private static string _filePath = @"D:\";
        private static string _fileName = "Test.iso";
        private static string _urlTest10GB = "http://192.168.1.4/Downloads/test10G.bin";
        #endregion

        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();

            string testDownloadFile = Path.Combine(_filePath, _fileName);


            using (WebClient client = new WebClient())
            {
                try
                {
                    Console.WriteLine("WebClient test");
                    if (File.Exists(testDownloadFile))
                    {
                        File.Delete(testDownloadFile);
                    }
                    sw.Start();
                    client.DownloadFile(_urlTest10GB, testDownloadFile);
                    sw.Stop();
                    Console.WriteLine(sw.Elapsed.ToString());
                    Console.WriteLine("-----------------");
                    sw.Reset();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Une erreur s'est produite lors du téléchargement : {ex.Message}");
                }
            }


            Console.WriteLine("ParallelDownloader test");
            ParallelDownloader downloader = new ParallelDownloader();
            downloader.DownloadStarted += Handle_DownloadStarted;
            downloader.DownloadFailed += Handle_DownloadFailed;
            downloader.DownloadCompleted += Handle_DownloadCompleted;
            //downloader.DownloadProgressChanged += Handle_DownloadProgressChanged;

            int testsCount = 8;

            for (int i = 1; i < testsCount; i++)
            {
                downloader.Chunks = i;

                if (File.Exists(testDownloadFile))
                {
                    File.Delete(testDownloadFile);
                }

                sw.Reset();
                Console.WriteLine($"Test {i} : {downloader.Chunks} Chunks");
                sw.Start();
                downloader.Download(_urlTest10GB, testDownloadFile);
                sw.Stop();
                Console.WriteLine(sw.Elapsed.ToString());
                Console.WriteLine("-----------------");
            }

            Console.ReadLine();

        }

        #region CALLBACKS
        private static void Handle_DownloadProgressChanged(float progress)
        {
            Console.WriteLine(string.Format($"{progress * 100} %"));
        }

        private static void Handle_DownloadCompleted(string obj)
        {
            Console.WriteLine(string.Format($"Download completed {obj}"));
        }

        private static void Handle_DownloadFailed(string obj)
        {
            Console.WriteLine(string.Format($"Download failed {obj}"));
        }

        private static void Handle_DownloadStarted()
        {
            Console.WriteLine(string.Format($"Download started..."));
        }
        #endregion
    }
}
