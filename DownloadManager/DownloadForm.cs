using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace DownloadManager
{
    public partial class DownloadForm : Form
    {
        WebClient client;
        public string Url { get; set; }
        public string FileName { get; set; }
        public string FileFormat { get; set; }
        public double FileSize { get; set; }
        public string Path { get; set; }
        public double Percentage { get; set; }
        private string[] inputFilePaths;
        private MainForm mainForm;

        public DownloadForm(MainForm form)
        {
            InitializeComponent();
            mainForm = form;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtAddress.Text) && !string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                try
                {
                    Url = txtAddress.Text;
                    //Uri uri = new Uri(this.Url);
                    FileName = txtFileName.Text;//System.IO.Path.GetFileName(uri.AbsolutePath);
                    FileFormat = txtFileFormat.Text;
                    int count = int.Parse(txtThreadCount.Text);
                    inputFilePaths = new string[count];
                    //client.DownloadFileAsync(uri, txtPath.Text + "/" + FileName);

                    var webRequest = HttpWebRequest.Create(Url);
                    webRequest.Method = "HEAD";
                    int fileSizeInByte;

                    using (var webResponse = webRequest.GetResponse())
                    {
                        var fileSize = webResponse.Headers.Get("Content-Length");
                        fileSizeInByte = Convert.ToInt32(fileSize);
                    }
                    
                    int packetSize = fileSizeInByte / (count - 1);
                    for (int i = 0; count > 0; i += packetSize)
                    {
                        Abc abc = new Abc(i, i + packetSize, 
                            (inputFilePaths.Length - count).ToString(), FileName, FileFormat);
                        if (count == 1)
                        {
                            abc.To = fileSizeInByte;
                        }

                        WaitCallback workItem = new WaitCallback(GetFileFromServer);
                        ThreadPool.QueueUserWorkItem(workItem, abc);
                        count--;
                    }
                }
                catch (ArgumentNullException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
                MessageBox.Show("Укажите Url файла", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void GetFileFromServer(object state)
        {
            Abc abc = state as Abc;
            string path = abc.FileName + abc.Part + "." + abc.FileFormat;
            var request = (HttpWebRequest)WebRequest.Create(Url);
            request.AddRange(abc.From, abc.To);
            using (var response = request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var output = File.Create(path))
            {
                stream.CopyTo(output);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            client.CancelAsync();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog() { Description = "Выберите путь." })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = dialog.SelectedPath;
                }
            }
        }
        
        private void DownloadForm_Load(object sender, EventArgs e)
        {
            client = new WebClient();
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            txtPath.Text = Environment.SpecialFolder.Desktop.ToString();
        }

        private void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Database.FilesRow row = App.DB.Files.NewFilesRow();
            row.Url = Url;
            row.FileName = FileName;
            row.FileSize = (string.Format("{0:0.##}", FileSize / 1024));
            row.DateTime = DateTime.Now;
            App.DB.Files.AddFilesRow(row);
            App.DB.AcceptChanges();
            App.DB.WriteXml(string.Format("{0}/data.dat", Application.StartupPath));
            ListViewItem item = new ListViewItem(row.Id.ToString());
            item.SubItems.Add(row.Url);
            item.SubItems.Add(row.FileName);
            item.SubItems.Add(row.FileSize);
            item.SubItems.Add(row.DateTime.ToLongDateString());
            mainForm.listView1.Items.Add(item);
            this.Close();
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Minimum = 0;
            double receive = double.Parse(e.BytesReceived.ToString());
            FileSize = double.Parse(e.TotalBytesToReceive.ToString());
            Percentage = receive / FileSize * 100;
            lblStatus.Text = $"Downloaded {string.Format("{0:0.##}", Percentage)}";
            progressBar.Value = int.Parse(Math.Truncate(Percentage).ToString());
            progressBar.Update();
        }

        private static void CombineMultipleFilesIntoSingleFile()
        {
            using (var outputStream = File.Create(outputFilePath))
            {
                foreach (var inputFilePath in inputFilePaths)
                {
                    using (var inputStream = File.OpenRead(inputFilePath))
                    {
                        // Buffer size can be passed as the second argument.
                        inputStream.CopyTo(outputStream);
                    }
                }
            }
        }
    }
}
