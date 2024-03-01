using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace dff2dsf {
    public partial class FrmMain : Form {
        private readonly BindingList<FileToProcess> _bindingList = new BindingList<FileToProcess>();

        public FrmMain() {
            InitializeComponent();

            var source = new BindingSource(_bindingList, null);
            dataGridView.DataSource = source;
        }

        private void BtnFolder_Click(object sender, EventArgs e) {
            using (var fbd = new FolderBrowserDialog()) {
                DialogResult result = fbd.ShowDialog();

                if (result != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
                    return;
                }

                var path = fbd.SelectedPath;

                var files = Directory.GetFiles(path);

                _bindingList.Clear();

                foreach (var file in files) {
                    if (Path.GetExtension(file).EndsWith(".dff", StringComparison.CurrentCultureIgnoreCase)) {
                        _bindingList.Add(new FileToProcess {
                            SrcFile = file,
                            DestFile = Path.ChangeExtension(file, ".dsf")
                        });
                    }
                }

                if (!(_bindingList.Count > 0)) {
                    MessageBox.Show("No .dff file found!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                btnFolder.Enabled = false;
                btnConvert.Enabled = true;
                btnClear.Enabled = true;
            }
        }

        private void BtnConvert_Click(object sender, EventArgs e) {
            btnConvert.Enabled = false;

            var converter = "assets/dff2dsf_win32.exe";
            if (!File.Exists(converter)) {
                MessageBox.Show("No converter found!");
            }

            var fileIndex = 0;
            foreach (FileToProcess file in _bindingList) {
                try {
                    var srcFileName = Path.GetDirectoryName(file.SrcFile);
                    if (srcFileName == null) {
                        continue;
                    }
                    var tempSrcFile = Path.Combine(srcFileName, $"{++fileIndex}.dff");
                    var tempTargetFile = Path.Combine(srcFileName, $"{fileIndex}.dsf");

                    if (File.Exists(file.SrcFile))
                        File.Copy(file.SrcFile, tempSrcFile);

                    using (var process = new Process()) {
                        var startInfo = new ProcessStartInfo {
                            FileName = Path.GetFullPath(converter),
                            UseShellExecute = false,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            Arguments = $"\"{tempSrcFile}\" \"{tempTargetFile}\""
                        };

                        process.StartInfo = startInfo;
                        process.Start();

                        file.Status = process.StandardError.ReadToEnd();
                        if (string.IsNullOrWhiteSpace(file.Status))
                            file.Status = process.StandardOutput.ReadToEnd();

                        process.WaitForExit();

                        if (File.Exists(tempTargetFile)) {
                            File.Move(tempTargetFile, file.DestFile);
                        }
                        if (chkDelete.Checked && File.Exists(tempSrcFile)) {
                            File.Delete(file.SrcFile);
                        }
                        File.Delete(tempSrcFile);
                    }
                } catch (Exception ex) {
                    file.Status = ex.Message;
                }
            }
        }

        private void ToolStripStatusLabel1_Click(object sender, EventArgs e) {
            Process.Start("http://www.microsoft.com/en-us/download/details.aspx?id=26999");
        }

        private void BtnClear_Click(object sender, EventArgs e) {
            _bindingList.Clear();
            btnFolder.Enabled = true;
            btnConvert.Enabled = false;
            btnClear.Enabled = false;
        }
    }
}
