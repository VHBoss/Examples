using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections.Generic;

public class MapController : ScreenInfo
{
    public static MapController I;
    public static int IconIndex;

    [Header("Route Settings")]
    [SerializeField] Color m_RouteColor;
    [SerializeField] float m_RouteWidth = 3;

    [Header("Markers")]
    [SerializeField] Texture2D[] m_MarkerIcons;
    [SerializeField] GameObject[] m_MarkerPrefab;
    [SerializeField] float m_MarkerScale = 0.5f;

    [Header("Navigation")]
    [SerializeField] Button m_ButtonAR;
    [SerializeField] Button m_ZoomIn;
    [SerializeField] Button m_ZoomOut;
    [SerializeField] Button m_CenterTarget;
    [SerializeField] Button m_MuteAudio;
    [SerializeField] Button m_StartRoute;
    [SerializeField] Button m_ConfirmRoute;
    [SerializeField] Button m_ButtonBack;
    [SerializeField] Button m_ButtonPrev;
    [SerializeField] Button m_ButtonNext;

    private double Lat, Lon;
    private double targetLng, targetLat;
    private float rotation = 180;
    private OnlineMaps map;
    private OnlineMapsMarker3D marker;
    private OnlineMapsTileSetControl control;
    private OnlineMapsCameraOrbit orbit;
    private Route m_Route;
    private List<OnlineMapsMarker3D> m_Markers = new List<OnlineMapsMarker3D>();
    private List<Point> m_Points = new List<Point>();
    private Point m_CurrentPoint;
    private int m_CurrentMarker;

    private void Awake()
    {
        I = this;
    }

    void Start()
    {
        m_ButtonAR.onClick.AddListener(OnClickedAR);
        m_ZoomIn.onClick.AddListener(OnClickedZoomIn);
        m_ZoomOut.onClick.AddListener(OnClickedZoomOut);
        m_CenterTarget.onClick.AddListener(OnClickedCenterTarget);
        m_MuteAudio.onClick.AddListener(OnClickedMuteAudio);
        m_StartRoute.onClick.AddListener(OnClickedStartRoute);
        m_ConfirmRoute.onClick.AddListener(OnClickedConfirmRoute);
        m_ButtonPrev.onClick.AddListener(PrevPoint);
        m_ButtonNext.onClick.AddListener(NextPoint);

        map = OnlineMaps.instance;
        control = OnlineMapsTileSetControl.instance;
        orbit = OnlineMapsCameraOrbit.instance;
        //Движение маркера по клику на карту
        //control.OnMapClick += OnMapClick;
    }

    public override void Show(object param)
    {
        m_ButtonBack.onClick.RemoveAllListeners();
        m_ButtonBack.onClick.AddListener(MainController.I.Back);

        DisplayData((Route)param);

        m_StartRoute.gameObject.SetActive(true);
        m_ButtonPrev.gameObject.SetActive(false);
        m_ButtonNext.gameObject.SetActive(false);

        gameObject.SetActive(true);
    }

    void DisplayData(Route route)
    {
        if (route != m_Route)
        {
            m_Markers.Clear();
            m_Points.Clear();
            m_CurrentMarker = -1;
            m_CurrentPoint = null;
            marker = null;

            m_Route = route;

            //Clear Markers
            OnlineMapsMarker3DManager.RemoveAllItems();
            //Clear Routes
            OnlineMapsDrawingElementManager.RemoveAllItems();

            IconIndex = 0;

            double latSumm = 0;
            double lonSumm = 0;
            int count = 0;

            foreach (Point item in route.route_points)
            {
                CreateMarker3D(item);
                latSumm += item.lat;
                lonSumm += item.lon;
                count++;
            }

            CreateUserMarker();

            //Draw route
            if (route.coordinates.Length > 1)
            {
                OnlineMapsDrawingElementManager.AddItem(new OnlineMapsDrawingLine(route.coordinates, m_RouteColor, m_RouteWidth));
            }
        }
        else
        {
            CreateUserMarker();
        }
    }


    void CreateUserMarker()
    {
        if (m_Route.route_points.Length > 0)
        {
            Point point = m_Route.route_points[0];
            float rLat = Mathf.Round((float)point.lat * 1000) / 1000f;
            float rLon = Mathf.Round((float)point.lon * 1000) / 1000f;
            CreateUserCursor(rLat, rLon);
            OnlineMaps.instance.SetPosition(point.lat, point.lon);
        }
    }

    void CreateMarker(Point point)
    {
        OnlineMapsMarker marker = new OnlineMapsMarker();
        marker.SetPosition(point.lat, point.lon);
        marker.scale = m_MarkerScale;
        marker.texture = m_MarkerIcons[point.type];
        marker.OnClick = delegate { MainController.I.ShowScreen(ScreenType.PlaceInfo, point); };
        OnlineMapsMarkerManager.AddItem(marker);
    }

    void CreateMarker3D(Point point)
    {
        OnlineMapsMarker3D marker = OnlineMapsMarker3DManager.CreateItem(point.lat, point.lon, m_MarkerPrefab[point.type]);
        marker.scale = 50;
        marker.OnClick = (p) => OnMarkerClick(point);

        //Если точка это достопримечательность, добавляем в список для Prev/Next
        if (point.type == 1)
        {
            m_Markers.Add(marker);
            m_Points.Add(point);
        }
    }

    void OnMarkerClick(Point point)
    {
        DOTween.KillAll();
        MainController.I.ShowScreen(ScreenType.PlaceInfo, point);
    }

    void CreateUserCursor(double lat, double lon)
    {
        if (marker == null)
        {
            marker = OnlineMapsMarker3DManager.CreateItem(lat, lon, m_MarkerPrefab[0]);
        }
        else
        {
            marker.SetPosition(lat, lon);
        }
        marker.scale = 50;
        marker.rotationY = 180;
    }

    void OnMapClick()
    {
        control.GetCoords(out targetLng, out targetLat);
        GoToPoint();
    }

    void GoToPoint()
    {
        DOTween.KillAll();

        map.GetPosition(out double matLon, out double matLat);
        marker.GetPosition(out Lon, out Lat);

        double tx1, ty1, tx2, ty2;
        map.projection.CoordinatesToTile(Lon, Lat, map.zoom, out tx1, out ty1);
        map.projection.CoordinatesToTile(targetLng, targetLat, map.zoom, out tx2, out ty2);

        rotation = (float)OnlineMapsUtils.Angle2D(tx1, ty1, tx2, ty2) - 90;

        double startLat = Lat;
        double startLon = Lon;
        double diffLat = matLat - startLat;
        double diffLon = matLon - startLon;
        float startRot = orbit.rotation.y;
        OnlineMapsUtils.DistanceBetweenPoints(Lon, Lat, targetLng, targetLat, out double dx, out double dy);
        float distance = (float)Math.Sqrt(dx * dx + dy * dy);
        OnlineMapsUtils.GetCoordinateInDistance(Lon, Lat, distance, rotation + 180, out Lon, out Lat);
        marker.rotationY = rotation;
        rotation += 180;

        if (Mathf.Abs(startRot - rotation) > 180)
        {
            if (startRot < rotation)
            {
                startRot += 360;
            }
            else
            {
                rotation += 360;
            }
        }

        //Поворачиваем камеру
        DOTween.To(() => startRot, x => startRot = x, rotation, 1)
            .OnUpdate(() =>
            {
                orbit.rotation.y = startRot;
            });

        //Центрируем карту на указатель
        DOTween.To(() => diffLat, x => diffLat = x, 0, 1);
        DOTween.To(() => diffLon, x => diffLon = x, 0, 1);
        //Перемещаем указатель
        DOTween.To(() => startLon, x => startLon = x, Lon, distance * 10);
        DOTween.To(() => startLat, x => startLat = x, Lat, distance * 10)
            .OnUpdate(() =>
            {
                marker.SetPosition(startLon, startLat);
                map.SetPosition(startLon + diffLon, startLat + diffLat);
            })
            .OnComplete(ShowPointInfo);
    }

    public static int GetIndex()
    {
        IconIndex++;
        return IconIndex;
    }

    void ShowPointInfo()
    {
        MainController.I.ShowScreen(ScreenType.PlaceInfo, m_CurrentPoint);
    }

    void OnClickedAR()
    {

    }

    void OnClickedZoomIn()
    {
        OnlineMaps.instance.floatZoom += 0.5f;
    }

    void OnClickedZoomOut()
    {
        OnlineMaps.instance.floatZoom -= 0.5f;
    }

    void OnClickedCenterTarget()
    {
        marker.GetPosition(out double lon, out double lat);
        map.GetPosition(out double matLon, out double matLat);

        DOTween.To(() => matLon, x => matLon = x, lon, 0.2f).SetEase(Ease.Linear);
        DOTween.To(() => matLat, x => matLat = x, lat, 0.2f).SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                map.SetPosition(matLon, matLat);
            });
    }

    void OnClickedMuteAudio()
    {

    }

    void NextPoint()
    {
        m_CurrentMarker++;
        if(m_CurrentMarker > m_Markers.Count - 1)
        {
            m_CurrentMarker = 0;
        }
        Prepare();
    }

    void PrevPoint()
    {
        m_CurrentMarker--;
        if (m_CurrentMarker < 0)
        {
            m_CurrentMarker = m_Markers.Count - 1;
        }
        Prepare();
    }

    void Prepare()
    {
        m_CurrentPoint = m_Points[m_CurrentMarker];
        Vector2 pos = m_Markers[m_CurrentMarker].position;
        targetLng = pos.x;
        targetLat = pos.y;
        GoToPoint();
    }

    void OnClickedStartRoute()
    {
        MainController.I.ShowPopup(ScreenType.RouteMap, m_Route);
        m_StartRoute.gameObject.SetActive(false);
        m_ButtonPrev.gameObject.SetActive(true);
        m_ButtonNext.gameObject.SetActive(true);
    }

    void OnClickedConfirmRoute()
    {
        m_ButtonBack.onClick.RemoveAllListeners();
        PopupData data = new PopupData
        {
            type = PopupType.ConfirmExitRoute,
            actionOk = delegate{ MainController.I.Back(); MainController.I.Back(); },
            actionCancel = delegate{ MainController.I.Back();}
        };
        m_ButtonBack.onClick.AddListener(() => MainController.I.ShowPopup(ScreenType.PopupInfo, data));
        MainController.I.Back();
    }
}
