using System;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;

public class FindSingletonsInScene : MonoBehaviour
{
    public bool printResult = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            FindSingletons();
        }
    }

    void FindSingletons()
    {
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
        List<MonoBehaviour> singletons = new List<MonoBehaviour>();

        foreach (var item in allScripts)
        {
            FieldInfo field_I = item.GetType().GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            FieldInfo field_i = item.GetType().GetField("instance", BindingFlags.Public | BindingFlags.Static);

            if (field_I != null || field_i != null)
            {
                //print(item.gameObject.name);
                singletons.Add(item);
            }
        }

        print("SINGLETONS COUNT: " + singletons.Count);

        if (printResult)
        {
            foreach (var item in singletons)
            {
                print(item.gameObject.name + " | " + item.GetType());
            }
        }

        Dictionary<Type, int> duplicates = new Dictionary<Type, int>();
        foreach (var item in singletons)
        {
            Type type = item.GetType();

            if (duplicates.ContainsKey(type))
            {
                duplicates[type]++;
            }
            else
            {
                duplicates.Add(type, 1);
            }
        }

        //RESULT
        foreach (var item in duplicates)
        {
            if (item.Value > 1)
            {
                print(item.Key + /*" , PARENT: " + item.Key.transform.parent.name +*/ " | " + item.Value);
            }
        }

        print("****** FIND SINGLETONES DONE ******");
    }
}
