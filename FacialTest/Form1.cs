// Project: FacialTest
// Filename; Form1.cs
// Created; 10/08/2018
// Edited: 04/09/2018

using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        private bool running = false;

        List<Thread> cameraThreads = new List<Thread>();

        public Form1()
        {
            this.InitializeComponent();
            this.trackingController.InitializeControl();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (!this.running)
            {
                foreach (Camera c in this.trackingController.Cameras)
                {
                    this.cameraThreads.Add(new Thread(() => this.trackingController.ReadAllFrames(c)));
                    this.cameraThreads.Last().Start();
                }

                this.FPSTimer.Start();
                this.running = !this.running;
            }
            else
            {
                this.StopDisplay();
                GC.Collect();
            }
        }

        private void FPSTimer_Tick(object sender, EventArgs e)
        {
            this.lbl_FPSNum.Text = this.trackingController.FPS.ToString();

            if (this.trackingController.DebugTools)
            {
                Debug.WriteLine("Cameras: " + this.trackingController.Cameras.Count);
                Debug.WriteLine("Trackers: " + this.trackingController.Trackers.Count);
                Debug.WriteLine("Faces: " + this.trackingController.Faces.Count);
            }
        }

        private void StopDisplay()
        {
            foreach (Thread thread in this.cameraThreads)
            {
                thread.Abort();
            }
            this.FPSTimer.Stop();
            this.running = !this.running;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.StopDisplay();
        }
    }
    
}