
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/*
This example results in:

InvalidOperationException: The Unity.Collections.NativeArray`1[System.Byte] can no longer be accessed, since its owner has been invalidated. You can simply Dispose() the container and create a new one.
Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle.CheckWriteAndThrowNoEarlyOut (Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle handle) (at <86acb61e0d2b4b36bc20af11093be9a5>:0)
Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle.CheckWriteAndThrow (Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle handle) (at <86acb61e0d2b4b36bc20af11093be9a5>:0)
Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr[T] (Unity.Collections.NativeArray`1[T] nativeArray) (at <86acb61e0d2b4b36bc20af11093be9a5>:0)
UnityEngine.Rendering.AsyncRequestNativeArrayData.CreateAndCheckAccess[T] (Unity.Collections.NativeArray`1[T] array) (at <86acb61e0d2b4b36bc20af11093be9a5>:0)
UnityEngine.Rendering.AsyncGPUReadback.RequestIntoNativeArray[T] (Unity.Collections.NativeArray`1[T]& output, UnityEngine.Texture src, System.Int32 mipIndex, System.Action`1[T] callback) (at <86acb61e0d2b4b36bc20af11093be9a5>:0)
GPUReadback_RequestIntoNativeArrayViaEditorUpdate.OnEditorUpdate () (at Assets/GPUReadback_RequestIntoNativeArrayViaEditorUpdate.cs:49)

*/
[ExecuteAlways]
public class GPUReadback_RequestIntoNativeArrayViaEditorUpdate : MonoBehaviour
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

        bucket = new NativeArray<byte>(
            1024 * 1024 * 40,
            Allocator.Persistent,
            NativeArrayOptions.UninitializedMemory
        );

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

        // No dispose or access of obj.GetData() - since we're using a persistent NativeArray.
        // Which I would have assumed is the proper way to do this.
    }
}
