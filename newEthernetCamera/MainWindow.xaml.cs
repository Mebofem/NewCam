using System;
using System.Windows;
using AVT.VmbAPINET;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows.Media.Imaging;
using Window = System.Windows.Window;

namespace newEthernetCamera
{
    public partial class MainWindow : Window
    {
        private Vimba _vimba;
        private Camera _camera;

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
                MessageBox.Show("Vimba started successfully.");

                CameraCollection cameras = _vimba.Cameras;
                MessageBox.Show("Cameras found: " + cameras.Count);

                if (cameras.Count > 0)
                {
                    _camera = cameras[0];
                    _camera.Open(VmbAccessModeType.VmbAccessModeFull);
                    MessageBox.Show("Camera opened with full access: " + _camera.Id);

                    SetupCameraForCapture();
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

        private void SetupCameraForCapture()
        {
            try
            {
                if (_camera.Features["PayloadSize"].IsReadable())
                {
                    long payloadSize = _camera.Features["PayloadSize"].IntValue;
                    MessageBox.Show("Payload size retrieved: " + payloadSize);

                    Frame frame = new Frame(payloadSize);
                    _camera.AnnounceFrame(frame);
                    MessageBox.Show("Frame announced.");

                    _camera.QueueFrame(frame);
                    MessageBox.Show("Frame queued.");

                    _camera.OnFrameReceived += OnFrameReceived;
                    MessageBox.Show("FrameReceived event handler added.");

                    _camera.StartCapture();
                    MessageBox.Show("Capture started.");

                    if (_camera.Features["AcquisitionStart"].IsWritable())
                    {
                        _camera.Features["AcquisitionStart"].RunCommand();
                        MessageBox.Show("Acquisition started.");
                    }
                    else
                    {
                        MessageBox.Show("AcquisitionStart feature is not writable.");
                    }
                }
                else
                {
                    MessageBox.Show("PayloadSize feature is not readable.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting up camera for capture: " + ex.Message);
            }
        }


        private void OnFrameReceived(Frame frame)
        {
            try
            {
                if (frame.ReceiveStatus == VmbFrameStatusType.VmbFrameStatusComplete && frame.Buffer != null)
                {
                    //MessageBox.Show("Frame received successfully.");
                    ProcessFrame(frame);
                    _camera.QueueFrame(frame);
                }
                else
                {
                    MessageBox.Show("Received incomplete frame or frame with errors.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error processing frame: " + ex.Message);
            }
        }

        private void ProcessFrame(Frame frame)
        {
            // Check if we need to invoke on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                // Use the Dispatcher to invoke the processing on the UI thread
                Dispatcher.Invoke(() =>
                {
                    ProcessFrame(frame);
                });
            }
            else
            {
                // Processing on the UI thread
                var mat = new Mat((int)frame.Height, (int)frame.Width, MatType.CV_8UC1, frame.Buffer);
                var bitmapSource = BitmapSourceConverter.ToBitmapSource(mat);
                imgFrame.Source = bitmapSource;
            }
        }


        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ShutdownCamera();
        }

        private void ShutdownCamera()
        {
            if (_camera != null)
            {
                try
                {
                    _camera.StopContinuousImageAcquisition();
                    MessageBox.Show("Continuous image acquisition stopped.");

                    _camera.OnFrameReceived -= OnFrameReceived;
                    MessageBox.Show("FrameReceived event handler removed.");

                    _camera.Close();
                    MessageBox.Show("Camera closed.");

                    _camera = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error shutting down camera: " + ex.Message);
                }
            }

            if (_vimba != null)
            {
                try
                {
                    _vimba.Shutdown();
                    MessageBox.Show("Vimba shut down.");
                    _vimba = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error shutting down Vimba: " + ex.Message);
                }
            }
        }
    }
}