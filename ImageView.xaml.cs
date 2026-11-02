using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageViewer
{
    /// <summary>
    /// Interaction logic for ImageView.xaml
    /// </summary>
    public partial class ImageView : UserControl
    {
        private VMS.TPS.Common.Model.API.Image _image;
        private int _sliceNumber;

        //Constructor =>What information do I need from ESAPI to produce this image.
        public ImageView(VMS.TPS.Common.Model.API.Image image)
        {
            InitializeComponent();
            //Set a couple of variables to allow access to my image and slice number in other methods.
            _image = image;
            _sliceNumber = image.ZSize / 2;
            GenerateImage();
        }

        private void GenerateImage()
        {
            //BitmapSource bitmapSource = null;
            int[,] pixels = new int[_image.YSize, _image.XSize];
            _image.GetVoxels(_sliceNumber, pixels);
            int[] flat_pixels = new int[_image.YSize * _image.XSize];
            //lay out pixels into single array
            for (int i = 0; i < _image.YSize; i++)
            {
                for (int ii = 0; ii < _image.XSize; ii++)
                {
                    flat_pixels[i + _image.XSize * ii] = pixels[i, ii];
                }
            }
            //translate into byte array
            var drr_max = flat_pixels.Max();
            var drr_min = flat_pixels.Min();
            PixelFormat format = PixelFormats.Gray8;//low res image, but only 1 byte per pixel. 
            int stride = (_image.XSize * format.BitsPerPixel + 7) / 8;
            byte[] image_bytes = new byte[stride * _image.YSize];
            for (int i = 0; i < flat_pixels.Length; i++)
            {
                double value = flat_pixels[i];
                image_bytes[i] = Convert.ToByte(255 * ((value - drr_min) / (drr_max - drr_min)));
            }
            //build the bitmapsource.
            TransverseImage.Source = BitmapSource.Create(_image.XSize, _image.YSize, 25.4 / _image.XRes, 25.4 / _image.YRes, format, null, image_bytes, stride);
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
