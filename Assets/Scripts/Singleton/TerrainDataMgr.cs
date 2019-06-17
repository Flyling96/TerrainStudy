using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomTerrain
{
    [ExecuteInEditMode]
    public class TerrainDataMgr : Singleton<TerrainDataMgr>
    {
        //地形Lod的最大等级
        public Vector2 maxLodVertexCount = new Vector2(63, 63);

        TerrainInstance insideTerrainInstance = null;
        InstanceChunk insideInstanceChunk = null;
        List<InstanceChunk> needColliderChunkList = new List<InstanceChunk>();

        Dictionary<int, TerrainInstance> terrainInstanceDic = new Dictionary<int, TerrainInstance>();

        public void Register(TerrainInstance terrainInstance)
        {
            terrainInstanceDic[terrainInstance.instanceIndex] = terrainInstance;
        }

        public void Remove(TerrainInstance terrainInstance)
        {
            if (terrainInstanceDic.ContainsKey(terrainInstance.instanceIndex))
            {
                terrainInstanceDic.Remove(terrainInstance.instanceIndex);
            }
        }

        private void Start()
        {
            RefreshCollider();
        }

        public InstanceChunk GetInsideChunk(Vector3 pos)
        {
            if(insideTerrainInstance == null)
            {
                insideTerrainInstance = (TerrainInstance)InstanceMgr.instance.InstanceList[0];
            }

            return insideTerrainInstance.GetChunkByPos(pos);
        }

        private void Update()
        {
            RefreshCollider();
        }

        void RefreshCollider()
        {
            if (!terrainInstanceDic.ContainsKey(0) || RenderPipeline.instance == null) return;
            insideTerrainInstance = terrainInstanceDic[0];
            InstanceChunk tempInstanceChunk = GetInsideChunk(RenderPipeline.instance.mainCameraPos);
            if(tempInstanceChunk == insideInstanceChunk && needColliderChunkList.Contains(tempInstanceChunk)|| tempInstanceChunk == null)
            {
                return;
            }
            insideInstanceChunk = tempInstanceChunk;
            //needColliderChunkList.Clear();

            //九宫格
            if(!needColliderChunkList.Contains(insideInstanceChunk))
                needColliderChunkList.Add(insideInstanceChunk);

            if (insideInstanceChunk.neighborChunk[0] != null)//上
            {
                if (!needColliderChunkList.Contains(insideInstanceChunk.neighborChunk[0]))
                    needColliderChunkList.Add(insideInstanceChunk.neighborChunk[0]);
                if(insideInstanceChunk.neighborChunk[0].neighborChunk[2]!=null &&
                    !needColliderChunkList.Contains(insideInstanceChunk.neighborChunk[0].neighborChunk[2]))//左上
                {
                    needColliderChunkList.Add(insideInstanceChunk.neighborChunk[0].neighborChunk[2]);
                }
                if (insideInstanceChunk.neighborChunk[0].neighborChunk[3] != null &&
                    !needColliderChunkList.Contains(insideInstanceChunk.neighborChunk[0].neighborChunk[3]))//右上
                {
                    needColliderChunkList.Add(insideInstanceChunk.neighborChunk[0].neighborChunk[3]);
                }
            }

            if (insideInstanceChunk.neighborChunk[1] != null)//下
            {
                if (!needColliderChunkList.Contains(insideInstanceChunk))
                    needColliderChunkList.Add(insideInstanceChunk.neighborChunk[1]);
                if (insideInstanceChunk.neighborChunk[1].neighborChunk[2] != null &&
                    !needColliderChunkList.Contains(insideInstanceChunk.neighborChunk[1].neighborChunk[2]))//左下
                {
                    needColliderChunkList.Add(insideInstanceChunk.neighborChunk[1].neighborChunk[2]);
                }
                if (insideInstanceChunk.neighborChunk[1].neighborChunk[3] != null &&
                    !needColliderChunkList.Contains(insideInstanceChunk.neighborChunk[1].neighborChunk[3]))//右下
                {
                    needColliderChunkList.Add(insideInstanceChunk.neighborChunk[1].neighborChunk[3]);
                }
            }

            if (insideInstanceChunk.neighborChunk[2] != null)//左
            {
                if (!needColliderChunkList.Contains(insideInstanceChunk.neighborChunk[2]))
                    needColliderChunkList.Add(insideInstanceChunk.neighborChunk[2]);
            }

            if (insideInstanceChunk.neighborChunk[3] != null)//右
            {
                if (!needColliderChunkList.Contains(insideInstanceChunk.neighborChunk[3]))
                    needColliderChunkList.Add(insideInstanceChunk.neighborChunk[3]);
            }

            needColliderChunkList.Remove(null);
            //过多剔除远距离
            needColliderChunkList.Sort(SortChunk);

            if (needColliderChunkList.Count > 16)
            {
                for (int i = needColliderChunkList.Count - 1; i >= 16; i--)
                {
                    InstanceChunk chunk = needColliderChunkList[i];
                    needColliderChunkList.RemoveAt(i);
                    chunk.RemoveCollider();
                }
            }


            for (int i=0;i< needColliderChunkList.Count;i++)
            {
                if (needColliderChunkList[i] != null)
                {
                    if (terrainInstanceDic.ContainsKey(needColliderChunkList[i].instanceIndex))
                    {
                        terrainInstanceDic[needColliderChunkList[i].instanceIndex].RefreshChunkCollider(needColliderChunkList[i].chunkIndex);
                    }
                }
            }
        }

        int SortChunk(InstanceChunk a,InstanceChunk b)
        {
            float dis = Vector2.Distance(new Vector2(RenderPipeline.instance.mainCamera.transform.position.x,
                RenderPipeline.instance.mainCamera.transform.position.z),
                new Vector2(a.transform.position.x, a.transform.position.z));

            float dis1 = Vector2.Distance(new Vector2(RenderPipeline.instance.mainCamera.transform.position.x,
                RenderPipeline.instance.mainCamera.transform.position.z),
                new Vector2(b.transform.position.x, b.transform.position.z));

            return dis > dis1 ? 1 : -1;
        }


        #region LOD计算相关
        public int CaculateLodLevel(Vector3 instancePos, Vector2 chunkMinAndMaxHeight)
        {
            if(RenderPipeline.instance == null)
            {
                return 0;
            }

            if (insideInstanceChunk != null && insideTerrainInstance.GetChunkByPos(instancePos) == insideInstanceChunk)
            {
                return 1;
            }

            float height = Mathf.Abs(chunkMinAndMaxHeight.y - chunkMinAndMaxHeight.x);
            float distance = Vector3.Distance(instancePos, RenderPipeline.instance.mainCameraPos);
            int lodLevel = 1;

            float power = distance * 0.2f + 600 / (height + 1) * 2.0f;

            if (power < 30)
            {
                lodLevel = 1;
            }
            else if (power < 100)
            {
                lodLevel = 2;
            }
            else if (power < 300)
            {
                lodLevel = 3;
            }
            else if (power < 1000)
            {
                lodLevel = 4;
            }
            else
            {
                lodLevel = 5;
            }

            return lodLevel;
        }

        public int CacuTessCount(int lodLevel, int chunkWidth = 100)
        {
            int result = 0;
            switch (lodLevel)
            {
                case 1:
                    result = 63;
                    break;
                case 2:
                    result = 31;
                    break;
                case 3:
                    result = 15;
                    break;
                case 4:
                    result = 7;
                    break;
                case 5:
                    result = 3;
                    break;
                default:
                    result = 1;
                    break;
            }
            //result = result * chunkWidth / 100;
            return result < 1 ? 1 : result;
        }
        #endregion

    }
}
