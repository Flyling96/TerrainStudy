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
    protected int instanceCountX = 0;
    [SerializeField]
    protected int instanceCountZ = 0;
    [SerializeField]
    protected int chunkWidth = 0;
    [SerializeField]
    protected MatData matData;

    [SerializeField]
    protected InstanceChunk[] instanceChunks;

    protected int instanceCount = 0;
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

    public void InitData(Mesh tempMesh, int countX, int countZ, int tChunkWidth, int tChunkLength, Quaternion rotation, Vector4[] tAlphaTexIndexs, Vector2[] tMinAndMaxHeight, Vector4[] startEndUVs)
    {
        int count = countX * countZ;
        instanceCount = count;
        instanceCountX = countX;
        instanceCountZ = countZ;
        chunkWidth = tChunkWidth;
        mesh = tempMesh;

        instanceChunks = new InstanceChunk[instanceCount];
        int index = 0;
        Matrix4x4 matr;
        Vector3 chunkPos;

        for (int j = 0; j < countZ; j++)
        {
            for (int i = 0; i < countX; i++)
            {
                index = j * countX + i;
                instanceChunks[index] = new GameObject(transform.name + "_chunk" + index).AddComponent<InstanceChunk>();
                instanceChunks[index].startEndUV = startEndUVs[index];
                instanceChunks[index].alphaTexIndex = tAlphaTexIndexs[index];
                instanceChunks[index].minAndMaxHeight = tMinAndMaxHeight[index] * matData.MaxHeight;
                matr = new Matrix4x4();
                chunkPos = transform.position + new Vector3(i * tChunkWidth, 0, j * tChunkLength);
                matr.SetTRS(chunkPos, rotation, Vector3.one);
                instanceChunks[index].chunkSize = new Vector2(tChunkWidth, tChunkLength);
                instanceChunks[index].trsMatrix = matr;
                instanceChunks[index].transform.position = chunkPos;
                instanceChunks[index].transform.rotation = rotation;
                instanceChunks[index].transform.SetParent(transform);
            }
        }

        for (int j = 0; j < countZ; j++)
        {
            for (int i = 0; i < countX; i++)
            {
                index = j * countX + i;
                instanceChunks[index].neighborChunk = new InstanceChunk[4];
                instanceChunks[index].neighborChunk[0] = (j == countZ - 1) ? null : instanceChunks[(j + 1) * countX + i];//上
                instanceChunks[index].neighborChunk[1] = (j == 0) ? null : instanceChunks[(j - 1) * countX + i];//下
                instanceChunks[index].neighborChunk[2] = (i == 0) ? null : instanceChunks[j * countX + i - 1];//左
                instanceChunks[index].neighborChunk[3] = (i == countX - 1) ? null : instanceChunks[j * countX + i + 1];//右
            }
        }
    }

    protected bool isInit = false;
    protected Vector3 sourcePos;

    public void Init()
    {
        isInit = true;
        instanceCount = instanceCountX * instanceCountZ;
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
