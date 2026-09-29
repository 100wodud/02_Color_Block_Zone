using System.Collections.Generic;
using UnityEngine;

// 모서리가 둥근 상자 메쉬. 블록 팩의 1x1 판은 모서리가 각져서 컨셉 이미지의 말랑한 캔디 블록처럼 안 보인다.
//
// WHY: 면마다 격자를 깔되 가운데는 한 칸으로 비워 두고 가장자리에만 촘촘히 둔다. 각 점을 안쪽 상자에
// 붙인 뒤 반지름만큼 바깥으로 밀면 평평한 윗면 + 둥근 모서리가 되고, 이웃한 면의 가장자리 점이
// 같은 자리에 떨어져서 틈이 생기지 않는다.
//
// 같은 조각 안에서 이웃 칸과 맞닿는 쪽(joined)은 둥글리지 않고 칸 경계까지 평평하게 채운 뒤 그 면은 뺀다.
// 그러면 칸마다 색이 달라도 한 덩어리로 이어져 보이고, 다른 조각과는 둥근 홈으로 갈린다
public static class RoundedBoxMesh
{
    private static readonly Dictionary<(Mesh, int, float, float), Mesh> Cache = new();

    // 축 순서: +X, -X, +Y, -Y, +Z, -Z (메쉬 로컬 기준)
    public static int SideBit(Vector3 localDir)
    {
        var a = new Vector3(Mathf.Abs(localDir.x), Mathf.Abs(localDir.y), Mathf.Abs(localDir.z));
        int axis = a.x >= a.y && a.x >= a.z ? 0 : a.y >= a.z ? 1 : 2;
        return 1 << (axis * 2 + (localDir[axis] > 0f ? 0 : 1));
    }

    // 원래 메쉬와 같은 자리·크기의 둥근 상자. joinedSides 쪽은 메쉬 원점에서 cellHalf 거리(칸 경계)까지 평평하게 편다.
    // WHY: 메쉬 크기로 경계를 어림하면 메쉬가 정사각형이 아니거나 중심이 어긋났을 때 이웃 칸과 틈이 생긴다
    public static Mesh For(Mesh source, float radiusRatio, int joinedSides = 0, float cellHalf = 0f)
    {
        if (source == null) return null;
        var key = (source, joinedSides, radiusRatio, cellHalf);
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var b = source.bounds;
        float r = Mathf.Min(Mathf.Min(b.size.x, b.size.z) * radiusRatio, b.size.y * 0.45f);
        var lo = -b.extents;
        var hi = b.extents;
        var roundLo = Vector3.one;
        var roundHi = Vector3.one;
        for (int axis = 0; axis < 3; axis++)
        {
            // lo/hi 는 메쉬 중심 기준이라 원점 기준 경계에서 중심만큼 뺀다
            if ((joinedSides & (1 << (axis * 2))) != 0) { hi[axis] = cellHalf - b.center[axis]; roundHi[axis] = 0f; }
            if ((joinedSides & (1 << (axis * 2 + 1))) != 0) { lo[axis] = -cellHalf - b.center[axis]; roundLo[axis] = 0f; }
        }

        var mesh = Build(b.center, lo, hi, roundLo, roundHi, r, 4, joinedSides);
        mesh.name = $"{source.name}_Rounded_{joinedSides}";
        Cache[key] = mesh;
        return mesh;
    }

    private static Mesh Build(Vector3 center, Vector3 lo, Vector3 hi, Vector3 roundLo, Vector3 roundHi,
        float radius, int segments, int skipSides)
    {
        var innerLo = lo + Vector3.Scale(roundLo, Vector3.one * radius);
        var innerHi = hi - Vector3.Scale(roundHi, Vector3.one * radius);
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        // (법선, u, v) — u×v 가 법선 방향이 되게 고른다. u, v 는 모두 양의 축이다
        var faces = new[]
        {
            (Vector3.right, Vector3.up, Vector3.forward),
            (Vector3.left, Vector3.forward, Vector3.up),
            (Vector3.up, Vector3.forward, Vector3.right),
            (Vector3.down, Vector3.right, Vector3.forward),
            (Vector3.forward, Vector3.right, Vector3.up),
            (Vector3.back, Vector3.up, Vector3.right),
        };

        foreach (var (n, u, v) in faces)
        {
            if ((skipSides & SideBit(n)) != 0) continue;

            int ua = Axis(u), va = Axis(v), na = Axis(n);
            var us = Coords(lo[ua], hi[ua], roundLo[ua] > 0f, roundHi[ua] > 0f, radius, segments);
            var vs = Coords(lo[va], hi[va], roundLo[va] > 0f, roundHi[va] > 0f, radius, segments);
            float depth = n[na] > 0f ? hi[na] : lo[na];
            int start = verts.Count;

            for (int i = 0; i < us.Count; i++)
                for (int j = 0; j < vs.Count; j++)
                {
                    var p = Vector3.zero;
                    p[na] = depth;
                    p[ua] = us[i];
                    p[va] = vs[j];
                    var clamped = new Vector3(
                        Mathf.Clamp(p.x, innerLo.x, innerHi.x),
                        Mathf.Clamp(p.y, innerLo.y, innerHi.y),
                        Mathf.Clamp(p.z, innerLo.z, innerHi.z));
                    var d = p - clamped;
                    bool curved = d.sqrMagnitude > 1e-10f;
                    var normal = curved ? d.normalized : n;
                    verts.Add(center + (curved ? clamped + normal * radius : p));
                    normals.Add(normal);
                    uvs.Add(new Vector2(i / (float)(us.Count - 1), j / (float)(vs.Count - 1)));
                }

            int stride = vs.Count;
            for (int i = 0; i < us.Count - 1; i++)
                for (int j = 0; j < vs.Count - 1; j++)
                {
                    int a = start + i * stride + j;
                    int b = a + 1;
                    int c = a + stride + 1;
                    int d = a + stride;
                    // Unity 는 Cross(1-0, 2-0) 쪽이 앞면이다: Cross(u+v, v) = u×v = 법선
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(a); tris.Add(d); tris.Add(c);
                }
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // 한 축의 격자 좌표: 둥근 끝만 반지름 구간을 segments 칸으로 나누고, 평평한 끝은 끝점 하나만 둔다
    private static List<float> Coords(float lo, float hi, bool roundLo, bool roundHi, float radius, int segments)
    {
        var list = new List<float>();
        if (roundLo) for (int i = 0; i <= segments; i++) list.Add(lo + radius * i / segments);
        else list.Add(lo);
        if (roundHi) for (int i = 0; i <= segments; i++) list.Add(hi - radius + radius * i / segments);
        else list.Add(hi);
        return list;
    }

    private static int Axis(Vector3 unit) => Mathf.Abs(unit.x) > 0.5f ? 0 : Mathf.Abs(unit.y) > 0.5f ? 1 : 2;
}
