using System;
using System.IO;


namespace CreateTestFile
{
    internal class Program
    {
        static void Main()
        {
            string filePath = @"D:\";
            string fileName= "test1G.bin";
            long fileSizeInBytes = 1024L * 1024L * 1024L; // 1 Go
            //long fileSizeInBytes = 10L * 1024L * 1024L * 1024L; // 10 Go

            try
            {
                using (FileStream fs = new FileStream(Path.Combine(filePath, fileName), FileMode.Create, FileAccess.Write))
                {
                    long blockSize = 1024L * 1024L;
                    long numBlocks = fileSizeInBytes / blockSize;

                    byte[] block = new byte[blockSize];

                    for (long i = 0; i < numBlocks; i++)
                    {
                        fs.Write(block, 0, block.Length);
                        Console.WriteLine($"Écriture du bloc {i + 1} sur {numBlocks}");
                    }

                    long remainingBytes = fileSizeInBytes % blockSize;

                    if (remainingBytes > 0)
                    {
                        byte[] remainingBlock = new byte[remainingBytes];
                        fs.Write(remainingBlock, 0, remainingBlock.Length);
                        Console.WriteLine("Écriture du bloc partiel");
                    }
                }

                Console.WriteLine($"Le fichier binaire de test de {fileSizeInBytes} octets a été créé avec succès.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Une erreur s'est produite lors de la création du fichier : {ex.Message}");
            }
        }
    }
}
