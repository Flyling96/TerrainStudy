using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TerrainECS
{
    public class ChunkDataSystem : JobComponentSystem
    {
        private EntityQuery m_Group;

        protected override void OnCreate()
        {
            m_Group = GetEntityQuery(typeof(LocalToWorld), ComponentType.ReadOnly<ChunkDataComponent>(), 
                ComponentType.ReadOnly<UpdateLodComponent>(),ComponentType.ReadOnly<ChunkViewOcclusionComponent>());
        }

        [BurstCompile]
        struct ChunkDataJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWroldType;
            [ReadOnly] public ArchetypeChunkComponentType<ChunkDataComponent> ChunkDataType;
            [ReadOnly] public ArchetypeChunkComponentType<UpdateLodComponent> UpdateLodType;
            [ReadOnly] public ArchetypeChunkComponentType<ChunkViewOcclusionComponent> ChunkViewOcclusionType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var chunkLocalToWorlds = chunk.GetNativeArray(LocalToWroldType);
                var chunkUpdateLods = chunk.GetNativeArray(UpdateLodType);
                var chunkDatas = chunk.GetNativeArray(ChunkDataType);
                var chunkViewOcclusions = chunk.GetNativeArray(ChunkViewOcclusionType);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var localToWorld = chunkLocalToWorlds[i];
                    var updateLodLevel = chunkUpdateLods[i];
                    var chunkData = chunkDatas[i];
                    var chunkViewOcclusion = chunkViewOcclusions[i];

                    ECSDataManager.instance.SetUpdateLodDic(chunkData.chunkIndex, updateLodLevel);
                    ECSDataManager.instance.SetChunkDataDic(chunkData.chunkIndex, chunkData);
                    ECSDataManager.instance.SetLocalToWorldDic(chunkData.chunkIndex, localToWorld);
                    ECSDataManager.instance.SetChunkViewOcclusionDic(chunkData.chunkIndex, chunkViewOcclusion);
                    
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var localToWorldType = GetArchetypeChunkComponentType<LocalToWorld>(true);
            var updateLodType = GetArchetypeChunkComponentType<UpdateLodComponent>(true);
            var chunkdataType = GetArchetypeChunkComponentType<ChunkDataComponent>(true);
            var chunkViewOcclusionType = GetArchetypeChunkComponentType<ChunkViewOcclusionComponent>(true);

            var job = new ChunkDataJob()
            {
                LocalToWroldType = localToWorldType,
                UpdateLodType = updateLodType,
                ChunkDataType = chunkdataType,
                ChunkViewOcclusionType = chunkViewOcclusionType
            };

            return job.Schedule(m_Group, inputDeps);
        }
    }
}
