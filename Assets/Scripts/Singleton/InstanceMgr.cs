using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;
using CustomTerrain;

[ExecuteInEditMode]
public class InstanceMgr : Singleton<InstanceMgr>
{

    public List<IInstance> InstanceList = new List<IInstance>();

    private void OnEnable()
    {

    }

    public void Register(IInstance terrainInstance)
    {
        if(!InstanceList.Contains(terrainInstance))
        {
            InstanceList.Add(terrainInstance);
        }
    }

    public void Remove(IInstance terrainInstance)
    {
        if (!InstanceList.Contains(terrainInstance))
        {
            InstanceList.Remove(terrainInstance);
        }
    }

    public void UpdateInstance()
    {
        for (int i = InstanceList.Count - 1; i > -1; i--)
        {
            if (InstanceList[i] == null)
            {
                InstanceList.RemoveAt(i);
                continue;
            }
            InstanceList[i].Draw();
        }
    }

    private void Update()
    {
        UpdateInstance();
    }


}
