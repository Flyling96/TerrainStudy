using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RenderPipeline : MonoBehaviour
{
    public static RenderPipeline instance;

    public ComputeShader terrainViewOccusionCS;

    RenderTexture curRT;

    private void Awake()
    {
        instance = this;
    }

    private void OnRenderObject()
    {
        //InstanceMgr.instance.UpdateTerrainInstance();
    }
}
