using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class Storage : MonoBehaviour
{
    public static Storage I;

    [SerializeField] Loader m_Loader;

    public Data Data;

    private readonly string urlPoints = "https://api/points";

    private void Awake()
    {
        I = this;

        LoadData();
    }

    void LoadData()
    {
        m_Loader.Init();
        StartCoroutine(LoadPoints());
    }

    IEnumerator LoadPoints()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(urlPoints))
        {
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError("Error: " + webRequest.error);
                    ShowError();
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError("HTTP Error: " + webRequest.error);
                    ShowError();
                    break;
                case UnityWebRequest.Result.Success:
                    Data = new Data(webRequest.downloadHandler.text);
                    StartCoroutine(GetData());
                    break;
            }
        }
    }

    IEnumerator GetData()
    {
        string dataRoot = Application.persistentDataPath + @"\Media\";
#if UNITY_EDITOR
        dataRoot = "Media/";
#endif
        if (!Directory.Exists(dataRoot))
        {
            Directory.CreateDirectory(dataRoot);
        }

        float totalCount = 0;
        //Preload
        foreach (var route in Data.Routes)
        {
            totalCount += route.images.Length;
            foreach (Point point in route.route_points)
            {
                totalCount += point.images.Length;
                if (!string.IsNullOrEmpty(point.audio.filename))
                {
                    totalCount++;
                }
            }
            if (!string.IsNullOrEmpty(route.audio.filename))
            {
                totalCount++;
            }
        }
         
        int current = 0;
        foreach (var route in Data.Routes)
        {
            string itemPath = dataRoot + route.id + "/";
            if (!Directory.Exists(itemPath))
            {
                Directory.CreateDirectory(itemPath);
            }

            foreach (Point point in route.route_points)
            {
                //Load Audio
                if (!string.IsNullOrEmpty(point.audio.filename))
                {
                    yield return StartCoroutine(LoadAudio(point.audio, itemPath));
                    current++;
                    m_Loader.SetProgress(current / totalCount);
                }

                //Load images
                foreach (Picture tex in point.images)
                {
                    yield return StartCoroutine(SetTexture(tex, itemPath));
                    current++;
                    m_Loader.SetProgress(current / totalCount);
                }
            }

            //Load Audio
            if (!string.IsNullOrEmpty(route.audio.filename))
            {
                yield return StartCoroutine(LoadAudio(route.audio, itemPath));
                current++;
                m_Loader.SetProgress(current / totalCount);
            }

            //Load images
            foreach (var tex in route.images)
            {
                yield return StartCoroutine(SetTexture(tex, itemPath));
                current++;
                m_Loader.SetProgress(current / totalCount);
            }            
        }

        m_Loader.Hide();
        MainController.I.ShowScreen(ScreenType.RouteType);
        if (Profile.User == null)
        {
            MainController.I.ShowPopup(ScreenType.Welcome);
        }
    }

    IEnumerator LoadAudio(Audio audioData, string itemPath)
    {
        string audioFile = itemPath + Path.GetFileName(audioData.filename);
        string path = audioData.filename;
        bool needSave = true;

        if (File.Exists(audioFile))
        {
            path = Path.GetFullPath(audioFile);
            needSave = false;
        }

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
            }
            else
            {
                audioData.clip = DownloadHandlerAudioClip.GetContent(www);
                if (needSave)
                {
                    File.WriteAllBytes(audioFile, www.downloadHandler.data);
                }
            }
        }
    }

    IEnumerator SetTexture(Picture tex, string itemPath)
    {
        string texFile = itemPath + Path.GetFileName(tex.filename);

        if (File.Exists(texFile))
        {
            tex.picture = LoadTexture(texFile);
        }
        else
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(tex.filename))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(uwr.error);
                }
                else
                {
                    tex.picture = DownloadHandlerTexture.GetContent(uwr);
                    File.WriteAllBytes(texFile, uwr.downloadHandler.data);
                }
            }
        }
    }

    Texture2D LoadTexture(string path)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(path))
        {
            fileData = File.ReadAllBytes(path);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
        }
        return tex;
    }

    void ShowError()
    {
        PopupData data = new PopupData
        {
            type = PopupType.ErrorConnection,
            actionOk = LoadData,
            actionCancel = Application.Quit
        };
        MainController.I.ShowPopup(ScreenType.PopupInfo, data);
    }
}
