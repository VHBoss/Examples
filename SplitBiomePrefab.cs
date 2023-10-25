using UnityEngine.AddressableAssets;
using UnityEditor;
using UnityEngine;
using DunGen;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public class SplitBiomePrefab
{
    [MenuItem("Assets/Split Prefab")]
    static void CreatePrefab()
    {
        GameObject[] objectArray = Selection.gameObjects;

        foreach (var selected in objectArray)
        {
            string path = AssetDatabase.GetAssetPath(selected);
            GameObject mainAsset = PrefabUtility.LoadPrefabContents(path);
            GameObject content = PrefabUtility.LoadPrefabContents(path);

            //1       
            //GameObject.DestroyImmediate(content.transform.GetChild(0).GetChild(0).gameObject); //Destroy doors
            Transform doorsContent = content.transform.GetChild(0).GetChild(0);
            for (int i = 0; i < doorsContent.transform.childCount; i++)
            {
                Transform door = doorsContent.GetChild(i);
                if (PrefabUtility.IsPartOfAnyPrefab(door))
                {
                    PrefabUtility.UnpackPrefabInstance(door.gameObject, PrefabUnpackMode.Completely, InteractionMode.UserAction);
                }
                GameObject.DestroyImmediate(door.GetChild(1).gameObject);
            }

            content.transform.GetChild(1).GetChild(1).gameObject.SetActive(true);
            content.transform.GetChild(0).tag = "Untagged";
            content.tag = "Untagged";
            GameObject.DestroyImmediate(content.GetComponent<_Sc_GetTileDepth>());
            GameObject.DestroyImmediate(content.GetComponent<Tile>());

            _Sc_doorResize[] sc_doorResize = content.GetComponentsInChildren<_Sc_doorResize>(true);
            for (int i = 0; i < sc_doorResize.Length; i++)
            {
                GameObject.DestroyImmediate(sc_doorResize[i].gameObject);
            }

            string splitPath = path.Replace(".prefab", "_Split.prefab");
            PrefabUtility.SaveAsPrefabAsset(content, splitPath);
            PrefabUtility.UnloadPrefabContents(content);

            //2. Create Empty object with bounds
            Sprite sprite = (Sprite)AssetDatabase.LoadAssetAtPath("Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/Square.png", typeof(Sprite));
            GameObject boundsObject = new GameObject("Bounds", typeof(SpriteRenderer));            
            SpriteRenderer renderer = boundsObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.clear;

            Bounds bounds = new Bounds();
            SpriteRenderer[] allSprites = mainAsset.GetComponentsInChildren<SpriteRenderer>();

            foreach (var item in allSprites)
            {
                bounds.Encapsulate(item.bounds);
            }

            boundsObject.transform.position = bounds.center;
            boundsObject.transform.localScale = bounds.size;

            //3
            Transform salle = mainAsset.transform.GetChild(0);
            for (int i = salle.transform.childCount; i > 1; --i)
                GameObject.DestroyImmediate(salle.transform.GetChild(1).gameObject);

            for (int i = mainAsset.transform.childCount; i > 1; --i)
                GameObject.DestroyImmediate(mainAsset.transform.GetChild(1).gameObject);

            boundsObject.transform.SetParent(mainAsset.transform);

            //4. Clear Blockers
            Transform doors = mainAsset.transform.GetChild(0).GetChild(0);
            for (int j = 0; j < doors.childCount; j++)
            {
                Transform door = doors.GetChild(j);
                if (PrefabUtility.IsPartOfAnyPrefab(door))
                {
                    PrefabUtility.UnpackPrefabInstance(door.gameObject, PrefabUnpackMode.Completely, InteractionMode.UserAction);
                }
                Transform blocker = door.GetChild(0);
                for (int i = blocker.childCount; i > 0; --i)
                {
                    GameObject.DestroyImmediate(blocker.GetChild(0).gameObject);
                }
            }


            //5. Create addressable asset
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings.FindGroup("Biome3");
            string guid = AssetDatabase.AssetPathToGUID(splitPath);
            settings.CreateOrMoveEntry(guid, group);
            AssetReference reference = new AssetReference(guid);
            _Sc_GetTileDepth script = mainAsset.GetComponent<_Sc_GetTileDepth>();
            script.Content = reference;


            //6
            PrefabUtility.SaveAsPrefabAsset(mainAsset, path);
            PrefabUtility.UnloadPrefabContents(mainAsset);
        }
    }


    [MenuItem("Assets/Check Missing Doors")]
    static void CheckMissingDoors()
    {
        GameObject[] objectArray = Selection.gameObjects;

        foreach (var selected in objectArray)
        {
            string path = AssetDatabase.GetAssetPath(selected);
            GameObject mainAsset = PrefabUtility.LoadPrefabContents(path);
            
            if(mainAsset.transform.GetChild(0).GetChild(0).childCount == 0)
            {
                Debug.Log("Missing Doors: " + mainAsset.name);
            }
            PrefabUtility.UnloadPrefabContents(mainAsset);
        }

        Debug.Log("DONE");
    }


    [MenuItem("Assets/Delete Unused Doors")]
    static void DeleteUnusedDoors()
    {
        GameObject[] objectArray = Selection.gameObjects;

        foreach (var selected in objectArray)
        {
            string path = AssetDatabase.GetAssetPath(selected);
            GameObject mainAsset = PrefabUtility.LoadPrefabContents(path);

            _Sc_doorResize[] doors = mainAsset.GetComponentsInChildren<_Sc_doorResize>(true);
            if (doors.Length != 0)
            {
                Debug.Log("Unused Doors: " + mainAsset.name);
            }
            for (int i = 0; i < doors.Length; i++)
            {
                GameObject.DestroyImmediate(doors[i].gameObject);
            }

            PrefabUtility.SaveAsPrefabAsset(mainAsset, path);
            PrefabUtility.UnloadPrefabContents(mainAsset);
        }

        Debug.Log("DONE");
    }





    // SPLIT IN SCENE

    [MenuItem("GameObject/Split GameObject")]
    static void SplitGameObject()
    {
        GameObject start = GameObject.Instantiate(Selection.activeGameObject);
        GameObject asset = GameObject.Instantiate(Selection.activeGameObject);
        start.name += "_start";
        asset.name += "_asset";

        //Clean Start Prefab
        Transform salle = start.transform.GetChild(0);
        for (int i = salle.transform.childCount; i > 1; --i)
            GameObject.DestroyImmediate(salle.transform.GetChild(1).gameObject);

        for (int i = start.transform.childCount; i > 1; --i)
            GameObject.DestroyImmediate(start.transform.GetChild(1).gameObject);

        Transform doors = start.transform.GetChild(0).GetChild(0);
        foreach (Transform door in doors)
        {
            Transform blocker = door.GetChild(0);
            for (int i = blocker.childCount; i > 0; --i)
                GameObject.DestroyImmediate(blocker.GetChild(0).gameObject);
        }

        //Clean Asset Prefab
        //GameObject.DestroyImmediate(asset.transform.GetChild(0).GetChild(0).gameObject);
        asset.transform.GetChild(1).GetChild(1).gameObject.SetActive(true);
        asset.transform.GetChild(0).tag = "Untagged";
        asset.tag = "Untagged";
        GameObject.DestroyImmediate(asset.GetComponent<_Sc_GetTileDepth>());
        GameObject.DestroyImmediate(asset.GetComponent<Tile>());

    }

    private static float _lastMenuCallTimestamp = 0f;

    [MenuItem("GameObject/Union GameObject")]
    static void UnionGameObject()
    {
        if (Time.unscaledTime.Equals(_lastMenuCallTimestamp)) return;
        _lastMenuCallTimestamp = Time.unscaledTime;

        Debug.Log("UnionGameObject");
        GameObject[] objectArray = Selection.gameObjects;

        Transform asset;
        Transform content;
        if (objectArray[0].transform.childCount == 1)
        {
            asset = objectArray[0].transform;
            content = objectArray[1].transform;
        }
        else
        {
            asset = objectArray[1].transform;
            content = objectArray[0].transform;
        }

        //Root
        for (int i = content.childCount; i > 1; --i)
        {
            content.GetChild(1).SetParent(asset);
        }

        //Salle
        for (int i = content.GetChild(0).childCount; i > 1; --i)
        {
            content.GetChild(0).GetChild(1).SetParent(asset.GetChild(0));
        }

        //Blockers
        Transform doors = content.GetChild(0).GetChild(0);
        Transform assetDoors = asset.GetChild(0).GetChild(0);
        for (int j = 0; j < 4; j++)
        {
            Transform blocker = doors.GetChild(j).GetChild(0);
            Debug.Log(blocker.childCount);
            for (int i = blocker.childCount; i > 0; --i)
            {
                blocker.GetChild(0).SetParent(assetDoors.GetChild(j).GetChild(0));
            }
        }
    }
}
