using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class TerrainExport : ScriptableWizard
{

    [MenuItem("Editor/ExportTerrainData")]
    public static void OpenExportTerrainWnd()
    {
        ScriptableWizard.DisplayWizard<TerrainExport>("保存地形", "保存", "取消");
    }

    string mapName = "map004";
    bool isChunk = true;
    bool isSaveCPUMesh = false;
    bool isSaveGPUMesh = true;
    bool isGPUInstance = true;
    bool isDivideMaterial = false;
    int chunkWidth = 100; //块宽度（单位米）
    int chunkLength = 100; //块长度 （单位米）
    int chunkQuadCountX = 100; //块中横向网格数
    int chunkQuadCountZ = 100; //块中纵向网格数
    protected override bool DrawWizardGUI()
    {
        mapName = EditorGUILayout.TextField("地形名称", mapName);

        isGPUInstance = EditorGUILayout.Toggle("采用GPU Instance", isGPUInstance);
        if (!isGPUInstance)
        {
            isSaveCPUMesh = EditorGUILayout.Toggle("CPU计算地形高度", isSaveCPUMesh);
            isSaveGPUMesh = EditorGUILayout.Toggle("GPU计算地形高度", isSaveGPUMesh);
            isChunk = EditorGUILayout.Toggle("是否分块", isChunk);
            if (isChunk)
            {
                chunkWidth = EditorGUILayout.IntField("块宽度(X)", chunkWidth);
                chunkLength = EditorGUILayout.IntField("块长度(Z)", chunkLength);
                chunkQuadCountX = EditorGUILayout.IntField("块横向网格数(X)", chunkQuadCountX);
                chunkQuadCountZ = EditorGUILayout.IntField("块纵向网格数(Z)", chunkQuadCountZ);
            }
            else
            {
                chunkQuadCountX = EditorGUILayout.IntField("横向网格数(X)", chunkQuadCountX);
                chunkQuadCountZ = EditorGUILayout.IntField("纵向网格数(Z)", chunkQuadCountZ);
            }
        }
        else
        {
            chunkWidth = EditorGUILayout.IntField("块宽度(X)", chunkWidth);
            chunkLength = EditorGUILayout.IntField("块长度(Z)", chunkLength);
            chunkQuadCountX = 1;
            chunkQuadCountZ = 1;
            isSaveCPUMesh = false;
            isSaveGPUMesh = true;
            isChunk = true;
        }

        if(isSaveGPUMesh)
        {
            isDivideMaterial = EditorGUILayout.Toggle("划分块材质", isDivideMaterial);
        }

        return true;
    }

    private void OnWizardUpdate()
    {
        errorString = "";
        isValid = true;

        if(Terrain.activeTerrain == null)
        {
            errorString = "当前场景没有地形";
            isValid = false;
            return;
        }

        if(string.IsNullOrEmpty(mapName))
        {
            errorString = "请输入要保存的地形名";
            isValid = false;
            return;
        }

        if(isChunk)
        {
            if(chunkWidth<0||chunkWidth > Terrain.activeTerrain.terrainData.size.x 
               || chunkLength <0 || chunkLength > Terrain.activeTerrain.terrainData.size.z)
            {
                errorString = "块长宽不能为负数且不能大于地形长宽";
                isValid = false;
                return;
            }
        }


    }

    private void OnWizardCreate()
    {
        ExportTerrainData();
    }

    private void OnWizardOtherButton()
    {
        Close();
    }


    TerrainData data = null;
    float[,] heights = null;
    Vector2[] chunkMinAndMaxHeight = null;
    Vector3[,] normals = null;
    int heightMapWidth = 0; //高度图宽度（单位像素）
    int heightMapHeight = 0;  //高度图长度（单位像素）
    int alphaMapWidth = 0; //权重图宽度（单位像素）
    int alphaMapHeight = 0; // 权重图高度（单位像素）
    int chunkCountX = 0;  //横向块数
    int chunkCountZ = 0;  //纵向块数
    int chunkHeightPixelCountX = 0; //单块对应高度图的像素数（横向）
    int chunkHeightPixelCountZ = 0; //单块对应高度图的像素数（纵向）
    int chunkAlphaPixelCountX = 0; //单块对应权重图的像素数（横向）
    int chunkAlphaPixelCountZ = 0; //单块对应权重图的像素数（纵向）
    float maxHeight = 0;
    string totalPath = "";
    string assetsPath = "";
    public void ExportTerrainData()
    {
        totalPath = Application.dataPath + "/TerrainData/";
        assetsPath = "Assets/TerrainData/";

        data = Terrain.activeTerrain.terrainData;
        totalPath = totalPath + mapName;
        assetsPath = assetsPath + mapName;
        if (Directory.Exists(totalPath))
        {
            DelectDir(totalPath);
        }
        Directory.CreateDirectory(totalPath);

        float[,] tempHeights = data.GetHeights(0, 0, data.heightmapWidth, data.heightmapHeight);
        heightMapWidth = data.heightmapHeight;
        heightMapHeight = data.heightmapWidth;
        alphaMapWidth = data.alphamapHeight;
        alphaMapHeight = data.alphamapWidth;

        if (isChunk)
        {
            chunkCountX = (int)(data.size.x / chunkWidth) + (data.size.x % chunkWidth != 0 ? 1 : 0);
            chunkCountZ = (int)(data.size.z / chunkLength) + (data.size.z % chunkLength != 0 ? 1 : 0);
            chunkHeightPixelCountX = (int)(heightMapWidth * chunkWidth / data.size.x) + 1;
            chunkHeightPixelCountZ = (int)(heightMapHeight * chunkLength / data.size.z) + 1;
            chunkAlphaPixelCountX = (int)(alphaMapWidth * chunkWidth / data.size.x) + 1;
            chunkAlphaPixelCountZ = (int)(alphaMapHeight * chunkLength / data.size.z) + 1;
        }
        else
        {
            chunkCountX = 1;
            chunkCountZ = 1;
            chunkHeightPixelCountX = heightMapWidth;
            chunkHeightPixelCountZ = heightMapHeight;
            chunkAlphaPixelCountX = alphaMapHeight;
            chunkAlphaPixelCountZ = alphaMapWidth;
        }

        heights = new float[heightMapWidth, heightMapHeight];
        chunkMinAndMaxHeight = new Vector2[chunkCountX * chunkCountZ];
        for (int j = 0; j < chunkCountZ; j++)
        {
            for (int i = 0; i < chunkCountX; i++)
            {
                chunkMinAndMaxHeight[j * chunkCountX + i] = new Vector2(1, 0);
            }
        }
        for (int i=0;i< heightMapHeight; i++)
        {
            for(int j=0;j< heightMapWidth; j++)
            {
                heights[i, j] = tempHeights[j, i];
                GetChunkMinAndMaxHeight(heights[i, j], i, j);
            }
        }

        normals = new Vector3[data.heightmapWidth, data.heightmapHeight];
        maxHeight = data.size.y;


        SaveHeightNormalMap();

        if (isChunk)
        {
            string chunkPath = totalPath + "/Chunks";
            if (!Directory.Exists(chunkPath))
            {
                Directory.CreateDirectory(chunkPath);
            }
            if (isSaveGPUMesh)
            {
                SaveTerrainMesh(chunkWidth, chunkLength, assetsPath, true);
                if (isDivideMaterial)
                {
                    SaveChunkHeightNormalMap(chunkPath);
                    SaveChunkTerrainMaterial(assetsPath + "/Chunks");
                    SaveChunkPrefab(assetsPath);
                }
                //SaveChunkTotalPrefab(assetsPath);
            }
            if (isSaveCPUMesh)
            {
                SaveChunkHeightMesh(assetsPath + "/Chunks");
            }
        }

        SaveAlphaMap();


        if (isSaveCPUMesh)
        {
            SaveHeightMesh();
        }

        if (isSaveGPUMesh)
        {
            SaveTerrainMesh(data.size.x, data.size.z, assetsPath, false);
            if (isDivideMaterial)
            {
                SaveTotalTerrainMaterial();
                SaveTotalPrefab();
            }
        }

        if(isGPUInstance)
        {
            GetChunkMinAndMaxHeight();
            SaveInstancePrefab();
        }

    }

    #region 保存 HeightNormalTex
    Texture2D heightNormalTex = null;

    void GetChunkMinAndMaxHeight(float height,int i,int j)
    {
        int chunkX = i / chunkHeightPixelCountX;
        int chunkZ = j / chunkHeightPixelCountZ;
        int chunkIndex = chunkZ * chunkCountX + chunkX;
        if (height < chunkMinAndMaxHeight[chunkIndex].x)
        {
            chunkMinAndMaxHeight[chunkIndex].x = height;
        }
        else if (height > chunkMinAndMaxHeight[chunkIndex].y)
        {
            chunkMinAndMaxHeight[chunkIndex].y = height;
        }
    }

    void GetChunkMinAndMaxHeight()
    {
        chunkMinAndMaxHeight = new Vector2[chunkCountX * chunkCountZ];
        for (int j = 0; j < chunkCountZ; j++)
        {
            for (int i = 0; i < chunkCountX; i++)
            {
                int chunkIndex = j * chunkCountX + i;
                chunkMinAndMaxHeight[chunkIndex] = new Vector2(1, 0);
                for(int z = j* chunkHeightPixelCountZ;z<=(j+1)* chunkHeightPixelCountZ;z++)
                {
                    for(int x = i * chunkHeightPixelCountX;x<=(i+1) * chunkHeightPixelCountX; x++)
                    {
                        int minx = Mathf.Min(x, heightMapWidth - 1);
                        int minz = Mathf.Min(z, heightMapHeight - 1);
                        if(heights[minx, minz] < chunkMinAndMaxHeight[chunkIndex].x)
                        {
                            chunkMinAndMaxHeight[chunkIndex].x = heights[minx, minz];
                        }
                        else if(heights[minx,minz] > chunkMinAndMaxHeight[chunkIndex].y)
                        {
                            chunkMinAndMaxHeight[chunkIndex].y = heights[minx, minz];
                        }
                    }
                }
            }
        }
    }

    Vector2 EncodeHeight(float height)
    {
        Vector2 result;
        if (height < 1)
        {
            result = new Vector2(1.0f, 255.0f) * height;
            result = new Vector2(result.x , result.y - (int)result.y);
            result.x -= result.y * 1.0f / 255.0f;
        }
        else
        {
            result.x = 1;
            result.y = 0;
        }
        return result;
    }

    void SaveHeightNormalMap()
    {
        EditorUtility.DisplayProgressBar("保存高度法线图", "保存高度法线图:0/1", 0);

        string imageName = mapName + "_Total_HeightNormalMap.png";

        heightNormalTex = new Texture2D(heightMapWidth, heightMapHeight,TextureFormat.RGBA32, true);
        Vector2 RG;

        GetNormalInfo();

        for (int j = 0; j < heightMapHeight; j++)
        {
            for (int i = 0; i < heightMapWidth; i++)
            {
                RG = EncodeHeight(heights[i, j]);
                heightNormalTex.SetPixel(i, j, new Color(RG.x, RG.y, normals[i,j].x, normals[i,j].z));
            }
        }
        heightNormalTex.Apply();

        byte[] rawData = heightNormalTex.EncodeToPNG();
        File.WriteAllBytes(totalPath + "/" + imageName, rawData);
        AssetDatabase.Refresh();

        //设置图片导入信息
        TextureImporter heightNormalTexImporter = AssetImporter.GetAtPath(assetsPath + "/" + imageName) as TextureImporter;
        heightNormalTexImporter.wrapMode = TextureWrapMode.Clamp;
        heightNormalTexImporter.npotScale = TextureImporterNPOTScale.None;
        TextureImporterPlatformSettings heightNormalTexSetting = heightNormalTexImporter.GetDefaultPlatformTextureSettings();
        heightNormalTexSetting.format = TextureImporterFormat.RGBA32;
        heightNormalTexSetting.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
        heightNormalTexSetting.maxTextureSize = 4096;
        heightNormalTexImporter.SetPlatformTextureSettings(heightNormalTexSetting);
        heightNormalTexImporter.SaveAndReimport();

        EditorUtility.ClearProgressBar();

    }

    void SaveChunkHeightNormalMap(string path)
    {
        string chunkPath = "";

        for (int j = 0; j < chunkCountZ; j++)
        {
            for (int i = 0; i < chunkCountX; i++)
            {
                chunkPath = path + "/Chunk_" + (j * chunkCountX + i + 1);
                EditorUtility.DisplayProgressBar("保存块高度法线图", "保存块高度法线图:" + (j * chunkCountX + i + 1)+"/"+ chunkCountX * chunkCountZ, (float)(j * chunkCountX + i + 1)/ (chunkCountX * chunkCountZ));
                SaveSingleChunkHeightAndNormalMap(i, j, chunkHeightPixelCountX, chunkHeightPixelCountZ, j * chunkCountX + i + 1, chunkPath);
            }
        }

        EditorUtility.ClearProgressBar();
    }

    void SaveSingleChunkHeightAndNormalMap(int countX,int countZ,int pixelCountX,int pixelCountZ,int sumCount,string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string heightNormalName = mapName + "_Chunk_"+ sumCount + "_HeightNormalMap.png";

        Texture2D heightNormalChunkMap = new Texture2D(pixelCountX + 1 , pixelCountZ + 1 ,TextureFormat.RGBA32, true);
        Vector2 RG;

        for (int j = countZ * pixelCountZ; j <= (countZ + 1) * pixelCountZ ; j++)
        {
            for (int i = countX * pixelCountX; i <= (countX + 1) * pixelCountX ; i++)
            {
                if (i >= heightMapWidth || j >= heightMapHeight)
                {
                    int mini = Mathf.Min(i, heightMapWidth - 1);
                    int minj = Mathf.Min(j, heightMapHeight - 1);
                    RG = EncodeHeight(heights[mini, minj]);
                    heightNormalChunkMap.SetPixel(i - countX * pixelCountX, j - countZ * pixelCountZ, new Color(RG.x, RG.y,normals[mini, minj].x, normals[mini, minj].z));
                }
                else
                {
                    RG = EncodeHeight(heights[i, j]);
                    heightNormalChunkMap.SetPixel(i - countX * pixelCountX, j - countZ * pixelCountZ, new Color(RG.x, RG.y, normals[i, j].x, normals[i, j].z));
                }
            }
        }

        heightNormalChunkMap.Apply();

        byte[] rawData = heightNormalChunkMap.EncodeToPNG();

        File.WriteAllBytes(path + "/" + heightNormalName, rawData);

        AssetDatabase.Refresh();

        //设置图片导入信息
        TextureImporter heightNormalTexImporter = AssetImporter.GetAtPath(path.Replace(Application.dataPath,"Assets") + "/" + heightNormalName) as TextureImporter;
        heightNormalTexImporter.wrapMode = TextureWrapMode.Clamp;
        heightNormalTexImporter.npotScale = TextureImporterNPOTScale.None;
        TextureImporterPlatformSettings heightNormalTexSetting = heightNormalTexImporter.GetDefaultPlatformTextureSettings();
        heightNormalTexSetting.format = TextureImporterFormat.RGBA32;
        heightNormalTexSetting.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
        heightNormalTexImporter.SetPlatformTextureSettings(heightNormalTexSetting);
        heightNormalTexImporter.SaveAndReimport();

        //保存材质
        //if(isSaveGPUMesh)
        //{
        //    string materialPath = path.Replace(Application.dataPath, "Assets");
        //    SaveTerrainMaterial(materialPath, mapName + "_Chunk_" + sumCount, heightNormalChunkMap);
        //}

    }

    void GetNormalInfo()
    {
        float scaleX = data.size.x/ heightMapWidth;
        float scaleZ = data.size.z / heightMapHeight;

        float up, down, left, right;
        Vector3 t, b, n;
        for (int j = 0; j < heightMapHeight; j++)
        {
            for (int i = 0; i < heightMapWidth; i++)
            {
                up = j != heightMapHeight - 1?heights[i, j + 1]:heights[i,j];
                down = j != 0 ? heights[i, j - 1] : heights[i, j];
                right = i != heightMapWidth - 1 ? heights[i + 1, j] : heights[i, j];
                left = i != 0? heights[i-1,j]: heights[i, j];
                t = new Vector3(scaleX * 2, (right - left) * maxHeight, 0);
                b = new Vector3(0, (up - down) * maxHeight, scaleZ * 2);
                n = Vector3.Normalize(Vector3.Cross(b,t));
                n = new Vector3(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f);
                normals[i, j] = n;
            }
        }

    }
    #endregion

    #region 保存 Mesh
    void SaveTerrainMesh(float width ,float length ,string path , bool isChunkMesh)
    {
        string meshName = "";
        string tips = "";
        if(isChunkMesh)
        {
            meshName = mapName + "_Chunk_Mesh.asset";
            tips = "保存地形块Mesh";
        }
        else
        {
            meshName = mapName + "_Terrain_Mesh.asset";
            tips = "保存地形Mesh";
        }
        int column = chunkQuadCountX * (isChunkMesh ? 1 : chunkCountX);
        int row = chunkQuadCountZ * (isChunkMesh ? 1 : chunkCountZ);


        float spaceWidth = width / column;
        float spaceLength = length / row;

        int numberOfVertices = (row + 1) * (column + 1);
        int numberOfIndex = row * column * 4;
        Vector3[] vertices = new Vector3[numberOfVertices];
        int[] indices = new int[numberOfIndex];
        Vector2[] uvs = new Vector2[numberOfVertices];

        for (int r = 0; r < row + 1; r++)
        {
            for (int c = 0; c < column + 1; c++)
            {
                vertices[r * (column + 1) + c] = new Vector3(c * spaceWidth, 0, r * spaceLength);
                uvs[r * (column + 1) + c] = new Vector2((float)c / column, (float)r / row);
            }
        }

        int index;
        int line;
        for (int r = 0; r < row; r++)
        {
            for (int c = 0; c < column; c++)
            {
                index = 4 * (r * column + c);
                line = (column + 1);
                indices[index] = (r + 1) * line + c;
                indices[index + 1] = (r + 1) * line + c + 1;
                indices[index + 2] = r * line + c + 1;
                indices[index + 3] = r * line + c;
            }
        }

        Mesh m_mesh = new Mesh();
        //设置网格缓冲区为32位，默认为16位
        m_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m_mesh.vertices = vertices;
        m_mesh.SetIndices(indices, MeshTopology.Quads, 0);
        List<Vector2> uvList = new List<Vector2>(uvs);
        m_mesh.SetUVs(0, uvList);

        EditorUtility.DisplayProgressBar(tips, tips+": 0/1", 0);

        AssetDatabase.CreateAsset(m_mesh, path + "/" + meshName);

        EditorUtility.ClearProgressBar();

    }

    void SaveChunkHeightMesh(string path)
    {
        string chunkPath = "";
        for (int j = 0; j < chunkCountZ; j++)
        {
            for (int i = 0; i < chunkCountX; i++)
            {
                chunkPath = path + "/Chunk_" + (j * chunkCountX + i + 1);
                EditorUtility.DisplayProgressBar("保存地形块高度Mesh", "保存地形块高度Mesh:" + (j * chunkCountX + i + 1) + "/" + chunkCountX * chunkCountZ, (float)(j * chunkCountX + i + 1) / (chunkCountX * chunkCountZ));
                SaveSingleHeightMesh(true, chunkPath, i, j);
            }
        }
        EditorUtility.ClearProgressBar();
    }

    void SaveHeightMesh()
    {
        EditorUtility.DisplayProgressBar("保存高度Mesh", "保存地形块高度Mesh: 0/1", 0);
        SaveSingleHeightMesh(false, assetsPath);
        EditorUtility.ClearProgressBar();
    }

    void SaveSingleHeightMesh(bool isChunkMesh,string path,int countX = 0,int countZ = 0)
    {
        int width;
        int length;
        int column = chunkQuadCountX;
        int row = chunkQuadCountZ;

        string meshName;
        int chunkCount = 0;

        int tempChunkCountX = 1;
        int tempChunkCountZ = 1;

        if (isChunkMesh)
        {
            tempChunkCountX = chunkCountX;
            tempChunkCountZ = chunkCountZ;
            chunkCount = countZ * chunkCountX + countX + 1;
            width = chunkWidth;
            length = chunkLength;
            meshName = mapName + "_Chunk_"+ chunkCount + "_CPUHeightMesh.asset";
        }
        else
        {
            width = (int)data.size.x;
            length = (int)data.size.z;
            meshName = mapName + "_Total_CPUHeightMesh.asset";
        }

        //之前的顶点数
        int startVertexCountX = countX * chunkQuadCountX;
        int startVertexCountZ = countZ * chunkQuadCountZ;

        float spaceWidth = width / column;
        float spaceLength = length / row;

        int numberOfVertices = (row + 1) * (column + 1);
        int numberOfIndex = row * column * 4;
        Vector3[] vertices = new Vector3[numberOfVertices];
        int[] indices = new int[numberOfIndex];
        Vector2[] uvs = new Vector2[numberOfVertices];

        int nowVertexCountX;
        int nowVertexCountZ;
        //高度图像素数和顶点数的比例
        float PixelVertexProX = (float)heightMapWidth / (chunkQuadCountX * tempChunkCountX + 1);
        float PixelVertexProZ = (float)heightMapHeight / (chunkQuadCountZ * tempChunkCountZ + 1);

        int pixelX;
        int pixelZ;

        for (int r = 0; r < row + 1; r++)
        {
            nowVertexCountZ = startVertexCountZ + r + 1;
            for (int c = 0; c < column + 1; c++)
            {
                nowVertexCountX = startVertexCountX + c + 1;
                pixelX = (int)((nowVertexCountX - 1) * PixelVertexProX);
                pixelZ = (int)((nowVertexCountZ - 1) * PixelVertexProZ);
                vertices[r * (column + 1) + c] = new Vector3(c * spaceWidth, heights[pixelX,pixelZ] * maxHeight, r * spaceLength);
                uvs[r * (column + 1) + c] = new Vector2((float)c / column, (float)r / row);
            }
        }

        int index;
        int line;
        for (int r = 0; r < row; r++)
        {
            for (int c = 0; c < column; c++)
            {
                index = 4 * (r * column + c);
                line = (column + 1);
                indices[index] = (r + 1) * line + c;
                indices[index + 1] = (r + 1) * line + c + 1;
                indices[index + 2] = r * line + c + 1;
                indices[index + 3] = r * line + c;
            }
        }

        Mesh m_mesh = new Mesh();
        m_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m_mesh.vertices = vertices;
        m_mesh.SetIndices(indices, MeshTopology.Quads, 0);
        List<Vector2> uvList = new List<Vector2>(uvs);
        m_mesh.SetUVs(0, uvList);

        AssetDatabase.CreateAsset(m_mesh, path + "/" + meshName);
    }
    #endregion

    #region 保存 Material
    void SaveTotalTerrainMaterial()
    {
        EditorUtility.DisplayProgressBar("保存整体地形材质", "保存整体地形材质: 0/1", 0);

        string ttotalPath = assetsPath + "/" + mapName + "_Total_HeightNormalMap.png";

        Texture2D heightNormalTex = AssetDatabase.LoadAssetAtPath(ttotalPath, typeof(Texture2D)) as Texture2D;

        SaveTerrainMaterial(assetsPath, mapName + "_Total", heightNormalTex);

        EditorUtility.ClearProgressBar();
    }

    void SaveChunkTerrainMaterial(string path)
    {
        string chunkPath = "";
        string matName = "";
        string ttotalPath = "";
        Texture2D heightNormalTex;
        TextureImporter heightNormalTexImporter;
        TextureImporterPlatformSettings heightNormalTexSetting;
        for (int j = 0; j < chunkCountZ; j++)
        {
            for (int i = 0; i < chunkCountX; i++)
            {
                chunkPath = path + "/Chunk_" + (j * chunkCountX + i + 1);
                matName = mapName + "_Chunk_" + (j * chunkCountX + i + 1);
                ttotalPath = chunkPath + "/" + matName + "_HeightNormalMap.png";
                //heightNormalTexImporter = AssetImporter.GetAtPath(totalPath) as TextureImporter;
                //heightNormalTexImporter.wrapMode = TextureWrapMode.Clamp;
                //heightNormalTexImporter.npotScale = TextureImporterNPOTScale.None;
                //heightNormalTexSetting = heightNormalTexImporter.GetDefaultPlatformTextureSettings();
                //heightNormalTexSetting.format = TextureImporterFormat.RGBA32;
                //heightNormalTexSetting.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
                //heightNormalTexImporter.SetPlatformTextureSettings(heightNormalTexSetting);
                //heightNormalTexImporter.SaveAndReimport();
                heightNormalTex = AssetDatabase.LoadAssetAtPath(ttotalPath, typeof(Texture2D)) as Texture2D;
                EditorUtility.DisplayProgressBar("保存地形块材质", "保存地形块材质:" + (j * chunkCountX + i + 1) + "/" + chunkCountX * chunkCountZ, (float)(j * chunkCountX + i + 1) / (chunkCountX * chunkCountZ));
                SaveTerrainMaterial(chunkPath, matName, heightNormalTex);
            }
        }

        EditorUtility.ClearProgressBar();
    }

    void SaveTerrainMaterial(string path,string name,Texture2D heightNormalTex)
    {
        string matPath = path + "/" +  name + "_Mat.mat";
        Material material = new Material(Shader.Find("Unlit/TerrainSimple"));
        material.SetTexture("_HeightNormalTex", heightNormalTex);
        material.SetFloat("_MaxHeight", data.size.y);
        AssetDatabase.CreateAsset(material, matPath);
    }
    #endregion

    #region 保存 Prefab
    void SaveTotalPrefab()
    {
        EditorUtility.DisplayProgressBar("保存地形Prefab", "保存地形Prefab: 0/1", 0);
        string prefabName = mapName + "_Total_Terrain.prefab";
        string meshName = mapName + "_Terrain_Mesh.asset";
        string matName = mapName + "_Total_Mat.mat";
        GameObject prefab = GetTerrainPrefab(assetsPath, meshName, matName, prefabName);
        GameObject.DestroyImmediate(prefab);
        prefab = null;
        EditorUtility.ClearProgressBar();

    }

    void SaveChunkPrefab(string path)
    {
        string chunkPath = "";
        string totalPrefabName = mapName + "_Chunk_Total_Terrain.prefab";
        string prefabName = mapName + "_Chunk_{0}_Terrain.prefab";
        string meshName = mapName + "_Chunk_Mesh.asset";
        string matName = mapName + "_Chunk_{0}_Mat.mat";
        int index = 0;
        GameObject childPrefab = null;
        GameObject prefab = new GameObject();
        for (int j = 0; j < chunkCountZ; j++)
        {
            for (int i = 0; i < chunkCountX; i++)
            {
                index = j * chunkCountX + i + 1;
                chunkPath = path + "/Chunks/Chunk_" + index;
                EditorUtility.DisplayProgressBar("保存地形块Prefab", "保存地形块Prefab:" + index + "/" + chunkCountX * chunkCountZ, (float)index / (chunkCountX * chunkCountZ));
                childPrefab = GetTerrainPrefab(chunkPath, meshName,string.Format(matName, index),string.Format(prefabName, index));
                childPrefab.name = string.Format(prefabName, index).Replace(".prefab","");
                childPrefab.transform.parent = prefab.transform;
                childPrefab.transform.localPosition = new Vector3(i * chunkWidth, 0, j * chunkLength);
                childPrefab = null;
            }
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(prefab, path + "/" + totalPrefabName, InteractionMode.UserAction);
        GameObject.DestroyImmediate(prefab);
        prefab = null;
        EditorUtility.ClearProgressBar();
    }

    //废弃，会导致Unity闪退
    //void SaveChunkTotalPrefab(string path)
    //{
    //    string prefabName = mapName + "_Chunk_Total_Terrain.prefab";
    //    string childPrefabName = mapName + "_Chunk_{0}_Terrain.prefab";
    //    string chunkPath = "";
    //    GameObject childPrefab = null;
    //    GameObject prefab = new GameObject();
    //    int index = 0;

    //    for (int j = 0; j < chunkCountZ; j++)
    //    {
    //        for (int i = 0; i < chunkCountX; i++)
    //        {
    //            index = j * chunkCountX + i + 1;
    //            EditorUtility.DisplayProgressBar("整合地形块Prefab", "整合地形块Prefab:" + index + "/" + chunkCountX * chunkCountZ, (float)index / (chunkCountX * chunkCountZ));
    //            chunkPath = path + "/Chunks/Chunk_" + index;
    //            childPrefab = PrefabUtility.LoadPrefabContents(chunkPath + "/" + string.Format(childPrefabName, index));
    //            Debug.Log("Load Prefab" + index);
    //            if (childPrefab == null) continue;
    //            childPrefab.transform.parent = prefab.transform;
    //            childPrefab.transform.localPosition = new Vector3(i * chunkWidth, 0, j * chunkLength);
    //            childPrefab = null;
    //        }
    //    }

    //    Debug.Log("Start Save Total Prefab");
    //    PrefabUtility.SaveAsPrefabAssetAndConnect(prefab, path + "/" + prefabName, InteractionMode.UserAction);
    //    AssetDatabase.ImportAsset(path + "/" + prefabName, ImportAssetOptions.Default);
    //    GameObject.DestroyImmediate(prefab);
    //    prefab = null;

    //    EditorUtility.ClearProgressBar();
    //}

    GameObject GetTerrainPrefab(string path,string meshName,string matName,string prefabName)
    {
        GameObject prefab = new GameObject();
        prefab.AddComponent<MeshFilter>();
        prefab.AddComponent<MeshRenderer>();
        prefab.GetComponent<MeshFilter>().mesh = AssetDatabase.LoadAssetAtPath(assetsPath + "/" + meshName, typeof(Mesh)) as Mesh;
        prefab.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath(path + "/" + matName, typeof(Material)) as Material;
        PrefabUtility.SaveAsPrefabAsset(prefab, path + "/" + prefabName);
        return prefab;
        //GameObject.DestroyImmediate(prefab);
        //prefab = null;
    }


    #endregion

    #region 保存 AlphaMap
    Vector4[] alphaWeightArray;
    Vector4[] alphaTexIndexArray;
    Texture2DArray terrainMapArray;
    Vector4[] terrainMapSize;
   void SaveAlphaMap()
    {
        EditorUtility.DisplayProgressBar("保存权重图", "保存权重图:0/1", 0);
     
        alphaWeightArray = new Vector4[alphaMapWidth * alphaMapHeight];
        alphaTexIndexArray = new Vector4[chunkCountX * chunkCountZ];
        GetAlphaMapInfo(ref alphaWeightArray, ref alphaTexIndexArray);
        Vector4[] tmpAlphaTexIndexArray = new Vector4[chunkCountX * chunkCountZ];
        for (int j = 0; j < chunkCountZ; j++)
        {
            for (int i = 0; i < chunkCountX; i++)
            {
                int chunkIndex = j * chunkCountX + i;
                tmpAlphaTexIndexArray[chunkIndex] = alphaTexIndexArray[chunkIndex];
            }
        }

        for (int j = 0; j < chunkCountZ; j++)
        {
            for (int i = 0; i < chunkCountX; i++)
            {
                alphaTexIndexArray[j * chunkCountX + i] = tmpAlphaTexIndexArray[i * chunkCountZ + j];
            }
        }


        GetAlphaMapTextureArray();

        string imageName = mapName + "_Total_AlphaMap.png";

        Texture2D alphaMapTex = new Texture2D(alphaMapWidth, alphaMapHeight, TextureFormat.RGBA32, true);
        //权重图采用双线性滤波
        alphaMapTex.filterMode = FilterMode.Bilinear;
        Vector4 alphaWeight;
        int alphaWeightIndex;

        for (int j = 0; j < alphaMapHeight; j++)
        {
            for (int i = 0; i < alphaMapWidth; i++)
            {
                alphaWeightIndex = j * alphaMapWidth + i;
                alphaWeight = alphaWeightArray[alphaWeightIndex];
                alphaMapTex.SetPixel(j, i, new Color(alphaWeight.x, alphaWeight.y, alphaWeight.z, alphaWeight.w));
            }
        }
        alphaMapTex.Apply();

        byte[] rawData = alphaMapTex.EncodeToPNG();
        File.WriteAllBytes(totalPath + "/" + imageName, rawData);

        string arrayName = mapName + "_Total_AlphaMapTextures.Asset";
        if (terrainMapArray!=null)
        {
            AssetDatabase.CreateAsset(terrainMapArray, assetsPath +"/"+ arrayName);
        }

        AssetDatabase.Refresh();

        //设置图片导入信息
        TextureImporter alphaMapImporter = AssetImporter.GetAtPath(assetsPath + "/" + imageName) as TextureImporter;
        alphaMapImporter.wrapMode = TextureWrapMode.Clamp;
        alphaMapImporter.npotScale = TextureImporterNPOTScale.None;
        TextureImporterPlatformSettings alphaMapSetting = alphaMapImporter.GetDefaultPlatformTextureSettings();
        alphaMapSetting.format = TextureImporterFormat.RGBA32;
        alphaMapSetting.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
        alphaMapSetting.maxTextureSize = 4096;
        alphaMapImporter.SetPlatformTextureSettings(alphaMapSetting);
        alphaMapImporter.SaveAndReimport();

        EditorUtility.ClearProgressBar();
    }

    void GetAlphaMapInfo(ref Vector4[] alphaWeights,ref Vector4[] alphaTexIndexs)
    {
        float[,,] alphaMaps = data.GetAlphamaps(0, 0, alphaMapWidth, alphaMapHeight);
        int alphaMapCount = alphaMaps.GetLength(2);
        int pixelX = 0;
        int pixelZ = 0;
        int chunkIndex = 0;
        int weightIndex = 0;
        int alphaTexIndex = 0;
        float weight = 0;
        int alphaTexIndexKey = 0;
        Vector4 alphaTex;
        Vector4 alphaWeight;
        for (int j = 0; j < chunkCountZ; j++)
        {
            for (int i = 0; i < chunkCountX; i++)
            {
                chunkIndex = j * chunkCountX + i;
                alphaTex = Vector4.zero;
                alphaTexIndex = 0;
                for (int z = 0; z < chunkAlphaPixelCountZ; z++)
                {
                    for (int x = 0; x < chunkAlphaPixelCountX; x++)
                    {
                        pixelX = Mathf.Min(i * chunkAlphaPixelCountX + x, alphaMapWidth - 1);
                        pixelZ = Mathf.Min(j * chunkAlphaPixelCountZ + z, alphaMapHeight - 1);
                        weightIndex = pixelZ * alphaMapWidth + pixelX;
                        alphaWeight = Vector4.zero ;
                        for (int k = 1; k < alphaMapCount + 1; k++)
                        {
                            weight = alphaMaps[pixelX, pixelZ, k - 1];
                            if (weight > 0)
                            {
                                alphaTexIndexKey = GetVectorKey(alphaTex, k);
                                //还未加入
                                if (alphaTexIndexKey == -1)
                                {
                                    SetVector4(ref alphaTex, alphaTexIndex, k);
                                    alphaTexIndexKey = alphaTexIndex;
                                    alphaTexIndex++;
                                }

                                if (alphaTexIndexKey != -1)
                                {
                                    SetVector4(ref alphaWeight, alphaTexIndexKey, weight);
                                }

                            }
                        }
                        //alphaWeight = new Vector4(alphaMaps[pixelX, pixelZ, 0], alphaMaps[pixelX, pixelZ, 1], alphaMaps[pixelX, pixelZ, 2], alphaMaps[pixelX, pixelZ, 3]);
                        alphaWeights[weightIndex] = alphaWeight;
                    }
                }
                //alphaTex = new Vector4(1, 2, 3, 4);
                alphaTexIndexs[chunkIndex] = alphaTex - Vector4.one;
            }
        }
    }

    void GetAlphaMapTextureArray()
    {
        if (data.terrainLayers.Length<1)
        {
            return;
        }

        terrainMapSize = new Vector4[data.terrainLayers.Length];

        Texture2D alphaMapTexture = data.terrainLayers[0].diffuseTexture;
        terrainMapArray = new Texture2DArray(alphaMapTexture.width, alphaMapTexture.height, data.terrainLayers.Length, alphaMapTexture.format, false);
        for (int i=0;i < data.terrainLayers.Length; i++)
        {
            terrainMapArray.SetPixels(data.terrainLayers[i].diffuseTexture.GetPixels(), i,0);
            terrainMapSize[i] = new Vector4(data.terrainLayers[i].tileSize.x, data.terrainLayers[i].tileSize.y,
                data.terrainLayers[i].tileOffset.x, data.terrainLayers[i].tileOffset.y);
        }


        
    }

    void SetVector4(ref Vector4 target,int index,float value)
    {
        if(index == 0)
        {
            target.x = value;
        }
        else if(index == 1)
        {
            target.y = value;
        }
        else if(index == 2)
        {
            target.z = value;
        }
        else if(index == 3)
        {
            target.w = value;
        }
    }

    int GetVectorKey(Vector4 target,float value)
    {
        int result = -1;
        if(target.x == value)
        {
            result = 0;
        }
        else if(target.y == value)
        {
            result = 1;
        }
        else if (target.z == value)
        {
            result = 2;
        }
        else if (target.w == value)
        {
            result = 3;
        }
        return result;
    }


    #endregion

    #region Instance 相关
    void SaveInstancePrefab()
    {
        GameObject prefab = new GameObject();
        prefab.name = mapName + "_Instance";
        TerrainInstance terrainInstance = prefab.AddComponent<TerrainInstance>();
        string meshName = mapName + "_Chunk_Mesh.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath(assetsPath + "/" + meshName, typeof(Mesh)) as Mesh;
        terrainInstance.InitData(mesh, chunkCountX,chunkCountZ,chunkWidth,chunkLength,terrainInstance.transform.rotation,alphaTexIndexArray,chunkMinAndMaxHeight);

        string path = assetsPath + "/" + mapName + "_Total_HeightNormalMap.png";
        Texture2D hnTex = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
        path = assetsPath + "/" + mapName + "_Total_AlphaMap.png";
        Texture2D aTex = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
        path = assetsPath + "/" + mapName + "_Total_AlphaMapTextures.Asset";
        Texture2DArray tMapArray = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2DArray)) as Texture2DArray;

        Vector4 chunkPixelCount = new Vector4(chunkHeightPixelCountX, chunkHeightPixelCountZ, chunkAlphaPixelCountX, chunkAlphaPixelCountZ);

        terrainInstance.SetMatData(hnTex, data.size.y,aTex, tMapArray,terrainMapSize, chunkPixelCount);


        string prefabName = mapName + "_Instance.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(prefab, assetsPath + "/" + prefabName,InteractionMode.AutomatedAction);
        //GameObject.DestroyImmediate(prefab);
        //prefab = null;

    }

    #endregion

    public void DelectDir(string srcPath)
    {
        try
        {
            DirectoryInfo dir = new DirectoryInfo(srcPath);
            FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录
            foreach (FileSystemInfo i in fileinfo)
            {
                if (i is DirectoryInfo)            //判断是否文件夹
                {
                    DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                    subdir.Delete(true);          //删除子目录和文件
                }
                else
                {
                    File.Delete(i.FullName);      //删除指定文件
                }
            }
        }
        catch (System.Exception e)
        {
            throw;
        }
    }

}
