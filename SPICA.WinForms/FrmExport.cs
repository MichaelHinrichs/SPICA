using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Formats.Generic.COLLADA;
using SPICA.Formats.Generic.StudioMdl;
using SPICA.Formats.Generic.MaterialScript;
using SPICA.WinForms.Formats;
using SPICA.WinForms.Properties;


using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

//TODO: save export settings between {runs}

namespace SPICA.WinForms
{
    public partial class FrmExport : Form
    {
        public FrmExport()
        {
            InitializeComponent();
        }

        //load export setings on frame load
        private void FrmExport_Load(object sender, EventArgs e)
        {
            TxtInputFolder.Text = Settings.Default.BatchInputFolder;
            TxtOutFolder.Text = Settings.Default.BatchOutputFolder;
            TxtIgnoredExt.Text = Settings.Default.BatchIgnoredExts;

            ChkExportModels.Checked = Settings.Default.BatchExportModels;
            ChkExportAnimations.Checked = Settings.Default.BatchExportAnims;
            ChkExportTextures.Checked = Settings.Default.BatchExportTexs;
            ChkPrefixNames.Checked = Settings.Default.BatchPrefixNames;
            ChkExportMaterials.Checked = Settings.Default.BatchExportMats;
            ChkRecurse.Checked = Settings.Default.BatchRecurse;

            CmbFormat.SelectedIndex = Settings.Default.BatchFormat;
            CmbMatFormat.SelectedIndex = Settings.Default.BatchMatFormat;
        }

        //save export setings on frame close
        private void FrmExport_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.BatchInputFolder = TxtInputFolder.Text;
            Settings.Default.BatchOutputFolder = TxtOutFolder.Text;
            Settings.Default.BatchIgnoredExts = TxtIgnoredExt.Text;

            Settings.Default.BatchExportModels = ChkExportModels.Checked;
            Settings.Default.BatchExportAnims = ChkExportAnimations.Checked;
            Settings.Default.BatchExportTexs = ChkExportTextures.Checked;
            Settings.Default.BatchPrefixNames = ChkPrefixNames.Checked;
            Settings.Default.BatchExportMats = ChkExportMaterials.Checked;
            Settings.Default.BatchRecurse = ChkRecurse.Checked;

            Settings.Default.BatchFormat = CmbFormat.SelectedIndex;
            Settings.Default.BatchMatFormat = CmbMatFormat.SelectedIndex;
        }

        //
        private void BtnBrowseIn_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog Browser = new FolderBrowserDialog())
            {
                if (Browser.ShowDialog() == DialogResult.OK) TxtInputFolder.Text = Browser.SelectedPath;
            }
        }

        //
        private void BtnBrowseOut_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog Browser = new FolderBrowserDialog())
            {
                if (Browser.ShowDialog() == DialogResult.OK) TxtOutFolder.Text = Browser.SelectedPath;
            }
        }


        private void BtnConvert_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(TxtInputFolder.Text))
            {
                MessageBox.Show(
                    "Input folder not found!",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!Directory.Exists(TxtOutFolder.Text))
            {
                //TODO: offer to create output dir "Output folder not found!  Should it be created?"
                MessageBox.Show(
                    "Output folder not found!",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }


            bool ExportModels = ChkExportModels.Checked;
            bool ExportAnims = ChkExportAnimations.Checked;
            bool ExportTexs = ChkExportTextures.Checked;
            bool PrefixNames = ChkPrefixNames.Checked;
            bool ExportMats = ChkExportMaterials.Checked;
            bool Recurse = ChkRecurse.Checked;

            int Format = CmbFormat.SelectedIndex;
            int MatFormat = CmbMatFormat.SelectedIndex;

            BtnConvert.Enabled = false;

            //get all files (optionally recursive) in input folder
            DirectoryInfo Folder = new DirectoryInfo(TxtInputFolder.Text);
            FileInfo[] Files = Folder.GetFiles("*.*", Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            //TODO: check and warn on empty input folder

            string subPath;
            string outPath = TxtOutFolder.Text;
            if (!outPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                outPath += Path.DirectorySeparatorChar;
            }

            int FileIndex = 0;

            string[] ignoredExts = TxtIgnoredExt.Text.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

            //for each one      //TODO: Use Parallel loop for more speed and keep UI responsive
            foreach (FileInfo File in Files)
            {
                //TODO: add ext blacklist
                if (Array.IndexOf(ignoredExts, File.Extension.ToLower().Remove(0, 1)) > -1)
                {
                    FileIndex++;
                    continue;
                }

                subPath = GetRelativePath(File.DirectoryName, TxtInputFolder.Text);

                if (subPath.Length > 0 && !Directory.Exists(outPath + subPath))
                {
                    Directory.CreateDirectory(outPath + subPath);
                }

                //open input file, read and convert to H3D data
                H3D Data = FormatIdentifier.IdentifyAndOpen(File.FullName);

                //if there is data (input file was valid)
                if (Data != null)
                {
                    string BaseName = PrefixNames ? Path.GetFileNameWithoutExtension(File.FullName) + "_" : string.Empty;

                    BaseName = Path.Combine(outPath + subPath, BaseName);

                    if (!PrefixNames) BaseName += Path.DirectorySeparatorChar;

                    if (ExportModels)
                    {
                        for (int Index = 0; Index < Data.Models.Count; Index++)
                        {
                            string FileName = BaseName + Data.Models[Index].Name;

                            switch (Format)
                            {
                                case 0: new DAE(Data, Index, -1, Settings.Default.DebugCopyVtxAlpha).Save(FileName + ".dae"); break;
                                case 1: new SMD(Data, Index).Save(FileName + ".smd"); break;
                            }
                        }
                    }

                    if (ExportMats)
                    {
                        for (int Index = 0; Index < Data.Models.Count; Index++)
                        {
                            string FileName = BaseName + Data.Models[Index].Name;

                            switch (MatFormat)
                            {
                                case 0: new MaterialScript(Data, Index).Save(FileName + ".ms"); break;
                                case 1: new MaterialDump(Data, Index).Save(FileName + ".txt"); break;
                            }
                        }
                    }

                    if (ExportAnims && Data.Models.Count > 0)
                    {
                        for (int Index = 0; Index < Data.SkeletalAnimations.Count; Index++)
                        {
                            string FileName = BaseName + Data.Models[0].Name + "_" + Data.SkeletalAnimations[Index].Name;

                            switch (Format)
                            {
                                case 0: new DAE(Data, 0, Index).Save(FileName + ".dae"); break;
                                case 1: new SMD(Data, 0, Index).Save(FileName + ".smd"); break;
                            }
                        }
                    }

                    if (ExportTexs)
                    {
                        foreach (H3DTexture Tex in Data.Textures)
                        {
                            Tex.ToBitmap().Save(Path.Combine(outPath + subPath, Tex.Name + ".png"));
                        }
                    }
                }

                //update progress bar
                float Progress = ++FileIndex;
                Progress = (Progress / Files.Length) * 100;
                ProgressConv.Value = (int)Progress;

                Application.DoEvents();
            }

            ProgressConv.Value = 100;
            BtnConvert.Enabled = true;
        }


        private string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            if (filespec + Path.DirectorySeparatorChar == folder) return "";
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
