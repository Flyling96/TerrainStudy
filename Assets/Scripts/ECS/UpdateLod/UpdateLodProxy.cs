#if UseTerrainECS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace TerrainECS
{
    [RequiresEntityConversion]
    public class UpdateLodProxy : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float SelfVertexCount;  

        public float4 NeighborVertexCount;

        public float4 NeighborChunkIndexs;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var data = new UpdateLodComponent
            {
                selfVertexCount = SelfVertexCount,
                neighborVertexCount = NeighborVertexCount,
                neighborChunkIndexs = NeighborChunkIndexs
            };
            dstManager.AddComponentData(entity, data);
        }
    }
}
#endif

