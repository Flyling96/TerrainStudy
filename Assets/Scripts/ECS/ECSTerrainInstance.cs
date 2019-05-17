using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace TerrainECS
{
    [ExecuteInEditMode]
    public class ECSTerrainInstance : TerrainInstanceSubClass
    {

        private void Start()
        {
            if(Application.isPlaying)
            {
                var entityManager = World.Active.EntityManager;
                for(int i=0;i< instanceChunks.Length;i++)
                {
                    Entity instance = GameObjectConversionUtility.ConvertGameObjectHierarchy(instanceChunks[i].gameObject, World.Active);
                    entityManager.SetComponentData(instance, new ChunkDataComponent
                    {
                        startEndUV = instanceChunks[i].startEndUV,
                        trsMatrix = instanceChunks[i].trsMatrix
                    });
                }
            }
        }

        List<Vector4> startEndUVList = new List<Vector4>();
        List<Matrix4x4> trsList = new List<Matrix4x4>();
        void UpdateMatProp()
        {
            if (mat == null)
            {
                return;
            }

            startEndUVList.Clear();
            trsList.Clear();

            var entityManager = World.Active.EntityManager;
            var entities = entityManager.GetAllEntities();
            ChunkDataComponent chunkData;

            for (int i=0;i< entities.Length;i++)
            {
                chunkData = entityManager.GetComponentData<ChunkDataComponent>(entities[i]);
                startEndUVList.Add(chunkData.startEndUV);
                trsList.Add(chunkData.trsMatrix);
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
            //prop.SetVectorArray("_TessVertexCounts", neighborVertexCountList.ToArray());
            //prop.SetFloatArray("_LODTessVertexCounts", selfVertexCountList.ToArray());

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
