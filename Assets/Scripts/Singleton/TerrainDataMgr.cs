using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomTerrain
{
    public class TerrainDataMgr : Singleton<TerrainDataMgr>
    {
        //地形Lod的最大等级
        public Vector2 maxLodVertexCount = new Vector2(100, 100);
    }
}
