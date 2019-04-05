// Project:     FacialTest
// Filename;    Camera.cs
// Created;     01/09/2018
// Edited:      04/09/2018

using System.Diagnostics;
using System.Linq;
using System.Threading;

using Emgu.CV.UI;

namespace FacialTest
{

    /*RaspberryPiCamV2Specs
     * Focal Length: 3.04mm
     * Pixel Size: 1.12um
     * sensor res: 3280x2464
     * HFOV: 62.2
     * VFOV: 48.8
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

        private int m_FrameNum = 1;

        private int m_FramesDisplayed = 0;

        private Camera m_PartnerCam;

        private MCvPoint3D32f m_partnerCameraLocation = new MCvPoint3D32f(0.0f, 0.0f, 0.0f);

        private List<Rectangle> ROIs = new List<Rectangle>();

        private readonly VideoCapture videoCap;

        private VectorOfRect VectorOfRect = new VectorOfRect();

        private Queue<KeyValuePair<int, Mat>> readFrames = new Queue<KeyValuePair<int, Mat>>();

        private readonly string CamName;

        private Dictionary<int, KeyValuePair<Mat, List<Rectangle>>> framePeopleDictionary = new Dictionary<int, KeyValuePair<Mat, List<Rectangle>>>();

        private DetectPerson detector = new DetectPerson();

        private Dictionary<int, Mat> m_CompletedFrames = new Dictionary<int, Mat>();

        private ImageViewer camImageViewer;

        private Dictionary<int, KeyValuePair<Mat,List<Rectangle>>> preProcessedFrames = new Dictionary<int, KeyValuePair<Mat, List<Rectangle>>>();

        private ReaderWriterLockSlim preProcessLock = new ReaderWriterLockSlim();

        private ReaderWriterLockSlim framePeopleDictionaryLock = new ReaderWriterLockSlim();
        
        private ReaderWriterLockSlim m_CompletedFramesLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Takes IP or Filename to run camera
        /// </summary>
        /// <param name="address"></param>
        public Camera(string address)
        {
            this.videoCap = new VideoCapture(address);
            this.CamName = address;
        }

        /// <summary>
        /// Takes an imput index 0 for webcam 1
        /// </summary>
        /// <param name="inputIndex"></param>
        public Camera(int inputIndex)
        {
            this.videoCap = new VideoCapture(inputIndex);
            this.CamName = inputIndex.ToString();
            this.videoCap.SetCaptureProperty(CapProp.Fps, 24);
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

        public Dictionary<int, KeyValuePair<Mat, List<Rectangle>>> FramePeopleDictionary
        {
            get
            {
                try
                {
                    this.framePeopleDictionaryLock.EnterReadLock();
                    return this.framePeopleDictionary;
                }
                finally
                {
                    this.framePeopleDictionaryLock.ExitReadLock();
                }
            }

            set
            {
                try
                {
                    this.framePeopleDictionaryLock.EnterWriteLock();
                    this.framePeopleDictionary = value;
                }
                finally
                {
                    this.framePeopleDictionaryLock.ExitWriteLock();
                }
            }
        }

        public Dictionary<int, Mat> CompletedFrames
        {
            get
            {
                try
                {
                    this.m_CompletedFramesLock.EnterReadLock();
                    return this.m_CompletedFrames;
                }
                finally
                {
                    this.m_CompletedFramesLock.ExitReadLock();
                }
            }

            set
            {
                try
                {
                    this.m_CompletedFramesLock.EnterWriteLock();
                    this.m_CompletedFrames = value;
                }
                finally
                {
                    this.m_CompletedFramesLock.ExitWriteLock();
                }
            }
        }

        public ImageViewer CamImageViewer
        {
            get
            {
                return this.camImageViewer;
            }
            set
            {
                this.camImageViewer = value;
            }
        }

        public Dictionary<int, KeyValuePair<Mat, List<Rectangle>>> PreProcessedFrames
        {
            get
            {
                try
                {
                    this.preProcessLock.EnterReadLock();
                    return this.preProcessedFrames;
                }
                finally
                {
                    this.preProcessLock.ExitReadLock();
                }
            }

            set
            {
                try
                {
                    this.preProcessLock.EnterWriteLock();
                    this.preProcessedFrames = value;
                }
                finally
                {
                    this.preProcessLock.ExitWriteLock();
                }

            }
        }

        public void GetFrame()
        {
            List<Task> taskList = new List<Task>();
            while (true)
            {
                if (this.PreProcessedFrames.Count > 0 && taskList.Count < 2)
                {
                    taskList.Add(Task.Run(this.ProcessFramesAsync));
                }

                if (taskList.Count > 0)
                {
                    for (int index = 0; index < taskList.Count; index++)
                    {
                        Task task = taskList[index];

                        if (task.IsCompleted || task.IsFaulted)
                        {
                            taskList.Remove(task);
                            index--;
                        }
                    }
                }

                if (this.readFrames.Count > 0)
                {
                    this.readFrames.OrderByDescending(frame => frame.Key);
                }




            }
        }

        public void ReadFrame()
        {
            Mat image = new Mat();

            if (this.readFrames.Count < 5)
            {
                image = this.videoCap.QueryFrame();

                double frameCount = this.videoCap.GetCaptureProperty(CapProp.FrameCount);

                CvInvoke.PutText(image, this.FrameNum + " / " + frameCount, new Point(0, 20), FontFace.HersheyPlain, 1, new MCvScalar(255, 255, 255));

                Face[] faces = this.Control.Faces.ToArray();

                foreach (Face f in faces)
                {
                    if (f.MCaptureCamera == this)
                    {
                        if (f.UpdateTracker(image))
                        {
                            Rectangle r = f.ROI;
                            CvInvoke.Circle(
                                image,
                                new Point(r.X + (r.Width / 2), r.Y + (r.Height / 2)),
                                20,
                                new MCvScalar(0, 255, 0),
                                2);
                        }
                    }
                }

                this.readFrames.Enqueue(new KeyValuePair<int, Mat>(this.m_FrameNum, image));
                this.m_FrameNum++;
            }
        }

        private async Task ProcessFramesAsync()
        {
            if (this.PreProcessedFrames.Count == 0)
                return;

            Mat frame = null;
            List<Rectangle> output = null;

            //Deep Copy the dictionary
            Dictionary<int, KeyValuePair<Mat, List<Rectangle>>> deepCopy = this.PreProcessedFrames.ToDictionary(c => c.Key, x => x.Value);

            KeyValuePair<int, KeyValuePair<Mat, List<Rectangle>>> frameToProcess = new KeyValuePair<int, KeyValuePair<Mat, List<Rectangle>>>(0, new KeyValuePair<Mat, List<Rectangle>>());
            if(deepCopy.Count>0)
                frameToProcess = deepCopy.First();
            
            int frameNum = frameToProcess.Key;

            //remove it
            deepCopy.Remove(frameNum);

            //reset it to the original so it write locks
            this.PreProcessedFrames = deepCopy;

            if (frameNum == 0 || this.FramePeopleDictionary.ContainsKey(frameNum))
                return;

            frame = frameToProcess.Value.Key;
            output = frameToProcess.Value.Value;

            List<Rectangle> peopleRectangles = new List<Rectangle>();

            foreach (Rectangle person in output)
            {
                if (person.Height > 250 && person.Width > 150)
                {
                    peopleRectangles.Add(person);
                }
            }

            foreach (Rectangle person in peopleRectangles)
            {
                CvInvoke.Rectangle(frame, person, new MCvScalar(255, 0, 0));
            }



            KeyValuePair<Mat, List<Rectangle>> temp = new KeyValuePair<Mat, List<Rectangle>>(frame, peopleRectangles);

            Dictionary<int, KeyValuePair<Mat, List<Rectangle>>> deepCopy2 = this.FramePeopleDictionary.ToDictionary(c => c.Key, x => x.Value);

            deepCopy2.OrderBy(item => item.Key);

            if (deepCopy2.ContainsKey(frameNum) && this.FramePeopleDictionary.ContainsKey(frameNum))
            {
                return;
            }
            else if (deepCopy2.ContainsKey(frameNum))
            {
                this.FramePeopleDictionary = deepCopy2;
            }
            else
            {
                deepCopy2.Add(frameNum, temp);
                FramePeopleDictionary = deepCopy2;
            }

            
        }

        public void InitializeCamera(ImageViewer thisCam)
        {
            this.videoCap.SetCaptureProperty(CapProp.Brightness, 20);
            this.Control = Controller.Instance;
            this.camImageViewer = thisCam;
        }

        public override string ToString()
        {
            return this.CamName;
        }

        public void FindAllFacesAsync()
        {
            int frameNum;
            Mat image;
            List<Rectangle> peopleRectangles;
            if (this.FramePeopleDictionary.Count > 0)
            {
                Dictionary<int, KeyValuePair<Mat, List<Rectangle>>> deepCopy = this.FramePeopleDictionary.ToDictionary(c => c.Key, x => x.Value);
                KeyValuePair<int,KeyValuePair<Mat,List<Rectangle>>> frame = deepCopy.First();
                frameNum = frame.Key;

                deepCopy.Remove(frameNum);
                FramePeopleDictionary = deepCopy;

                image = frame.Value.Key;
                peopleRectangles = frame.Value.Value;


            }
            else
            {
                return;
            }

            if (this.CompletedFrames.ContainsKey(frameNum) || frameNum == 0)
            {
                return;
            }

            List<Rectangle> faces = new List<Rectangle>();
            List<Rectangle> peopleFaces = new List<Rectangle>();
            
            this.ROIs.Clear();

            foreach (Rectangle person in peopleRectangles)
            {
                using (Image<Bgr, byte> personImage = image.ToImage<Bgr, byte>().GetSubRect(person))
                { 
                    detector.Detect(
                        personImage,
                        peopleFaces);
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


            if (this.CompletedFrames.ContainsKey(frameNum))
            {
                return;
            }

            Dictionary<int, Mat> deepCopy2 = this.CompletedFrames.ToDictionary(c => c.Key, x => x.Value);

            deepCopy2.Add(frameNum, image);

            deepCopy2.OrderBy(item => item.Key);

            this.CompletedFrames = deepCopy2;

            List<Mat> Images = ImageMan.Instance.CropImage(image, this.ROIs);

            for (int index = 0; index < faces.Count; index++)
            {
                if (this.Control.Faces.Count > 0)
                {
                    if (!this.Control.Faces[index].ROI.IntersectsWith(this.ROIs[Images.FindIndex(a => a == Images[index])]))
                    {
                        this.Control.Faces.Add(
                            new Face(
                                Images[index],
                                DateTime.Now.ToString(),
                                this.ROIs,
                                Images.FindIndex(a => a == Images[index]),
                                this,
                                image));
                    }
                }
                else
                {
                    this.Control.Faces.Add(
                        new Face(
                            Images[index],
                            DateTime.Now.ToString(),
                            this.ROIs,
                            Images.FindIndex(a => a == Images[index]),
                            this,
                            image));
                }

            }

            this.VectorOfRect.Clear();
            this.VectorOfRect.Push(this.ROIs.ToArray());
        }

        public void PartnerCamera(Camera c)
        {
            this.m_PartnerCam = c;

            if (this.m_PartnerCam.PartnerCam == null)
            {
                this.m_PartnerCam.PartnerCamera(this);
            }
        }

        public void CudaFindPeople()
        {
            if (this.readFrames.Count == 0)
            {
                return;
            }

            KeyValuePair<int, Mat> frameDequeue = this.readFrames.Dequeue();

            Mat frame = frameDequeue.Value;
            int frameNum = frameDequeue.Key;
            

            List<Rectangle> output = new List<Rectangle>();

            this.detector.FindPerson(frame, output);

            KeyValuePair<Mat, List<Rectangle>> temp = new KeyValuePair<Mat, List<Rectangle>>(frame, output);

            this.PreProcessedFrames.Add(frameNum, temp);
        }
    }
}