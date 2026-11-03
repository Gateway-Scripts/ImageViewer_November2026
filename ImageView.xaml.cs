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

        private unsafe void GenerateImage()
        {
            int width = _image.XSize;
            int height = _image.YSize;
            int totalPixels = width * height;

            // Get voxel data from ESAPI
            int[,] pixels = new int[height, width];
            _image.GetVoxels(_sliceNumber, pixels);

            // Find min/max using pointer arithmetic over the 2D array's contiguous memory
            int drr_min = int.MaxValue;
            int drr_max = int.MinValue;
            fixed (int* pPixels = pixels)
            {
                int* p = pPixels;
                for (int i = 0; i < totalPixels; i++, p++)
                {
                    if (*p < drr_min) drr_min = *p;
                    if (*p > drr_max) drr_max = *p;
                }
            }

            // Build the byte array using pointer arithmetic — no intermediate flat array needed
            PixelFormat format = PixelFormats.Gray8;
            int stride = (width * format.BitsPerPixel + 7) / 8;
            byte[] image_bytes = new byte[stride * height];
            double range = drr_max - drr_min;
            fixed (int* pPixels = pixels)
            fixed (byte* pBytes = image_bytes)
            {
                int* pSrc = pPixels;
                byte* pDst = pBytes;
                for (int i = 0; i < totalPixels; i++, pSrc++, pDst++)
                {
                    *pDst = (byte)(255 * (*pSrc - drr_min) / range);
                }
            }

            // Build the BitmapSource and correct the 90-degree rotation
            BitmapSource bitmapSource = BitmapSource.Create(width, height, 25.4 / _image.XRes, 25.4 / _image.YRes, format, null, image_bytes, stride);
            TransverseImage.Source = new TransformedBitmap(bitmapSource, new RotateTransform(90));
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            _sliceNumber--;
            //what happens if the slice number is already 0? can't have -1
            if (_sliceNumber < 0)
            {
                _sliceNumber = _image.ZSize - 1;
            }
            GenerateImage();
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            _sliceNumber++;
            if(_sliceNumber>= _image.ZSize)
            {
                _sliceNumber = 0;
            }
            GenerateImage();
        }
    }
}
