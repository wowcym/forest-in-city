using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.XR.WSA.Input;

public class ImageCapture : MonoBehaviour
{
    /// <summary>
    /// Allows this class to behave like a singleton
    /// </summary>
    public static ImageCapture instance;
   
    /// <summary>
    /// Keeps track of tapCounts to name the captured images 
    /// </summary>
    private int tapsCount = 0;

    private bool state = false;

    /// <summary>
    /// PhotoCapture object used to capture images on HoloLens 
    /// </summary>
    private UnityEngine.Windows.WebCam.PhotoCapture photoCaptureObject = null;

    /// <summary>
    /// HoloLens class to capture user gestures
    /// </summary>
    private GestureRecognizer recognizer;


    float timeUse;

    /// <summary>
    /// Initialises this class
    /// </summary>
    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Called right after Awake
    /// </summary>
    void Start()
    {
        // Initialises user gestures capture 
        recognizer = new GestureRecognizer();
         recognizer.SetRecognizableGestures(GestureSettings.Tap);
         recognizer.Tapped += TapHandler;
        recognizer.StartCapturingGestures();
      
    }
    void Update()
    {
        TimeCount();
        if (timeUse > 0.5)
        {
            if (state == false)
            {
                state = true;
                tapsCount++;
                //Debug.Log("第几次：" + tapsCount);
                ExecuteImageCaptureAndAnalysis();
            }
            timeUse = 0;
        }
    }
    /// <summary>
    /// Respond to Tap Input.
    /// </summary>
    /// 

    void TimeCount()
    {
        timeUse += Time.deltaTime;
    }

    private void TapHandler(TappedEventArgs obj)
    {
       //tapsCount++;
        //ExecuteImageCaptureAndAnalysis();
    }
  
    /// <summary>
    /// Begin process of Image Capturing and send To Azure Computer Vision service.
    /// </summary>
    private void ExecuteImageCaptureAndAnalysis()
    {
        Resolution cameraResolution = UnityEngine.Windows.WebCam.PhotoCapture.SupportedResolutions.OrderByDescending
            ((res) => res.width * res.height).First();
        FaceAnalysis.Instance.targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

        UnityEngine.Windows.WebCam.PhotoCapture.CreateAsync(false, delegate (UnityEngine.Windows.WebCam.PhotoCapture captureObject)
        {
            photoCaptureObject = captureObject;

            UnityEngine.Windows.WebCam.CameraParameters c = new UnityEngine.Windows.WebCam.CameraParameters();
            c.hologramOpacity = 0.0f;
            c.cameraResolutionWidth = FaceAnalysis.Instance.targetTexture.width;
            c.cameraResolutionHeight = FaceAnalysis.Instance.targetTexture.height;
            c.pixelFormat = UnityEngine.Windows.WebCam.CapturePixelFormat.BGRA32;

            captureObject.StartPhotoModeAsync(c, delegate (UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result)
            {
                string filename = string.Format(@"CapturedImage{0}.jpg", tapsCount);
                string filePath = Path.Combine(Application.persistentDataPath, filename);

                // Set the image path on the FaceAnalysis class
                FaceAnalysis.Instance.imagePath = filePath;

                photoCaptureObject.TakePhotoAsync
                (filePath, UnityEngine.Windows.WebCam.PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
            });
        });
    }


    /// <summary>
    /// Called right after the photo capture process has concluded
    /// </summary>
    void OnCapturedPhotoToDisk(UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        
    }

    /// <summary>
    /// Register the full execution of the Photo Capture. If successful, it will begin the Image Analysis process.
    /// </summary>
    void OnStoppedPhotoMode(UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
        // Request image caputer analysis
        StartCoroutine(FaceAnalysis.Instance.DetectFacesFromImage());
        state = false;
    }

}
