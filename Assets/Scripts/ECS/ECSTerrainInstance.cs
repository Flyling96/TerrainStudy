using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TerrainECS
{
    [ExecuteInEditMode]
    public class ECSTerrainInstance : TerrainInstanceSubClass
    {
        public void Awake()
        {
            //InstanceMgr.instance.chunkSize = chunkSize;
        }

        public override void InitData(Mesh tempMesh, int countX, int countZ, int tChunkWidth, int tChunkLength, Quaternion rotation, Vector4[] tAlphaTexIndexs, Vector2[] tMinAndMaxHeight, Vector4[] startEndUVs)
        {
            base.InitData(tempMesh, countX, countZ, tChunkWidth, tChunkLength, rotation, tAlphaTexIndexs, tMinAndMaxHeight, startEndUVs);

            Matrix4x4 matr;
            for (int j = 0; j < countZ; j++)
            {
                for (int i = 0; i < countX; i++)
                {
                    int index = j * countX + i;
                    instanceChunkGos[index].AddComponent<ConvertToEntity>();
                    ChunkDataProxy chunkDataProxy = instanceChunkGos[index].AddComponent<ChunkDataProxy>();
                    chunkDataProxy.StartEndUV = startEndUVs[index];
                    chunkDataProxy.AlphaTexIndex = tAlphaTexIndexs[index];
                    chunkDataProxy.ChunkIndex = index;
                    matr = new Matrix4x4();
                    matr.SetTRS(instanceChunkGos[index].transform.position, rotation, Vector3.one);

                    UpdateLodProxy updateLodProxy = instanceChunkGos[index].AddComponent<UpdateLodProxy>();
                    updateLodProxy.NeighborChunkIndexs = new float4();
                    updateLodProxy.NeighborChunkIndexs[0] = (j == countZ - 1) ? -1 : (j + 1) * countX + i;//上
                    updateLodProxy.NeighborChunkIndexs[1] = (j == 0) ? -1 : (j - 1) * countX + i;//下
                    updateLodProxy.NeighborChunkIndexs[2] = (i == 0) ? -1 : j * countX + i - 1;//左
                    updateLodProxy.NeighborChunkIndexs[3] = (i == countX - 1) ? -1 : j * countX + i + 1;//右
                    updateLodProxy.SelfVertexCount = 1;
                    updateLodProxy.NeighborVertexCount = new float4(1, 1, 1, 1);

                    ChunkViewOcclusionProxy chunkViewOcclusionProxy = instanceChunkGos[index].AddComponent<ChunkViewOcclusionProxy>();
                    chunkViewOcclusionProxy.MinAndMaxHeight = tMinAndMaxHeight[index] * matData.MaxHeight;
                    chunkViewOcclusionProxy.ChunkSize = chunkSize;

                }
            }

        }

        List<Vector4> startEndUVList = new List<Vector4>();
        List<Vector4> alphaTexIndexList = new List<Vector4>();
        List<Matrix4x4> trsList = new List<Matrix4x4>();
        List<float> selfVertexCountList = new List<float>();
        List<Vector4> neighborVertexCountList = new List<Vector4>();

        void UpdatePropInfo()
        {
            if (mat == null)
            {
                return;
            }

            startEndUVList.Clear();
            trsList.Clear();
            selfVertexCountList.Clear();
            neighborVertexCountList.Clear();
            alphaTexIndexList.Clear();


            Dictionary<int, UpdateLodComponent> lodLevelDic = ECSDataManager.instance.UpdateLodDic;

            foreach (int key in ECSDataManager.instance.ChunkDataDic.Keys)
            {
                ChunkViewOcclusionComponent chunkViewOcclusion = ECSDataManager.instance.ChunkViewOcclusuinDic[key];
                if(!chunkViewOcclusion.isShow)
                {
                    continue;
                }
                ChunkDataComponent chunkData = ECSDataManager.instance.ChunkDataDic[key];
                startEndUVList.Add(chunkData.startEndUV);
                alphaTexIndexList.Add(chunkData.alphaTexIndex);
                LocalToWorld localToWorld = ECSDataManager.instance.LocalToWorldDic[key];
                trsList.Add(localToWorld.Value);
                UpdateLodComponent updateLod = ECSDataManager.instance.UpdateLodDic[key];
                selfVertexCountList.Add(updateLod.selfVertexCount);

                float4 neighborChunkIndexs = lodLevelDic[key].neighborChunkIndexs;
                neighborVertexCountList.Add(new Vector4(
                    Mathf.Min(lodLevelDic[key].selfVertexCount, (neighborChunkIndexs[0] != -1 ? lodLevelDic[(int)neighborChunkIndexs[0]].selfVertexCount : lodLevelDic[key].selfVertexCount)),
                    Mathf.Min(lodLevelDic[key].selfVertexCount, (neighborChunkIndexs[1] != -1 ? lodLevelDic[(int)neighborChunkIndexs[1]].selfVertexCount : lodLevelDic[key].selfVertexCount)),
                    Mathf.Min(lodLevelDic[key].selfVertexCount, (neighborChunkIndexs[2] != -1 ? lodLevelDic[(int)neighborChunkIndexs[2]].selfVertexCount : lodLevelDic[key].selfVertexCount)),
                    Mathf.Min(lodLevelDic[key].selfVertexCount, (neighborChunkIndexs[3] != -1 ? lodLevelDic[(int)neighborChunkIndexs[3]].selfVertexCount : lodLevelDic[key].selfVertexCount))
                    ));
            }


            //mat.SetTexture("_TerrainMapArray", matData.TerrainMapArray);
            //mat.SetVectorArray("_TerrainMapSize", matData.TerrainMapTiling);

            ////if (matData != null)
            ////{
            ////    mat.SetTexture("_TerrainMapArray", terrainMapArray);
            ////    mat.SetVectorArray("_TerrainMapSize", terrainMapTilingList.ToArray());
            ////}

            //prop = new MaterialPropertyBlock();
            //if (startEndUVList.Count < 1) return;

            //prop.SetVectorArray("_StartEndUV", startEndUVList.ToArray());
            //prop.SetVectorArray("_AlphaTexIndexs", alphaTexIndexList.ToArray());
            //prop.SetVectorArray("_TessVertexCounts", neighborVertexCountList.ToArray());
            //prop.SetFloatArray("_LODTessVertexCounts", selfVertexCountList.ToArray());

        }

        void UpdateMat()
        {

            if (mat != null && matData != null)
            {
                mat.SetTexture("_TerrainMapArray", matData.TerrainMapArray);
                mat.SetVectorArray("_TerrainMapSize", matData.TerrainMapTiling);
            }

        }

        void DrawInstance(int index)
        {
            int maxListCount = trsList.Count > (index + 1) * 1023 ? 1023 : trsList.Count - index * 1023;

            prop = new MaterialPropertyBlock();

            prop.SetVectorArray("_StartEndUV", startEndUVList.GetRange(index * 1023, maxListCount).ToArray());
            prop.SetVectorArray("_AlphaTexIndexs", alphaTexIndexList.GetRange(index * 1023, maxListCount).ToArray());
            prop.SetVectorArray("_TessVertexCounts", neighborVertexCountList.GetRange(index * 1023, maxListCount).ToArray());
            prop.SetFloatArray("_LODTessVertexCounts", selfVertexCountList.GetRange(index * 1023, maxListCount).ToArray());

            Graphics.DrawMeshInstanced(mesh, 0, mat, trsList.GetRange(index * 1023, maxListCount).ToArray(), maxListCount, prop);

        }


        public override void Draw()
        {
            if (instanceCount == 0 || mesh == null || !isInit) return;
            if (!Application.isPlaying) return;

            base.Draw();

            UpdatePropInfo();

            if (trsList.Count < 1) return;

            UpdateMat();

            for (int i = 0; i < trsList.Count / 1023 + 1; i++)
            {
                DrawInstance(i);
            }

        }
    }
}
