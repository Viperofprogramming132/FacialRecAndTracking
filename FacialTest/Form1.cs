using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
    public partial class Form1 : Form
    {
        private Controller TrackingController = Controller.Instance;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TrackingController.ReadAllFrames();
        }
    }
}
