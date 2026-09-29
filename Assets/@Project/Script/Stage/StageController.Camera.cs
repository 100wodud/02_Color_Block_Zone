using System.Collections.Generic;
using UnityEngine;

public partial class StageController
{
    private void FrameCamera()
    {
        if (cam == null) return;

        var rends = new List<Renderer>();
        if (boardRoot != null) rends.AddRange(boardRoot.GetComponentsInChildren<Renderer>());
        if (trayRoot != null) rends.AddRange(trayRoot.GetComponentsInChildren<Renderer>());
        if (rends.Count == 0) return;

        Bounds bounds = rends[0].bounds;
        for (int i = 1; i < rends.Count; i++) bounds.Encapsulate(rends[i].bounds);

        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
        var rot = Quaternion.Euler(cameraTilt, 0f, 0f);
        cam.transform.rotation = rot;

        const float distance = 20f;
        Vector3 forward = rot * Vector3.forward;
        cam.transform.position = bounds.center - forward * distance;

        float maxX = 0f, maxY = 0f;
        Vector3 c = bounds.center;
        Vector3 e = bounds.extents;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    var corner = c + new Vector3(e.x * sx, e.y * sy, e.z * sz);
                    var local = cam.transform.InverseTransformPoint(corner);
                    maxX = Mathf.Max(maxX, Mathf.Abs(local.x));
                    maxY = Mathf.Max(maxY, Mathf.Abs(local.y));
                }

        float aspect = cam.aspect <= 0f ? (9f / 16f) : cam.aspect;
        float r = hudTopReserve;
        float baseSize = Mathf.Max(maxY / (1f - r), maxX / aspect) * 1.1f;
        float size = baseSize * Mathf.Max(1f, screenZoomOut);
        cam.orthographicSize = size;

        // 보이는 세로 범위 [-size, size] 중 위 2·r·size 가 HUD 자리다. 멀리 잡아도 판 윗변과 HUD 사이 간격(화면 비율)은
        // 꽉 맞게 잡았을 때와 같게 두고 판을 위로 붙인다 — 그래야 늘어난 여백이 전부 아래(배너 자리)로 간다
        float gap = baseSize * (1f - r) - maxY;
        float center = size * (1f - 2f * r) - gap * size / baseSize - maxY;
        cam.transform.position -= cam.transform.up * center;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = distance + e.magnitude * 2f + 100f;
    }
}
