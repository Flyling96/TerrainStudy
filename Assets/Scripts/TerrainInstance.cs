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
        Texture2D heightNormalTex;
        [SerializeField]
        float maxHeight;
        [SerializeField]
        public bool isChange = false;

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

        public void SetValue(Texture2D tex, float max )
        {
            heightNormalTex = tex;
            maxHeight = max;
            isChange = true;
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
    private MatData matData;

    private int instanceCount = 0;
    private Matrix4x4[] tempTrs;

    private MaterialPropertyBlock prop;
    private Material mat;
    string shaderName = "Unlit/TerrainInstance";

    private void OnEnable()
    {
        InstanceMgr.instance.Register(this);
        if(mat == null)
        {
            Init();
        }
    }

    private void OnDisabled()
    {
        InstanceMgr.instance.Remove(this);
    }

    public void SetMatData(Texture2D tex,float max)
    {
        if(matData == null)
        {
            matData = new MatData();
        }
        matData.SetValue(tex, max);
    }


    public void InitData(Mesh tempMesh,int countX,int countZ,int chunkWidth,int chunkLength,Quaternion rotation)
    {
        int count = countX * countZ;
        mesh = tempMesh;
        trs = new Matrix4x4[count];
        startEndUVs = new Vector4[count];
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
        tempTrs = new Matrix4x4[instanceCount];
        if(trs!=null)
        {
            tempTrs = trs.Clone() as Matrix4x4[];
        }
        lodLevels = new float[instanceCount];
        selfVertexCounts = new float[instanceCount];
        neighborVertexCounts = new Vector4[instanceCount];
        mat.enableInstancing = true;
        sourcePos = transform.position;
    }


    void AddTRS(Matrix4x4 matr,int index)
    {
        if (index >= trs.Length) return;
        trs[index] = matr;
    }

    void UpdateMaterial()
    {
        if (mat == null || prop == null) return;
        if (matData.isChange)
        {
            mat.SetTexture("_HeightNormalTex", matData.HeightNormalTex);
            mat.SetFloat("_MaxHeight", matData.MaxHeight);
            matData.isChange = false;
        }

        prop.SetVectorArray("_StartEndUV", startEndUVs);
        UpdateLodLevel();
        prop.SetVectorArray("_TessVertexCounts", neighborVertexCounts);
        prop.SetFloatArray("_LODTessVertexCounts", selfVertexCounts);
    }

    void UpdateLodLevel()
    {
        Vector3 instancePos;
        for(int i=0;i<instanceCount;i++)
        {
            instancePos = new Vector3(trs[i].m03, trs[i].m13, trs[i].m23);
            lodLevels[i] = CaculateLodLevel(instancePos);
            selfVertexCounts[i] = CacuTessCount(lodLevels[i]);
        }

        Vector4 neighborVertex;//上下左右
        for(int j=0;j<instanceCountZ;j++)
        {
            for(int i=0;i<instanceCountX;i++)
            {
                neighborVertex = new Vector4();
                neighborVertex.x = j == instanceCountZ - 1 ? selfVertexCounts[j * instanceCountX + i] : selfVertexCounts[(j + 1) * instanceCountX + i];//上
                neighborVertex.y = j == 0 ? selfVertexCounts[j * instanceCountX + i] : selfVertexCounts[(j - 1) * instanceCountX + i];//下
                neighborVertex.z = i == 0 ? selfVertexCounts[j * instanceCountX + i] : selfVertexCounts[j * instanceCountX + i - 1];//左
                neighborVertex.w = i == instanceCountX - 1 ? selfVertexCounts[j * instanceCountX + i] : selfVertexCounts[j * instanceCountX + i + 1];//右
                //以小的为准，防止接缝
                neighborVertex.x = Mathf.Min(selfVertexCounts[j * instanceCountX + i], neighborVertex.x);
                neighborVertex.y = Mathf.Min(selfVertexCounts[j * instanceCountX + i], neighborVertex.y);
                neighborVertex.z = Mathf.Min(selfVertexCounts[j * instanceCountX + i], neighborVertex.z);
                neighborVertex.w = Mathf.Min(selfVertexCounts[j * instanceCountX + i], neighborVertex.w);
                neighborVertexCounts[j * instanceCountX + i] = neighborVertex;
            }
        }

    }

    int CaculateLodLevel(Vector3 instancePos)
    {
        float distance = Vector3.Distance(instancePos, InstanceMgr.instance.mainCamera.transform.position);
        int lodLevel = 1;

        if(distance < 30)
        {
            lodLevel = 1;
        }
        else if(distance < 100)
        {
            lodLevel = 2;
        }
        else if(distance < 300)
        {
            lodLevel = 3;
        }
        else if(distance < 1000)
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

        for(int i = 0;i< trs.Length;i++)
        {
            tempTrs[i].m03 += (transform.position.x - sourcePos.x);
            tempTrs[i].m13 += (transform.position.y - sourcePos.y);
            tempTrs[i].m23 += (transform.position.z - sourcePos.z);
        }
        sourcePos = transform.position;

        UpdateMaterial();

        Graphics.DrawMeshInstanced(mesh, 0, mat, tempTrs, instanceCount, prop);  

    }

}
