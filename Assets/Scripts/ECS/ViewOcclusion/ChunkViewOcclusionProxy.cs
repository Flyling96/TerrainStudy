#if UseTerrainECS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace TerrainECS
{

    [RequiresEntityConversion]
    public class ChunkViewOcclusionProxy : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float2 MinAndMaxHeight;

        public float2 ChunkSize;

        public bool IsShow;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var data = new ChunkViewOcclusionComponent
            {
                minAndMaxHeight = MinAndMaxHeight,
                chunkSize = ChunkSize,
                isShow = IsShow
            };
            dstManager.AddComponentData(entity, data);
        }
    }

}
#endif