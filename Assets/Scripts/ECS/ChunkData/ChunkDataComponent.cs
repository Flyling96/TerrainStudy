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

        public int chunkIndex;
    }

}
