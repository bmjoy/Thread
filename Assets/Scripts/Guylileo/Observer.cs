﻿using OSG;
using OSG.Debug;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using Plane = OSG.Plane;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class Observer : MonoBehaviour
{
    [SerializeField] float longitude;
    [SerializeField] float latitude;
    [SerializeField] float altitude;
    [SerializeField] float cameraSpeed = 0.25f;
    [SerializeField] float bearing = 0;
    [SerializeField] float elevation;
    [Header("UI")]
    [SerializeField] Image compass;
    [SerializeField] TextMeshProUGUI bearingText;
    [SerializeField] TextMeshProUGUI positionText;
    [SerializeField] Slider latitudeSlider;
    [SerializeField] Slider longitudeSlider;
    [Header("MeshControl")]
    [SerializeField] SphereMeshBuilder mesh;
    public bool showHandles;

    Vector3 localLookAt;
    Vector3 lookAtPosition;
    bool fromSlider;

    Vector3 downPosition;
    float downBearing;
    float downElevation;
    Camera _main;

    public Camera main
    {
        get
        {
            if(!_main)
                _main = GetComponent<Camera>();
            return _main;
        }
    }


    void OnEnable()
    {
        SetPos();
    }

    void OnValidate()
    {
        parent = transform.parent;

        if(altitude<0) altitude = 0;
        
        while(longitude < -180)
            longitude += 360;
        while(longitude > 180)
            longitude -= 360;
        while (bearing<0)
        {
            bearing += 360;
        }
        while (bearing>360)
        {
            bearing -= 360;
        }
        latitude = Mathf.Clamp(latitude,-89.9f, 89.9f);
        elevation = Mathf.Clamp(elevation, -90, 90);

        if(!fromSlider)
        {
            if(latitudeSlider)
                latitudeSlider.value = latitude;
            if(longitudeSlider)
                longitudeSlider.value = longitude;
        }
        SetPos();

    }

	void Update ()
	{
	    if(Input.GetMouseButtonDown(1))
	    {
	        downPosition = Input.mousePosition;
            downBearing = bearing;
            downElevation = elevation;
            oldFlags = main.clearFlags;
            if(oldFlags == CameraClearFlags.Nothing)
                main.clearFlags = CameraClearFlags.SolidColor;
	    }

        if(Input.GetMouseButton(1))
        {
            var deltaMouse = cameraSpeed*(Input.mousePosition - downPosition);
            bearing = downBearing + deltaMouse.x;
            elevation = downElevation + deltaMouse.y;
            OnValidate();
        }

        if(Input.GetMouseButtonUp(1))
        {
            main.clearFlags = oldFlags;
        }

        //Camera.main.fieldOfView = 180 * Input.GetAxis("wheel");

	}

    Color[] colors =
    {
        Color.red, Color.green, Color.blue, Color.yellow
    };

    public bool useForwardForBackPlane;

    public bool showBackPlane;
    public bool[] showPlane = new bool[4];
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        //Handles.color = Color.white;
        //Handles.SphereHandleCap(0, lookAtPosition, Quaternion.identity, .025f, Event.current.type);

        //DrawArrow(up, Color.yellow);
        //DrawArrow(north, Color.yellow);
        //DrawArrow(east, Color.yellow);
        //DrawArrow(localLookAt*2, Color.cyan);

        Vector3[] corners = new Vector3[4];
        main.CalculateFrustumCorners(main.rect,
                                     main.farClipPlane,
                                     Camera.MonoOrStereoscopicEye.Mono, 
                                     corners);

        for (var index = 0; index < corners.Length; index++)
        {
            corners[index] = transform.TransformPoint(corners[index]);
        }

        Transform t = transform.parent;
        Vector3 myPosition = transform.position;
        Vector3 center = t.position;


        PlaneSphereIntersection planeInter;
        float meshRadius = mesh.radius;
        Handles.zTest = CompareFunction.LessEqual;
        if(showBackPlane)
        {
            Vector3 planeNormal = useForwardForBackPlane ? -transform.forward : myPosition - center;

            Plane backPlane = new Plane(normal: planeNormal, point: center);
            Handles.color = Color.Lerp(Color.yellow, Color.red, 0.25f);
            backPlane.DrawGizmo(3);
            planeInter = new PlaneSphereIntersection(backPlane, center, meshRadius);
            planeInter.DrawGizmo(0.01f);
        }


        for (int i = 0; i < 4; ++i)
        {
            if(!showPlane[i])
                continue;

            Vector3 p0 = myPosition;
            Vector3 p1 = corners[i];
            Vector3 p2 = corners.Modulo(i+1);

            Plane p = new Plane(p0, p2, p1);
            Handles.color = colors[i];
            
            Handles.DrawPolyLine(p0, p1, p2, p0);


            planeInter = new PlaneSphereIntersection(p, center, meshRadius);
            const float gizmoSize = 0.0125f;
            planeInter.DrawGizmo(gizmoSize);

            RaySphereIntersection rayInter = new RaySphereIntersection(myPosition, p1-myPosition, center, meshRadius);
            rayInter.DrawGizmo(gizmoSize, "P"+(i+1));

            //Quaternion q=new Quaternion();
            //q.SetLookRotation(p.normal);
            //Handles.zTest = CompareFunction.Always;
            //Handles.ArrowHandleCap(0, inter.onPlane, q, 1f, Event.current.type);
        }


    }
#endif
    private void DrawArrow(Vector3 localDirection, Color color)
    {
        Vector3 worldDirection = parent.TransformDirection(localDirection);
        DebugUtils.DrawArrow(transform.position, worldDirection, color);
    }
    
    public Vector3 up,north,east;
    Transform parent;
    private CameraClearFlags oldFlags;
    public Vector3 bearingDirection 
    {
        private set ;get;
    }

    public static Vector3 CoordinatesToNormal(float longitude, float latitude)
    {
        latitude *= Mathf.Deg2Rad;
        longitude *= Mathf.Deg2Rad;
        float cosl = Mathf.Cos(latitude);
        float sinl = Mathf.Sin(latitude);
        float cosL = Mathf.Cos(longitude);
        float sinL = Mathf.Sin(longitude);
        return new Vector3(cosl * cosL, sinl, cosl * sinL);
    }
    private const float r2d=Mathf.Rad2Deg;

    public static Vector2 NormalToCoordinates(Vector3 n)
    {
        float latitude = Mathf.Atan2(n.y, Mathf.Sqrt(n.x*n.x+n.z*n.z)) * r2d;
        float longitude = Mathf.Atan2(n.z, n.x) * r2d;
        return new Vector2(longitude, latitude);
    }

    private void SetPos()
    {
        float mainNearClipPlane = main.farClipPlane/65536;
        main.nearClipPlane = mainNearClipPlane;
        parent = transform.parent;
        float d2r = Mathf.Deg2Rad;
        float l = d2r * latitude;
        float L = d2r * longitude;

        float cosl = Mathf.Cos(l);
        float sinl = Mathf.Sin(l);
        float cosL = Mathf.Cos(L);
        float sinL = Mathf.Sin(L);
        up = new Vector3(cosl * cosL, sinl, cosl * sinL);
        transform.localPosition = (mesh.radius + 2*mainNearClipPlane + altitude/1000f) * up;
        
        north = new Vector3(-cosL * sinl, cosl, -sinl * sinL);
        east = new Vector3(-sinL*cosl, 0,cosl*cosL);

        float bR = bearing * d2r;
        float cosB = Mathf.Cos(bR);
        float sinB = Mathf.Sin(bR);
        float eR = elevation * d2r;
        float cosE = Mathf.Cos(eR);
        float sinE = Mathf.Sin(eR);

        bearingDirection =  cosB * north + sinB * east;
        localLookAt = cosE * bearingDirection + sinE * up;
        lookAtPosition = transform.position + parent.TransformDirection(localLookAt);
        if(compass)
        {
            compass.transform.rotation = new Quaternion(){eulerAngles = new Vector3(0,0,bearing)};
        }
        if(bearingText)
        {
            bearingText.text = AngleToString(bearing);
        }
        if(positionText)
        {
            positionText.text = AngleToString(Mathf.Abs(longitude)) +  (longitude>=0 ? " E " : " W ") +
                                AngleToString(Mathf.Abs(latitude)) +  (latitude>=0 ? " N" : " S");
        }
        transform.LookAt(lookAtPosition, parent.TransformDirection(up));
        if(mesh)
            mesh.BuildMesh(this);
    }

    private string AngleToString(float a)
    {
        int aI = Mathf.FloorToInt(a);
        
        float m = (a-aI)*60;
        int mI = Mathf.FloorToInt(m);
        float s = (m-mI)*60;
        int sI = Mathf.FloorToInt(s);
        return aI+"°"+mI+"'" + sI + "\"";
    }

    public void SetLongitude(float f)
    {
        longitude = f;
        fromSlider = true;
        OnValidate();
    }

    public void SetCoordinates(Vector2 c)
    {
        latitude = c.y;
        longitude = c.x;
        fromSlider = false;
        OnValidate();
    }

    public void SetLatitude(float f)
    {
        latitude = f;
        fromSlider = true;
        OnValidate();
    }

    public void SetAltitude(float f)
    {
        altitude = f;
        fromSlider = true;
        OnValidate();
    }

    public Vector2 GetCoordinates()
    {
        return new Vector2(longitude, latitude);
    }

  



}
