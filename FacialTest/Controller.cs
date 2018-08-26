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

        ImageMan imageMan = ImageMan.Instance;

        List<Rectangle> ROIs = new List<Rectangle>();
        Emgu.CV.Util.VectorOfRect VectorOfRect = new Emgu.CV.Util.VectorOfRect();

        List<Tracker> Trackers = new List<Tracker>();

        List<Face> m_Faces = new List<Face>();

        private int m_FrameNum;

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


                await FindAllFacesAsync().ConfigureAwait(false);

                if (TIMERS)
                {
                    Debug.WriteLine("After Find Faces " + Stopwatch.ElapsedMilliseconds);
                }

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

                CvInvoke.Imshow("Finish", image);

                if (TIMERS)
                {
                    Debug.WriteLine("After End " + Stopwatch.ElapsedMilliseconds);
                    
                    Stopwatch.Reset();
                }

                await Task.Delay(100);
            }
        }
        private async Task FindAllFacesAsync()
        {
            long detectionTime;
            List<Rectangle> faces = new List<Rectangle>();
            List<Rectangle> eyes = new List<Rectangle>();
            List<Task<string> > tasks = new List<Task<string>>();
            List<int> indexList = new List<int>();
            string DT = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
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

            for (int index = 0; index < Images.Count; index++)
            {
                Image<Gray, byte> i = Images[index];
                
                Task<string> task = imageMan.CompareImagesAsync(i);

                tasks.Add(task);
                indexList.Add(index);
            }

            foreach (var task in tasks)
            {
                await task;
            }

            for (int index = 0; index < tasks.Count; index++)
            {
                Task<string> task = tasks[index];
                if (task.Result == "Failed")
                {
                    ImageMan.Instance.saveJpeg(Directory.GetCurrentDirectory() + "\\photos\\" + DT + ".jpg", Images[indexList[index]].Bitmap,
                        100);
                    m_Faces.Add(new Face(Images[indexList[index]], DT, image.Bitmap, ROIs, Images.FindIndex(a => a == Images[indexList[index]])));
                }

                if (task.Result != "Failed" && task.Result != "Unuseable")
                {
                    bool exists = false;
                    foreach (Face f in m_Faces)
                    {
                        if (f.FileName == task.Result)
                        {
                            exists = true;
                        }
                    }

                    if (!exists)
                    {
                        m_Faces.Add(new Face(Images[indexList[index]], task.Result, image.Bitmap, ROIs, Images.FindIndex(a => a == Images[indexList[index]])));
                    }
                }
            }

            VectorOfRect.Clear();
            VectorOfRect.Push(ROIs.ToArray());

            foreach (Rectangle eye in eyes)
            {
                CvInvoke.Rectangle(image, eye, new Bgr(Color.Blue).MCvScalar, 2);
            }
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
