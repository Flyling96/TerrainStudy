#if UseTerrainECS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;


namespace TerrainECS
{
    [SerializeField]
    public struct UpdateLodComponent : IComponentData
    {
        public float selfVertexCount;      //自身一条边顶点数

        public float4 neighborVertexCount; //邻居一条边顶点数

        public float4 neighborChunkIndexs; //邻居索引

    }

}
#endif
