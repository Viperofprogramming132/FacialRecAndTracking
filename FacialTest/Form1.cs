// Project: FacialTest
// Filename; Form1.cs
// Created; 10/08/2018
// Edited: 04/09/2018

using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Emgu.CV;

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

        private int framesDisplayed;

        private int previousFramesDisplayed;

        Thread frameReader = new Thread(ReadFrames);

        Thread cudaThread = new Thread(CudaTimer);

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
                this.UpdateImageViewers.Start();
                this.cudaThread.Start();
                this.frameReader.Start();
                this.running = !this.running;
            }
            else
            {
                this.StopDisplay();
                //GC.Collect();
            }
        }

        private void FPSTimer_Tick(object sender, EventArgs e)
        {
            if (this.trackingController.DebugTools)
            {
                Debug.WriteLine("Cameras: " + this.trackingController.Cameras.Count);
                Debug.WriteLine("Trackers: " + this.trackingController.Trackers.Count);
                Debug.WriteLine("Faces: " + this.trackingController.Faces.Count);
            }

            //GC.Collect();

            int frames = this.framesDisplayed - this.previousFramesDisplayed;

            this.previousFramesDisplayed = this.framesDisplayed;

            this.lbl_FrameTimeNum.Text = frames + " fps";
        }

        private void StopDisplay()
        {
            foreach (Thread thread in this.cameraThreads)
            {
                thread.Abort();
            }
            this.FPSTimer.Stop();
            this.UpdateImageViewers.Stop();
            this.running = !this.running;
            this.cudaThread.Abort();
            this.frameReader.Abort();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.StopDisplay();
        }

        private void UpdateImageViewers_Tick(object sender, EventArgs e)
        {
            int tempFramesDisplayed = 0;
            foreach (Camera c in this.trackingController.Cameras)
            {
                tempFramesDisplayed = this.trackingController.DisplayFrame(c);
            }

            this.framesDisplayed += tempFramesDisplayed;
        }

        private static void CudaTimer()
        {
            while (true)
            {
                foreach (Camera c in Controller.Instance.Cameras)
                {
                    c.CudaFindPeople();
                    c.FindAllFacesAsync();
                }
            }
        }

        private static void ReadFrames()
        {
            while (true)
            {
                foreach (Camera c in Controller.Instance.Cameras)
                {
                    c.ReadFrame();
                }

                foreach (Face f in Controller.Instance.Faces.ToArray())
                {
                    f.CheckForCollition();
                }
            }
        }
    }
    
}