using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Windows.Forms;

namespace DownloadManager
{
    public partial class DownloadForm : Form
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        public double FileSize { get; set; }
        public string Path { get; set; }
        public double Percentage { get; set; }
        private string[] inputFilePaths;
        private MainForm mainForm;
        int location = 0;
        static int s = 0;
        public DownloadForm(MainForm form)
        {
            InitializeComponent();
            mainForm = form;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtAddress.Text) && !string.IsNullOrWhiteSpace(txtAddress.Text)
                && !string.IsNullOrEmpty(txtThreadCount.Text) && !string.IsNullOrWhiteSpace(txtThreadCount.Text)
                && !string.IsNullOrEmpty(txtFileName.Text) && !string.IsNullOrWhiteSpace(txtFileName.Text)
                && !string.IsNullOrEmpty(txtFileFormat.Text) && !string.IsNullOrWhiteSpace(txtFileFormat.Text))
            {
                try
                {
                    Url = txtAddress.Text;
                    FileName = txtFileName.Text;
                    Path = txtPath.Text + '/' + FileName + '.' + txtFileFormat.Text;
                    int count = int.Parse(txtThreadCount.Text);
                    inputFilePaths = new string[count];

                    var webRequest = HttpWebRequest.Create(Url);
                    webRequest.Method = "HEAD";
                    int fileSizeInByte;

                    using (var webResponse = webRequest.GetResponse())
                    {
                        var fileSize = webResponse.Headers.Get("Content-Length");
                        FileSize = fileSizeInByte = Convert.ToInt32(fileSize);
                    }
                    
                    int packetSize = fileSizeInByte / (count - 1);
                    for (int i = 0; count > 0; i += packetSize)
                    {
                        inputFilePaths[inputFilePaths.Length - count] = FileName + (inputFilePaths.Length - count);
                        Abc abc = new Abc(i, i + packetSize - 1, FileName + (inputFilePaths.Length - count));
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
                MessageBox.Show("Укажите необходимые данные", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void GetFileFromServer(object state)
        {
            try
            {
                Abc abc = state as Abc;

                var request = (HttpWebRequest)WebRequest.Create(Url);
                request.AddRange(abc.From, abc.To);
                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var output = File.Create(abc.Path))
                {
                    stream.CopyTo(output);
                    progressBar.Minimum = 0;
                    double receive = (double)output.Length;
                    Percentage = receive / FileSize * 100;
                    Interlocked.Add(ref location, (int)Percentage);
                    Invoke(new Action(() =>
                    {
                        lblStatus.Text = $"Downloaded {string.Format("{0}%", location)}";
                        progressBar.Value = int.Parse(Math.Truncate((double)location).ToString());
                        progressBar.Update();
                        s++;
                    }));
                }
                if (s == inputFilePaths.Length)
                {
                    CombineMultipleFilesIntoSingleFile();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } 
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
            txtPath.Text = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private void CombineMultipleFilesIntoSingleFile()
        {
            try
            {
                using (var outputStream = File.Create(Path))
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
            catch {}
            
            Database.FilesRow row = App.DB.Files.NewFilesRow();
            row.Url = Url;
            row.FileName = FileName;
            row.FileSize = (string.Format("{0:0.##} MB", (FileSize / 1024 / 1024)));
            row.DateTime = DateTime.Now;
            row.Path = Path;
            row.Format = txtFileFormat.Text;
            App.DB.Files.AddFilesRow(row);
            App.DB.AcceptChanges();
            App.DB.WriteXml(string.Format("{0}/data.dat", Application.StartupPath));
            ListViewItem item = new ListViewItem(row.Id.ToString());
            item.SubItems.Add(row.Url);
            item.SubItems.Add(row.FileName);
            item.SubItems.Add(row.FileSize);
            item.SubItems.Add(row.DateTime.ToLongDateString());
            item.SubItems.Add(row.Format);
            item.SubItems.Add(row.Path);
            
            Invoke(new Action(() =>
            {
                lblStatus.Text = $"Downloaded {string.Format("{0}%", 100)}";
                progressBar.Value = 100;
                progressBar.Update();
                mainForm.listView1.Items.Add(item);
            }));
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
