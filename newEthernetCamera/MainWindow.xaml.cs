using System;
using System.Windows;
using AVT.VmbAPINET;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows.Threading;
using Window = System.Windows.Window;
using System.Windows.Media.Imaging;

namespace newEthernetCamera
{
    public partial class MainWindow : Window
    {
        private Vimba _vimba;
        private Camera _camera;
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeCamera();
        }

        private void InitializeCamera()
        {
            try
            {
                _vimba = new Vimba();
                _vimba.Startup();
                CameraCollection cameras = _vimba.Cameras;

                if (cameras.Count > 0)
                {
                    _camera = cameras[0];
                    _camera.Open(VmbAccessModeType.VmbAccessModeFull);
                    Console.WriteLine("Camera opened: " + _camera.Id);
                    _camera.OnFrameReceived += OnFrameReceived;
                    _camera.StartContinuousImageAcquisition(10);
                    Console.WriteLine("Acquisition started");
                }
                else
                {
                    MessageBox.Show("No cameras found.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Initialization failed: " + ex.Message);
            }
        }

        private void OnFrameReceived(Frame frame)
        {
            Dispatcher.Invoke(() =>
            {
                if (frame.ReceiveStatus == VmbFrameStatusType.VmbFrameStatusComplete && frame.Buffer != null)
                {
                    if (frame.BufferSize == frame.Width * frame.Height) // Ensuring buffer size matches the expected size for monochrome
                    {
                        int width = (int)frame.Width;
                        int height = (int)frame.Height;

                        // Creating a Mat object assuming the image is monochrome
                        Mat mat = new Mat(height, width, MatType.CV_8UC1, frame.Buffer);

                        // Convert the Mat to a BitmapSource for display
                        BitmapSource bitmapSource = BitmapSourceConverter.ToBitmapSource(mat);

                        // Update the image on the UI thread
                        imgFrame.Source = bitmapSource;
                    }
                    else
                    {
                        MessageBox.Show($"Unexpected frame buffer size: {frame.BufferSize}, expected: {frame.Width * frame.Height}");
                    }

                    // Re-queue the frame to continue receiving frames
                    _camera.QueueFrame(frame);
                }
            });
        }





        protected override void OnClosed(EventArgs e)
        {
            if (_camera != null)
            {
                _camera.StopContinuousImageAcquisition();
                _camera.OnFrameReceived -= OnFrameReceived;
                _camera.Close();
            }

            if (_vimba != null)
            {
                _vimba.Shutdown();
            }

            base.OnClosed(e);
        }
    }
}
