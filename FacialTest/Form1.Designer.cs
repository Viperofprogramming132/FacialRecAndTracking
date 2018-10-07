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
            this.ltv_Cameras = new System.Windows.Forms.ListView();
            this.lbl_FPS = new System.Windows.Forms.Label();
            this.lbl_FPSNum = new System.Windows.Forms.Label();
            this.FPSTimer = new System.Windows.Forms.Timer(this.components);
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
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // ltv_Cameras
            // 
            this.ltv_Cameras.Location = new System.Drawing.Point(13, 34);
            this.ltv_Cameras.Name = "ltv_Cameras";
            this.ltv_Cameras.Size = new System.Drawing.Size(332, 298);
            this.ltv_Cameras.TabIndex = 2;
            this.ltv_Cameras.UseCompatibleStateImageBehavior = false;
            // 
            // lbl_FPS
            // 
            this.lbl_FPS.AutoSize = true;
            this.lbl_FPS.Location = new System.Drawing.Point(168, 9);
            this.lbl_FPS.Name = "lbl_FPS";
            this.lbl_FPS.Size = new System.Drawing.Size(27, 13);
            this.lbl_FPS.TabIndex = 3;
            this.lbl_FPS.Text = "FPS";
            // 
            // lbl_FPSNum
            // 
            this.lbl_FPSNum.AutoSize = true;
            this.lbl_FPSNum.Location = new System.Drawing.Point(201, 9);
            this.lbl_FPSNum.Name = "lbl_FPSNum";
            this.lbl_FPSNum.Size = new System.Drawing.Size(13, 13);
            this.lbl_FPSNum.TabIndex = 4;
            this.lbl_FPSNum.Text = "0";
            // 
            // FPSTimer
            // 
            this.FPSTimer.Interval = 1000;
            this.FPSTimer.Tick += new System.EventHandler(this.FPSTimer_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(928, 613);
            this.Controls.Add(this.lbl_FPSNum);
            this.Controls.Add(this.lbl_FPS);
            this.Controls.Add(this.ltv_Cameras);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ListView ltv_Cameras;
        private System.Windows.Forms.Label lbl_FPS;
        private System.Windows.Forms.Label lbl_FPSNum;
        private System.Windows.Forms.Timer FPSTimer;
    }
}

