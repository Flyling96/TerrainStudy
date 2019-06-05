using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RenderPipeline : MonoBehaviour
{
    public static RenderPipeline instance;

    public ComputeShader terrainViewOccusionCS;

    RenderTexture curRT;

    public Camera mainCamera;
    public Vector3 mainCameraPos;
    //public Matrix4x4 mainCameraProjectionMatrix;
    //public Matrix4x4 mainCameraWorldToProjection;
    public Matrix4x4 occlusionPrijectionMatrix;
    //public Matrix4x4 occlusionWorldToPrijection;

    private void Awake()
    {
        instance = this;
        mainCamera = gameObject.GetComponent<Camera>();
        mainCameraPos = mainCamera.transform.position;
        //mainCameraProjectionMatrix = mainCamera.projectionMatrix;
        //mainCameraWorldToProjection = mainCamera.projectionMatrix * mainCamera.worldToCameraMatrix;
        //剔除时扩大fov
        occlusionPrijectionMatrix = CalcProjectionMatrix(mainCamera.fieldOfView + 5, mainCamera.farClipPlane, mainCamera.nearClipPlane,mainCamera.aspect);
        //occlusionWorldToPrijection = occlusionPrijectionMatrix * mainCamera.worldToCameraMatrix;

    }

    //计算投影矩阵
    Matrix4x4 CalcProjectionMatrix(float fov,float far,float near,float aspect)
    {
        Matrix4x4 result = Matrix4x4.zero;
        result.m00 = 1 / Mathf.Tan((fov / 2) * Mathf.Deg2Rad) / aspect;
        result.m11 = 1 / Mathf.Tan((fov / 2) * Mathf.Deg2Rad);
        result.m22 = -((far + near) / (far - near));
        result.m23 = -(2 * near * far / (far - near));
        result.m32 = -1;

        return result;
    }

    void CloneCamera(Camera source,Camera target)
    {
        source.farClipPlane = target.farClipPlane;
        source.nearClipPlane = target.nearClipPlane;
        source.fieldOfView = target.focalLength;
    }

    private void OnPreCull()
    {
        mainCameraPos = mainCamera.transform.position;
        //mainCameraWorldToProjection = mainCamera.projectionMatrix * mainCamera.worldToCameraMatrix;
        //occlusionWorldToPrijection = occlusionPrijectionMatrix * mainCamera.worldToCameraMatrix;
    }

    private void OnRenderObject()
    {
        //InstanceMgr.instance.UpdateTerrainInstance();
    }
}
