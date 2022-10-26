
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/**
 * Variation of GPUReadback_RequestIntoNativeArrayViaEditorUpdate
 * that does call .Dispose() and creates a new NativeArray target each time.
 *
 * This also leaks when running with the editor out of focus.
 */
[ExecuteAlways]
public class GPUReadback_RequestIntoNativeArrayViaEditorUpdateWithDispose : MonoBehaviour
{
    Camera cam;
    RenderTexture rt;
    NativeArray<byte> bucket;
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
        bucket.Dispose();
    }

    private void OnEditorUpdate()
    {
        if (request.hasError || request.done)
        {
            Debug.Log("New request");

            // The only way you can .Dispose() with a RequestIntoNativeArray is to
            // create a new NativeArray every time you want to make that async request.
            // But wouldn't that defeat the purpose of creating a persistent one to
            // then readback into?
            bucket = new NativeArray<byte>(
                1024 * 1024 * 40,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );

            request = AsyncGPUReadback.RequestIntoNativeArray(ref bucket, rt, 0, OnAsyncReadbackIntoNativeArray);
        }
        else
        {
            request.Update(); // Required when the editor is out of focus.
        }
    }

    private void OnAsyncReadbackIntoNativeArray(AsyncGPUReadbackRequest obj)
    {
        unsafe
        {
            Debug.Log(
                $"Async readback write {obj.layerDataSize} bytes to existing NativeArray {new IntPtr(bucket.GetUnsafePtr())}"
            );
        }

        bucket.Dispose();
    }
}
