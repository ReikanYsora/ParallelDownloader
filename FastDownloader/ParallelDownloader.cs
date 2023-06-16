using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

public class ParallelDownloader
{
    public event Action DownloadStarted;
    public event Action<string> DownloadCompleted;
    public event Action<string> DownloadFailed;
    public event Action<int> DownloadProgressChanged;

    public async Task DownloadAsync(string url, string destinationPath, int maxParallelDownloads)
    {
        try
        {
            DownloadStarted?.Invoke();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";

            long totalBytes = 0;

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                totalBytes = response.ContentLength;
            }

            if (totalBytes <= 0)
            {
                throw new Exception("Impossible de récupérer la taille du fichier.");
            }

            DownloadProgressChanged?.Invoke(0);

            int chunkSize = (int)Math.Ceiling((double)totalBytes / maxParallelDownloads);

            string tempDirectory = Path.GetDirectoryName(destinationPath);

            long totalDownloadedBytes = 0;

            object lockObject = new object();

            List<Task> downloadTasks = new List<Task>();

            for (int chunkIndex = 0; chunkIndex < maxParallelDownloads; chunkIndex++)
            {
                int startRange = chunkIndex * chunkSize;
                int endRange = startRange + chunkSize - 1;

                string tempFilePath = Path.Combine(tempDirectory, $"{Path.GetRandomFileName()}.part");

                downloadTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        long chunkDownloadedBytes = 0;

                        HttpWebRequest rangeRequest = (HttpWebRequest)WebRequest.Create(url);
                        rangeRequest.Method = "GET";
                        rangeRequest.AddRange(startRange, endRange);

                        using (HttpWebResponse rangeResponse = (HttpWebResponse)await rangeRequest.GetResponseAsync())
                        using (Stream responseStream = rangeResponse.GetResponseStream())
                        using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead;

                            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, bytesRead);
                                chunkDownloadedBytes += bytesRead;

                                lock (lockObject)
                                {
                                    totalDownloadedBytes += bytesRead;
                                    int progress = (int)((totalDownloadedBytes * 100) / totalBytes);
                                    OnDownloadProgressChanged(progress);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DownloadFailed?.Invoke(ex.Message);
                    }
                }));
            }

            await Task.WhenAll(downloadTasks);

            MergeTempFiles(tempDirectory, destinationPath);

            DownloadCompleted?.Invoke(destinationPath);
        }
        catch (Exception ex)
        {
            DownloadFailed?.Invoke(ex.Message);
        }
    }

    private void OnDownloadProgressChanged(int progress)
    {
        DownloadProgressChanged?.Invoke(progress);
    }

    private void MergeTempFiles(string tempDirectory, string destinationPath)
    {
        using (FileStream destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
        {
            string[] tempFiles = Directory.GetFiles(tempDirectory, "*.part", SearchOption.TopDirectoryOnly);
            Array.Sort(tempFiles);

            foreach (string tempFilePath in tempFiles)
            {
                using (FileStream tempFileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                {
                    tempFileStream.CopyTo(destinationStream);
                }
            }
        }

        // Supprimer les fichiers temporaires
        foreach (string tempFilePath in Directory.GetFiles(tempDirectory, "*.part", SearchOption.TopDirectoryOnly))
        {
            File.Delete(tempFilePath);
        }
    }
}
