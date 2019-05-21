using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TerrainECS
{
    public class UpdateLodSystem : JobComponentSystem
    {
        private EntityQuery m_Group;

        protected override void OnCreate()
        {
            m_Group = GetEntityQuery(typeof(Translation),ComponentType.ReadWrite<UpdateLodComponent>());
        }

        struct UpdateLodJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<Translation> TranslationType;
            public ArchetypeChunkComponentType<UpdateLodComponent> UpdateLodType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var chunkTranslations = chunk.GetNativeArray(TranslationType);
                var chunkUpdateLods = chunk.GetNativeArray(UpdateLodType);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var translation = chunkTranslations[i];
                    var updateLodLevel = chunkUpdateLods[i];

                    int lodLevel = InstanceMgr.instance.CaculateLodLevel(translation.Value, new Vector2(2000,0));
                    float4 neighborChunkIndexs = chunkUpdateLods[i].neighborChunkIndexs;

                    chunkUpdateLods[i] = new UpdateLodComponent
                    {
                        selfVertexCount = InstanceMgr.instance.CacuTessCount(lodLevel),
                        neighborChunkIndexs = neighborChunkIndexs
                    };
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var translationType = GetArchetypeChunkComponentType<Translation>(true);
            var updateLodType = GetArchetypeChunkComponentType<UpdateLodComponent>(false);

            var job = new UpdateLodJob()
            {
                TranslationType = translationType,
                UpdateLodType = updateLodType,
            };

            return job.Schedule(m_Group, inputDeps);
        }
    }
}
