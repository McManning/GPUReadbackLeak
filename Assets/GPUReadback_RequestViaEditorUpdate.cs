using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/**
 * QA suggests we do a .Dispose() after working working with the data.
 *
 * This still leaks.
 */
[ExecuteAlways]
public class GPUReadback_RequestViaEditorUpdate : MonoBehaviour
{
    Camera cam;
    RenderTexture rt;
    bool pending;
    AsyncGPUReadbackRequest request;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = gameObject.AddComponent<Camera>();
        }

        EditorApplication.update += OnEditorUpdate;

        rt = new RenderTexture(1920, 1080, 1, RenderTextureFormat.ARGBFloat);
        cam.targetTexture = rt;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;

        cam.targetTexture = null;
        DestroyImmediate(rt);
    }

    private void OnEditorUpdate()
    {
        if (request.hasError || request.done)
        {
            Debug.Log("New request");
            request = AsyncGPUReadback.Request(rt, 0, OnAsyncReadback);
        }
        else
        {
            request.Update(); // Required when the editor is out of focus.
        }
    }

    private void OnAsyncReadback(AsyncGPUReadbackRequest obj)
    {
        var data = obj.GetData<byte>();
        unsafe
        {
            Debug.Log(
                $"Async readback write {obj.layerDataSize} bytes to {new IntPtr(data.GetUnsafePtr())}"
            );
        }

        data.Dispose(); // <-- Added, still leaks while the editor is out of focus.
    }
}
