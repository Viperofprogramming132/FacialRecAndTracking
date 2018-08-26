using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Drawing.Imaging;
using Emgu.CV.UI;
using Emgu.CV.Tracking;
using System.IO;
using System.Diagnostics;

namespace FacialTest
{
    public class Controller
    {
        const bool DEBUG = false;
        const bool TIMERS = false;

        
    

        private static Controller instance;

        public static Controller Instance { get { if (instance == null) { instance = new Controller(); } return instance; } }

        //VideoCapture v = new VideoCapture("E:\\Videos\\Dis\\21-43-49.mp4");
        //VideoCapture v = new VideoCapture(0);
        VideoCapture v = new VideoCapture("http://192.168.1.191:8081");

        Stopwatch Stopwatch;
        Mat image = null;
        Mat preImage = null;
        Mat grayImage = null;
        Mat grayPreImage = null;
        Mat compareImage = null;
        Mat thresholdImage = null;
        ImageMan imageMan = ImageMan.Instance;
        Rectangle objectBoundingRectangle = new Rectangle(0, 0, 0, 0);
        //MultiTracker MultiTracker = new MultiTracker();
        List<Rectangle> Objects = new List<Rectangle>();
        List<Rectangle> ROIs = new List<Rectangle>();
        Emgu.CV.Util.VectorOfRect VectorOfRect = new Emgu.CV.Util.VectorOfRect();
        List<Tracker> Trackers = new List<Tracker>();

        List<Face> m_Faces = new List<Face>();

        private double m_FrameTotal, m_FPS;
        private int m_FrameNum;

        public double FrameTotal { get { return m_FrameTotal; } set { m_FrameTotal = value; } }
        public double FPS { get { return m_FPS; } set { m_FPS = value; } }
        public int FrameNum { get { return m_FrameNum; } set { m_FrameNum = value; } }


        private Controller()
        {
            if (TIMERS)
            {
                Stopwatch = new Stopwatch();
            }
        }

        public async void ReadAllFrames()
        {
            image = new Mat();
            preImage = new Mat();
            grayImage = new Mat();
            grayPreImage = new Mat();
            compareImage = new Mat();
            thresholdImage = new Mat();
            while (true)
            {
                if (TIMERS)
                {
                    Stopwatch.Start();
                }
                v.SetCaptureProperty(CapProp.PosFrames, m_FrameNum);
                v.Read(preImage);
                ++m_FrameNum;
                v.SetCaptureProperty(CapProp.PosFrames, m_FrameNum);
                v.Read(image);

                if (TIMERS)
                {
                    Debug.WriteLine("After Cap " + Stopwatch.ElapsedMilliseconds);
                }


                //CvInvoke.CvtColor(image, grayImage, ColorConversion.Bgr2Gray);
                //CvInvoke.CvtColor(preImage, grayPreImage, ColorConversion.Bgr2Gray);
                //CvInvoke.AbsDiff(grayImage, grayPreImage, compareImage);
                //CvInvoke.Threshold(compareImage, thresholdImage, 90, 255, ThresholdType.Binary);

                //if (TIMERS)
                //{
                //    Debug.WriteLine("After Threshold " + Stopwatch.ElapsedMilliseconds);
                //}

                //if (DEBUG)
                //{
                //    CvInvoke.Imshow("Diff Image", compareImage);
                //    CvInvoke.Imshow("Threshold Image", thresholdImage);
                //}
                //else
                //{
                //    CvInvoke.DestroyWindow("Diff Image");
                //    CvInvoke.DestroyWindow("Threshold Image");
                //}

                //CvInvoke.Blur(thresholdImage, thresholdImage, new Size(20, 20), new Point(-1, -1));
                //CvInvoke.Threshold(thresholdImage, thresholdImage, 50, 255, ThresholdType.Binary);

                //if (TIMERS)
                //{
                //    Debug.WriteLine("After threshold 2 " + Stopwatch.ElapsedMilliseconds);
                //}

                //if (DEBUG)
                //{
                //    CvInvoke.Imshow("Blured", thresholdImage);
                //}
                //else
                //{
                //    CvInvoke.DestroyWindow("Blured");
                //}



                //searchForMovement(thresholdImage);


                FindAllFaces();

                if (TIMERS)
                {
                    Debug.WriteLine("After Find Faces " + Stopwatch.ElapsedMilliseconds);
                }

                //MultiTracker.Update(image, VectorOfRect);

                foreach (Tracker t in Trackers)
                {
                    foreach(Face f in m_Faces)
                    { 
                        Rectangle r = f.ROI;
                        t.Update(image, out r);
                        CvInvoke.Circle(image, new Point(r.X + (r.Width / 2), r.Y + (r.Height / 2)), 20, new MCvScalar(0, 255, 0), 2);
                    }
                }

                if (TIMERS)
                {
                    Debug.WriteLine("After Trackers " + Stopwatch.ElapsedMilliseconds);
                }

                //pictureBox1.Image = image.Bitmap;
                CvInvoke.Imshow("Finish", image);

                if (TIMERS)
                {
                    Debug.WriteLine("After End " + Stopwatch.ElapsedMilliseconds);
                    
                    Stopwatch.Reset();
                }

                if (m_FPS != 0)
                    await Task.Delay(1000 / Convert.ToInt16(m_FPS));
                else
                    await Task.Delay(100);
            }
        }
        private async void FindAllFaces()
        {
            long detectionTime;
            List<Rectangle> faces = new List<Rectangle>();
            List<Rectangle> eyes = new List<Rectangle>();
            ROIs.Clear();

            DetectFace.Detect(
                image, "haarcascade_frontalface_default.xml",
                "haarcascade_eye.xml",
                faces, eyes,
                out detectionTime);

            foreach (Rectangle face in faces)
            {
                CvInvoke.Rectangle(image, face, new Bgr(Color.Red).MCvScalar, 2);
                ROIs.Add(face);
            }
            List<Image<Gray, byte>> Images = ImageMan.Instance.CropImage(image, ROIs);
            
            foreach (Image<Gray, byte> i in Images)
            {
                string DT = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
                string response = imageMan.CompareImages(i/*mage.ToImage<Gray,byte>()*/);


                //    if (response == "Failed")
                //    {
                //        ImageMan.Instance.saveJpeg(Directory.GetCurrentDirectory() + "\\photos\\" + DT + ".jpg", i.Bitmap, 100);
                //        m_Faces.Add(new Face(i, DT, image.Bitmap, ROIs, Images.FindIndex(a => a == i)));
                //    }
                //     if (response != "Failed" && response != "Unuseable")
                //    {
                //        bool exists = false;
                //        foreach(Face f in m_Faces)
                //        {
                //            if (f.FileName == response)
                //            {
                //                exists = true;
                //            }
                //        }
                //        if (!exists)
                //        {
                //            m_Faces.Add(new Face(i, response, image.Bitmap, ROIs, Images.FindIndex(a => a == i)));
                //        }

                //    }
            }

            VectorOfRect.Clear();
            VectorOfRect.Push(ROIs.ToArray());

            foreach (Rectangle eye in eyes)
                CvInvoke.Rectangle(image, eye, new Bgr(Color.Blue).MCvScalar, 2);
        }

        public void tracking(Face f, Rectangle ROI)
        {
            for (int i = 0; i < ROIs.Count; i++)
            {
                Trackers.Add(new TrackerBoosting());
                Trackers[i].Init(image, ROI);
                f.Tracker = Trackers[i];
                f.FlipTracker();
            }

        }
    }
}
