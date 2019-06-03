using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomTerrain;

public class InstanceChunk : MonoBehaviour
{
    public Vector2 chunkIndex;         //块在instance中的行列

    public Vector4 startEndUV;          //块相对与Instance开始和结束的
    public Vector2 minAndMaxHeight;     //最小和最大的高度
    public Vector4 alphaTexIndex;       //地形贴图编号
    public Matrix4x4 trsMatrix;         //TRS

    public float selfVertexCount;       //自身一条边顶点数
    public Vector4 neighborVertexCount; //邻居一条边顶点数
    public InstanceChunk[] neighborChunk; //上下左右的块

    public MeshCollider meshCollider; //地形块的碰撞体

    public Vector2 chunkSize;

    bool isShow = true;
    bool isNeedHide = false;

    float DecodeHeight(Vector2 heightXY)
    {
        Vector2 decodeDot = new Vector2(1.0f, 1.0f / 255.0f);
        return Vector2.Dot(heightXY, decodeDot);
    }

    public void InitCollider(Texture2D heightMap,float maxHeight,Vector2 pixelVertexPro)
    {
        int column = (int)TerrainDataMgr.instance.maxLodVertexCount.x;
        int row = (int)TerrainDataMgr.instance.maxLodVertexCount.y;

        int startVertexCountX = (int)chunkIndex.x * column;
        int startVertexCountZ = (int)chunkIndex.y * row;

        float spaceWidth = chunkSize.x / column;
        float spaceLength = chunkSize.y / row;

        int numberOfVertices = (row + 1) * (column + 1);
        int numberOfIndex = row * column * 4;
        Vector3[] vertices = new Vector3[numberOfVertices];
        int[] indices = new int[numberOfIndex];

        for (int r = 0; r < row + 1; r++)
        {
            int nowVertexCountZ = startVertexCountZ + r + 1;
            for (int c = 0; c < column + 1; c++)
            {
                int nowVertexCountX = startVertexCountX + c + 1;
                int pixelX = (int)((nowVertexCountX - 1) * pixelVertexPro.x);
                int pixelZ = (int)((nowVertexCountZ - 1) * pixelVertexPro.y);
                Color color = heightMap.GetPixel(pixelX, pixelZ);
                float height = DecodeHeight(new Vector2(color.r, color.g)) * maxHeight;
                vertices[r * (column + 1) + c] = new Vector3(c * spaceWidth, height, r * spaceLength);
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

        meshCollider = gameObject.GetComponent<MeshCollider>();
        if(meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        meshCollider.sharedMesh = m_mesh;

    }

    public bool IsShow
    {
        get
        {
            return isShow;
        }
        set
        {
            isShow = value;
            //if (Application.isPlaying)
            //{
            //    if (!value && !isNeedHide)
            //    {
            //        isNeedHide = true;
            //        StartCoroutine(DelayHide());
            //    }
            //    else
            //    {
            //        if (isNeedHide)
            //        {
            //            StopCoroutine("DelayHide");
            //        }
            //        isShow = value;
            //        isNeedHide = false;
            //    }
            //}
            //else
            //{
            //    isShow = value;
            //}
        }
    }

    //延迟消失
    IEnumerator DelayHide()
    {
        yield return new WaitForSeconds(5);

        isShow = false;
        
    }

    public InstanceMgr.AABoundingBox GetAABB()
    {
        InstanceMgr.AABoundingBox aabb = new InstanceMgr.AABoundingBox();
        aabb.min = new Vector3(transform.position.x, minAndMaxHeight.x, transform.position.z);
        aabb.max = new Vector3(transform.position.x + chunkSize.x, minAndMaxHeight.y, transform.position.z + chunkSize.y);
        return aabb;
    }

    public void CacuIsBoundInCamera()
    {
        InstanceMgr.AABoundingBox aabb = GetAABB();
        if (!isNeedHide || InstanceMgr.instance.IsBoundInCameraByBox(aabb, InstanceMgr.instance.mainCamera))
        {
            IsShow = InstanceMgr.instance.IsBoundInCameraByBox(aabb, InstanceMgr.instance.mainCamera);
        }
    }

    public void ChangePos(Vector3 newPos)
    {
        trsMatrix.m03 = newPos.x;
        trsMatrix.m13 = newPos.y;
        trsMatrix.m23 = newPos.z;

        transform.position = newPos;
    }

    public void CaculateNeighborVertexCount()
    {
        neighborVertexCount.x = (neighborChunk[0] == null || !neighborChunk[0].IsShow)
        ? selfVertexCount : neighborChunk[0].selfVertexCount;//上
        neighborVertexCount.y = (neighborChunk[1] == null || !neighborChunk[1].IsShow)
        ? selfVertexCount : neighborChunk[1].selfVertexCount;//下
        neighborVertexCount.z = (neighborChunk[2] == null || !neighborChunk[2].IsShow)
        ? selfVertexCount : neighborChunk[2].selfVertexCount;//左
        neighborVertexCount.w = (neighborChunk[3] == null || !neighborChunk[3].IsShow)
        ? selfVertexCount : neighborChunk[3].selfVertexCount;//右

        //以小的为准，防止接缝
        neighborVertexCount.x = Mathf.Min(selfVertexCount, neighborVertexCount.x);
        neighborVertexCount.y = Mathf.Min(selfVertexCount, neighborVertexCount.y);
        neighborVertexCount.z = Mathf.Min(selfVertexCount, neighborVertexCount.z);
        neighborVertexCount.w = Mathf.Min(selfVertexCount, neighborVertexCount.w);
    }
}
