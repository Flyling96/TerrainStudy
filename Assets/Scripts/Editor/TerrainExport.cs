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
    int chunkWidth = 100; //块宽度（单位米）
    int chunkLength = 100; //块长度 （单位米）
    int chunkQuadCountX = 100; //块中横向网格数
    int chunkQuadCountZ = 100; //块中纵向网格数
    protected override bool DrawWizardGUI()
    {
        mapName = EditorGUILayout.TextField("地形名称", mapName);
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

        isGPUInstance = EditorGUILayout.Toggle("采用GPU Instance", isGPUInstance);
        if (!isGPUInstance)
        {
            isSaveCPUMesh = EditorGUILayout.Toggle("CPU计算地形高度", isSaveCPUMesh);
            isSaveGPUMesh = EditorGUILayout.Toggle("GPU计算地形高度", isSaveGPUMesh);
        }
        else
        {
            isSaveCPUMesh = false;
            isSaveGPUMesh = true;
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
    private float[,] heights = null;
    private Vector3[,] normals = null;
    private int heightmapWidth = 0; //高度图宽度（单位像素）
    private int heightmapHeight = 0;  //高度图长度（单位像素）
    int chunkCountX = 0;  //横向块数
    int chunkCountZ = 0;  //纵向块数
    int chunkPixelCountX = 0; //单块的对应高度图的像素数（横向）
    int chunkPixelCountZ = 0; //单块的对应高度图的像素数（纵向）
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
        if (!Directory.Exists(totalPath))
        {
            Directory.CreateDirectory(totalPath);
        }

        float[,] tempHeights = data.GetHeights(0, 0, data.heightmapWidth, data.heightmapHeight);
        heightmapWidth = data.heightmapHeight;
        heightmapHeight = data.heightmapWidth;
        heights = new float[heightmapWidth, heightmapHeight];
        for (int i=0;i< heightmapHeight; i++)
        {
            for(int j=0;j< heightmapWidth; j++)
            {
                heights[i, j] = tempHeights[j, i];
            }
        }

        normals = new Vector3[data.heightmapWidth, data.heightmapHeight];
        maxHeight = data.size.y;


        SaveHeightNormalMap();

        if (isChunk)
        {
            chunkCountX = (int)(data.size.x / chunkWidth) + (data.size.x % chunkWidth != 0 ? 1 : 0);
            chunkCountZ = (int)(data.size.z / chunkLength) + (data.size.z % chunkLength != 0 ? 1 : 0);
            chunkPixelCountX = (int)(heightmapWidth * chunkWidth / data.size.x) + 1;
            chunkPixelCountZ = (int)(heightmapHeight * chunkLength / data.size.z) + 1;

            string chunkPath = totalPath + "/Chunks";
            if (!Directory.Exists(chunkPath))
            {
                Directory.CreateDirectory(chunkPath);
            }
            SaveChunkHeightNormalMap(chunkPath);
            if (isSaveGPUMesh)
            {
                SaveTerrainMesh(chunkWidth, chunkLength, assetsPath, true);
                SaveChunkTerrainMaterial(assetsPath + "/Chunks");
                SaveChunkPrefab(assetsPath);
                //SaveChunkTotalPrefab(assetsPath);
            }
            if (isSaveCPUMesh)
            {
                SaveChunkHeightMesh(assetsPath + "/Chunks");
            }
        }
        else
        {
            chunkCountX = 1;
            chunkCountZ = 1;
            chunkPixelCountX = heightmapWidth;
            chunkPixelCountZ = heightmapHeight;
        }


        if (isSaveCPUMesh)
        {
            SaveHeightMesh();
        }

        if (isSaveGPUMesh)
        {
            SaveTerrainMesh(data.size.x, data.size.z, assetsPath, false);
            SaveTotalTerrainMaterial();
            SaveTotalPrefab();
        }

        if(isGPUInstance)
        {
            SaveInstancePrefab();
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

    Texture2D heightNormalTex = null;

    #region 保存 HeightNormalTex
    void SaveHeightNormalMap()
    {
        EditorUtility.DisplayProgressBar("保存高度法线图", "保存高度法线图:0/1", 0);

        string imageName = mapName + "_Total_HeightNormalMap.png";

        heightNormalTex = new Texture2D(data.heightmapWidth, data.heightmapHeight,TextureFormat.RGBA32, true);
        Vector2 RG;

        GetNormalInfo();

        for (int j = 0; j < heightmapHeight; j++)
        {
            for (int i = 0; i < heightmapWidth; i++)
            {
                RG = EncodeHeight(heights[i, j]);
                heightNormalTex.SetPixel(i, j, new Color(RG.x, RG.y, normals[i,j].x, normals[i,j].z));
            }
        }
        heightNormalTex.Apply();

        byte[] rawData = heightNormalTex.EncodeToPNG();
        File.WriteAllBytes(totalPath + "/" + imageName, rawData);
        AssetDatabase.Refresh();

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
                SaveSingleChunkHeightAndNormalMap(i, j, chunkPixelCountX, chunkPixelCountZ, j * chunkCountX + i + 1, chunkPath);
            }
        }

        AssetDatabase.Refresh();
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
                if (i >= heightmapWidth || j >= heightmapHeight)
                {
                    int mini = Mathf.Min(i, heightmapWidth - 1);
                    int minj = Mathf.Min(j, heightmapHeight - 1);
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

        //保存材质
        //if(isSaveGPUMesh)
        //{
        //    string materialPath = path.Replace(Application.dataPath, "Assets");
        //    SaveTerrainMaterial(materialPath, mapName + "_Chunk_" + sumCount, heightNormalChunkMap);
        //}

    }

    void GetNormalInfo()
    {
        float scaleX = data.size.x/ heightmapWidth;
        float scaleZ = data.size.z / heightmapHeight;

        float up, down, left, right;
        Vector3 t, b, n;
        for (int j = 0; j < heightmapHeight; j++)
        {
            for (int i = 0; i < heightmapWidth; i++)
            {
                up = j != heightmapHeight - 1?heights[i, j + 1]:heights[i,j];
                down = j != 0 ? heights[i, j - 1] : heights[i, j];
                right = i != heightmapWidth - 1 ? heights[i + 1, j] : heights[i, j];
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
        float PixelVertexProX = (float)heightmapWidth / (chunkQuadCountX * tempChunkCountX + 1);
        float PixelVertexProZ = (float)heightmapHeight / (chunkQuadCountZ * tempChunkCountZ + 1);

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

        string totalPath = assetsPath + "/" + mapName + "_Total_HeightNormalMap.png";

        TextureImporter heightNormalTexImporter = AssetImporter.GetAtPath(totalPath) as TextureImporter;
        heightNormalTexImporter.wrapMode = TextureWrapMode.Clamp;
        heightNormalTexImporter.npotScale = TextureImporterNPOTScale.None;
        TextureImporterPlatformSettings heightNormalTexSetting = heightNormalTexImporter.GetDefaultPlatformTextureSettings();
        heightNormalTexSetting.format = TextureImporterFormat.RGBA32;
        heightNormalTexSetting.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
        heightNormalTexImporter.SetPlatformTextureSettings(heightNormalTexSetting);
        heightNormalTexImporter.SaveAndReimport();
        Texture2D heightNormalTex = AssetDatabase.LoadAssetAtPath(totalPath, typeof(Texture2D)) as Texture2D;

        SaveTerrainMaterial(assetsPath, mapName + "_Total", heightNormalTex);

        EditorUtility.ClearProgressBar();
    }

    void SaveChunkTerrainMaterial(string path)
    {
        string chunkPath = "";
        string matName = "";
        string totalPath = "";
        Texture2D heightNormalTex;
        TextureImporter heightNormalTexImporter;
        TextureImporterPlatformSettings heightNormalTexSetting;
        for (int j = 0; j < chunkCountZ; j++)
        {
            for (int i = 0; i < chunkCountX; i++)
            {
                chunkPath = path + "/Chunk_" + (j * chunkCountX + i + 1);
                matName = mapName + "_Chunk_" + (j * chunkCountX + i + 1);
                totalPath = chunkPath + "/" + matName + "_HeightNormalMap.png";
                heightNormalTexImporter = AssetImporter.GetAtPath(totalPath) as TextureImporter;
                heightNormalTexImporter.wrapMode = TextureWrapMode.Clamp;
                heightNormalTexImporter.npotScale = TextureImporterNPOTScale.None;
                heightNormalTexSetting = heightNormalTexImporter.GetDefaultPlatformTextureSettings();
                heightNormalTexSetting.format = TextureImporterFormat.RGBA32;
                heightNormalTexSetting.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
                heightNormalTexImporter.SetPlatformTextureSettings(heightNormalTexSetting);
                heightNormalTexImporter.SaveAndReimport();
                heightNormalTex = AssetDatabase.LoadAssetAtPath(totalPath, typeof(Texture2D)) as Texture2D;
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

    #region Instance 相关
    void SaveInstancePrefab()
    {
        GameObject prefab = new GameObject();
        prefab.name = mapName + "_Instance";
        TerrainInstance terrainInstance = prefab.AddComponent<TerrainInstance>();
        string meshName = mapName + "_Chunk_Mesh.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath(assetsPath + "/" + meshName, typeof(Mesh)) as Mesh;
        terrainInstance.InitData(mesh, chunkCountX,chunkCountZ);

        string path = assetsPath + "/" + mapName + "_Total_HeightNormalMap.png";
        Texture2D tex = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
        terrainInstance.SetMatData(tex, data.size.y);
        //Material mat = terrainInstance.Mat;
        //MaterialPropertyBlock prop = terrainInstance.Prop;
        //UpdateProp(prop);
        //UpdateMat(mat);
        UpdateTRS(terrainInstance);
        string prefabName = mapName + "_Instance.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(prefab, assetsPath + "/" + prefabName,InteractionMode.AutomatedAction);
        //GameObject.DestroyImmediate(prefab);
        //prefab = null;

    }

    //void UpdateProp(MaterialPropertyBlock prop)
    //{
    //    Vector4[] startEndUVs = new Vector4[chunkCountZ * chunkCountX];
    //    int index = 0;
    //    for (int j = 0; j < chunkCountZ; j++)
    //    {
    //        for(int i = 0; i < chunkCountX; i++)
    //        {
    //            index = j * chunkCountX + i;
    //            startEndUVs[index] = (new Vector4((float)i / chunkCountX, (float)j / chunkCountZ, (float)(i + 1) / chunkCountX, (float)(j + 1) / chunkCountZ));
    //        }
    //    }
    //    prop.SetVectorArray("_StartEndUV", startEndUVs);
    //}

    //void UpdateMat(Material mat)
    //{
    //    mat.SetTexture("_HeightNormalTex", heightNormalTex);
    //    mat.SetFloat("_MaxHeight", data.size.y);
    //}

    void UpdateTRS(TerrainInstance terrainInstance)
    {
        Matrix4x4 matr;
        int index = 0;
        for (int j = 0; j < chunkCountZ; j++)
        {
            for (int i = 0; i < chunkCountX; i++)
            {
                index = j * chunkCountX + i;
                matr = new Matrix4x4();
                matr.SetTRS(new Vector3(i * chunkWidth, 0, j * chunkLength),
                    terrainInstance.transform.rotation, Vector3.one);
                terrainInstance.AddTRS(matr,index);
            }
        }
    }

    #endregion


}
