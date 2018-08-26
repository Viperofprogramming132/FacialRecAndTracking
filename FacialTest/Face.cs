using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.Tracking;
using System.Drawing;
using Emgu.CV;
using System.Drawing.Imaging;
using Emgu.CV.Structure;

namespace FacialTest
{
    public class Face
    {
        private bool m_Tracked;
        private Tracker m_Tracker;
        private string m_fileName;
        private Image<Gray, byte> m_Face;
        private Rectangle m_ROI;

        public bool Tracked { get { return m_Tracked; } }
        public Tracker Tracker { get { return m_Tracker; } set { m_Tracker = value; } }
        public string FileName { get { return m_fileName; } set { m_fileName = value; } }
        public Image<Gray, byte> FaceImage { get { return m_Face; } set { m_Face = value; } }
        public Rectangle ROI { get { return m_ROI; } set { m_ROI = value; } }

        public Face(Image<Gray, byte> face, string fileName, Image frame, List<Rectangle> ROIs, int Index)
        {
            m_Face = face;
            m_fileName = fileName;
            Controller.Instance.tracking(this, ROIs[Index]);
            m_ROI = ROIs[Index];
        }

        public void FlipTracker()
        {
            m_Tracked = !m_Tracked;
        }
    }
}
