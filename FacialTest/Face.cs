// Project: FacialTest
// Filename; Face.cs
// Created; 14/08/2018
// Edited: 04/09/2018

using System;
using System.Linq;
using System.Threading;

namespace FacialTest
{
    using System.Collections.Generic;
    using System.Drawing;

    using Emgu.CV;
    using Emgu.CV.Structure;
    using Emgu.CV.Tracking;

    public class Face
    {
        private Camera m_captureCamera;

        private Mat m_Face;

        private string m_fileName;

        private Rectangle m_ROI;

        private bool m_Tracked;

        private Tracker m_Tracker;

        private ReaderWriterLockSlim m_ROILock = new ReaderWriterLockSlim();

        public Face(
            Mat face,
            string fileName,
            List<Rectangle> ROIs,
            int Index,
            Camera captureCamera,
            Mat image)
        {
            this.m_Face = face;
            this.m_fileName = fileName;
            this.MCaptureCamera = captureCamera;
            this.m_ROI = ROIs[Index];
            Tracking(image);
        }

        public Mat FaceImage
        {
            get => this.m_Face;
            set => this.m_Face = value;
        }

        public string FileName
        {
            get => this.m_fileName;
            set => this.m_fileName = value;
        }

        public Rectangle ROI
        {
            get
            {
                try
                {
                    this.m_ROILock.EnterReadLock();
                    return this.m_ROI;
                }
                finally
                {
                    this.m_ROILock.ExitReadLock();
                }
            }
            set
            {
                try
                {
                    this.m_ROILock.EnterWriteLock();
                    this.m_ROI = value;
                }
                finally
                {
                    this.m_ROILock.ExitWriteLock();
                }
            } 
        }

        public bool Tracked => this.m_Tracked;

        public Tracker Tracker
        {
            get => this.m_Tracker;
            set => this.m_Tracker = value;
        }

        public Camera MCaptureCamera
        {
            get
            {
                return this.m_captureCamera;
            }
            set
            {
                this.m_captureCamera = value;
            }
        }

        public void FlipTracker()
        {
            this.m_Tracked = !this.m_Tracked;
        }

        public void Tracking(Mat image)
        {
            Controller.Instance.Trackers.Add(new TrackerBoosting());
            Controller.Instance.Trackers.Last().Init(image, this.m_ROI);
            Tracker = Controller.Instance.Trackers.Last();
            FlipTracker();
        }

        public bool UpdateTracker(Mat frame)
        {
            Rectangle r = new Rectangle();
            bool success = false;
            try
            {
                success = this.m_Tracker.Update(frame, out r);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            

            ROI = r;

            return success;
        }

        public void CheckForCollition()
        {
            Face[] faces = Controller.Instance.Faces.ToArray();

            List<Face> facesList = faces.ToList();

            facesList.Remove(this);

            foreach (Face f in facesList)
            {
                if (this.m_ROI.IntersectsWith(f.ROI))
                {
                    Controller.Instance.Faces.Remove(f);
                }
            }
        }
    }
}