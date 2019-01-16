// Project: FacialTest
// Filename; Camera.cs
// Created; 01/09/2018
// Edited: 04/09/2018

using System.Diagnostics;
using System.Linq;

namespace FacialTest
{

    /*RaspberryPiCamV2Specs
     * Focal Length 3.04mm
     * Pixel Size 1.12um
     * sensor res 3280x2464
     * HFOV 62.2
     * VFOV 48.8
     */
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Threading.Tasks;

    using Emgu.CV;
    using Emgu.CV.Cuda;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using Emgu.CV.Tracking;
    using Emgu.CV.Util;

    public class Camera
    {
        private Controller Control;

        private MCvPoint3D32f m_cameraLocation = new MCvPoint3D32f(0.0f, 0.0f, 0.0f);

        private int m_FrameNum;

        private int m_FramesDisplayed = 0;

        private Camera m_PartnerCam;

        private MCvPoint3D32f m_partnerCameraLocation = new MCvPoint3D32f(0.0f, 0.0f, 0.0f);

        private List<Rectangle> ROIs = new List<Rectangle>();

        private VideoCapture v;

        private VectorOfRect VectorOfRect = new VectorOfRect();

        private List<Mat> readFrames = new List<Mat>();

        private List<Task<Mat>> taskList = new List<Task<Mat>>();

        private readonly string CamName;

        /// <summary>
        /// Takes IP or Filename to run camera
        /// </summary>
        /// <param name="address"></param>
        public Camera(string address)
        {
            this.v = new VideoCapture(address);
            this.CamName = address;
        }

        /// <summary>
        /// Takes an imput index 0 for webcam 1
        /// </summary>
        /// <param name="inputIndex"></param>
        public Camera(int inputIndex)
        {
            this.v = new VideoCapture(inputIndex);
            this.CamName = inputIndex.ToString();
        }

        public MCvPoint3D32f CameraLocation
        {
            get => this.m_cameraLocation;
            set => this.m_cameraLocation = value;
        }

        public int FrameNum
        {
            get
            {
                return this.m_FrameNum;
            }

            set
            {
                this.m_FrameNum = value;
            }
        }

        public Camera PartnerCam
        {
            get
            {
                return this.m_PartnerCam;
            }

            set
            {
                this.m_PartnerCam = value;
            }
        }

        public MCvPoint3D32f PartnerCameraLocation
        {
            get => this.m_partnerCameraLocation;
            set => this.m_partnerCameraLocation = value;
        }

        public int FramesDisplayed
        {
            get
            {
                return this.m_FramesDisplayed;
            }
            set => this.m_FramesDisplayed = value;
        }

        public List<Task<Mat>> TaskList
        {
            get
            {
                return this.taskList;
            }
        }

        public void GetFrame()
        {
            Task.Run(() => this.Control.DisplayFrame(this));
            while (true)
            {
                if (this.readFrames.Count <= 10)
                {
                    Task.WaitAll(Task.Run(this.GetFramesAsync));
                }

                if (this.readFrames.Count > 0)
                {
                    this.TaskList.Add(Task.Run(this.ProcessFramesAsync));
                }

                if (this.TaskList.Count > 0)
                {
                    for (int index = 0; index < this.TaskList.Count; index++)
                    {
                        Task<Mat> task = this.TaskList[index];

                        if (task.IsFaulted)
                        {
                            this.TaskList.Remove(task);
                            index--;
                        }
                    }
                }

                GC.Collect();
            }
        }

        private async Task GetFramesAsync()
        {
            Mat image = new Mat();

            try
            {
                ++this.m_FrameNum;
                this.v.SetCaptureProperty(CapProp.PosFrames, this.m_FrameNum);
                this.v.Read(image);

                this.readFrames.Add(image);
            }
            catch
            {
                Debug.WriteLine("Trying to access open file");
            }
        }

        private async Task<Mat> ProcessFramesAsync()
        {
            Mat frame = readFrames[0];
            readFrames.RemoveAt(0);
            List<Rectangle> output = DetectPerson.FindPerson(frame, out long time).ToList();
            List<Rectangle> peopleRectangles = new List<Rectangle>();

            foreach (Rectangle person in output)
            {
                if (person.Height > 250 && person.Width > 150)
                {
                    peopleRectangles.Add(person);
                }
            }

            //this.FindAllFacesAsync(peopleRectangles,frame);


            foreach (Rectangle person in peopleRectangles)
            {
                CvInvoke.Rectangle(frame, person, new MCvScalar(255, 0, 0));
            }

            //foreach (Face f in this.Control.Faces)
            //{
            //    Rectangle r = f.ROI;
            //    if (f.Tracker.Update(frame, out r))
            //    {
            //        CvInvoke.Circle(
            //            frame,
            //            new Point(r.X + (r.Width / 2), r.Y + (r.Height / 2)),
            //            20,
            //            new MCvScalar(0, 255, 0),
            //            2);
            //        f.ROI = r;
            //    }
            //    else
            //    {
            //        f.Tracker = new TrackerBoosting();
            //    }
            //}

            return frame;
        }

        public void InitializeCamera()
        {
            this.v.SetCaptureProperty(CapProp.Brightness, 20);
            this.Control = Controller.Instance;
        }

        public override string ToString()
        {
            return this.CamName;
        }

        private async Task FindAllFacesAsync(List<Rectangle> peopleRectangles,Mat image)
        {
            List<Rectangle> faces = new List<Rectangle>();
            List<Rectangle> peopleFaces = new List<Rectangle>();
            string DT = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
            this.ROIs.Clear();

            foreach (Rectangle person in peopleRectangles)
            {
                using (Image<Bgr, byte> personImage = image.ToImage<Bgr, byte>().GetSubRect(person))
                { 
                    DetectPerson.Detect(
                        personImage,
                        "haarcascade_frontalface_default.xml",
                        peopleFaces,
                        out long detectionTime);
                }

                for (int index = 0; index < peopleFaces.Count; index++)
                {
                    Rectangle face = peopleFaces[index];
                    face.Y += person.Y;
                    face.X += person.X;

                    faces.Add(face);
                }
            }
            

            foreach (Rectangle face in faces)
            {
                CvInvoke.Rectangle(image, face, new Bgr(Color.Red).MCvScalar, 2);
                this.ROIs.Add(face);
            }

            List<Mat> Images = ImageMan.Instance.CropImage(image, this.ROIs);

            for (int index = 0; index < faces.Count; index++)
            {
                this.Control.Faces.Add(
                    new Face(
                        Images[index],
                        DT,
                        this.ROIs,
                        Images.FindIndex(a => a == Images[index]),
                        this,
                        image));
            }

            this.VectorOfRect.Clear();
            this.VectorOfRect.Push(this.ROIs.ToArray());
        }

        public void partnerCamera(Camera c)
        {
            this.m_PartnerCam = c;

            if (this.m_PartnerCam.PartnerCam == null)
            {
                this.m_PartnerCam.partnerCamera(this);
            }
        }
    }
}