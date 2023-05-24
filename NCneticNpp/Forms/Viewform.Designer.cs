namespace NCneticNpp
{
    partial class ViewForm
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
            this.trackBar = new System.Windows.Forms.TrackBar();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.yStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.lrStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.xStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.zStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.sStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.fStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.glControl = new OpenTK.GLControl();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // trackBar
            // 
            this.trackBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.trackBar.Location = new System.Drawing.Point(0, 545);
            this.trackBar.Name = "trackBar";
            this.trackBar.Size = new System.Drawing.Size(771, 45);
            this.trackBar.TabIndex = 13;
            this.trackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            // 
            // statusStrip
            // 
            this.statusStrip.BackColor = System.Drawing.SystemColors.ControlDark;
            this.statusStrip.Dock = System.Windows.Forms.DockStyle.Top;
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.xStatusLabel,
            this.yStatusLabel,
            this.zStatusLabel,
            this.lrStatusLabel,
            this.fStatusLabel,
            this.sStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 0);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(771, 24);
            this.statusStrip.SizingGrip = false;
            this.statusStrip.TabIndex = 14;
            this.statusStrip.Text = "statusStrip";
            // 
            // yStatusLabel
            // 
            this.yStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.yStatusLabel.Name = "yStatusLabel";
            this.yStatusLabel.Size = new System.Drawing.Size(59, 19);
            this.yStatusLabel.Text = "Y = 0.000";
            // 
            // lrStatusLabel
            // 
            this.lrStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.lrStatusLabel.Name = "lrStatusLabel";
            this.lrStatusLabel.Size = new System.Drawing.Size(58, 19);
            this.lrStatusLabel.Text = "L = 0.000";
            // 
            // xStatusLabel
            // 
            this.xStatusLabel.BorderStyle = System.Windows.Forms.Border3DStyle.Etched;
            this.xStatusLabel.Name = "xStatusLabel";
            this.xStatusLabel.Size = new System.Drawing.Size(55, 19);
            this.xStatusLabel.Text = "X = 0.000";
            // 
            // zStatusLabel
            // 
            this.zStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.zStatusLabel.Name = "zStatusLabel";
            this.zStatusLabel.Size = new System.Drawing.Size(59, 19);
            this.zStatusLabel.Text = "Z = 0.000";
            // 
            // sStatusLabel
            // 
            this.sStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.sStatusLabel.Name = "sStatusLabel";
            this.sStatusLabel.Size = new System.Drawing.Size(58, 19);
            this.sStatusLabel.Text = "S = 0.000";
            // 
            // fStatusLabel
            // 
            this.fStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.fStatusLabel.Name = "fStatusLabel";
            this.fStatusLabel.Size = new System.Drawing.Size(58, 19);
            this.fStatusLabel.Text = "F = 0.000";
            // 
            // glControl
            // 
            this.glControl.BackColor = System.Drawing.Color.Black;
            this.glControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.glControl.Location = new System.Drawing.Point(0, 24);
            this.glControl.Name = "glControl";
            this.glControl.Size = new System.Drawing.Size(771, 521);
            this.glControl.TabIndex = 17;
            this.glControl.VSync = false;
            // 
            // ViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(771, 590);
            this.Controls.Add(this.glControl);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.trackBar);
            this.Name = "ViewForm";
            this.Text = "frmMyDlg";
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TrackBar trackBar;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel xStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel yStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel zStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel lrStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel fStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel sStatusLabel;
        private OpenTK.GLControl glControl;
    }
}