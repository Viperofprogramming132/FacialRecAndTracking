// Project: FacialTest
// Filename; Form1.cs
// Created; 10/08/2018
// Edited: 04/09/2018

namespace FacialTest
{
    using System;
    using System.Diagnostics;
    using System.Windows.Forms;

    public partial class Form1 : Form
    {
        /// <summary>
        /// The tracking controller.
        /// </summary>
        private readonly Controller trackingController = Controller.Instance;

        public Form1()
        {
            this.InitializeComponent();
            this.trackingController.InitializeControl();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.trackingController.ReadAllFrames();
            this.FPSTimer.Start();
        }

        private void FPSTimer_Tick(object sender, EventArgs e)
        {
            this.lbl_FPSNum.Text = this.trackingController.FPS.ToString();
            this.trackingController.FPS = 0;

            if (this.trackingController.Console)
            {
                Console.WriteLine("Cameras: " + this.trackingController.Cameras.Count);
                Console.WriteLine("Trackers: " + this.trackingController.Trackers.Count);
                Console.WriteLine("Faces: " + this.trackingController.Faces.Count);
            }
        }
    }
    
}