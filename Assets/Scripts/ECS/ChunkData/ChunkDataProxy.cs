#if UseTerrainECS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace TerrainECS
{
    [RequiresEntityConversion]
    public class ChunkDataProxy : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float4 StartEndUV;

        public float4 AlphaTexIndex;

        public int ChunkIndex;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var data = new ChunkDataComponent
            {
                startEndUV = StartEndUV,
                alphaTexIndex = AlphaTexIndex,
                chunkIndex = ChunkIndex
            };
            dstManager.AddComponentData(entity, data);
        }
    }
}
#endif
