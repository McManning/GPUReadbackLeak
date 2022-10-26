
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/**
 * Variation that uses CommandBuffer.RequestAsyncReadback
 *
 * It'll be the same leak - but faster due to not waiting for the previous to finish.
 * It doesn't matter if we wait or not.
 */
[ExecuteAlways]
public class CmdBufferReadback_RequestViaEditorUpdate : MonoBehaviour
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
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;

        rt = new RenderTexture(1920, 1080, 1, RenderTextureFormat.ARGBFloat);
        cam.targetTexture = rt;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

        cam.targetTexture = null;
        DestroyImmediate(rt);
        bucket.Dispose();
    }

    private void OnEditorUpdate()
    {
        // Force render to happen while the editor isn't focused
        cam.Render();
    }

    private void OnEndCameraRendering(ScriptableRenderContext ctx, Camera current)
    {
        if (current != cam)
            return;

        Debug.Log("OnEndCameraRendering");

        var cmd = CommandBufferPool.Get();

        cmd.RequestAsyncReadback(rt, OnAsyncReadback);

        ctx.ExecuteCommandBuffer(cmd);

        // Forcing the run due to the editor not being in focus
        ctx.Submit();

        cmd.Clear();
        CommandBufferPool.Release(cmd);
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

        data.Dispose();
    }
}
