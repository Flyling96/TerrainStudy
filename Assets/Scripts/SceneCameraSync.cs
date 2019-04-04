using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SceneCameraSync : MonoBehaviour
{
    private void OnRenderObject()
    {
        if(Camera.current != null)
        {
            if(Camera.current.name.Equals("SceneCamera"))
            {
                transform.position = Camera.current.transform.position;
                transform.rotation = Camera.current.transform.rotation;
            }
        }
    }
}
