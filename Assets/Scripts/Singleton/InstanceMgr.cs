using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomTerrain;

[ExecuteInEditMode]
public class InstanceMgr : Singleton<InstanceMgr>
{

    public List<TerrainInstance> InstanceList = new List<TerrainInstance>();

    private void OnEnable()
    {

    }

    public void Register(TerrainInstance terrainInstance)
    {
        if(!InstanceList.Contains(terrainInstance))
        {
            InstanceList.Add(terrainInstance);
        }
    }

    public void Remove(TerrainInstance terrainInstance)
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
            if (InstanceList[i] == null || InstanceList[i].transform == null)
            {
                InstanceList.RemoveAt(i);
                continue;
            }
            InstanceList[i].Draw();
        }
    }

    public TerrainInstance GetInstanceByPos(Vector3 pos)
    {
        for(int i=0;i<InstanceList.Count;i++)
        {
            if(InstanceList[i].IsInside(pos))
            {
                return InstanceList[i];
            }
        }
        return null;
    }

    private void Update()
    {
        //UpdateInstance();
    }


}
