// Project: FacialTest
// Filename; Face.cs
// Created; 14/08/2018
// Edited: 04/09/2018

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

        public Face(
            Mat face,
            string fileName,
            List<Rectangle> ROIs,
            int Index,
            Camera captureCamera)
        {
            this.m_Face = face;
            this.m_fileName = fileName;
            this.m_captureCamera = captureCamera;
            captureCamera.Tracking(this, ROIs[Index]);
            this.m_ROI = ROIs[Index];
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
            get => this.m_ROI;
            set => this.m_ROI = value;
        }

        public bool Tracked => this.m_Tracked;

        public Tracker Tracker
        {
            get => this.m_Tracker;
            set => this.m_Tracker = value;
        }

        public void FlipTracker()
        {
            this.m_Tracked = !this.m_Tracked;
        }
    }
}