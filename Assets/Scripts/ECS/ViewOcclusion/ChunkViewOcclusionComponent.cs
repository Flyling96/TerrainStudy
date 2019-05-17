using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;

namespace TerrainECS
{
    [SerializeField]
    public struct ChunkViewOcclusionComponent : IComponentData
    {
        public float2 minAndMaxHeight;     //最小和最大的高度

        public float2 chunkSize;

        public bool isShow;          //剔除是否显示
    }
}
