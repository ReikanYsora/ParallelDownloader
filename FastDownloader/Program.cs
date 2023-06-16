using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FastDownloader
{
    internal class Program
    {
        #region ATTRIBUTES
        private static string _filePath = @"D:\";
        private static string _fileName1GB = "debian-12.0.0-amd64-netinst.iso";
        private static string _urlTest1GB = "https://cdimage.debian.org/debian-cd/current/amd64/iso-cd/debian-12.0.0-amd64-netinst.iso";
        #endregion

        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();

            string testDownloadFile = Path.Combine(_filePath, _fileName1GB);

            ParallelDownloader downloader = new ParallelDownloader();
            downloader.DownloadStarted += Handle_DownloadStarted;
            downloader.DownloadFailed += Handle_DownloadFailed;
            downloader.DownloadCompleted += Handle_DownloadCompleted;
            downloader.DownloadProgressChanged += Handle_DownloadProgressChanged;

            if (File.Exists(testDownloadFile))
            {
                File.Delete(testDownloadFile);  
            }

            Task.Run(async () =>
            {
                sw.Start();
                await downloader.DownloadAsync(_urlTest1GB, testDownloadFile, 8);
                sw.Stop();

                Console.WriteLine(sw.Elapsed.ToString());
            });

            Console.ReadLine();

        }

        #region CALLBACKS
        private static void Handle_DownloadProgressChanged(int progress)
        {
            Console.WriteLine(string.Format($"{progress} %"));
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
