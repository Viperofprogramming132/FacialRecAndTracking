// Project: FacialTest
// Filename; Camera.cs
// Created; 01/09/2018
// Edited: 04/09/2018

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

        private Mat image = new Mat();

        private ImageMan imageMan = ImageMan.Instance;

        private MCvPoint3D32f m_cameraLocation = new MCvPoint3D32f(0.0f, 0.0f, 0.0f);

        private int m_FrameNum;

        private Camera m_PartnerCam;

        private MCvPoint3D32f m_partnerCameraLocation = new MCvPoint3D32f(0.0f, 0.0f, 0.0f);

        private List<Rectangle> ROIs = new List<Rectangle>();

        private VideoCapture v;

        private VectorOfRect VectorOfRect = new VectorOfRect();

        /// <summary>
        /// Takes IP or Filename to run camera
        /// </summary>
        /// <param name="address"></param>
        public Camera(string address)
        {
            this.v = new VideoCapture(address);
        }

        /// <summary>
        /// Takes an imput index 0 for webcam 1
        /// </summary>
        /// <param name="inputIndex"></param>
        public Camera(int inputIndex)
        {
            this.v = new VideoCapture(inputIndex);
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

        public async Task GetFrame()
        {
            ++this.m_FrameNum;
            this.v.SetCaptureProperty(CapProp.PosFrames, this.m_FrameNum);
            this.v.Read(this.image);

            //await this.FindAllFacesAsync().ConfigureAwait(false);

            Rectangle[] output = DetectPerson.FindPerson(this.image, out long time);

            foreach (Face f in this.Control.Faces)
            {
                Rectangle r = f.ROI;
                if (f.Tracker.Update(this.image, out r))
                {
                    CvInvoke.Circle(
                        this.image,
                        new Point(r.X + (r.Width / 2), r.Y + (r.Height / 2)),
                        20,
                        new MCvScalar(0, 255, 0),
                        2);
                    f.ROI = r;
                }
                else
                {
                    f.Tracker = new TrackerBoosting();
                }
            }

            CvInvoke.Imshow("Finish", this.image);

            this.Control.FPS++;
        }

        public void InitializeCamera()
        {
            this.v.SetCaptureProperty(CapProp.Brightness, 20);
            this.Control = Controller.Instance;
        }

        public override string ToString()
        {
            return this.v.CaptureSource.ToString();
        }

        public void Tracking(Face f, Rectangle ROI)
        {
            this.Control.Trackers.Add(new TrackerBoosting());
            this.Control.Trackers[this.Control.Trackers.Count - 1].Init(this.image, ROI);
            f.Tracker = this.Control.Trackers[this.Control.Trackers.Count - 1];
            f.FlipTracker();
        }

        private async Task FindAllFacesAsync()
        {
            List<Rectangle> faces = new List<Rectangle>();
            List<Rectangle> eyes = new List<Rectangle>();
            List<Task<string>> tasks = new List<Task<string>>();
            List<int> indexList = new List<int>();
            string DT = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
            this.ROIs.Clear();

            DetectPerson.Detect(
                this.image,
                "haarcascade_frontalface_default.xml",
                "haarcascade_eye.xml",
                faces,
                eyes,
                out long detectionTime);

            foreach (Rectangle face in faces)
            {
                CvInvoke.Rectangle(this.image, face, new Bgr(Color.Red).MCvScalar, 2);
                this.ROIs.Add(face);
            }

            List<Mat> Images = ImageMan.Instance.CropImage(this.image, this.ROIs);

            for (int index = 0; index < Images.Count; index++)
            {
                Mat i = Images[index];

                Task<string> task = this.imageMan.CompareImagesAsync(i);

                tasks.Add(task);
                indexList.Add(index);
            }

            

            Task.WaitAll(tasks.ToArray(), 1000);

            for (int index = 0; index < tasks.Count; index++)
            {
                //this.imageMan.SavePng(DT, Images[index].Bitmap);
                Task<string> task = tasks[index];
                if (task.Result == "Failed")
                {
                    this.Control.Faces.Add(
                        new Face(
                            Images[indexList[index]],
                            DT,
                            this.ROIs,
                            Images.FindIndex(a => a == Images[indexList[index]]),
                            this));
                    this.imageMan.SavePng(DT,Images[indexList[index]]);
                }

                if (task.Result != "Failed" && task.Result != "Unusable")
                {
                    bool exists = false;
                    foreach (Face f in this.Control.Faces)
                        if (f.FileName == task.Result)
                            exists = true;

                    if (!exists)
                        this.Control.Faces.Add(
                            new Face(
                                Images[indexList[index]],
                                task.Result,
                                this.ROIs,
                                Images.FindIndex(a => a == Images[indexList[index]]),
                                this));
                }
            }

            this.VectorOfRect.Clear();
            this.VectorOfRect.Push(this.ROIs.ToArray());

            foreach (Rectangle eye in eyes) CvInvoke.Rectangle(this.image, eye, new Bgr(Color.Blue).MCvScalar, 2);
        }

        private void FindFocalLength()
        {
            
        }
    }
}