using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[ExecuteInEditMode]
public class TerrainInstance : TerrainInstanceSubClass
{

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
        int result = 0;
        switch (lodLevel)
        {
            case 1:
                result = 100;
                break;
            case 2:
                result = 50;
                break;
            case 3:
                result = 20;
                break;
            case 4:
                result = 5;
                break;
            case 5:
                result = 1;
                break;
            default:
                result = 1;
                break;
        }
        result = result * chunkWidth / 100;
        return result < 1 ? 1 : result;
    }

    public override void Draw()
    {
        base.Draw();
        if (instanceCount == 0 || mesh == null ||!isInit) return;

        UpdateTRS();
        UpdateLodLevel();
        ViewOcclusion();

        if (showChunkCount < 1)
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

        if (showChunkCount < 1)
        {
            return;
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
