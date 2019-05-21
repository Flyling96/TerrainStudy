using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TerrainECS
{
    public class ChunkViewOcclusionSystem : JobComponentSystem
    {
        private EntityQuery m_Group;

        protected override void OnCreate()
        {
            m_Group = GetEntityQuery(ComponentType.ReadOnly<ChunkDataComponent>(), ComponentType.ReadWrite<ChunkViewOcclusionComponent>());
        }

        [BurstCompile]
        struct ChunkViewOcclusionJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<Translation> TranslationType;
            public ArchetypeChunkComponentType<ChunkViewOcclusionComponent> ChunkViewOcclusionType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var translations = chunk.GetNativeArray(TranslationType);
                var chunkViewOcclusions = chunk.GetNativeArray(ChunkViewOcclusionType);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var translation = translations[i];
                    var chunkViewOcclusion = chunkViewOcclusions[i];
                    InstanceMgr.AABoundingBox aabb = new InstanceMgr.AABoundingBox();
                    aabb.min = new Vector3(translation.Value.x, chunkViewOcclusion.minAndMaxHeight.x, translation.Value.z);
                    aabb.max = new Vector3(translation.Value.x + chunkViewOcclusion.chunkSize.x, chunkViewOcclusion.minAndMaxHeight.y, translation.Value.z + chunkViewOcclusion.chunkSize.y);
                    chunkViewOcclusions[i] = new ChunkViewOcclusionComponent
                    {
                        minAndMaxHeight = chunkViewOcclusion.minAndMaxHeight,
                        chunkSize = chunkViewOcclusion.chunkSize,
                        isShow = InstanceMgr.instance.IsBoundInCamera(aabb, InstanceMgr.instance.mainCamera)
                    };

                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var translationType = GetArchetypeChunkComponentType<Translation>(true);
            var chunkViewOcclusionType = GetArchetypeChunkComponentType<ChunkViewOcclusionComponent>(false);

            var job = new ChunkViewOcclusionJob()
            {
                TranslationType = translationType,
                ChunkViewOcclusionType = chunkViewOcclusionType
            };

            return job.Schedule(m_Group, inputDeps);
        }
    }
}
