﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[ExecuteInEditMode]
public class TerrainInstance : TerrainInstanceSubClass
{
    [SerializeField]
    private InstanceChunk[] instanceChunks;

    public override void InitData(Mesh tempMesh, int countX, int countZ, int tChunkWidth, int tChunkLength, Quaternion rotation, Vector4[] tAlphaTexIndexs, Vector2[] tMinAndMaxHeight, Vector4[] startEndUVs)
    {
        base.InitData(tempMesh, countX, countZ, tChunkWidth, tChunkLength, rotation, tAlphaTexIndexs, tMinAndMaxHeight, startEndUVs);
        instanceChunks = new InstanceChunk[instanceCount];
        int index = 0;
        Matrix4x4 matr;
        Vector3 chunkPos;

        for (int j = 0; j < countZ; j++)
        {
            for (int i = 0; i < countX; i++)
            {
                index = j * countX + i;
                instanceChunks[index] = instanceChunkGos[index].AddComponent<InstanceChunk>();
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

    void UpdateMat()
    {

        if (mat!=null && matData != null)
        {
            mat.SetTexture("_TerrainMapArray", terrainMapArray);
            mat.SetVectorArray("_TerrainMapSize", terrainMapTilingList.ToArray());
        }

    }

    void DrawInstance(int index)
    {
        int maxListCount = showChunkCount > (index+1) * 1023 ? 1023 : showChunkCount - index * 1023;

        prop = new MaterialPropertyBlock();

        prop.SetVectorArray("_StartEndUV", startEndUVList.GetRange(index * 1023, maxListCount).ToArray());
        prop.SetVectorArray("_AlphaTexIndexs", alphaTexIndexList.GetRange(index * 1023, maxListCount).ToArray());
        prop.SetVectorArray("_TessVertexCounts", neighborVertexCountList.GetRange(index * 1023, maxListCount).ToArray());
        prop.SetFloatArray("_LODTessVertexCounts", selfVertexCountList.GetRange(index * 1023, maxListCount).ToArray());

        Graphics.DrawMeshInstanced(mesh, 0, mat, trsList.GetRange(index * 1023, maxListCount).ToArray(), maxListCount, prop);

    }

    void UpdateLodLevel()
    {
        Vector3 instancePos;
        for(int i=0;i<instanceCount;i++)
        {
            instancePos = instanceChunks[i].transform.position;
            int lodLevel = InstanceMgr.instance.CaculateLodLevel(instancePos, instanceChunks[i].minAndMaxHeight);
            instanceChunks[i].selfVertexCount = InstanceMgr.instance.CacuTessCount(lodLevel,(int)chunkSize.x);
        }

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

        UpdateMat();

        for (int i = 0; i < showChunkCount/1023 + 1; i++)
        {
            DrawInstance(i);
        }

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
    List<int> useAlphaTexIndexList = new List<int>();
    List<int> terrainMapArrayIndexList = new List<int>();
    int showChunkCount = 0;

    const int maxTerrainMapArrayCount = 10;

    //cpu剔除块
    void ViewOcclusion()
    {
        startEndUVList.Clear();
        alphaTexIndexList.Clear();
        selfVertexCountList.Clear();
        neighborVertexCountList.Clear();
        trsList.Clear();
        useAlphaTexIndexList.Clear();
        showChunkCount = 0;

        for(int i=0;i<instanceCount; i++)
        {
            instanceChunks[i].CacuIsBoundInCamera();
            if (instanceChunks[i].IsShow)
            {
                showChunkCount++;
                instanceChunks[i].CaculateNeighborVertexCount();
                startEndUVList.Add(instanceChunks[i].startEndUV);
                selfVertexCountList.Add(instanceChunks[i].selfVertexCount);
                neighborVertexCountList.Add(instanceChunks[i].neighborVertexCount);
                trsList.Add(instanceChunks[i].trsMatrix);

                if (instanceChunks[i].alphaTexIndex.x != -1 && !useAlphaTexIndexList.Contains((int)instanceChunks[i].alphaTexIndex.x))
                {
                    useAlphaTexIndexList.Add((int)instanceChunks[i].alphaTexIndex.x);
                }
                if (instanceChunks[i].alphaTexIndex.y != -1 && !useAlphaTexIndexList.Contains((int)instanceChunks[i].alphaTexIndex.y))
                {
                    useAlphaTexIndexList.Add((int)instanceChunks[i].alphaTexIndex.y);
                }
                if (instanceChunks[i].alphaTexIndex.z != -1 && !useAlphaTexIndexList.Contains((int)instanceChunks[i].alphaTexIndex.z))
                {
                    useAlphaTexIndexList.Add((int)instanceChunks[i].alphaTexIndex.z);
                }
                if (instanceChunks[i].alphaTexIndex.w != -1 && !useAlphaTexIndexList.Contains((int)instanceChunks[i].alphaTexIndex.w))
                {
                    useAlphaTexIndexList.Add((int)instanceChunks[i].alphaTexIndex.w);
                }

            }
        }

        if (showChunkCount < 1 || matData == null)
        {
            return;
        }

        useAlphaTexIndexList.Remove(-1);

 
        if (terrainMapArray == null)
        {
            Texture2D terrainTex = matData.TerrainTexArray[0];
            terrainMapArray = new Texture2DArray(terrainTex.width, terrainTex.height, maxTerrainMapArrayCount, terrainTex.format, true);
            terrainMapArrayIndexList.Clear();
            for (int i=0;i<maxTerrainMapArrayCount;i++)
            {
                terrainMapArrayIndexList.Add(-1);
                terrainMapTilingList.Add(new Vector4(1, 1, 0, 0));
            }
        }

        bool isTerrainMapArrayChange = false;
        for (int i =0;i<useAlphaTexIndexList.Count;i++)
        {
            if(!terrainMapArrayIndexList.Contains(useAlphaTexIndexList[i]))
            {
                for(int j=0;j<terrainMapArrayIndexList.Count;j++)
                {
                    if(terrainMapArrayIndexList[j] == -1)
                    {
                        terrainMapArrayIndexList[j] = useAlphaTexIndexList[i];
                        terrainMapArray.SetPixels(matData.TerrainTexArray[useAlphaTexIndexList[i]].GetPixels(), j);
                        terrainMapTilingList[j] = matData.TerrainMapTiling[useAlphaTexIndexList[i]];
                        isTerrainMapArrayChange = true;
                        break;
                    }
                }
            }
        }

        if(isTerrainMapArrayChange)
        {
            terrainMapArray.Apply();
        }

        for (int i = 0; i < maxTerrainMapArrayCount; i++)
        {
            if (!useAlphaTexIndexList.Contains(terrainMapArrayIndexList[i]))
            {
                terrainMapArrayIndexList[i] = -1;
            }
        }

        for (int i = 0; i < instanceCount; i++)
        {
            if (instanceChunks[i].IsShow)
            {
                alphaTexIndexList.Add(new Vector4(terrainMapArrayIndexList.IndexOf((int)instanceChunks[i].alphaTexIndex.x),
                terrainMapArrayIndexList.IndexOf((int)instanceChunks[i].alphaTexIndex.y),
                terrainMapArrayIndexList.IndexOf((int)instanceChunks[i].alphaTexIndex.z),
                terrainMapArrayIndexList.IndexOf((int)instanceChunks[i].alphaTexIndex.w)));
            }

        }

        //bool isNeedChangeTexArray = false;

        //if(useAlphaTexIndexList.Count != tempUseAlphaTexIndexList.Count)
        //{
        //    isNeedChangeTexArray = true;
        //}
        //else
        //{
        //    for(int i=0;i<tempUseAlphaTexIndexList.Count;i++)
        //    {
        //        if(!useAlphaTexIndexList.Contains(tempUseAlphaTexIndexList[i]))
        //        {
        //            isNeedChangeTexArray = true;
        //            break;
        //        }
        //    }
        //}

        //for (int i = 0; i < instanceCount; i++)
        //{
        //    if (instanceChunks[i].IsShow)
        //    {
        //        if (!isNeedChangeTexArray)
        //        {
        //            alphaTexIndexList.Add(new Vector4(useAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.x),
        //            useAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.y),
        //            useAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.z),
        //            useAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.w)));
        //        }
        //        else
        //        {
        //            alphaTexIndexList.Add(new Vector4(tempUseAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.x),
        //            tempUseAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.y),
        //            tempUseAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.z),
        //            tempUseAlphaTexIndexList.IndexOf(instanceChunks[i].alphaTexIndex.w)));
        //        }
        //    }
        //}

        //if (isNeedChangeTexArray && matData != null && matData.TerrainTexArray.Length > 0)
        //{
        //    Texture2D terrainTex = matData.TerrainTexArray[0];
        //    terrainMapArray = new Texture2DArray(terrainTex.width, terrainTex.height, tempUseAlphaTexIndexList.Count, terrainTex.format, true);
        //    terrainMapTilingList = new List<Vector4>();

        //    for(int i=0;i<tempUseAlphaTexIndexList.Count;i++)
        //    {
        //        int index = (int)tempUseAlphaTexIndexList[i];
        //        if(index > matData.TerrainTexArray.Length - 1)
        //        {
        //            continue;
        //        }

        //        terrainMapArray.SetPixels(matData.TerrainTexArray[index].GetPixels(), i);
        //        terrainMapTilingList.Add(matData.TerrainMapTiling[index]);
        //    }
        //    terrainMapArray.Apply(true);

        //    useAlphaTexIndexList = tempUseAlphaTexIndexList;
        //}

    }

}
