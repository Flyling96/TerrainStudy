using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;

namespace TerrainECS
{
    public class ChunkViewOcclusionSystem : JobComponentSystem
    {
        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            throw new System.NotImplementedException();
        }
    }
}
