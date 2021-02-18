namespace SPICA.WinForms {
	partial class FrmGFTFormat {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.formatCombo = new System.Windows.Forms.ComboBox();
            this.saveBut = new System.Windows.Forms.Button();
            this.cancelBut = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.formatCombo);
            this.groupBox1.Location = new System.Drawing.Point(16, 15);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Size = new System.Drawing.Size(184, 66);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "GFTexture Format";
            // 
            // formatCombo
            // 
            this.formatCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.formatCombo.FormattingEnabled = true;
            this.formatCombo.Items.AddRange(new object[] {
            "PC",
            "Raw"});
            this.formatCombo.Location = new System.Drawing.Point(8, 23);
            this.formatCombo.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.formatCombo.Name = "formatCombo";
            this.formatCombo.Size = new System.Drawing.Size(160, 24);
            this.formatCombo.TabIndex = 0;
            this.formatCombo.SelectedIndexChanged += new System.EventHandler(this.FormatCombo_SelectedIndexChanged);
            // 
            // saveBut
            // 
            this.saveBut.Location = new System.Drawing.Point(16, 89);
            this.saveBut.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.saveBut.Name = "saveBut";
            this.saveBut.Size = new System.Drawing.Size(92, 30);
            this.saveBut.TabIndex = 2;
            this.saveBut.Text = "Save";
            this.saveBut.UseVisualStyleBackColor = true;
            this.saveBut.Click += new System.EventHandler(this.saveBut_Click);
            // 
            // cancelBut
            // 
            this.cancelBut.Location = new System.Drawing.Point(116, 89);
            this.cancelBut.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cancelBut.Name = "cancelBut";
            this.cancelBut.Size = new System.Drawing.Size(84, 30);
            this.cancelBut.TabIndex = 3;
            this.cancelBut.Text = "Cancel";
            this.cancelBut.UseVisualStyleBackColor = true;
            this.cancelBut.Click += new System.EventHandler(this.cancelBut_Click);
            // 
            // FrmGFTFormat
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(216, 132);
            this.Controls.Add(this.cancelBut);
            this.Controls.Add(this.saveBut);
            this.Controls.Add(this.groupBox1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "FrmGFTFormat";
            this.Text = "Export";
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.ComboBox formatCombo;
		private System.Windows.Forms.Button saveBut;
		private System.Windows.Forms.Button cancelBut;
	}
}