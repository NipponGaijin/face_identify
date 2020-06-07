﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Cvb;
using Emgu.Util;
using System.Net;

namespace faceRecognition
{
    /// <summary>
    /// Данный класс производит инициализацию веб-камеры
    /// </summary>
    class getWebCam
    {
        private Capture cam = null;

        /// <summary>
        /// Метод получения доступа к веб-камере
        /// </summary>
        public  getWebCam()
        {
            
            try
            {
                if (cam == null)
                {
                    cam = new Capture();
                }
            }
            catch(Exception e)
            {
                return;
            }
        }

        public Capture setCam()
        {
            return cam;
        }
        public void disposeCam()
        {
            cam.Dispose();
        }

    }
}
