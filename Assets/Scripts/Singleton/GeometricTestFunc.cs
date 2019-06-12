using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GeometricTestFunc : Singleton<GeometricTestFunc>
{
    #region 视锥体裁剪相关

    public struct AABoundingBox
    {
        public Vector3 min;
        public Vector3 max;
    }

    //视口检测相关
    /// <summary>
    /// 四边形块是否在视口内
    /// </summary>
    /// <param name="leftDown"></param>
    /// <param name="leftUp"></param>
    /// <param name="rightUp"></param>
    /// <param name="rightDown"></param>
    /// <returns></returns>
    public bool IsInView(Vector3 leftDown, Vector3 leftUp, Vector3 rightUp, Vector3 rightDown)
    {
        Vector3 viewLeftDown = RenderPipeline.instance.mainCamera.WorldToViewportPoint(leftDown);
        Vector3 viewLeftUp = RenderPipeline.instance.mainCamera.WorldToViewportPoint(leftUp);
        Vector3 viewRightUp = RenderPipeline.instance.mainCamera.WorldToViewportPoint(rightUp);
        Vector3 viewRightDown = RenderPipeline.instance.mainCamera.WorldToViewportPoint(rightDown);

        if (Mathf.Min(Mathf.Min(viewLeftDown.z, viewLeftUp.z), Mathf.Min(viewRightUp.z, viewRightDown.z)) > RenderPipeline.instance.mainCamera.farClipPlane ||
           Mathf.Max(Mathf.Max(viewLeftDown.z, viewLeftUp.z), Mathf.Max(viewRightUp.z, viewRightDown.z)) < RenderPipeline.instance.mainCamera.nearClipPlane)
        {
            return false;
        }

        Vector2 viewLeftDownPoint = new Vector2(0, 0);
        Vector2 viewLeftUpPoint = new Vector2(0, 1);
        Vector2 viewRightUpPoint = new Vector2(1, 1);
        Vector2 viewRightDownPoint = new Vector2(1, 0);

        Vector2[] viewChunk = new Vector2[] { viewLeftDown, viewLeftUp, viewRightUp, viewRightDown };
        Vector2[] viewSpacePoint = new Vector2[] { viewLeftDownPoint, viewLeftUpPoint, viewRightUpPoint, viewRightDownPoint };

        //四边形的某点是否在视口内
        if (IsContantPoint(viewLeftDown, viewSpacePoint) ||
           IsContantPoint(viewLeftUp, viewSpacePoint) ||
           IsContantPoint(viewRightUp, viewSpacePoint) ||
           IsContantPoint(viewRightDown, viewSpacePoint))
        {
            return true;
        }

        //视口的某点是否在四边形内
        if (IsContantPoint(viewLeftDownPoint, viewChunk) ||
           IsContantPoint(viewLeftUpPoint, viewChunk) ||
           IsContantPoint(viewRightUpPoint, viewChunk) ||
           IsContantPoint(viewRightDownPoint, viewChunk))
        {
            return true;
        }


        return false;

    }

    /// <summary>
    /// 多边形中是否包含某点
    /// </summary>
    /// <param name="point">点坐标</param>
    /// <param name="rect">多边形顶点坐标</param>
    /// <returns></returns>
    public bool IsContantPoint(Vector2 point, Vector2[] rect)
    {
        int crossCount = 0;
        Vector2 p0, p1 = Vector2.one;
        for (int i = 0; i < rect.Length; i++)
        {
            if (i != rect.Length - 1)
            {
                p0 = rect[i];
                p1 = rect[i + 1];
            }
            else
            {
                p0 = rect[i];
                p1 = rect[0];
            }

            if (p0.y == p1.y ||
               point.y < Mathf.Min(p0.y, p1.y) ||
               point.y >= Mathf.Max(p0.y, p1.y))
            {
                continue;
            }

            float x = (point.y - p0.y) * (p1.x - p0.x) / (p1.y - p0.y) + p0.x;
            if (x > point.x)
                crossCount++; // 只统计单边交点 

        }

        return (crossCount % 2 == 1);
    }

    /// <summary>
    /// 两条线段是否相交
    /// </summary>
    /// <param name="a">点0的x</param>
    /// <param name="b">点0的y</param>
    /// <param name="c">点1的x</param>
    /// <param name="d">点1的y</param>
    /// <returns></returns>
    public bool IsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        //排斥试验
        if (Mathf.Max(a.x, b.x) < Mathf.Min(c.x, d.x))
        {
            return false;
        }

        if (Mathf.Min(a.x, b.x) > Mathf.Max(c.x, d.x))
        {
            return false;
        }

        if (Mathf.Max(a.y, b.y) < Mathf.Min(c.y, d.y))
        {
            return false;
        }

        if (Mathf.Min(a.y, b.y) > Mathf.Max(c.y, d.y))
        {
            return false;
        }

        //跨立试验
        Vector2 ca = a - c;
        Vector2 cd = d - c;
        Vector2 cb = b - c;
        Vector2 ac = c - a;
        Vector2 ab = b - a;
        Vector2 ad = d - a;

        //float cross_ca_cd = ca.x * cd.y - cd.x * ca.y;
        //float cross_cb_cd = cb.x * cd.y - cd.x * cb.y;
        //float cross_ac_ab = ac.x * ab.y - ab.x * ac.y;
        //float cross_ad_ab = ad.x * ab.y - ab.x * ad.y;

        float cross_ca_cd = GetCross(ca, cd);
        float cross_cb_cd = GetCross(cb, cd);
        float cross_ac_ab = GetCross(ac, ab);
        float cross_ad_ab = GetCross(ad, ab);

        return (cross_ca_cd * cross_cb_cd <= 0 && cross_ac_ab * cross_ad_ab <= 0);
    }

    float GetCross(Vector2 v0, Vector2 v1)
    {
        return v0.x * v1.y - v1.x * v0.y;
    }

    //视锥检测相关
    //精确进行视锥检测
    public bool IsBoundInCameraByBox(AABoundingBox aabb, Camera camera)
    {
        if (camera == null)
        {
            return false;
        }

        int mask = Convert.ToInt32("FF", 16);

        Vector4 worldPos;
        for (int i = 0; i < 8; i++)
        {
            worldPos = new Vector4(((i & 0x01) == 0 ? aabb.min : aabb.max).x,
                ((i & 0x02) == 0 ? aabb.min : aabb.max).y,
                ((i & 0x04) == 0 ? aabb.min : aabb.max).z, 1);

            //mask &= ComputeProjectionMask(worldPos, mainCameraWorldToProjection);
            mask &= ComputeProjectionMask(worldPos, camera.projectionMatrix * camera.worldToCameraMatrix);
        }

        //存在AABB八个顶点在一个视锥体面之外
        if (mask != 0) return false;

        //Debug.Log("Real Matrix" + camera.projectionMatrix * camera.worldToCameraMatrix);
        //Debug.Log("Custom Matrix" + RenderPipeline.instance.occlusionPrijectionMatrix * camera.worldToCameraMatrix);

        return true;

    }

    int ComputeProjectionMask(Vector4 pos, Matrix4x4 projection)
    {
        pos = projection * pos;
        int mask = 0;
        if (pos.x < -pos.w) mask |= 0x01;
        if (pos.x > pos.w) mask |= 0x02;
        if (pos.y < -pos.w) mask |= 0x04;
        if (pos.y > pos.w) mask |= 0x08;
        if (pos.z < -pos.w) mask |= 0x10;
        if (pos.z > pos.w) mask |= 0x20;
        return mask;
    }


    public struct Sphere
    {
        public Vector3 spherePoint;
        public float radius;
    }

    public struct Cone
    {
        public Vector3 coneVertex;
        public Vector3 coneBottomSpherePoint;
        public float radius;
    }

    //粗略进行视锥检测
    public bool IsBoundInCameraBySphere(AABoundingBox aabb, Camera camera)
    {
        Sphere sphere = new Sphere
        {
            spherePoint = (aabb.min+aabb.max)/ 2,
            radius = Vector3.Distance(aabb.max, aabb.min) / 2
        };

        float farY = camera.farClipPlane * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float farX = Screen.width / Screen.height * farY;
        float farDis = Mathf.Sqrt(farX * farX + farY * farY);

        Cone cone = new Cone
        {
            coneVertex = camera.transform.position,
            coneBottomSpherePoint = camera.transform.position + camera.transform.forward * camera.farClipPlane,
            radius = farDis
        };

        return SphereConeCross(sphere, cone);
    }

    //用圆锥体和球体进行相交检测来粗略判断是否在视锥内
    //视锥用圆锥体进行粗略模拟
    //aabb包围盒用球体来进行粗略模拟
    public bool SphereConeCross(Sphere sphere, Cone cone)
    {
        Vector3 bottomDir = cone.coneBottomSpherePoint - cone.coneVertex;
        float height = Vector3.Distance(bottomDir, Vector3.zero);
        bottomDir = bottomDir.normalized;
        float length = Vector3.Dot(sphere.spherePoint - cone.coneVertex, bottomDir);
        if (Vector3.Distance(cone.coneVertex, sphere.spherePoint) < sphere.radius) return true;
        if (length < 0 || length > height) return false;

        Vector3 coneSpherePoint = cone.coneVertex + bottomDir * length;
        float coneRadius = cone.radius / height * length;

        return Vector3.Distance(coneSpherePoint, sphere.spherePoint) < (sphere.radius + coneRadius);

    }


    #endregion
}
