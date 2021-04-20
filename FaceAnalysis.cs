using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.Calib3dModule;
/// <summary>
/// The Person Group object
/// </summary>
public class Group_RootObject
{
    public string personGroupId { get; set; }
    public string name { get; set; }
    public object userData { get; set; }
}

/// <summary>
/// The Person Face object
/// </summary>
public class Face_RootObject
{
    public string faceId { get; set; }
    public FaceRectangle faceRectangle { get; set;}
    public FaceLandmarks faceLandmarks { get; set; }
}
public class FaceRectangle
{
    public float top { get; set; }
    public float left { get; set; }
    public float width { get; set; }
    public float height { get; set; }

}
public class FaceLandmarks
{
    public Vector2 pupilLeft { get; set; }
    public Vector2 pupilRight { get; set; }
    public Vector2 noseTip { get; set; }
    public Vector2 mouthLeft { get; set; }
    public Vector2 mouthRight { get; set; }
}

/// <summary>
/// Collection of faces that needs to be identified
/// </summary>
public class FacesToIdentify_RootObject
{
    public string personGroupId { get; set; }
    public List<string> faceIds { get; set; }
    public int maxNumOfCandidatesReturned { get; set; }
    public double confidenceThreshold { get; set; }
}

/// <summary>
/// Collection of Candidates for the face
/// </summary>
public class Candidate_RootObject
{
    public string faceId { get; set; }
    public List<Candidate> candidates { get; set; }
}

public class Candidate
{
    public string personId { get; set; }
    public double confidence { get; set; }
}

/// <summary>
/// Name and Id of the identified Person
/// </summary>
public class IdentifiedPerson_RootObject
{
    public string personId { get; set; }
    public string name { get; set; }
}



public class FaceAnalysis : MonoBehaviour
{


    /// <summary>
    /// Allows this class to behave like a singleton
    /// </summary>
    public static FaceAnalysis Instance;

    /// <summary>
    /// The analysis result text
    /// </summary>
    private TextMesh labelText;
    private TextMesh labelTextLandmarks;
    /// <summary>
    /// Bytes of the image captured with camera
    /// </summary>
    internal byte[] imageBytes;
    internal Texture2D targetTexture;
    /// <summary>
    /// Path of the image captured with camera
    /// </summary>
    internal string imagePath;

    /// <summary>
    /// Base endpoint of Face Recognition Service
    /// </summary>
    const string baseEndpoint = "https://buptface.cognitiveservices.azure.com/face/v1.0/";

    /// <summary>
    /// Auth key of Face Recognition Service
    /// </summary>
    private const string key = "e359615304804072a91a8592e6806103";

    /// <summary>
    /// Id (name) of the created person group 
    /// </summary>
    private int sentCount = 0;

    Vector2 pupilLeft ;
    Vector2 pupilRight;
    Vector2 noseTip;
    Vector2 mouthLeft;
    Vector2 mouthRight;


    public Face_RootObject[] faceRectangle_RootObject;
    // Start is called before the first frame update

    // Update is called once per frame

    Mat webCamTextureMat;
    MatOfPoint3f objectPoints;
    MatOfPoint2f imagePoints;
    Mat rvec;
    Mat tvec;
    Mat rotM;

    public object OpenCVForUnityUtils { get; private set; }

    ParticleSystem[] mouthParticleSystem;
    Texture2D texture;
    public Camera ARCamera;
    Mat camMatrix;
    MatOfDouble distCoeffs;
    Matrix4x4 invertYM;
    Matrix4x4 transformationM = new Matrix4x4();
    Matrix4x4 invertZM;
    Matrix4x4 ARM;
    public GameObject ARGameObject;


    public GameObject axes;

    /// <summary>
    /// The head. 头部
    /// </summary>
    public GameObject head;

    /// <summary>
    /// The right eye.右眼
    /// </summary>
    public GameObject rightEye;

    /// <summary>
    /// The left eye.左眼
    /// </summary>
    public GameObject leftEye;

    /// <summary>
    /// The mouth. 嘴巴
    /// </summary>
    public GameObject mouth;


    public bool shouldMoveARCamera;

    bool state_mode;

    void Start()
    {
        Debug.Log("我开始了！！！");
        Run();

    }




    private void Awake()
    {
        Debug.Log("我觉醒了！！！");
        // Allows this instance to behave like a singleton
        Instance = this;

        // Add the ImageCapture Class to this Game Object
       gameObject.AddComponent<ImageCapture>();

        // Create the text label in the scene
        //CreateRect();
       
       //StartCoroutine(FaceAnalysis.Instance.DetectFacesFromImage());
    }


    private void Run()
    {
        //set 3d face object points.
        objectPoints = new MatOfPoint3f(
            new Point3(-31, 72, 86),//l eye
            new Point3(31, 72, 86),//r eye
            new Point3(0, 40, 114),//nose
            new Point3(-20, 15, 90),//l mouse
            new Point3(20, 15, 90)//r mouse
        );
        imagePoints = new MatOfPoint2f();
        rvec = new Mat();
        tvec = new Mat();
        rotM = new Mat(3, 3, CvType.CV_64FC1);
        OnWebCamTextureToMatHelperInited();
    }

    /// <summary>
    /// Raises the web cam texture to mat helper inited event.
    /// </summary>
    public void OnWebCamTextureToMatHelperInited()
    {
        Debug.Log("我OnWebCamTextureToMatHelperInited了！！！");
        LoadByIO();
        if (true)
        {
            Debug.Log("我进来校准相机了相机了像极了");
            // gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            float width = 1280;
            float height = 720;

            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                imageSizeScale = (float)Screen.height / (float)Screen.width;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }


            //set cameraparam
            //参数都是拍摄的画面的
            int max_d = (int)Mathf.Max(width, height);
            double fx = max_d;
            double fy = max_d;
            double cx = width / 2.0f;
            double cy = height / 2.0f;
            camMatrix = new Mat(3, 3, CvType.CV_64FC1);
            camMatrix.put(0, 0, fx);
            camMatrix.put(0, 1, 0);
            camMatrix.put(0, 2, cx);
            camMatrix.put(1, 0, 0);
            camMatrix.put(1, 1, fy);
            camMatrix.put(1, 2, cy);
            camMatrix.put(2, 0, 0);
            camMatrix.put(2, 1, 0);
            camMatrix.put(2, 2, 1.0f);
            Debug.Log("camMatrix " + camMatrix.dump());


            distCoeffs = new MatOfDouble(0, 0, 0, 0);
            Debug.Log("distCoeffs " + distCoeffs.dump());


            //calibration camera
            //校准相机
            Size imageSize = new Size(width * imageSizeScale, height * imageSizeScale);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point(0, 0);
            double[] aspectratio = new double[1];

            Calib3d.calibrationMatrixValues(camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);

            Debug.Log("imageSize " + imageSize.ToString());
            Debug.Log("apertureWidth " + apertureWidth);
            Debug.Log("apertureHeight " + apertureHeight);
            Debug.Log("fovx " + fovx[0]);
            Debug.Log("fovy " + fovy[0]);
            Debug.Log("focalLength " + focalLength[0]);
            Debug.Log("principalPoint " + principalPoint.ToString());
            Debug.Log("aspectratio " + aspectratio[0]);


            //To convert the difference of the FOV value of the OpenCV and Unity. 
            double fovXScale = (2.0 * Mathf.Atan((float)(imageSize.width / (2.0 * fx)))) / (Mathf.Atan2((float)cx, (float)fx) + Mathf.Atan2((float)(imageSize.width - cx), (float)fx));
            double fovYScale = (2.0 * Mathf.Atan((float)(imageSize.height / (2.0 * fy)))) / (Mathf.Atan2((float)cy, (float)fy) + Mathf.Atan2((float)(imageSize.height - cy), (float)fy));

            Debug.Log("fovXScale " + fovXScale);
            Debug.Log("fovYScale " + fovYScale);


            //Adjust Unity Camera FOV https://github.com/opencv/opencv/commit/8ed1945ccd52501f5ab22bdec6aa1f91f1e2cfd4
            if (widthScale < heightScale)
            {
                ARCamera.fieldOfView = (float)(fovx[0] * fovXScale);
            }
            else
            {
                ARCamera.fieldOfView = (float)(fovy[0] * fovYScale);
            }



            invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
            Debug.Log("invertYM " + invertYM.ToString());

            invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
            Debug.Log("invertZM " + invertZM.ToString());


            axes.SetActive(false);
            head.SetActive(false);
            rightEye.SetActive(false);
            leftEye.SetActive(false);
            mouth.SetActive(false);


            mouthParticleSystem = mouth.GetComponentsInChildren<ParticleSystem>(true);
        }
        else
        {
            Debug.Log("state_mode------false！！！！");
        }
    }

    /// <summary>
    /// Raises the web cam texture to mat helper disposed event.
    /// </summary>
    public void OnWebCamTextureToMatHelperDisposed()
    {
        Debug.Log("我OnWebCamTextureToMatHelperDisposed了！！！！");

        camMatrix.Dispose();
        distCoeffs.Dispose();
    }


    private void CreateRect()
    {

        GameObject newLabel = new GameObject();

        // Attach the label to the Main Camera
        newLabel.transform.parent = gameObject.transform;

        // Resize and position the new cursor
        newLabel.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        newLabel.transform.position = new Vector3(0f, 14f, 60f);

        // Creating the text of the Label
        labelText = newLabel.AddComponent<TextMesh>();
        labelText.anchor = TextAnchor.MiddleCenter;
        labelText.alignment = TextAlignment.Center;
        labelText.tabSize = 4;
        labelText.fontSize = 50;
        labelText.text = "你是谁你是谁你是谁";
    }



   /*
    private void CreateLabel()
    {


        GameObject newLabelmarks = new GameObject();

        // Attach the label to the Main Camera
        newLabelmarks.transform.parent = gameObject.transform;

        // Resize and position the new cursor
        newLabelmarks.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        newLabelmarks.transform.position = new Vector3(0f, 3f, 60f);

        // Creating the text of the Label
        labelTextLandmarks = newLabelmarks.AddComponent<TextMesh>();
        labelTextLandmarks.anchor = TextAnchor.MiddleCenter;
        labelTextLandmarks.alignment = TextAlignment.Center;
        labelTextLandmarks.tabSize = 4;
        labelTextLandmarks.fontSize = 25;
        labelTextLandmarks.text = "左瞳孔";
    }
    */

    void LoadByIO()
    {
        if (imagePath != null)
        {
            Debug.Log("LoadByIO的图片path：----" + imagePath);
            float time = Time.time;
            FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            fs.Seek(0, SeekOrigin.Begin);
            byte[] bytes = new byte[fs.Length];
            fs.Read(bytes, 0, (int)fs.Length);
            fs.Close();
            fs.Dispose();
            fs = null;
            //Debug.Log("main_camera" + targetTexture.height + "main_camera" + targetTexture.width);
            Texture2D t = new Texture2D(targetTexture.height, targetTexture.width);//!!!!!要改
            t.LoadImage(bytes);
            //gameObject.GetComponent<Renderer>().material.mainTexture = t;
            webCamTextureMat = new Mat(t.height, t.width, CvType.CV_8UC4);
           // Debug.Log(" webCamTextureMat " + webCamTextureMat.width());
            Utils.texture2DToMat(t, webCamTextureMat);
           // Debug.Log("让我看看你是不是空了：" + (webCamTextureMat == null).ToString());
            //webCamTextureMat = new Mat(t.height, t.width, CvType.CV_8UC4, t.GetRawTextureData());
            state_mode = true;
        }
        else
        {
            Debug.Log("没有图片啊");
            state_mode = false;

        }
    }

    /// <summary>
    /// Detect faces from a submitted image
    /// </summary>
    internal IEnumerator DetectFacesFromImage()
    {
        sentCount++;
        WWWForm webForm = new WWWForm();
        string detectFacesEndpoint = $"{baseEndpoint}detect?returnFaceId=true&returnFaceLandmarks=true&recognitionModel=recognition_01&returnRecognitionModel=false&detectionModel=detection_01";
        //imagePath = Application.streamingAssetsPath + @"/face1.jpg";
        imageBytes = GetImageAsByteArray(imagePath);
        //Debug.Log("pathpathpath:  " + imagePath);
        LoadByIO();
        using (UnityWebRequest www =
            UnityWebRequest.Post(detectFacesEndpoint, webForm))
        {
            www.SetRequestHeader("Ocp-Apim-Subscription-Key", key);
            www.SetRequestHeader("Content-Type", "application/octet-stream");
            www.uploadHandler.contentType = "application/octet-stream";
            www.uploadHandler = new UploadHandlerRaw(imageBytes);
            www.downloadHandler = new DownloadHandlerBuffer();

            // yield return null;
            yield return www.SendWebRequest();

            Debug.Log("SEND!!!!" + sentCount);
            string jsonResponse = www.downloadHandler.text;
            Debug.Log("jsonResponse:   " + jsonResponse);
            Face_RootObject[] faceRectangle_RootObject =
                JsonConvert.DeserializeObject<Face_RootObject[]>(jsonResponse);
            // Debug.Log(faceRectangle_RootObject);
            // Debug.Log(faceRectangle_RootObject[0].faceId);
            //Debug.Log(faceRectangle_RootObject[0].faceRectangle.top);
            Debug.Log("返回的json有多长？？？？：  " + faceRectangle_RootObject.Length);
            // Create a list with the face Ids of faces detected in image
            if (faceRectangle_RootObject.Length != 0)
            {
                pupilLeft = faceRectangle_RootObject[0].faceLandmarks.pupilLeft;

                pupilRight = faceRectangle_RootObject[0].faceLandmarks.pupilRight;

                noseTip = faceRectangle_RootObject[0].faceLandmarks.noseTip;

                mouthLeft = faceRectangle_RootObject[0].faceLandmarks.mouthLeft;

                mouthRight = faceRectangle_RootObject[0].faceLandmarks.mouthRight;

                //labelText.text = faceRectangle_RootObject[0].faceId;

                Mat rgbaMat = webCamTextureMat;

                imagePoints.fromArray(
                    new Point(pupilLeft.x, pupilLeft.y),//l eye
                    new Point(pupilRight.x, pupilRight.y),//r eye
                    new Point(noseTip.x, noseTip.y),//nose
                    new Point(mouthLeft.x, mouthLeft.y),//l mouth
                    new Point(mouthRight.x, mouthRight.y)//r mouth               
                );

                //Debug.Log("objectPoints：" + (objectPoints==null).ToString());
               // Debug.Log("imagePoints：" + (imagePoints == null).ToString());
               // Debug.Log("camMatrix ：" + (camMatrix == null).ToString());
                //Debug.Log(" distCoeffs：" + (distCoeffs == null).ToString());
               // Debug.Log("rvec：" + (rvec == null).ToString());
               // Debug.Log("tvec：" + (tvec == null).ToString());
                Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);
                /*
                    //眼睛的特效
                    if (tvec.get(2, 0)[0] > 0)
                    {

                        if (Mathf.Abs((float)(points[43].y - points[46].y)) > Mathf.Abs((float)(points[42].x - points[45].x)) / 6.0)
                        {
                            if (isShowingEffects)
                                rightEye.SetActive(true);
                        }

                        if (Mathf.Abs((float)(points[38].y - points[41].y)) > Mathf.Abs((float)(points[39].x - points[36].x)) / 6.0)
                        {
                            if (isShowingEffects)
                                leftEye.SetActive(true);
                        }
                        if (isShowingHead)
                            head.SetActive(true);
                        if (isShowingAxes)
                            axes.SetActive(true);

                        //嘴部特效                    
                        float noseDistance = Mathf.Abs((float)(points[27].y - points[33].y));
                        float mouseDistance = Mathf.Abs((float)(points[62].y - points[66].y));
                        if (mouseDistance > noseDistance / 5.0)
                        {
                            if (isShowingEffects)
                            {
                                mouth.SetActive(true);
                                foreach (ParticleSystem ps in mouthParticleSystem)
                                {
                                    ps.enableEmission = true;
                                    ps.startSize = 500 * (mouseDistance / noseDistance);
                                }
                            }
                        }
                        else
                        {
                            if (isShowingEffects)
                            {
                                foreach (ParticleSystem ps in mouthParticleSystem)
                                {
                                    ps.enableEmission = false;
                                }
                            }
                        }
                        */

                Calib3d.Rodrigues(rvec, rotM);

                transformationM.SetRow(0, new Vector4((float)rotM.get(0, 0)[0], (float)rotM.get(0, 1)[0], (float)rotM.get(0, 2)[0], (float)tvec.get(0, 0)[0]));
                transformationM.SetRow(1, new Vector4((float)rotM.get(1, 0)[0], (float)rotM.get(1, 1)[0], (float)rotM.get(1, 2)[0], (float)tvec.get(1, 0)[0]));
                transformationM.SetRow(2, new Vector4((float)rotM.get(2, 0)[0], (float)rotM.get(2, 1)[0], (float)rotM.get(2, 2)[0], (float)tvec.get(2, 0)[0]));
                transformationM.SetRow(3, new Vector4(0, 0, 0, 1));

                if (shouldMoveARCamera)
                {

                    if (ARGameObject != null)
                    {
                        ARM = ARGameObject.transform.localToWorldMatrix * invertZM * transformationM.inverse * invertYM;
                        ARUtils.SetTransformFromMatrix(ARCamera.transform, ref ARM);
                        ARGameObject.SetActive(true);

                    }
                }
                else
                {
                    ARM = ARCamera.transform.localToWorldMatrix * invertYM * transformationM * invertZM;
                    Debug.Log("ARGameObject有东西吗？？？：" + ARGameObject);
                    if (ARGameObject != null)
                    {
                        Debug.Log("ARGameObject有东西吗？？？：" + ARGameObject);
                        ARUtils.SetTransformFromMatrix(ARGameObject.transform, ref ARM);
                        ARGameObject.SetActive(true);
                        Debug.Log("我Active兔子耳朵了！！！");
                    }
                }

            }

        }
        }


    /// <summary>
    /// Returns the contents of the specified file as a byte array.
    /// </summary>
    static byte[] GetImageAsByteArray(string imageFilePath)
    {
        FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
        BinaryReader binaryReader = new BinaryReader(fileStream);
        Debug.Log("binary!!!");
        return binaryReader.ReadBytes((int)fileStream.Length);
    }

}
