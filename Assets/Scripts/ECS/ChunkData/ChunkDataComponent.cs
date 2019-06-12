#if UseTerrainECS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace TerrainECS
{
    [SerializeField]
    public struct ChunkDataComponent : IComponentData
    {
        public float4 startEndUV;

        public float4 alphaTexIndex;

        public int chunkIndex;
    }

}
#endif
