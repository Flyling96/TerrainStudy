#if UseTerrainECS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Transforms;

namespace TerrainECS
{
    public class ECSDataManager : Singleton<ECSDataManager>
    {
        Dictionary<int, ChunkDataComponent> chunkDataDic;

        public Dictionary<int, ChunkDataComponent> ChunkDataDic
        {
            get
            {
                return chunkDataDic;
            }
        }

        Dictionary<int, UpdateLodComponent> updateLodDic;

        public Dictionary<int, UpdateLodComponent> UpdateLodDic
        {
            get
            {
                return updateLodDic;
            }
        }

        Dictionary<int, LocalToWorld> localToWorldDic;

        public Dictionary<int, LocalToWorld> LocalToWorldDic
        {
            get
            {
                return localToWorldDic;
            }
        }

        Dictionary<int, ChunkViewOcclusionComponent> chunkViewOcclusionDic;

        public Dictionary<int,ChunkViewOcclusionComponent> ChunkViewOcclusuinDic
        {
            get
            {
                return chunkViewOcclusionDic;
            }
        }

        private void Awake()
        {
            chunkDataDic = new Dictionary<int, ChunkDataComponent>();
            updateLodDic = new Dictionary<int, UpdateLodComponent>();
            localToWorldDic = new Dictionary<int, LocalToWorld>();
            chunkViewOcclusionDic = new Dictionary<int, ChunkViewOcclusionComponent>();
        }

        private static object _lock = new object();

        public void SetChunkDataDic(int chunkIndex, ChunkDataComponent chunkData)
        {
            lock (_lock)
            {
                if (chunkDataDic.ContainsKey(chunkIndex))
                {
                    chunkDataDic[chunkIndex] = chunkData;
                }
                else
                {
                    chunkDataDic.Add(chunkIndex, chunkData);
                }
            }
        }


        public void SetUpdateLodDic(int chunkIndex, UpdateLodComponent updateLodData)
        {
            lock (_lock)
            {
                if (updateLodDic.ContainsKey(chunkIndex))
                {
                    updateLodDic[chunkIndex] = updateLodData;
                }
                else
                {
                    updateLodDic.Add(chunkIndex, updateLodData);
                }
            }
        }

        public void SetLocalToWorldDic(int chunkIndex, LocalToWorld localToWorld)
        {
            lock (_lock)
            {
                if (localToWorldDic.ContainsKey(chunkIndex))
                {
                    localToWorldDic[chunkIndex] = localToWorld;
                }
                else
                {
                    localToWorldDic.Add(chunkIndex, localToWorld);
                }
            }
        }

        public void SetChunkViewOcclusionDic(int chunkIndex, ChunkViewOcclusionComponent chunkViewOcclusion)
        {
            lock (_lock)
            {
                if (chunkViewOcclusionDic.ContainsKey(chunkIndex))
                {
                    chunkViewOcclusionDic[chunkIndex] = chunkViewOcclusion;
                }
                else
                {
                    chunkViewOcclusionDic.Add(chunkIndex, chunkViewOcclusion);
                }
            }
        }


    }
}
#endif
