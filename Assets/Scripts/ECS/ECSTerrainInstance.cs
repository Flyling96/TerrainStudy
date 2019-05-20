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

        private void Start()
        {
            if(Application.isPlaying)
            {
                //var entityManager = World.Active.EntityManager;
                //GameObject go = instanceChunks[0].gameObject;
                //for (int i=0;i< instanceChunks.Length;i++)
                //{
                //    Entity instance = GameObjectConversionUtility.ConvertGameObjectHierarchy(go, World.Active);
                //    entityManager.SetComponentData(instance, new ChunkDataComponent
                //    {
                //        startEndUV = instanceChunks[i].startEndUV,
                //        trsMatrix = instanceChunks[i].trsMatrix
                //    });
                //}
            }
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

                }
            }

        }

        List<Vector4> startEndUVList = new List<Vector4>();
        List<Matrix4x4> trsList = new List<Matrix4x4>();
        List<float> selfVertexCountList = new List<float>();
        List<Vector4> neighborVertexCountList = new List<Vector4>();

        void UpdateMatProp()
        {
            if (mat == null)
            {
                return;
            }

            startEndUVList.Clear();
            trsList.Clear();
            selfVertexCountList.Clear();
            neighborVertexCountList.Clear();

            var entityManager = World.Active.EntityManager;
            var entities = entityManager.GetAllEntities();
            Dictionary<int, UpdateLodComponent> lodLevelDic = new Dictionary<int, UpdateLodComponent>();

            for (int i=0;i< entities.Length;i++)
            {
                ChunkDataComponent chunkData = entityManager.GetComponentData<ChunkDataComponent>(entities[i]);
                startEndUVList.Add(chunkData.startEndUV);
                LocalToWorld localToWorld = entityManager.GetComponentData<LocalToWorld>(entities[i]);
                trsList.Add(localToWorld.Value);
                UpdateLodComponent updateLod = entityManager.GetComponentData<UpdateLodComponent>(entities[i]);
                selfVertexCountList.Add(updateLod.selfVertexCount);
                lodLevelDic.Add(chunkData.chunkIndex, updateLod);
            }

            foreach(int chunkIndex in lodLevelDic.Keys)
            {
                float4 neighborChunkIndexs = lodLevelDic[chunkIndex].neighborChunkIndexs;
                neighborVertexCountList.Add(new Vector4(
                    Mathf.Min(lodLevelDic[chunkIndex].selfVertexCount,(neighborChunkIndexs[0] != -1 ? lodLevelDic[(int)neighborChunkIndexs[0]].selfVertexCount : lodLevelDic[chunkIndex].selfVertexCount)),
                    Mathf.Min(lodLevelDic[chunkIndex].selfVertexCount, (neighborChunkIndexs[1] != -1 ? lodLevelDic[(int)neighborChunkIndexs[1]].selfVertexCount : lodLevelDic[chunkIndex].selfVertexCount)),
                    Mathf.Min(lodLevelDic[chunkIndex].selfVertexCount, (neighborChunkIndexs[2] != -1 ? lodLevelDic[(int)neighborChunkIndexs[2]].selfVertexCount : lodLevelDic[chunkIndex].selfVertexCount)),
                    Mathf.Min(lodLevelDic[chunkIndex].selfVertexCount, (neighborChunkIndexs[3] != -1 ? lodLevelDic[(int)neighborChunkIndexs[3]].selfVertexCount : lodLevelDic[chunkIndex].selfVertexCount))
                    ));
            }

            //mat.SetTexture("_TerrainMapArray", matData.TerrainMapArray);
            //mat.SetVectorArray("_TerrainMapSize", matData.TerrainMapTiling);

            //if (matData != null)
            //{
            //    mat.SetTexture("_TerrainMapArray", terrainMapArray);
            //    mat.SetVectorArray("_TerrainMapSize", terrainMapTilingList.ToArray());
            //}

            prop = new MaterialPropertyBlock();

            prop.SetVectorArray("_StartEndUV", startEndUVList.ToArray());
            //prop.SetVectorArray("_AlphaTexIndexs", alphaTexIndexList.ToArray());
            prop.SetVectorArray("_TessVertexCounts", neighborVertexCountList.ToArray());
            prop.SetFloatArray("_LODTessVertexCounts", selfVertexCountList.ToArray());

        }


        public override void Draw()
        {
            if (instanceCount == 0 || mesh == null || !isInit) return;
            if (!Application.isPlaying) return;

            base.Draw();

            UpdateMatProp();

            Graphics.DrawMeshInstanced(mesh, 0, mat, trsList.ToArray(), instanceCount, prop);

        }
    }
}
