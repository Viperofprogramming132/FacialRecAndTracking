// Project: FacialTest
// Filename; Program.cs
// Created; 10/08/2018
// Edited: 04/09/2018

namespace FacialTest
{
    using System;
    using System.Windows.Forms;


    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }



    }
}