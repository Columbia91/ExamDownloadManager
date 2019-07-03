namespace DownloadManager
{
    public class Abc
    {
        public int From { get; set; }
        public int To { get; set; }
        public string Part { get; set; }
        public string FileName { get; set; }
        public string FileFormat { get; set; }

        public Abc(int from, int to, string part, string fileName, string fileFormat)
        {
            From = from;
            To = to;
            Part = part;
            FileName = fileName;
            FileFormat = fileFormat;
        }
    }
}