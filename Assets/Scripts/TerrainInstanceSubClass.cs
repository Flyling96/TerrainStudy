using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MatData
{
    [SerializeField]
    Texture2D heightNormalTex;      //高度法线图
    [SerializeField]
    Texture2D alphaMap;             //权重图
    [SerializeField]
    Texture2DArray terrainMapArray; //地形贴图数组
    [SerializeField]
    Texture2D[] terrainTexArray;    //地形贴图数组
    [SerializeField]
    Vector4[] terrainMapTiling;     //地形贴图的 Tiling Size 和 Offset
    [SerializeField]
    float maxHeight;                //地形的最大高度
    [SerializeField]
    Vector4 chunkPixelCount;        //每个地形块对应高度图的横向、纵向的像素数（zw为权重图）

    public Texture2D HeightNormalTex
    {
        get
        {
            return heightNormalTex;
        }
    }

    public float MaxHeight
    {
        get
        {
            return maxHeight;
        }
    }

    public Texture2D AlphaMap
    {
        get
        {
            return alphaMap;
        }
    }

    public Texture2DArray TerrainMapArray
    {
        get
        {
            return terrainMapArray;
        }
    }

    public Texture2D[] TerrainTexArray
    {
        get
        {
            return terrainTexArray;
        }
    }

    public Vector4[] TerrainMapTiling
    {
        get
        {
            return terrainMapTiling;
        }
    }

    public Vector4 ChunkPixelCount
    {
        get
        {
            return chunkPixelCount;
        }
    }

    public void SetValue(Texture2D hnTex, float max, Texture2D aTex, Texture2DArray tMapArray, Texture2D[] tTexArray, Vector4[] tMapSize, Vector4 cPixelCount)
    {
        heightNormalTex = hnTex;
        maxHeight = max;
        alphaMap = aTex;
        terrainMapArray = tMapArray;
        terrainTexArray = tTexArray;
        terrainMapTiling = tMapSize;
        chunkPixelCount = cPixelCount;
    }
}

public class TerrainInstanceSubClass : MonoBehaviour, ITerrainInstance
{
    [SerializeField]
    protected Mesh mesh;
    [SerializeField]
    protected int chunkCountX = 0;
    [SerializeField]
    protected int chunkCountZ = 0;
    [SerializeField]
    protected Vector2 chunkSize;
    [SerializeField]
    protected MatData matData;

    [SerializeField]
    protected GameObject[] instanceChunkGos;

    protected int chunkCount = 0;
    protected MaterialPropertyBlock prop;
    protected Material mat;
    protected string shaderName = "Unlit/TerrainInstance";


    private void OnEnable()
    {
        InstanceMgr.instance.Register(this);
        if (mat == null)
        {
            Init();
        }
    }

    private void OnDisabled()
    {
        InstanceMgr.instance.Remove(this);
    }

    public void SetMatData(Texture2D tex, float max, Texture2D aTex, Texture2DArray tMapArray, Texture2D[] tTexArray, Vector4[] tMapSize, Vector4 cPixelCount)
    {
        if (matData == null)
        {
            matData = new MatData();
        }
        matData.SetValue(tex, max, aTex, tMapArray, tTexArray, tMapSize, cPixelCount);
    }

    public virtual void InitData(Mesh tempMesh, int countX, int countZ, int tChunkWidth, int tChunkLength, Quaternion rotation, Vector4[] tAlphaTexIndexs, Vector2[] tMinAndMaxHeight, Vector4[] startEndUVs)
    {
        int count = countX * countZ;
        chunkCount = count;
        chunkCountX = countX;
        chunkCountZ = countZ;
        chunkSize = new Vector2(tChunkWidth, tChunkLength);
        mesh = tempMesh;

        instanceChunkGos = new GameObject[chunkCount];

        for (int j = 0; j < countZ; j++)
        {
            for (int i = 0; i < countX; i++)
            {
                int index = j * countX + i;
                instanceChunkGos[index] = new GameObject(transform.name + "_chunk" + index);
                instanceChunkGos[index].transform.SetParent(this.transform);
                instanceChunkGos[index].transform.position = transform.position + new Vector3(i * tChunkWidth, 0, j * tChunkLength);
                instanceChunkGos[index].transform.rotation = rotation;
                instanceChunkGos[index].transform.localScale = Vector3.one;
            }
        }
    }

    protected bool isInit = false;
    protected Vector3 sourcePos;

    public virtual void Init()
    {
        isInit = true;
        prop = new MaterialPropertyBlock(); 
        chunkCount = chunkCountX * chunkCountZ;
        InitMat();
        sourcePos = transform.position;

    }

    void InitMat()
    {
        mat = new Material(Shader.Find(shaderName));
        mat.enableInstancing = true;

        if (matData == null) return;
        //for(int i=0;i<instanceCount;i++)
        //{
        //    instanceChunks[i].minAndMaxHeight *= matData.MaxHeight;
        //}

        mat.SetTexture("_HeightNormalTex", matData.HeightNormalTex);
        mat.SetFloat("_MaxHeight", matData.MaxHeight);
        mat.SetTexture("_AlphaMap", matData.AlphaMap);
        mat.SetVector("_ChunkPixelCount", matData.ChunkPixelCount);
        mat.SetVector("_MapSize", new Vector4(matData.HeightNormalTex.width, matData.HeightNormalTex.height, matData.AlphaMap.width, matData.AlphaMap.height));
    }


    public virtual void Draw()
    {

    }
}
