using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


[ExecuteInEditMode]
public class TerrainInstance : MonoBehaviour
{
    [Serializable]
    class MatData
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

        public void SetValue(Texture2D hnTex, float max, Texture2D aTex,Texture2DArray tMapArray,Texture2D[] tTexArray,Vector4[] tMapSize,Vector4 cPixelCount)
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

    [SerializeField]
    private Mesh mesh;
    [SerializeField]
    private int instanceCountX = 0;
    [SerializeField]
    private int instanceCountZ = 0;
    [SerializeField]
    private MatData matData;
    [SerializeField]
    private InstanceChunk[] instanceChunks;

    int instanceCount = 0;
    Matrix4x4[] tempTrs;
    Vector3[,] chunkPoints; //块的四个顶点坐标
    Vector3 chunkDis;


    MaterialPropertyBlock prop;
    Material mat;
    string shaderName = "Unlit/TerrainInstance";


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

    public void SetMatData(Texture2D tex,float max,Texture2D aTex,Texture2DArray tMapArray, Texture2D[] tTexArray, Vector4[] tMapSize, Vector4 cPixelCount)
    {
        if(matData == null)
        {
            matData = new MatData();
        }
        matData.SetValue(tex, max, aTex, tMapArray, tTexArray,tMapSize, cPixelCount);
    }


    public void InitData(Mesh tempMesh,int countX,int countZ,int chunkWidth,int chunkLength,Quaternion rotation,Vector4[] tAlphaTexIndexs,Vector2[] tMinAndMaxHeight)
    {
        int count = countX * countZ;
        instanceCount = count;
        instanceCountX = countX;
        instanceCountZ = countZ;
        mesh = tempMesh;
        //alphaTexIndexs = tAlphaTexIndexs;
        //startEndUVs = new Vector4[count];
        //chunkMinAndMaxHeights = tMinAndMaxHeight;
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
                instanceChunks[index].startEndUV = (new Vector4((float)i / countX, (float)j / countZ, (float)(i + 1) / countX, (float)(j + 1) / countZ));
                instanceChunks[index].alphaTexIndex = tAlphaTexIndexs[index];
                instanceChunks[index].minAndMaxHeight = tMinAndMaxHeight[index] * matData.MaxHeight;
                matr = new Matrix4x4();
                chunkPos = transform.position + new Vector3(i * chunkWidth, 0, j * chunkLength);
                matr.SetTRS(chunkPos,rotation, Vector3.one);
                instanceChunks[index].chunkSize = new Vector2(chunkWidth, chunkLength);
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

    float[] selfVertexCounts;
    Vector4[] neighborVertexCounts;
    bool isInit = false;

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


    void UpdateMatProp()
    {
        if(mat == null)
        {
            return;
        }


        //mat.SetTexture("_TerrainMapArray", matData.TerrainMapArray);
        //mat.SetVectorArray("_TerrainMapSize", matData.TerrainMapTiling);

        if (matData != null)
        {
            mat.SetTexture("_TerrainMapArray", terrainMapArray);
            mat.SetVectorArray("_TerrainMapSize", terrainMapTilingList.ToArray());
        }

        prop = new MaterialPropertyBlock();

        prop.SetVectorArray("_StartEndUV", startEndUVList.ToArray());
        prop.SetVectorArray("_AlphaTexIndexs", alphaTexIndexList.ToArray());
        prop.SetVectorArray("_TessVertexCounts", neighborVertexCountList.ToArray());
        prop.SetFloatArray("_LODTessVertexCounts", selfVertexCountList.ToArray());

    }

    void UpdateLodLevel()
    {
        Vector3 instancePos;
        for(int i=0;i<instanceCount;i++)
        {
            instancePos = instanceChunks[i].transform.position;
            float lodLevel = CaculateLodLevel(instancePos, instanceChunks[i].minAndMaxHeight);
            instanceChunks[i].selfVertexCount = CacuTessCount(lodLevel);
        }

    }

    int CaculateLodLevel(Vector3 instancePos,Vector2 chunkMinAndMaxHeight)
    {
        float height = Mathf.Abs(chunkMinAndMaxHeight.y - chunkMinAndMaxHeight.x);
        float distance = Vector3.Distance(instancePos, InstanceMgr.instance.mainCamera.transform.position);
        int lodLevel = 1;

        float power = distance * 0.2f + 600/(height+1) * 2.0f;

        if (power < 30)
        {
            lodLevel = 1;
        }
        else if(power < 100)
        {
            lodLevel = 2;
        }
        else if(power < 300)
        {
            lodLevel = 3;
        }
        else if(power < 1000)
        {
            lodLevel = 4;
        }
        else
        {
            lodLevel = 5;
        }

        return lodLevel;
    }

    int CacuTessCount(float lodLevel)
    {
        switch (lodLevel)
        {
            case 1:
                return 100;
            case 2:
                return 50;
            case 3:
                return 20;
            case 4:
                return 5;
            case 5:
                return 1;
            default:
                return 1;
        }


    }

    Vector3 sourcePos;

    public void Draw()
    {
        if (instanceCount == 0 || mesh == null ||!isInit) return;

        UpdateTRS();
        UpdateLodLevel();
        ViewOcclusion();

        if(showChunkCount < 1)
        {
            return;
        }

        UpdateMatProp();

        Graphics.DrawMeshInstanced(mesh, 0, mat, trsList.ToArray(), showChunkCount, prop);  

    }

    void UpdateTRS()
    {
        Vector3 tempPos;
        //位移
        if (transform.position != sourcePos)
        {
            for (int i = 0; i < instanceCount; i++)
            {
                tempPos = new Vector3(instanceChunks[i].trsMatrix.m03, instanceChunks[i].trsMatrix.m13, instanceChunks[i].trsMatrix.m23);
                tempPos += transform.position - sourcePos;
                instanceChunks[i].ChangePos(tempPos);
            }
            sourcePos = transform.position;
        }
    }

    //剔除相关List
    List<Vector4> startEndUVList = new List<Vector4>();
    List<Vector4> alphaTexIndexList = new List<Vector4>();
    List<Vector4> neighborVertexCountList = new List<Vector4>();
    List<float> selfVertexCountList = new List<float>();
    Texture2DArray terrainMapArray;
    List<Vector4> terrainMapTilingList = new List<Vector4>();
    List<Matrix4x4> trsList = new List<Matrix4x4>();
    List<float> useAlphaTexIndexList = new List<float>();
    int showChunkCount = 0;

    //cpu剔除块
    void ViewOcclusion()
    {
        startEndUVList.Clear();
        alphaTexIndexList.Clear();
        selfVertexCountList.Clear();
        neighborVertexCountList.Clear();
        trsList.Clear();
        showChunkCount = 0;

        List<float> tempUseAlphaTexIndexList = new List<float>();

        for(int i=0;i<instanceCount; i++)
        {
            instanceChunks[i].CacuIsBoundInCamera();
            if (instanceChunks[i].isShow)
            {
                showChunkCount++;
                instanceChunks[i].CaculateNeighborVertexCount();
                startEndUVList.Add(instanceChunks[i].startEndUV);
                selfVertexCountList.Add(instanceChunks[i].selfVertexCount);
                neighborVertexCountList.Add(instanceChunks[i].neighborVertexCount);
                trsList.Add(instanceChunks[i].trsMatrix);

                if (instanceChunks[i].alphaTexIndex.x != -1 && !tempUseAlphaTexIndexList.Contains(instanceChunks[i].alphaTexIndex.x))
                {
                    tempUseAlphaTexIndexList.Add(instanceChunks[i].alphaTexIndex.x);
                }
                if (instanceChunks[i].alphaTexIndex.y != -1 && !tempUseAlphaTexIndexList.Contains(instanceChunks[i].alphaTexIndex.y))
                {
                    tempUseAlphaTexIndexList.Add(instanceChunks[i].alphaTexIndex.y);
                }
                if (instanceChunks[i].alphaTexIndex.z != -1 && !tempUseAlphaTexIndexList.Contains(instanceChunks[i].alphaTexIndex.z))
                {
                    tempUseAlphaTexIndexList.Add(instanceChunks[i].alphaTexIndex.z);
                }
                if (instanceChunks[i].alphaTexIndex.w != -1 && !tempUseAlphaTexIndexList.Contains(instanceChunks[i].alphaTexIndex.w))
                {
                    tempUseAlphaTexIndexList.Add(instanceChunks[i].alphaTexIndex.w);
                }

            }
        }

        tempUseAlphaTexIndexList.Remove(-1);

        bool isNeedChangeTexArray = false;

        if(useAlphaTexIndexList.Count != tempUseAlphaTexIndexList.Count)
        {
            isNeedChangeTexArray = true;
        }
        else
        {
            for(int i=0;i<tempUseAlphaTexIndexList.Count;i++)
            {
                if(!useAlphaTexIndexList.Contains(tempUseAlphaTexIndexList[i]))
                {
                    isNeedChangeTexArray = true;
                    break;
                }
            }
        }

        for (int i = 0; i < instanceCount; i++)
        {
            if (instanceChunks[i].isShow)
            {
                if (!isNeedChangeTexArray)
                {
                    alphaTexIndexList.Add(new Vector4(useAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.x),
                    useAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.y),
                    useAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.z),
                    useAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.w)));
                }
                else
                {
                    alphaTexIndexList.Add(new Vector4(tempUseAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.x),
                    tempUseAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.y),
                    tempUseAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.z),
                    tempUseAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.w)));
                }
            }
        }



        if (isNeedChangeTexArray && matData != null && matData.TerrainTexArray.Length > 0)
        {
            Texture2D terrainTex = matData.TerrainTexArray[0];
            terrainMapArray = new Texture2DArray(terrainTex.width, terrainTex.height, tempUseAlphaTexIndexList.Count, terrainTex.format, true);
            terrainMapTilingList = new List<Vector4>();

            for(int i=0;i<tempUseAlphaTexIndexList.Count;i++)
            {
                int index = (int)tempUseAlphaTexIndexList[i];
                if(index > matData.TerrainTexArray.Length - 1)
                {
                    continue;
                }

                terrainMapArray.SetPixels(matData.TerrainTexArray[index].GetPixels(), i);
                terrainMapTilingList.Add(matData.TerrainMapTiling[index]);
            }
            terrainMapArray.Apply(true);

            useAlphaTexIndexList = tempUseAlphaTexIndexList;
        }

    }

}
