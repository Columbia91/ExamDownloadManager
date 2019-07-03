namespace DownloadManager
{
    public class Abc
    {
        public int From { get; set; }
        public int To { get; set; }
        public string Path { get; set; }

        public Abc(int from, int to, string path)
        {
            From = from;
            To = to;
            Path = path;
        }
    }
}