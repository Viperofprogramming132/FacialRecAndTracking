namespace FacialTest
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            this.button1 = new System.Windows.Forms.Button();
            this.lbl_FPS = new System.Windows.Forms.Label();
            this.lbl_FrameTimeNum = new System.Windows.Forms.Label();
            this.FPSTimer = new System.Windows.Forms.Timer(this.components);
            this.UpdateImageViewers = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(13, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Play";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Button1_Click);
            // 
            // lbl_FPS
            // 
            this.lbl_FPS.AutoSize = true;
            this.lbl_FPS.Location = new System.Drawing.Point(133, 9);
            this.lbl_FPS.Name = "lbl_FPS";
            this.lbl_FPS.Size = new System.Drawing.Size(27, 13);
            this.lbl_FPS.TabIndex = 3;
            this.lbl_FPS.Text = "FPS";
            // 
            // lbl_FrameTimeNum
            // 
            this.lbl_FrameTimeNum.AutoSize = true;
            this.lbl_FrameTimeNum.Location = new System.Drawing.Point(201, 9);
            this.lbl_FrameTimeNum.Name = "lbl_FrameTimeNum";
            this.lbl_FrameTimeNum.Size = new System.Drawing.Size(13, 13);
            this.lbl_FrameTimeNum.TabIndex = 4;
            this.lbl_FrameTimeNum.Text = "0";
            // 
            // FPSTimer
            // 
            this.FPSTimer.Interval = 1000;
            this.FPSTimer.Tick += new System.EventHandler(this.FPSTimer_Tick);
            // 
            // UpdateImageViewers
            // 
            this.UpdateImageViewers.Interval = 5;
            this.UpdateImageViewers.Tick += new System.EventHandler(this.UpdateImageViewers_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(267, 40);
            this.Controls.Add(this.lbl_FrameTimeNum);
            this.Controls.Add(this.lbl_FPS);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label lbl_FPS;
        private System.Windows.Forms.Label lbl_FrameTimeNum;
        private System.Windows.Forms.Timer FPSTimer;
        private System.Windows.Forms.Timer UpdateImageViewers;
    }
}

