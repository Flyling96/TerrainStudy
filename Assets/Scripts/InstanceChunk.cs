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

    public Vector2 chunkSize;

    bool isShow = true;
    bool isNeedHide = false;
    public bool IsShow
    {
        get
        {
            return isShow;
        }
        set
        {
            if(!value)
            {
                isNeedHide = true;
                StartCoroutine(DelayHide());
            }
            else
            {
                if (isNeedHide)
                {
                    StopCoroutine("DelayHide");
                }
                isShow = value;
                isNeedHide = false;
            }
        }
    }

    IEnumerator DelayHide()
    {
        yield return new WaitForSeconds(5);

        isShow = false;
        
    }



    public void CacuIsBoundInCamera()
    {
        InstanceMgr.AABoundingBox aabb = new InstanceMgr.AABoundingBox();
        aabb.min = new Vector3(transform.position.x, minAndMaxHeight.x, transform.position.z);
        aabb.max = new Vector3(transform.position.x + chunkSize.x, minAndMaxHeight.y, transform.position.z + chunkSize.y);
        if (!isNeedHide || InstanceMgr.instance.IsBoundInCamera(aabb, InstanceMgr.instance.mainCamera))
        {
            IsShow = InstanceMgr.instance.IsBoundInCamera(aabb, InstanceMgr.instance.mainCamera);
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
