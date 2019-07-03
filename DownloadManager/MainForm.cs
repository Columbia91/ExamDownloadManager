using System;
using System.IO;
using System.Windows.Forms;

namespace DownloadManager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void tsAddUrl_Click(object sender, EventArgs e)
        {
            DownloadForm downloadForm = new DownloadForm(this);
            downloadForm.Show();
        }

        private void tsRemove_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Вы уверены что хотите удалить запись?", "Предупреждение", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                for (int i = listView1.SelectedItems.Count; i > 0; i--)
                {
                    ListViewItem item = listView1.SelectedItems[i - 1];
                    App.DB.Files.Rows[item.Index].Delete();
                    listView1.Items[item.Index].Remove();
                }

                App.DB.AcceptChanges();
                App.DB.WriteXml(string.Format("{0}/data.dat", Application.StartupPath));
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string fileName = string.Format("{0}/data.dat", Application.StartupPath);
            if (File.Exists(fileName))
                App.DB.ReadXml(fileName);

            foreach (Database.FilesRow row in App.DB.Files)
            {
                ListViewItem item = new ListViewItem(row.Id.ToString());
                item.SubItems.Add(row.Url);
                item.SubItems.Add(row.FileName);
                item.SubItems.Add(row.FileSize);
                item.SubItems.Add(row.DateTime.ToLongDateString());
                listView1.Items.Add(item);
            }
        }
    }
}
