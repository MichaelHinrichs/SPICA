namespace SPICA.WinForms
{
    partial class FrmExport
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.GrpInput = new System.Windows.Forms.GroupBox();
            this.BtnBrowseIn = new System.Windows.Forms.Button();
            this.TxtInputFolder = new System.Windows.Forms.TextBox();
            this.GrpOutput = new System.Windows.Forms.GroupBox();
            this.ChkRecurse = new System.Windows.Forms.CheckBox();
            this.CmbMatFormat = new System.Windows.Forms.ComboBox();
            this.ChkExportMaterials = new System.Windows.Forms.CheckBox();
            this.ChkPrefixNames = new System.Windows.Forms.CheckBox();
            this.ChkExportModels = new System.Windows.Forms.CheckBox();
            this.ChkExportAnimations = new System.Windows.Forms.CheckBox();
            this.ChkExportTextures = new System.Windows.Forms.CheckBox();
            this.CmbFormat = new System.Windows.Forms.ComboBox();
            this.BtnBrowseOut = new System.Windows.Forms.Button();
            this.TxtOutFolder = new System.Windows.Forms.TextBox();
            this.BtnConvert = new System.Windows.Forms.Button();
            this.ProgressConv = new System.Windows.Forms.ProgressBar();
            this.TxtIgnoredExt = new System.Windows.Forms.TextBox();
            this.LblIgnoredExt = new System.Windows.Forms.Label();
            this.GrpInput.SuspendLayout();
            this.GrpOutput.SuspendLayout();
            this.SuspendLayout();
            // 
            // GrpInput
            // 
            this.GrpInput.Controls.Add(this.BtnBrowseIn);
            this.GrpInput.Controls.Add(this.TxtInputFolder);
            this.GrpInput.Location = new System.Drawing.Point(12, 12);
            this.GrpInput.Name = "GrpInput";
            this.GrpInput.Size = new System.Drawing.Size(360, 51);
            this.GrpInput.TabIndex = 0;
            this.GrpInput.TabStop = false;
            this.GrpInput.Text = "Input folder";
            // 
            // BtnBrowseIn
            // 
            this.BtnBrowseIn.Location = new System.Drawing.Point(322, 21);
            this.BtnBrowseIn.Name = "BtnBrowseIn";
            this.BtnBrowseIn.Size = new System.Drawing.Size(32, 24);
            this.BtnBrowseIn.TabIndex = 1;
            this.BtnBrowseIn.Text = "...";
            this.BtnBrowseIn.UseVisualStyleBackColor = true;
            this.BtnBrowseIn.Click += new System.EventHandler(this.BtnBrowseIn_Click);
            // 
            // TxtInputFolder
            // 
            this.TxtInputFolder.Location = new System.Drawing.Point(6, 22);
            this.TxtInputFolder.Name = "TxtInputFolder";
            this.TxtInputFolder.Size = new System.Drawing.Size(310, 26);
            this.TxtInputFolder.TabIndex = 0;
            // 
            // GrpOutput
            // 
            this.GrpOutput.Controls.Add(this.LblIgnoredExt);
            this.GrpOutput.Controls.Add(this.TxtIgnoredExt);
            this.GrpOutput.Controls.Add(this.ChkRecurse);
            this.GrpOutput.Controls.Add(this.CmbMatFormat);
            this.GrpOutput.Controls.Add(this.ChkExportMaterials);
            this.GrpOutput.Controls.Add(this.ChkPrefixNames);
            this.GrpOutput.Controls.Add(this.ChkExportModels);
            this.GrpOutput.Controls.Add(this.ChkExportAnimations);
            this.GrpOutput.Controls.Add(this.ChkExportTextures);
            this.GrpOutput.Controls.Add(this.CmbFormat);
            this.GrpOutput.Controls.Add(this.BtnBrowseOut);
            this.GrpOutput.Controls.Add(this.TxtOutFolder);
            this.GrpOutput.Location = new System.Drawing.Point(12, 69);
            this.GrpOutput.Name = "GrpOutput";
            this.GrpOutput.Size = new System.Drawing.Size(360, 221);
            this.GrpOutput.TabIndex = 1;
            this.GrpOutput.TabStop = false;
            this.GrpOutput.Text = "Output folder";
            // 
            // ChkRecurse
            // 
            this.ChkRecurse.AutoSize = true;
            this.ChkRecurse.Location = new System.Drawing.Point(6, 165);
            this.ChkRecurse.Name = "ChkRecurse";
            this.ChkRecurse.Size = new System.Drawing.Size(88, 23);
            this.ChkRecurse.TabIndex = 11;
            this.ChkRecurse.Text = "Recursive";
            this.ChkRecurse.UseVisualStyleBackColor = true;
            // 
            // CmbMatFormat
            // 
            this.CmbMatFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CmbMatFormat.FormattingEnabled = true;
            this.CmbMatFormat.Items.AddRange(new object[] {
            "MaxScript (*.ms)",
            "Material Dump (*.txt)"});
            this.CmbMatFormat.Location = new System.Drawing.Point(162, 92);
            this.CmbMatFormat.Name = "CmbMatFormat";
            this.CmbMatFormat.Size = new System.Drawing.Size(192, 27);
            this.CmbMatFormat.TabIndex = 8;
            // 
            // ChkExportMaterials
            // 
            this.ChkExportMaterials.AutoSize = true;
            this.ChkExportMaterials.Location = new System.Drawing.Point(6, 94);
            this.ChkExportMaterials.Name = "ChkExportMaterials";
            this.ChkExportMaterials.Size = new System.Drawing.Size(157, 23);
            this.ChkExportMaterials.TabIndex = 7;
            this.ChkExportMaterials.Text = "Export Material Data";
            this.ChkExportMaterials.UseVisualStyleBackColor = true;
            // 
            // ChkPrefixNames
            // 
            this.ChkPrefixNames.AutoSize = true;
            this.ChkPrefixNames.Location = new System.Drawing.Point(6, 140);
            this.ChkPrefixNames.Name = "ChkPrefixNames";
            this.ChkPrefixNames.Size = new System.Drawing.Size(197, 23);
            this.ChkPrefixNames.TabIndex = 10;
            this.ChkPrefixNames.Text = "Add original name as prefix";
            this.ChkPrefixNames.UseVisualStyleBackColor = true;
            // 
            // ChkExportModels
            // 
            this.ChkExportModels.AutoSize = true;
            this.ChkExportModels.Location = new System.Drawing.Point(6, 48);
            this.ChkExportModels.Name = "ChkExportModels";
            this.ChkExportModels.Size = new System.Drawing.Size(118, 23);
            this.ChkExportModels.TabIndex = 4;
            this.ChkExportModels.Text = "Export models";
            this.ChkExportModels.UseVisualStyleBackColor = true;
            // 
            // ChkExportAnimations
            // 
            this.ChkExportAnimations.AutoSize = true;
            this.ChkExportAnimations.Location = new System.Drawing.Point(6, 117);
            this.ChkExportAnimations.Name = "ChkExportAnimations";
            this.ChkExportAnimations.Size = new System.Drawing.Size(141, 23);
            this.ChkExportAnimations.TabIndex = 9;
            this.ChkExportAnimations.Text = "Export animations";
            this.ChkExportAnimations.UseVisualStyleBackColor = true;
            // 
            // ChkExportTextures
            // 
            this.ChkExportTextures.AutoSize = true;
            this.ChkExportTextures.Location = new System.Drawing.Point(6, 71);
            this.ChkExportTextures.Name = "ChkExportTextures";
            this.ChkExportTextures.Size = new System.Drawing.Size(123, 23);
            this.ChkExportTextures.TabIndex = 6;
            this.ChkExportTextures.Text = "Export textures";
            this.ChkExportTextures.UseVisualStyleBackColor = true;
            // 
            // CmbFormat
            // 
            this.CmbFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CmbFormat.FormattingEnabled = true;
            this.CmbFormat.Items.AddRange(new object[] {
            "COLLADA 1.4.1 (*.dae)",
            "Valve StudioMdl (*.smd)"});
            this.CmbFormat.Location = new System.Drawing.Point(162, 49);
            this.CmbFormat.Name = "CmbFormat";
            this.CmbFormat.Size = new System.Drawing.Size(192, 27);
            this.CmbFormat.TabIndex = 5;
            // 
            // BtnBrowseOut
            // 
            this.BtnBrowseOut.Location = new System.Drawing.Point(322, 19);
            this.BtnBrowseOut.Name = "BtnBrowseOut";
            this.BtnBrowseOut.Size = new System.Drawing.Size(32, 24);
            this.BtnBrowseOut.TabIndex = 3;
            this.BtnBrowseOut.Text = "...";
            this.BtnBrowseOut.UseVisualStyleBackColor = true;
            this.BtnBrowseOut.Click += new System.EventHandler(this.BtnBrowseOut_Click);
            // 
            // TxtOutFolder
            // 
            this.TxtOutFolder.Location = new System.Drawing.Point(6, 20);
            this.TxtOutFolder.Name = "TxtOutFolder";
            this.TxtOutFolder.Size = new System.Drawing.Size(310, 26);
            this.TxtOutFolder.TabIndex = 2;
            // 
            // BtnConvert
            // 
            this.BtnConvert.Location = new System.Drawing.Point(276, 329);
            this.BtnConvert.Name = "BtnConvert";
            this.BtnConvert.Size = new System.Drawing.Size(96, 24);
            this.BtnConvert.TabIndex = 11;
            this.BtnConvert.Text = "Convert";
            this.BtnConvert.UseVisualStyleBackColor = true;
            this.BtnConvert.Click += new System.EventHandler(this.BtnConvert_Click);
            // 
            // ProgressConv
            // 
            this.ProgressConv.Location = new System.Drawing.Point(12, 329);
            this.ProgressConv.Name = "ProgressConv";
            this.ProgressConv.Size = new System.Drawing.Size(258, 24);
            this.ProgressConv.TabIndex = 0;
            // 
            // TxtIgnoredExt
            // 
            this.TxtIgnoredExt.Location = new System.Drawing.Point(137, 185);
            this.TxtIgnoredExt.Name = "TxtIgnoredExt";
            this.TxtIgnoredExt.Size = new System.Drawing.Size(217, 26);
            this.TxtIgnoredExt.TabIndex = 12;
            this.TxtIgnoredExt.Text = "bgrs, bcls, bcls2";
            // 
            // LblIgnoredExt
            // 
            this.LblIgnoredExt.AutoSize = true;
            this.LblIgnoredExt.Location = new System.Drawing.Point(6, 188);
            this.LblIgnoredExt.Name = "LblIgnoredExt";
            this.LblIgnoredExt.Size = new System.Drawing.Size(125, 19);
            this.LblIgnoredExt.TabIndex = 13;
            this.LblIgnoredExt.Text = "Ignored Extensions";
            // 
            // FrmExport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 361);
            this.Controls.Add(this.ProgressConv);
            this.Controls.Add(this.BtnConvert);
            this.Controls.Add(this.GrpOutput);
            this.Controls.Add(this.GrpInput);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FrmExport";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Batch Exporter";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmExport_FormClosing);
            this.Load += new System.EventHandler(this.FrmExport_Load);
            this.GrpInput.ResumeLayout(false);
            this.GrpInput.PerformLayout();
            this.GrpOutput.ResumeLayout(false);
            this.GrpOutput.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox GrpInput;
        private System.Windows.Forms.Button BtnBrowseIn;
        private System.Windows.Forms.TextBox TxtInputFolder;
        private System.Windows.Forms.GroupBox GrpOutput;
        private System.Windows.Forms.CheckBox ChkPrefixNames;
        private System.Windows.Forms.CheckBox ChkExportModels;
        private System.Windows.Forms.CheckBox ChkExportAnimations;
        private System.Windows.Forms.CheckBox ChkExportTextures;
        private System.Windows.Forms.ComboBox CmbFormat;
        private System.Windows.Forms.Button BtnBrowseOut;
        private System.Windows.Forms.TextBox TxtOutFolder;
        private System.Windows.Forms.Button BtnConvert;
        private System.Windows.Forms.ProgressBar ProgressConv;
        private System.Windows.Forms.ComboBox CmbMatFormat;
        private System.Windows.Forms.CheckBox ChkExportMaterials;
        private System.Windows.Forms.CheckBox ChkRecurse;
        private System.Windows.Forms.Label LblIgnoredExt;
        private System.Windows.Forms.TextBox TxtIgnoredExt;
    }
}