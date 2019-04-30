using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstanceChunk : MonoBehaviour
{
    public Vector4 startEndUV;          //块相对与Instance开始和结束的
    public Vector2 minAndMaxHeight;     //最小和最大的高度
    public Vector4 alphaTexIndex;       //地形贴图编号
    public Matrix4x4 trsMatrix;         //TRS

    public float selfVertexCount;       //自身一条边顶点数
    public Vector4 neighborVertexCount; //邻居一条边顶点数
    public InstanceChunk[] neighborChunk; //上下左右的块

    public bool isShow = true;

    public void ChangePos(Vector3 newPos)
    {
        trsMatrix.m03 = newPos.x;
        trsMatrix.m13 = newPos.y;
        trsMatrix.m23 = newPos.z;

        transform.position = newPos;
    }

    public void CaculateNeighborVertexCount()
    {
        neighborVertexCount.x = (neighborChunk[0] == null || !neighborChunk[0].isShow)
        ? selfVertexCount : neighborChunk[0].selfVertexCount;//上
        neighborVertexCount.y = (neighborChunk[1] == null || !neighborChunk[1].isShow)
        ? selfVertexCount : neighborChunk[1].selfVertexCount;//下
        neighborVertexCount.z = (neighborChunk[2] == null || !neighborChunk[2].isShow)
        ? selfVertexCount : neighborChunk[2].selfVertexCount;//左
        neighborVertexCount.w = (neighborChunk[3] == null || !neighborChunk[3].isShow)
        ? selfVertexCount : neighborChunk[3].selfVertexCount;//右

        //以小的为准，防止接缝
        neighborVertexCount.x = Mathf.Min(selfVertexCount, neighborVertexCount.x);
        neighborVertexCount.y = Mathf.Min(selfVertexCount, neighborVertexCount.y);
        neighborVertexCount.z = Mathf.Min(selfVertexCount, neighborVertexCount.z);
        neighborVertexCount.w = Mathf.Min(selfVertexCount, neighborVertexCount.w);
    }
}
