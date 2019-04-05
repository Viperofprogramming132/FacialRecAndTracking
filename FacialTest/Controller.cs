// Project:     FacialTest
// Filename;    Controller.cs
// Created;     14/08/2018
// Edited:      04/09/2018

using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using Emgu.CV.UI;

namespace FacialTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using Emgu.CV;
    using Emgu.CV.Cuda;
    using Emgu.CV.Tracking;

    public class Controller
    {
        private const bool DEBUG = true;

        private const bool TIMERS = false;

        private static Controller instance;

        private List<Camera> m_Cameras = new List<Camera>();

        private List<Face> m_Faces = new List<Face>();

        private double m_FPS = 0;

        private List<Tracker> m_Trackers = new List<Tracker>();

        private Stopwatch Stopwatch;

        private readonly bool cuda = true;

        List<ImageViewer> imageViewers = new List<ImageViewer>();

        ReaderWriterLockSlim m_FaceLock = new ReaderWriterLockSlim();
        

        private Controller()
        {
            this.cuda = CudaInvoke.HasCuda;
        }

        public static Controller Instance
        {
            get
            {
                if (instance == null) instance = new Controller();

                return instance;
            }
        }

        public List<Camera> Cameras
        {
            get => this.m_Cameras;
            set => this.m_Cameras = value;
        }

        public double FPS
        {
            get => this.m_FPS;
            set => this.m_FPS = value;
        }

        public List<Tracker> Trackers
        {
            get => this.m_Trackers;
            set => this.m_Trackers = value;
        }

        public bool Cuda
        {
            get => this.cuda;
        }

        public List<Face> Faces
        {
            get
            {
                try
                {
                    this.m_FaceLock.EnterReadLock();
                    return this.m_Faces;
                }
                finally
                {
                    this.m_FaceLock.ExitReadLock();
                }
            }
            set
            {
                try
                {
                    this.m_FaceLock.EnterWriteLock();
                    this.m_Faces = value;
                }
                finally
                {
                    this.m_FaceLock.ExitWriteLock();
                }
                
            }
        }

        public bool DebugTools
        {
            get => DEBUG;
        }

        public void InitializeControl()
        {
            this.Stopwatch = new Stopwatch();

            this.Cameras.Add(new Camera("Above Curtain.mp4"));
            this.Cameras.Add(new Camera("Above Stage.mp4"));

            foreach (Camera c in this.Cameras)
            {
                this.imageViewers.Add(new ImageViewer(null, c.ToString()));
                c.InitializeCamera(this.imageViewers.Last());

                this.imageViewers.Last().Name = c.ToString();
                this.imageViewers.Last().Show();
                this.imageViewers.Last().Size = new Size(1280, 960);
            }

            for (int index = 1; index <= this.Cameras.Count; ++index)
            {
                if (index % 2 == 0)
                {
                    this.Cameras[index-1].PartnerCamera(this.Cameras[index - 2]);
                }
            }
        }

        public void ReadAllFrames(Camera c)
        {
            c.GetFrame();
        }

        public int DisplayFrame(Camera c)
        {

            if (c.CompletedFrames.Count > 0)
            {
                Mat frame = c.CompletedFrames.First().Value;

                c.CompletedFrames.Remove(c.CompletedFrames.First().Key);
                c.FramesDisplayed++;

                c.CamImageViewer.Image = frame;


                return 1;
            }

            return 0;
        }
    }
}