using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

namespace CustomTerrain
{
    [ExecuteInEditMode]
    public class TerrainInstance : TerrainInstanceSubClass
    {
        public InstanceChunk[] instanceChunks;

        public TerrainInstance[] neighborInstance = new TerrainInstance[4];//上下左右

        public int instanceIndex;

        public Vector2 InstanceSize
        {
            get
            {
                return new Vector2(chunkCountX * chunkSize.x, chunkCountZ * chunkSize.y);
            }
        }

        public void RefreshNeighborChunk()
        {
            if (neighborInstance[0] != null)//上
            {
                for (int i = 0; i < chunkCountX; i++)
                {
                    int index = (chunkCountZ - 1) * chunkCountX + i;
                    int neighborIndex = i;
                    instanceChunks[index].neighborChunk[0] = neighborInstance[0].instanceChunks[neighborIndex];
                    neighborInstance[0].instanceChunks[neighborIndex].neighborChunk[1] = instanceChunks[index];
                }
            }
            else if (neighborInstance[3] != null)//右
            {
                for (int i = 0; i < chunkCountZ; i++)
                {
                    int index = (i + 1) * chunkCountX - 1;
                    int neighborIndex = i * chunkCountX;
                    instanceChunks[index].neighborChunk[3] = neighborInstance[3].instanceChunks[neighborIndex];
                    neighborInstance[3].instanceChunks[neighborIndex].neighborChunk[2] = instanceChunks[index];
                }
            }
        }

        public override void InitData(int tInstanceIndex, Mesh tempMesh, int countX, int countZ, int tChunkWidth, int tChunkLength, Quaternion rotation, Vector4[] tAlphaTexIndexs, Vector2[] tMinAndMaxHeight, Vector4[] startEndUVs)
        {
            base.InitData(instanceIndex, tempMesh, countX, countZ, tChunkWidth, tChunkLength, rotation, tAlphaTexIndexs, tMinAndMaxHeight, startEndUVs);
            instanceChunks = new InstanceChunk[chunkCount];
            int index = 0;
            Matrix4x4 matr;
            Vector3 chunkPos;

            instanceIndex = tInstanceIndex;

            for (int j = 0; j < countZ; j++)
            {
                for (int i = 0; i < countX; i++)
                {
                    index = j * countX + i;
                    instanceChunks[index] = instanceChunkGos[index].AddComponent<InstanceChunk>();
                    instanceChunks[index].instanceIndex = instanceIndex;
                    instanceChunks[index].chunkIndex = new Vector2(i, j);
                    instanceChunks[index].startEndUV = startEndUVs[index];
                    instanceChunks[index].alphaTexIndex = tAlphaTexIndexs[index];
                    instanceChunks[index].minAndMaxHeight = tMinAndMaxHeight[index] * matData.MaxHeight;
                    matr = new Matrix4x4();
                    chunkPos = transform.position + new Vector3(i * tChunkWidth, 0, j * tChunkLength);
                    matr.SetTRS(chunkPos, rotation, Vector3.one);
                    instanceChunks[index].chunkSize = new Vector2(tChunkWidth, tChunkLength);
                    instanceChunks[index].trsMatrix = matr;
                    instanceChunks[index].transform.position = chunkPos;
                    instanceChunks[index].transform.rotation = rotation;
                    instanceChunks[index].transform.SetParent(transform);
                }
            }

            for (int j = 0; j < countZ; j++)
            {
                for (int i = 0; i < countX; i++)
                {
                    index = j * countX + i;
                    instanceChunks[index].neighborChunk = new InstanceChunk[4];
                    instanceChunks[index].neighborChunk[0] = (j == countZ - 1) ? null : instanceChunks[(j + 1) * countX + i];//上
                    instanceChunks[index].neighborChunk[1] = (j == 0) ? null : instanceChunks[(j - 1) * countX + i];//下
                    instanceChunks[index].neighborChunk[2] = (i == 0) ? null : instanceChunks[j * countX + i - 1];//左
                    instanceChunks[index].neighborChunk[3] = (i == countX - 1) ? null : instanceChunks[j * countX + i + 1];//右
                }
            }
        }

        private void Start()
        {
            InitComputeBuffer();
            InitData();
            InitChunk();
        }

        void InitChunk()
        {
            for (int i = 0; i < instanceChunks.Length; i++)
            {
                instanceChunks[i].Init();
            }
        }

        void UpdateMat()
        {

            if (mat != null && matData != null)
            {
                if (!isUseBigTex)
                {
                    mat.SetTexture("_TerrainMapArray", terrainMapArray);
                    mat.SetFloat("_IsTerrainBigMap", -1);
                }
                else
                {
                    mat.SetTexture("_TerrainBigMap", terrainBigMap);
                    mat.SetVectorArray("_TerrainBigMapChunkUV",terrainBipMapUV);
                    mat.SetFloat("_IsTerrainBigMap", 1);
                }
                mat.SetVectorArray("_TerrainMapSize", terrainMapTilingList.ToArray());
            }

        }

        void DrawInstance(int index)
        {
            if (prop == null)
            {
                Debug.Log("Prop is Null");
                prop = new MaterialPropertyBlock();
            }

            if (mat == null)
            {
                Debug.Log("Mat is Null");
                InitMat();
            }

            int maxListCount = showChunkCount > (index + 1) * 1023 ? 1023 : showChunkCount - index * 1023;

            prop.Clear();

            prop.SetVectorArray("_StartEndUV", startEndUVList.GetRange(index * 1023, maxListCount).ToArray());
            prop.SetVectorArray("_AlphaTexIndexs", alphaTexIndexList.GetRange(index * 1023, maxListCount).ToArray());
            prop.SetVectorArray("_TessVertexCounts", neighborVertexCountList.GetRange(index * 1023, maxListCount).ToArray());
            prop.SetFloatArray("_LODTessVertexCounts", selfVertexCountList.GetRange(index * 1023, maxListCount).ToArray());

            Graphics.DrawMeshInstanced(mesh, 0, mat, trsList.GetRange(index * 1023, maxListCount).ToArray(), maxListCount, prop);

        }

        void UpdateLodLevel()
        {
            Vector3 instancePos;
            for (int i = 0; i < chunkCount; i++)
            {
                instancePos = instanceChunks[i].transform.position;
                int lodLevel = TerrainDataMgr.instance.CaculateLodLevel(instancePos, instanceChunks[i].minAndMaxHeight);
                instanceChunks[i].selfVertexCount = TerrainDataMgr.instance.CacuTessCount(lodLevel, (int)chunkSize.x);
            }

        }

        public override void Draw()
        {
            if (chunkCount == 0 || mesh == null || !isInit || transform == null) return;
            base.Draw();

            UpdateTRS();
            UpdateLodLevel();

            //ViewOcclusionByCPU();
            ViewOcclusionByGPU();
            OcclusionUpdateData();

            if (showChunkCount < 1)
            {
                return;
            }

            UpdateMat();

            for (int i = 0; i < showChunkCount / 1023 + 1; i++)
            {
                DrawInstance(i);
            }


        }

        void UpdateTRS()
        {
            Vector3 tempPos;
            //位移
            if (transform.position != sourcePos)
            {
                for (int i = 0; i < chunkCount; i++)
                {
                    tempPos = new Vector3(instanceChunks[i].trsMatrix.m03, instanceChunks[i].trsMatrix.m13, instanceChunks[i].trsMatrix.m23);
                    tempPos += transform.position - sourcePos;
                    instanceChunks[i].ChangePos(tempPos);
                }
                sourcePos = transform.position;
            }
        }

        #region 剔除及数据组织相关
        //剔除相关List
        List<Vector4> startEndUVList = new List<Vector4>();
        List<Vector4> alphaTexIndexList = new List<Vector4>();
        List<Vector4> neighborVertexCountList = new List<Vector4>();
        List<float> selfVertexCountList = new List<float>();
        Texture2DArray terrainMapArray;
        List<Vector4> terrainMapTilingList = new List<Vector4>();
        List<Matrix4x4> trsList = new List<Matrix4x4>();
        List<int> useAlphaTexIndexList = new List<int>();
        List<int> terrainMapArrayIndexList = new List<int>();

        int showChunkCount = 0;

        const int maxTerrainMapLineCount = 4;//单行列最大图数
        const int maxTerrainBigMapSize = 4096;
        public bool isUseBigTex = false;
        Texture2D terrainBigMap;
        List<Vector4> terrainBipMapUV = new List<Vector4>();

        //cpu剔除块
        void ViewOcclusionByCPU()
        {
            for (int i = 0; i < instanceChunks.Length; i++)
            {
                instanceChunks[i].CacuIsBoundInCamera();
            }
        }

        ComputeBuffer inputBuffer;
        ComputeBuffer resultBuffer;
        ComputeBuffer debugBuffer;
        ComputeShader viewOccusionCS;

        struct ProjectVertex
        {
            Vector4 v0;
            Vector4 v1;
            Vector4 v2;
            Vector4 v3;
            Vector4 v4;
            Vector4 v5;
            Vector4 v6;
            Vector4 v7;
        }

        GeometricTestFunc.AABoundingBox[] aabbs = null;
        ProjectVertex[] prejectVertexs = null;
        float[] isCrossBuffers = null;

        int kernel = -1;

        void ViewOcclusionByGPU()
        {
            if (viewOccusionCS == null || resultBuffer == null || kernel == -1) return;

            viewOccusionCS.SetMatrix("cameraWorldToProjectMat", RenderPipeline.instance.occlusionPrijectionMatrix * RenderPipeline.instance.mainCamera.worldToCameraMatrix);

            viewOccusionCS.SetBuffer(kernel, "aabbArray", inputBuffer);
            viewOccusionCS.SetBuffer(kernel, "isCrossBuffers", resultBuffer);
            viewOccusionCS.SetBuffer(kernel, "projectVertexs", debugBuffer);

            viewOccusionCS.Dispatch(kernel, 16, 16, 1);

            resultBuffer.GetData(isCrossBuffers);
            debugBuffer.GetData(prejectVertexs);

            for (int i = 0; i < instanceChunks.Length; i++)
            {
                instanceChunks[i].IsShow = isCrossBuffers[i] == 1;
            }

        }

        void OcclusionUpdateData()
        {
            startEndUVList.Clear();
            alphaTexIndexList.Clear();
            selfVertexCountList.Clear();
            neighborVertexCountList.Clear();
            trsList.Clear();
            useAlphaTexIndexList.Clear();
            showChunkCount = 0;

            for (int i = 0; i < chunkCount; i++)
            {
                //instanceChunks[i].CacuIsBoundInCamera();
                if (instanceChunks[i].IsShow)
                {
                    showChunkCount++;
                    instanceChunks[i].CaculateNeighborVertexCount();
                    startEndUVList.Add(instanceChunks[i].startEndUV);
                    selfVertexCountList.Add(instanceChunks[i].selfVertexCount);
                    neighborVertexCountList.Add(instanceChunks[i].neighborVertexCount);
                    trsList.Add(instanceChunks[i].trsMatrix);

                    if (instanceChunks[i].alphaTexIndex.x != -1 && !useAlphaTexIndexList.Contains((int)instanceChunks[i].alphaTexIndex.x))
                    {
                        useAlphaTexIndexList.Add((int)instanceChunks[i].alphaTexIndex.x);
                    }
                    if (instanceChunks[i].alphaTexIndex.y != -1 && !useAlphaTexIndexList.Contains((int)instanceChunks[i].alphaTexIndex.y))
                    {
                        useAlphaTexIndexList.Add((int)instanceChunks[i].alphaTexIndex.y);
                    }
                    if (instanceChunks[i].alphaTexIndex.z != -1 && !useAlphaTexIndexList.Contains((int)instanceChunks[i].alphaTexIndex.z))
                    {
                        useAlphaTexIndexList.Add((int)instanceChunks[i].alphaTexIndex.z);
                    }
                    if (instanceChunks[i].alphaTexIndex.w != -1 && !useAlphaTexIndexList.Contains((int)instanceChunks[i].alphaTexIndex.w))
                    {
                        useAlphaTexIndexList.Add((int)instanceChunks[i].alphaTexIndex.w);
                    }

                }
            }

            if (showChunkCount < 1 || matData == null)
            {
                return;
            }

            useAlphaTexIndexList.Remove(-1);


            if (!isUseBigTex)
            {
                if (terrainMapArray == null)
                {
                    Texture2D terrainTex = matData.TerrainTexArray[0];
                    terrainMapArray = new Texture2DArray(terrainTex.width, terrainTex.height, maxTerrainMapLineCount * maxTerrainMapLineCount, terrainTex.format, true);
                    terrainMapArrayIndexList.Clear();
                    terrainMapTilingList.Clear();
                    for (int i = 0; i < maxTerrainMapLineCount * maxTerrainMapLineCount; i++)
                    {
                        terrainMapArrayIndexList.Add(-1);
                        terrainMapTilingList.Add(new Vector4(1, 1, 0, 0));
                    }
                }

                StartCoroutine(RefreshTerrainMapArray());

                //bool isTerrainMapArrayChange = false;

                //for (int i = 0; i < useAlphaTexIndexList.Count; i++)
                //{
                //    if (!terrainMapArrayIndexList.Contains(useAlphaTexIndexList[i]))
                //    {
                //        for (int j = 0; j < terrainMapArrayIndexList.Count; j++)
                //        {
                //            if (terrainMapArrayIndexList[j] == -1)
                //            {
                //                terrainMapArrayIndexList[j] = useAlphaTexIndexList[i];
                //                //terrainMapArray.SetPixels(matData.TerrainTexArray[useAlphaTexIndexList[i]].GetPixels(), j);
                //                Graphics.CopyTexture(matData.TerrainTexArray[useAlphaTexIndexList[i]], 0, 0, terrainMapArray, j, 0);
                //                terrainMapTilingList[j] = matData.TerrainMapTiling[useAlphaTexIndexList[i]];
                //                isTerrainMapArrayChange = true;
                //                break;
                //            }
                //        }
                //    }
                //}

                //if (isTerrainMapArrayChange)
                //{
                //    terrainMapArray.Apply();
                //}
            }
            else
            {
                if(terrainBigMap == null)
                {
                    Texture2D terrainTex = matData.TerrainTexArray[0];
                    terrainBigMap = new Texture2D(terrainTex.width * maxTerrainMapLineCount > maxTerrainBigMapSize? maxTerrainBigMapSize: (terrainTex.width * maxTerrainMapLineCount),
                        terrainTex.height * maxTerrainMapLineCount > maxTerrainBigMapSize ? maxTerrainBigMapSize : (terrainTex.height * maxTerrainMapLineCount),
                        terrainTex.format,true);
                    terrainBigMap.filterMode = FilterMode.Bilinear;
                    terrainBigMap.wrapMode = TextureWrapMode.Clamp;

                    terrainMapArrayIndexList.Clear();
                    terrainMapTilingList.Clear();
                    terrainBipMapUV.Clear();
                    for (int i = 0; i < maxTerrainMapLineCount * maxTerrainMapLineCount; i++)
                    {
                        terrainMapArrayIndexList.Add(-1);
                        terrainMapTilingList.Add(new Vector4(1, 1, 0, 0));
                    }

                    int maxTextureCount = matData.TerrainTexArray.Length > maxTerrainMapLineCount * maxTerrainMapLineCount ?
                        maxTerrainMapLineCount * maxTerrainMapLineCount : matData.TerrainTexArray.Length;

                    float uvPro = 1.0f / maxTerrainMapLineCount;
                    for (int i = 0;i< maxTextureCount; i++)
                    {
                        //向内收缩半像素
                        float minUVX = 0.0f / terrainBigMap.width;
                        float minUVY = 0.0f / terrainBigMap.height;

                        int bigIndexX = i % maxTerrainMapLineCount;
                        int bigIndexY = i / maxTerrainMapLineCount;

                        terrainBipMapUV.Add(new Vector4(bigIndexX * uvPro + minUVX, bigIndexY * uvPro + minUVY,
                            (bigIndexX + 1) * uvPro - minUVX, (bigIndexY + 1) * uvPro - minUVY));
                    }

                }

                bool isTerrainBigMapChange = false;

                for (int i = 0; i < useAlphaTexIndexList.Count; i++)
                {
                    if (!terrainMapArrayIndexList.Contains(useAlphaTexIndexList[i]))
                    {
                        for (int j = 0; j < terrainMapArrayIndexList.Count; j++)
                        {
                            if (terrainMapArrayIndexList[j] == -1)
                            {
                                terrainMapArrayIndexList[j] = useAlphaTexIndexList[i];
                                //terrainMapArray.SetPixels(matData.TerrainTexArray[useAlphaTexIndexList[i]].GetPixels(), j);
                                InsertTexToBigTex(matData.TerrainTexArray[useAlphaTexIndexList[i]], j);
                                terrainMapTilingList[j] = matData.TerrainMapTiling[useAlphaTexIndexList[i]];
                                isTerrainBigMapChange = true;
                                break;
                            }
                        }
                    }
                }

                if (isTerrainBigMapChange)
                {
                    terrainBigMap.Apply();
                }

            }

            //当TerrainMapArray中没有空位了，清除一下没有用到的Texture
            if (!terrainMapArrayIndexList.Contains(-1))
            {
                for (int i = 0; i < maxTerrainMapLineCount * maxTerrainMapLineCount; i++)
                {
                    if (!useAlphaTexIndexList.Contains(terrainMapArrayIndexList[i]))
                    {
                        terrainMapArrayIndexList[i] = -1;
                    }
                }
            }

            for (int i = 0; i < chunkCount; i++)
            {
                if (instanceChunks[i].IsShow)
                {
                    alphaTexIndexList.Add(new Vector4(terrainMapArrayIndexList.IndexOf((int)instanceChunks[i].alphaTexIndex.x),
                    terrainMapArrayIndexList.IndexOf((int)instanceChunks[i].alphaTexIndex.y),
                    terrainMapArrayIndexList.IndexOf((int)instanceChunks[i].alphaTexIndex.z),
                    terrainMapArrayIndexList.IndexOf((int)instanceChunks[i].alphaTexIndex.w)));
                }

            }

        }

        IEnumerator RefreshTerrainMapArray()
        {
            bool isTerrainMapArrayChange = false;
            for (int i = 0; i < useAlphaTexIndexList.Count; i++)
            {
                if (!terrainMapArrayIndexList.Contains(useAlphaTexIndexList[i]))
                {
                    for (int j = 0; j < terrainMapArrayIndexList.Count; j++)
                    {
                        if (terrainMapArrayIndexList[j] == -1)
                        {
                            terrainMapArrayIndexList[j] = useAlphaTexIndexList[i];
                            //terrainMapArray.SetPixels(matData.TerrainTexArray[useAlphaTexIndexList[i]].GetPixels(), j);
                            Graphics.CopyTexture(matData.TerrainTexArray[useAlphaTexIndexList[i]], 0, 0, terrainMapArray, j, 0);
                            terrainMapTilingList[j] = matData.TerrainMapTiling[useAlphaTexIndexList[i]];
                            isTerrainMapArrayChange = true;
                            yield return null;
                            break;
                        }
                    }
                }
            }

            if (isTerrainMapArrayChange)
            {
                terrainMapArray.Apply();
            }
        }

        void InsertTexToBigTex(Texture2D tex,int index)
        {
            int indexX = index % maxTerrainMapLineCount;
            int indexY = index / maxTerrainMapLineCount;

            int bigMapChunkWidth = terrainBigMap.width / maxTerrainMapLineCount;
            int bigMapChunkHeight = terrainBigMap.height / maxTerrainMapLineCount;

            //当一张图太大，加进bigMap时会进行压缩
            for (int j = 0; j < tex.height; j++)
            {
                int bigTexY = indexY * bigMapChunkHeight + (int)(j * bigMapChunkHeight / (float)tex.height);
                for (int i = 0; i < tex.width; i++)
                {
                    int bigTexX = indexX * bigMapChunkWidth + (int)(i * bigMapChunkWidth / (float)tex.width);
                    terrainBigMap.SetPixel(bigTexX, bigTexY, tex.GetPixel(i, j));
                }
            }
        }

        #endregion

        #region 初始化相关
        void InitComputeBuffer()
        {
            inputBuffer = new ComputeBuffer(instanceChunks.Length, 24);
            resultBuffer = new ComputeBuffer(instanceChunks.Length, 4);
            debugBuffer = new ComputeBuffer(instanceChunks.Length, 128);

            viewOccusionCS = RenderPipeline.instance.terrainViewOccusionCS;

            if (viewOccusionCS == null) return;

            kernel = viewOccusionCS.FindKernel("CSMain");

            isCrossBuffers = new float[instanceChunks.Length];
            aabbs = new GeometricTestFunc.AABoundingBox[instanceChunks.Length];
            prejectVertexs = new ProjectVertex[instanceChunks.Length];

            for (int i = 0; i < instanceChunks.Length; i++)
            {
                aabbs[i] = instanceChunks[i].GetAABB();
            }

            inputBuffer.SetData(aabbs);
        }
        void InitData()
        {
            Texture2D heightMap = matData.HeightNormalTex;

            float pixelVertexProX = (float)heightMap.width / (CustomTerrain.TerrainDataMgr.instance.maxLodVertexCount.x * chunkCountX + 1);
            float pixelVertexProZ = (float)heightMap.height / (CustomTerrain.TerrainDataMgr.instance.maxLodVertexCount.y * chunkCountZ + 1);
            pixelVertexPro = new Vector2(pixelVertexProX, pixelVertexProZ);
        }
        #endregion

        Vector2 pixelVertexPro;
        public void RefreshChunkCollider(Vector2 chunkIndex)
        {
            int index = (int)chunkIndex.y * chunkCountX + (int)chunkIndex.x;
            instanceChunks[index].InitCollider(matData.HeightNormalTex, matData.MaxHeight, pixelVertexPro);
        }

        public InstanceChunk GetChunkByPos(Vector3 pos)
        {
            if (transform == null)
            {
                return null;
            }
            Vector3 relativePos = pos - transform.position;
            int x = (int)(relativePos.x / chunkSize.x);
            int z = (int)(relativePos.z / chunkSize.y);
            int index = z * chunkCountX + x;
            if (index < 0 || index > instanceChunks.Length - 1)
            {
                return null;
            }
            return instanceChunks[index];
        }

        private void OnDestroy()
        {
            if (resultBuffer != null)
            {
                resultBuffer.Release();
            }

            if (inputBuffer != null)
            {
                inputBuffer.Release();
            }

            if (debugBuffer != null)
            {
                debugBuffer.Release();
            }
        }

        protected override void OnEnable()
        {
            InstanceMgr.instance.Register(this);
            if (mat == null)
            {
                Init();
            }

            TerrainDataMgr.instance.Register(this);
        }

        public bool IsInside(Vector3 pos)
        {
            return pos.x >= transform.position.x && pos.x < transform.position.x + InstanceSize.x &&
                pos.z >= transform.position.z && pos.z < transform.position.z + InstanceSize.y;
        }

        //protected override void OnDisable()
        //{
        //    base.OnDisable();
        //    TerrainDataMgr.instance.Remove(this);
        //}

    }
}
