using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class InstanceMgr : Singleton<InstanceMgr>
{
    List<TerrainInstance> terrainInstanceList = new List<TerrainInstance>();
    public Camera mainCamera;

    private void OnEnable()
    {
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
    }

    public void Register(TerrainInstance terrainInstance)
    {
        if(!terrainInstanceList.Contains(terrainInstance))
        {
            terrainInstanceList.Add(terrainInstance);
        }
    }

    public void Remove(TerrainInstance terrainInstance)
    {
        if (!terrainInstanceList.Contains(terrainInstance))
        {
            terrainInstanceList.Remove(terrainInstance);
        }
    }

    private void Update()
    {
        for(int i= terrainInstanceList.Count-1; i>-1;i--)
        {
            if (terrainInstanceList[i] == null)
            {
                terrainInstanceList.RemoveAt(i);
                continue;
            }
            terrainInstanceList[i].Draw();
        }
    }

}
