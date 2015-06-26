namespace MRobot.Windows.Utilities
{
    using System;
    using System.IO;
    using System.Threading;

    public static class FileOpenUtil
    {
        private static int numberOfTries = 1200;
        private static int timeIntervalBetweenTries = 500;

        public static FileStream TryGetStream(string filePath, FileAccess fileAccess, FileMode mode = FileMode.Open)
        {
            for (int tries = 0; tries < numberOfTries; tries++)
            {
                try
                {
                    return File.Open(filePath, mode, fileAccess, System.IO.FileShare.None);
                }
                catch (IOException exc)
                {
                    if (exc is PathTooLongException || exc is FileNotFoundException)
                    {
                        throw;
                    }

                    Thread.Sleep(timeIntervalBetweenTries);
                }

            }

            throw new Exception("The file is locked too long");
        }
    }
}
