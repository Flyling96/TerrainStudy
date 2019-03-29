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
    private int instanceCount = 0;
    [SerializeField]
    private Vector4[] startEndUVs;
    [SerializeField]
    private MatData matData;

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

    //public Material Mat
    //{
    //    get
    //    {
    //        return mat;
    //    }
    //}

    //public MaterialPropertyBlock Prop
    //{
    //    get
    //    {
    //        return prop;
    //    }
    //}

    public void SetMatData(Texture2D tex,float max)
    {
        if(matData == null)
        {
            matData = new MatData();
        }
        matData.SetValue(tex, max);
    }


    public void InitData(Mesh tempMesh,int countX,int countZ)
    {
        int count = countX * countZ;
        mesh = tempMesh;
        trs = new Matrix4x4[count];
        startEndUVs = new Vector4[count];
        instanceCount = count;

        int index = 0;
        for (int j = 0; j < countZ; j++)
        {
            for (int i = 0; i < countX; i++)
            {
                index = j * countX + i;
                startEndUVs[index] = (new Vector4((float)i / countX, (float)j / countZ, (float)(i + 1) / countX, (float)(j + 1) / countZ));
            }
        }
    }

    void Init()
    {
        prop = new MaterialPropertyBlock();
        mat = new Material(Shader.Find(shaderName));
        mat.enableInstancing = true;
        sourcePos = transform.position;
    }


    public void AddTRS(Matrix4x4 matr,int index)
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
    }

    Vector3 sourcePos;

    public void Draw()
    {
        if (instanceCount == 0 || mesh == null) return;

        for(int i = 0;i< trs.Length;i++)
        {
            trs[i].m03 += (transform.position.x - sourcePos.x);
            trs[i].m13 += (transform.position.y - sourcePos.y);
            trs[i].m23 += (transform.position.z - sourcePos.z);
        }
        sourcePos = transform.position;

        UpdateMaterial();

        Graphics.DrawMeshInstanced(mesh, 0, mat, trs, instanceCount, prop);  

    }

}
