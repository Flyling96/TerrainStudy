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
    private Matrix4x4[] trs;
    [SerializeField]
    private int instanceCountX = 0;
    [SerializeField]
    private int instanceCountZ = 0;
    [SerializeField]
    private Vector4[] startEndUVs;
    [SerializeField]
    private Vector2[] chunkMinAndMaxHeights;
    [SerializeField]
    private Vector4[] alphaTexIndexs;
    [SerializeField]
    private MatData matData;

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
        mesh = tempMesh;
        trs = new Matrix4x4[count];
        alphaTexIndexs = tAlphaTexIndexs;
        startEndUVs = new Vector4[count];
        chunkMinAndMaxHeights = tMinAndMaxHeight;
        instanceCount = count;
        instanceCountX = countX;
        instanceCountZ = countZ;

        int index = 0;
        for (int j = 0; j < countZ; j++)
        {
            for (int i = 0; i < countX; i++)
            {
                index = j * countX + i;
                startEndUVs[index] = (new Vector4((float)i / countX, (float)j / countZ, (float)(i + 1) / countX, (float)(j + 1) / countZ));
            }
        }

        Matrix4x4 matr;
        index = 0;
        for (int j = 0; j < countZ; j++)
        {
            for (int i = 0; i < countX; i++)
            {
                index = j * countX + i;
                matr = new Matrix4x4();
                matr.SetTRS(new Vector3(i * chunkWidth, 0, j * chunkLength),
                    rotation, Vector3.one);
                AddTRS(matr, index);
            }
        }

        Init();
    }

    float[] lodLevels;
    float[] selfVertexCounts;
    Vector4[] neighborVertexCounts;
    bool isInit = false;

    void Init()
    {
        isInit = true;
        instanceCount = instanceCountX * instanceCountZ;
        prop = new MaterialPropertyBlock();

        mat = new Material(Shader.Find(shaderName));
        mat.enableInstancing = true;
        InitMat();

        tempTrs = new Matrix4x4[instanceCount];
        chunkPoints = new Vector3[instanceCount, 4];
        if (trs!=null)
        {
            tempTrs = trs.Clone() as Matrix4x4[];
        }

        if(instanceCountZ>1)
        {
            chunkDis = new Vector3(trs[instanceCountX + 1].m03 - trs[0].m03,
                0, trs[instanceCountX + 1].m23 - trs[0].m23);
        }
        else
        {
            chunkDis = new Vector3(trs[1].m03 - trs[0].m03, 0, 0);
        }


        lodLevels = new float[instanceCount];
        selfVertexCounts = new float[instanceCount];
        neighborVertexCounts = new Vector4[instanceCount];
        sourcePos = transform.position;



    }

    void InitMat()
    {
        if (mat == null || matData == null) return;
        for(int i=0;i<instanceCount;i++)
        {
            chunkMinAndMaxHeights[i] *= matData.MaxHeight;
        }
        mat.SetTexture("_HeightNormalTex", matData.HeightNormalTex);
        mat.SetFloat("_MaxHeight", matData.MaxHeight);
        mat.SetTexture("_AlphaMap", matData.AlphaMap);
        mat.SetVector("_ChunkPixelCount", matData.ChunkPixelCount);
        mat.SetVector("_MapSize", new Vector4(matData.HeightNormalTex.width, matData.HeightNormalTex.height, matData.AlphaMap.width, matData.AlphaMap.height));
    }


    void AddTRS(Matrix4x4 matr,int index)
    {
        if (index >= trs.Length) return;
        trs[index] = matr;
    }

    void UpdateMatProp()
    {
        if(prop == null|| mat == null)
        {
            return;
        }

        mat.SetTexture("_TerrainMapArray", matData.TerrainMapArray);
        mat.SetVectorArray("_TerrainMapSize", matData.TerrainMapTiling);

        //prop.SetVectorArray("_StartEndUV", startEndUVs);
        //prop.SetVectorArray("_AlphaTexIndexs", alphaTexIndexs);
        //prop.SetVectorArray("_TessVertexCounts", neighborVertexCounts);
        //prop.SetFloatArray("_LODTessVertexCounts", selfVertexCounts);

        //if (matData != null)
        //{
        //    mat.SetTexture("_TerrainMapArray", terrainMapArray);
        //    mat.SetVectorArray("_TerrainMapSize", terrainMapTilingList.ToArray());
        //}

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
            instancePos = new Vector3(tempTrs[i].m03, tempTrs[i].m13, tempTrs[i].m23);
            lodLevels[i] = CaculateLodLevel(instancePos,chunkMinAndMaxHeights[i]);
            selfVertexCounts[i] = CacuTessCount(lodLevels[i]);
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
        UpdateMatProp();

        Graphics.DrawMeshInstanced(mesh, 0, mat, trsList.ToArray(), instanceIndexInViewList.Count, prop);  

    }

    void UpdateTRS()
    {
        Vector3 tempPos;
        //位移
        for (int i = 0; i < tempTrs.Length; i++)
        {
            tempTrs[i].m03 += (transform.position.x - sourcePos.x);
            tempTrs[i].m13 += (transform.position.y - sourcePos.y);
            tempTrs[i].m23 += (transform.position.z - sourcePos.z);

            tempPos = new Vector3(tempTrs[i].m03, tempTrs[i].m13, tempTrs[i].m23);

            chunkPoints[i, 0] = tempPos +  new Vector3(-chunkDis.x * 0.5f ,0,-chunkDis.z * 0.5f);
            chunkPoints[i, 1] = tempPos + new Vector3(-chunkDis.x * 0.5f, 0,chunkDis.z * 0.5f);
            chunkPoints[i, 2] = tempPos + new Vector3(chunkDis.x * 0.5f, 0,chunkDis.z * 0.5f);
            chunkPoints[i, 3] = tempPos + new Vector3(chunkDis.x * 0.5f, 0,-chunkDis.z * 0.5f);
        }

        sourcePos = transform.position;

        //y轴相关旋转
        //Vector3 angle = transform.eulerAngles;
        //for (int i = 0; i < tempTrs.Length; i++)
        //{
        //    tempTrs[i].m00 = Mathf.Cos(angle.y * Mathf.Deg2Rad);
        //    tempTrs[i].m02 = -Mathf.Sin(angle.y * Mathf.Deg2Rad);
        //    tempTrs[i].m20 = Mathf.Sin(angle.y * Mathf.Deg2Rad);
        //    tempTrs[i].m22 = Mathf.Cos(angle.y * Mathf.Deg2Rad);
        //}


    }

    //剔除相关List
    List<Vector4> startEndUVList = new List<Vector4>();
    List<Vector4> alphaTexIndexList = new List<Vector4>();
    List<Vector4> neighborVertexCountList = new List<Vector4>();
    List<float> selfVertexCountList = new List<float>();
    Texture2DArray terrainMapArray;
    List<Vector4> terrainMapTilingList = new List<Vector4>();
    List<int> instanceIndexInViewList = new List<int>();
    List<Matrix4x4> trsList = new List<Matrix4x4>();

    //cpu剔除块
    void ViewOcclusion()
    {
        instanceIndexInViewList.Clear();
        for(int i =0;i<chunkPoints.GetLength(0);i++)
        {
            if (InstanceMgr.instance.IsInView(chunkPoints[i, 0], chunkPoints[i, 1], chunkPoints[i, 2], chunkPoints[i, 3]))
            {
                instanceIndexInViewList.Add(i);
            }
        }

        //确定剔除块之后再计算周围块的lod等级
        Vector4 neighborVertex;//上下左右
        for (int j = 0; j < instanceCountZ; j++)
        {
            for (int i = 0; i < instanceCountX; i++)
            {
                int instanceIndex = j * instanceCountX + i;
                neighborVertex = new Vector4();
                neighborVertex.x = (j == instanceCountZ - 1 || !instanceIndexInViewList.Contains((j + 1) * instanceCountX + i)) 
                    ? selfVertexCounts[instanceIndex] : selfVertexCounts[(j + 1) * instanceCountX + i];//上
                neighborVertex.y = (j == 0 || !instanceIndexInViewList.Contains((j - 1) * instanceCountX + i))
                    ? selfVertexCounts[instanceIndex] : selfVertexCounts[(j - 1) * instanceCountX + i];//下
                neighborVertex.z = (i == 0 || !instanceIndexInViewList.Contains(j * instanceCountX + i - 1)) 
                    ? selfVertexCounts[instanceIndex] : selfVertexCounts[j * instanceCountX + i - 1];//左
                neighborVertex.w = (i == instanceCountX - 1 || !instanceIndexInViewList.Contains(j * instanceCountX + i + 1)) 
                    ? selfVertexCounts[instanceIndex] : selfVertexCounts[j * instanceCountX + i + 1];//右
                //以小的为准，防止接缝
                neighborVertex.x = Mathf.Min(selfVertexCounts[instanceIndex], neighborVertex.x);
                neighborVertex.y = Mathf.Min(selfVertexCounts[instanceIndex], neighborVertex.y);
                neighborVertex.z = Mathf.Min(selfVertexCounts[instanceIndex], neighborVertex.z);
                neighborVertex.w = Mathf.Min(selfVertexCounts[instanceIndex], neighborVertex.w);
                neighborVertexCounts[j * instanceCountX + i] = neighborVertex;
            }
        }

        startEndUVList.Clear();
        alphaTexIndexList.Clear();
        selfVertexCountList.Clear();
        neighborVertexCountList.Clear();
        trsList.Clear();

        List<float> useAlphaTexIndexList = new List<float>();

        for (int i=0;i<instanceCount;i++)
        {
            if(instanceIndexInViewList.Contains(i))
            {
                startEndUVList.Add(startEndUVs[i]);
                neighborVertexCountList.Add(neighborVertexCounts[i]);
                selfVertexCountList.Add(selfVertexCounts[i]);
                alphaTexIndexList.Add(alphaTexIndexs[i]);
                trsList.Add(tempTrs[i]);
                if (!useAlphaTexIndexList.Contains(alphaTexIndexs[i].x))
                {
                    useAlphaTexIndexList.Add(alphaTexIndexs[i].x);
                }
                if (!useAlphaTexIndexList.Contains(alphaTexIndexs[i].y))
                {
                    useAlphaTexIndexList.Add(alphaTexIndexs[i].y);
                }
                if (!useAlphaTexIndexList.Contains(alphaTexIndexs[i].z))
                {
                    useAlphaTexIndexList.Add(alphaTexIndexs[i].z);
                }
                if (!useAlphaTexIndexList.Contains(alphaTexIndexs[i].w))
                {
                    useAlphaTexIndexList.Add(alphaTexIndexs[i].w);
                }

            }
        }

        return;

        if (matData != null && matData.TerrainTexArray.Length>0)
        {
            Texture2D terrainTex = matData.TerrainTexArray[0];
            terrainMapArray = new Texture2DArray(terrainTex.width, terrainTex.height, useAlphaTexIndexList.Count, terrainTex.format, false);
            terrainMapTilingList = new List<Vector4>();
            for(int i=0;i< matData.TerrainTexArray.Length;i++)
            {
                if (useAlphaTexIndexList.Contains(i))
                {
                    terrainMapArray.SetPixels(matData.TerrainTexArray[i].GetPixels(), useAlphaTexIndexList.IndexOf(i));
                    terrainMapTilingList.Add(matData.TerrainMapTiling[i]);
                }
            }
        }

    }

}
