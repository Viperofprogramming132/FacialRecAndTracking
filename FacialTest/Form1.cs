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
        


        int[] theObject = new int[2] { 0, 0 };

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Controller.Instance.ReadAllFrames();
        }
        


        /*void searchForMovement(Mat thresholdImage)
        {
            //notice how we use the '&' operator for the cameraFeed. This is because we wish
            //to take the values passed into the function and manipulate them, rather than just working with a copy.
            //eg. we draw to the cameraFeed in this function which is then displayed in the main() function.
            bool objectDetected = false;
            Mat temp;
            temp = thresholdImage;
            //these two vectors needed for output of findContours
            Emgu.CV.Util.VectorOfVectorOfPoint contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
            Mat hierarchy = new Mat();
            //find contours of filtered image using openCV findContours function
            //findContours(temp,contours,hierarchy,CV_RETR_CCOMP,CV_CHAIN_APPROX_SIMPLE );// retrieves all contours
            CvInvoke.FindContours(temp, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);// retrieves external contours

            //if contours vector is not empty, we have found some objects
            if (contours.Size > 0) objectDetected = true;
            else objectDetected = false;

            if (objectDetected)
            { 
                //the largest contour is found at the end of the contours vector
                //we will simply assume that the biggest contour is the object we are looking for.
                Emgu.CV.Util.VectorOfVectorOfPoint largestContourVec = new Emgu.CV.Util.VectorOfVectorOfPoint();
                largestContourVec.Push(contours[contours.Size - 1]);
                //make a bounding rectangle around the largest contour then find its centroid
                //this will be the object's final estimated position.
                objectBoundingRectangle = CvInvoke.BoundingRectangle(largestContourVec[0]);
                int xpos = objectBoundingRectangle.X + objectBoundingRectangle.Width / 2;
                int ypos = objectBoundingRectangle.Y + objectBoundingRectangle.Height / 2;

                //update the objects positions by changing the 'theObject' array values
                theObject[0] = xpos;
                theObject[1] = ypos;
            }
            //make some temp x and y variables so we dont have to type out so much
            int x = theObject[0];
            int y = theObject[1];
            //draw some crosshairs on the object
            CvInvoke.Circle(image, new Point(x, y), 20, new MCvScalar(0, 255, 0), 2);
            CvInvoke.Line(image, new Point(x, y), new Point(x, y - 25), new MCvScalar(0, 255, 0), 2);
            CvInvoke.Line(image, new Point(x, y), new Point(x, y + 25), new MCvScalar(0, 255, 0), 2);
            CvInvoke.Line(image, new Point(x, y), new Point(x - 25, y), new MCvScalar(0, 255, 0), 2);
            CvInvoke.Line(image, new Point(x, y), new Point(x + 25, y), new MCvScalar(0, 255, 0), 2);
            CvInvoke.PutText(image, "Tracking object at (" + x + "," + y + ")", new Point(x, y), FontFace.HersheyComplex, 1, new MCvScalar(255, 0, 0), 2);



        }*/



        

        

        private  void pictureBox1_Click(object sender, EventArgs e)
        {
            
            /*while (true)
            {


                v.Read(image);

                long detectionTime;
                List<Rectangle> faces = new List<Rectangle>();
                List<Rectangle> eyes = new List<Rectangle>();

                DetectFace.Detect(
                    image, "E:\\FaceTest\\FaceTest\\bin\\Debug\\haarcascade_frontalface_default.xml",
                    "E:\\FaceTest\\FaceTest\\bin\\Debug\\haarcascade_eye.xml",
                    faces, eyes,
                    out detectionTime);

                foreach (Rectangle face in faces)
                    CvInvoke.Rectangle(image, face, new Bgr(Color.Red).MCvScalar, 2);
                foreach (Rectangle eye in eyes)
                    CvInvoke.Rectangle(image, eye, new Bgr(Color.Blue).MCvScalar, 2);

                //display the image 
                /*using (InputArray iaImage = image.GetInputArray())
                    ImageViewer.Show(image, String.Format(
                        "Completed face and eye detection using {0} in {1} milliseconds",
                        (iaImage.Kind == InputArray.Type.CudaGpuMat && CudaInvoke.HasCuda) ? "CUDA" :
                        (iaImage.IsUMat && CvInvoke.UseOpenCL) ? "OpenCL"
                        : "CPU",
                        detectionTime));
                await Task.Delay(1000);
                pictureBox1.Image = image.Bitmap;
            }*/
        }

        

        
    }
}
