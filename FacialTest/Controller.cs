// Project: FacialTest
// Filename; Controller.cs
// Created; 14/08/2018
// Edited: 04/09/2018

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

        private const bool CONSOLE = true;

        private static Controller instance;

        private List<Camera> m_Cameras = new List<Camera>();

        private List<Face> m_Faces = new List<Face>();

        private double m_FPS = 0;

        private List<Tracker> m_Trackers = new List<Tracker>();

        private Stopwatch Stopwatch;

        private readonly bool cuda = true;
        

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
            get
            {
                return this.cuda;
            }
        }

        public bool Console
        {
            get
            {
                return CONSOLE;
            }
        }

        public List<Face> Faces
        {
            get
            {
                return this.m_Faces;
            }
            set
            {
                this.m_Faces = value;
            }
        }

        public void InitializeControl()
        {
            if (TIMERS) this.Stopwatch = new Stopwatch();

            //this.Cameras.Add(new Camera(0));

            this.Cameras.Add(new Camera("http://192.168.1.191:8081"));

            //this.Cameras.Add(new Camera("http://192.168.1.192:8081"));

            foreach (Camera c in this.Cameras) c.InitializeCamera();
        }

        public async void ReadAllFrames()
        {
            // List<Task> cameraTaskList = new List<Task>();
            while (true)
            {
                foreach (Camera c in this.Cameras)
                {
                    try
                    {
                        await c.GetFrame().ConfigureAwait(false);
                        
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }

                await Task.Delay(100).ConfigureAwait(true);
            }
        }
    }
}