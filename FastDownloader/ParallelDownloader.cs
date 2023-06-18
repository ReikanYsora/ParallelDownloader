using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

public class ParallelDownloader
{
    #region EVENTS
    public event Action DownloadStarted;
    public event Action<string> DownloadCompleted;
    public event Action<string> DownloadFailed;
    public event Action<float> DownloadProgressChanged;
    #endregion

    #region ATTRIBUTES
    private int _defaultChunkCount = 1;
    #endregion

    #region PROPERTIES
    public WebProxy WebProxy { get; set; }

    public int Chunks
    {
        set
        {
            if (value != _defaultChunkCount)
            {
                _defaultChunkCount = value;
            }
        }
        get
        {
            return _defaultChunkCount;
        }
    }
    #endregion

    #region METHODS
    public void Download(string url, string destinationPath)
    {
        try
        {
            long totalBytes = 0;
            int fixChunksCount = Chunks;

            DownloadStarted?.Invoke();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            if (WebProxy != null)
            {
                request.Proxy = WebProxy;
            }

            request.Method = "HEAD";

            using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
            {
                totalBytes = response.ContentLength;
            }

            if (totalBytes <= 0)
            {
                throw new Exception("Unable to retrieve file size");
            }

            DownloadProgressChanged?.Invoke(0);

            int chunkSize = (int)Math.Ceiling((double)totalBytes / fixChunksCount);
            string tempDirectory = Path.GetDirectoryName(destinationPath);
            long totalDownloadedBytes = 0;
            bool downloadFailed = false;
            ConcurrentBag<string> tempFiles = new ConcurrentBag<string>();

            Parallel.For(0, fixChunksCount, chunkIndex =>
            {
                int startRange = chunkIndex * chunkSize;
                int endRange = startRange + chunkSize - 1;
                string tempFilePath = Path.Combine(tempDirectory, $"{Path.GetRandomFileName()}.part");

                try
                {
                    HttpWebRequest partRequest = (HttpWebRequest)WebRequest.Create(url);
                    partRequest.Method = "GET";

                    if (WebProxy != null)
                    {
                        partRequest.Proxy = WebProxy;
                    }

                    partRequest.AddRange(startRange, endRange);

                    using (HttpWebResponse rangeResponse = (HttpWebResponse)partRequest.GetResponse())
                    using (Stream responseStream = rangeResponse.GetResponseStream())
                    using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[10240];
                        int bytesRead;

                        while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            totalDownloadedBytes += bytesRead;
                            float progress = totalDownloadedBytes / (float)totalBytes;
                            OnDownloadProgressChanged(progress);
                            fileStream.Write(buffer, 0, bytesRead);
                        }
                    }
                }
                catch (Exception ex)
                {
                    downloadFailed = true;
                    DownloadFailed?.Invoke(ex.Message);
                }
                finally
                {
                    if (!downloadFailed)
                    {
                        tempFiles.Add(tempFilePath);
                    }
                }
            });

            if (!downloadFailed)
            {
                MergeTempFiles(tempFiles.ToList(), destinationPath);
                DownloadCompleted?.Invoke(destinationPath);
            }
        }
        catch (Exception ex)
        {
            DownloadFailed?.Invoke(ex.GetBaseException().Message);
        }
    }

    private void MergeTempFiles(List<string> tempFiles, string destinationPath)
    {
        using (FileStream destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
        {
            foreach (string tempFilePath in tempFiles)
            {
                using (FileStream tempFileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                {
                    tempFileStream.CopyTo(destinationStream);
                }
            }
        }

        // Supprimer les fichiers temporaires
        foreach (string tempFilePath in tempFiles)
        {
            File.Delete(tempFilePath);
        }
    }
    #endregion

    #region CALLBACKS
    private void OnDownloadProgressChanged(float progress)
    {
        DownloadProgressChanged?.Invoke(progress);
    }
    #endregion
}
