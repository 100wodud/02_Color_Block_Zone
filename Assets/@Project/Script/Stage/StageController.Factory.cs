using System.Collections.Generic;
using UnityEngine;

public partial class StageController
{
    private void MeasureBrick()
    {
        var temp = Instantiate(brickPrefab);
        temp.hideFlags = HideFlags.HideAndDontSave;
        var rends = temp.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            _cellSize = Mathf.Max(b.size.x, b.size.z);
            _brickHeight = Mathf.Max(0.01f, b.size.y);
        }
        Destroy(temp);
    }

    private GameObject SpawnBrickVisual(string name, Transform parent, Material material)
        => SpawnBrickVisual(name, parent, material, boardTilePrefab);

    private GameObject SpawnBrickVisual(string name, Transform parent, Material material, GameObject prefab)
    {
        var go = Instantiate(prefab != null ? prefab : brickPrefab, parent);
        go.name = name;
        foreach (var col in go.GetComponentsInChildren<Collider>()) Destroy(col);
        ApplyMaterial(go, material);
        return go;
    }

    private static void ApplyMaterial(GameObject go, Material material)
    {
        if (material == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            if (r.GetComponentInParent<KeepMaterial>() == null) r.sharedMaterial = material;
    }

    private void BuildPieceMesh(Transform root, List<Vector2Int> local, Material material)
        => BuildPieceMesh(root, local, material, brickPrefab);

    private void BuildPieceMesh(Transform root, List<Vector2Int> local, Material material, GameObject prefab)
    {
        material = BrickCellMaterial(material);

        var placements = new List<Matrix4x4>(local.Count);
        foreach (var lc in local)
            placements.Add(Matrix4x4.TRS(new Vector3(lc.x * _cellSize, 0f, lc.y * _cellSize), Quaternion.identity, Vector3.one * cellScale));

        CombineInto(root, "Cells", material, placements, prefab);
        if (roundedBlocks && prefab == brickPrefab) RoundPieceCells(root.Find("Cells"), local);
    }

    private static readonly Vector2Int[] PieceDirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

    // 칸마다 둥근 상자로 바꾸되, 같은 조각의 이웃 칸 쪽은 평평하게 붙여서 한 덩어리로 보이게 한다.
    // CombineInto 는 local 순서대로 칸을 만든다
    private void RoundPieceCells(Transform cellsRoot, List<Vector2Int> local)
    {
        if (cellsRoot == null) return;
        var set = new HashSet<Vector2Int>(local);
        for (int i = 0; i < local.Count && i < cellsRoot.childCount; i++)
        {
            var cell = cellsRoot.GetChild(i);
            foreach (var mf in cell.GetComponentsInChildren<MeshFilter>())
            {
                // 조각 좌표 (x, y) 는 판 위에서 (x, 0, y) 방향이다. 메쉬 로컬 축으로 옮겨서 면을 고른다
                int joined = 0;
                foreach (var dir in PieceDirs)
                {
                    if (!set.Contains(local[i] + dir)) continue;
                    var world = cellsRoot.TransformDirection(new Vector3(dir.x, 0f, dir.y));
                    joined |= RoundedBoxMesh.SideBit(mf.transform.InverseTransformDirection(world));
                }
                // 칸은 _cellSize 간격으로 놓이고 크기는 cellScale 배다 — 메쉬 로컬에서 칸 경계는 원점에서 이만큼이다
                float scale = mf.transform.lossyScale.x / cellsRoot.lossyScale.x;
                float cellHalf = _cellSize * 0.5f / scale;
                mf.sharedMesh = RoundedBoxMesh.For(mf.sharedMesh, blockRoundness, joined, cellHalf);
            }
        }
    }

    private Dictionary<Material, Material> _brickMats;
    private Material BrickCellMaterial(Material src)
    {
        if (src == null) return null;
        _brickMats ??= new Dictionary<Material, Material>();
        if (!_brickMats.TryGetValue(src, out var m))
        {
            m = CreateTintedMaterial(src, _brickTint);
            _brickMats[src] = m;
        }
        return m;
    }

    private void CombineInto(Transform parent, string name, Material material, List<Matrix4x4> placements)
        => CombineInto(parent, name, material, placements, brickPrefab);

    private void CombineInto(Transform parent, string name, Material material, List<Matrix4x4> placements, GameObject prefab)
    {
        if (placements.Count == 0 || prefab == null) return;

        var container = new GameObject(name);
        container.transform.SetParent(parent, false);

        foreach (var placement in placements)
        {
            var go = Instantiate(prefab, container.transform);
            go.name = name;
            foreach (var col in go.GetComponentsInChildren<Collider>()) Destroy(col);
            ApplyMaterial(go, material);

            go.transform.localPosition = placement.GetColumn(3);
            go.transform.localRotation = placement.rotation;
            go.transform.localScale = placement.lossyScale;
        }
    }

    private Material CreateOutlineMaterial(Material src) => CreateTintedMaterial(src, _outlineTint);

    private Material OutlineMaterial(int colorIndex, Material blockMaterial)
    {
        if (outlinePalette != null && colorIndex >= 0 && colorIndex < outlinePalette.Length
            && outlinePalette[colorIndex] != null)
            return outlinePalette[colorIndex];
        return CreateOutlineMaterial(blockMaterial);
    }

    // 판·트레이 밑에 까는 평평한 판. 윗면을 칸들의 바닥보다 drop 만큼 아래에 둔다 —
    // 여러 장을 겹치면 drop 이 작은 것(안쪽 판)이 위에 보이고, 큰 것은 바깥 테두리로만 보인다
    private void AddPanel(Transform parent, string name, Bounds tiles, float margin, Color color, float drop)
    {
        const float thickness = 0.05f;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, true);
        go.GetComponent<Renderer>().sharedMaterial = CreateColoredMaterial(baseMaterial, color);
        go.transform.position = new Vector3(tiles.center.x, tiles.min.y - drop - thickness * 0.5f, tiles.center.z);
        go.transform.localScale = new Vector3(tiles.size.x + margin * 2f, thickness, tiles.size.z + margin * 2f);
    }

    private static Bounds TileBounds(IEnumerable<GameObject> tiles)
    {
        bool any = false;
        var b = new Bounds();
        foreach (var t in tiles)
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
        return b;
    }

    // 원래 재질의 명암 단계는 살리고 색만 바꾼다 — FlatKit 은 밝은 면(_BaseColor)과 그늘(_ColorDim 등)을 따로 가진다
    private static Material CreateColoredMaterial(Material src, Color color)
    {
        var mat = new Material(src);
        Color main = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
        float lum = Mathf.Max(0.001f, main.grayscale);
        foreach (var prop in new[] { "_BaseColor", "_ColorDim", "_ColorDimExtra", "_ColorGradient", "_FlatRimColor" })
        {
            if (!mat.HasProperty(prop)) continue;
            float k = prop == "_BaseColor" ? 1f : Mathf.Clamp01(mat.GetColor(prop).grayscale / lum);
            mat.SetColor(prop, new Color(color.r * k, color.g * k, color.b * k, 1f));
        }
        return mat;
    }

    private Material CreateTintedMaterial(Material src, Color tint)
    {
        if (src == null) return null;
        var mat = new Material(src);
        var sh = mat.shader;
        int count = sh.GetPropertyCount();
        for (int i = 0; i < count; i++)
        {
            if (sh.GetPropertyType(i) != UnityEngine.Rendering.ShaderPropertyType.Color) continue;
            string name = sh.GetPropertyName(i);
            if (name.IndexOf("Emiss", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
            var c = mat.GetColor(name);
            mat.SetColor(name, new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a));
        }
        return mat;
    }

    // 판에 놓인 조각은 칸마다 그 칸의 구역 색을 받아서, 한 조각 안에서도 칸마다 색이 다를 수 있다
    private void BuildPieceVisual(Transform root, List<Vector2Int> local, IReadOnlyList<BrickColor> colors)
    {
        BuildPieceMesh(root, local, palette[(int)colors[0]]);

        // CombineInto 는 local 순서대로 칸을 만든다
        var cellsRoot = root.Find("Cells");
        if (cellsRoot != null)
            for (int i = 1; i < local.Count && i < cellsRoot.childCount; i++)
                if (colors[i] != colors[0])
                    ApplyMaterial(cellsRoot.GetChild(i).gameObject, BrickCellMaterial(palette[(int)colors[i]]));

        if (!pieceOutlines) return;
        var colorOf = new Dictionary<Vector2Int, int>();
        for (int i = 0; i < local.Count; i++) colorOf[local[i]] = (int)colors[i];
        AddPieceOutline(root, local, colorOf);
    }

    // 테두리는 조각 전체를 한 번에 두르고, 토막마다 그 토막이 붙은 칸의 색을 쓴다
    private void AddPieceOutline(Transform root, List<Vector2Int> local, Dictionary<Vector2Int, int> colorOf)
    {
        var set = new HashSet<Vector2Int>(local);
        float thickness = _cellSize * 0.06f;
        float barHeight = _brickHeight * 0.2f;
        float y = _brickHeight;
        float sy = _brickHeight > 0f ? barHeight / _brickHeight : barHeight;
        float edge = _cellSize * 0.5f - thickness * 0.5f + 0.01f;
        float lengthScale = cellScale;
        var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        float capRadius = thickness * 1f;
        float capInset = thickness * 0.5f;
        float capEdge = edge - capInset;

        var placements = new Dictionary<int, List<Matrix4x4>>();
        void Add(Vector2Int cell, Matrix4x4 m)
        {
            int ci = colorOf[cell];
            if (!placements.TryGetValue(ci, out var list)) placements[ci] = list = new List<Matrix4x4>();
            list.Add(m);
        }
        var caps = new List<(Vector3 pos, Vector2Int bulge, int color)>();
        foreach (var cell in local)
        {
            foreach (var dir in dirs)
            {
                if (set.Contains(cell + dir)) continue;

                var pos = new Vector3(
                    cell.x * _cellSize + dir.x * edge,
                    y,
                    cell.y * _cellSize + dir.y * edge);

                Vector2Int endA = dir.x != 0 ? Vector2Int.up : Vector2Int.right;
                Vector2Int endB = dir.x != 0 ? Vector2Int.down : Vector2Int.left;
                bool turnA = !set.Contains(cell + endA);
                bool turnB = !set.Contains(cell + endB);
                float trimA = turnA ? capRadius : 0f;
                float trimB = turnB ? capRadius : 0f;

                float lenScale = lengthScale * (_cellSize - trimA - trimB) / _cellSize;
                float shift = (trimB - trimA) * 0.5f;

                Vector3 scale;
                if (dir.x != 0)
                {
                    pos.z += shift;
                    scale = new Vector3(thickness / _cellSize, sy, lenScale);
                }
                else
                {
                    pos.x += shift;
                    scale = new Vector3(lenScale, sy, thickness / _cellSize);
                }
                Add(cell, Matrix4x4.TRS(pos, Quaternion.identity, scale));

                if (turnA) caps.Add((new Vector3(
                    cell.x * _cellSize + (dir.x + endA.x) * capEdge, y + 0.03f,
                    cell.y * _cellSize + (dir.y + endA.y) * capEdge), endA, colorOf[cell]));
                if (turnB) caps.Add((new Vector3(
                    cell.x * _cellSize + (dir.x + endB.x) * capEdge, y + 0.03f,
                    cell.y * _cellSize + (dir.y + endB.y) * capEdge), endB, colorOf[cell]));
            }
        }

        float cornerScale = thickness / _cellSize;
        foreach (var cell in local)
        {
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                if (set.Contains(cell + new Vector2Int(sx, sz))) continue;
                if (!set.Contains(cell + new Vector2Int(sx, 0))) continue;
                if (!set.Contains(cell + new Vector2Int(0, sz))) continue;
                var cornerPos = new Vector3(
                    cell.x * _cellSize + sx * edge, y,
                    cell.y * _cellSize + sz * edge);
                Add(cell, Matrix4x4.TRS(cornerPos, Quaternion.identity,
                    new Vector3(cornerScale, sy, cornerScale)));
            }
        }

        var outline = new GameObject("Outline").transform;
        outline.SetParent(root, false);
        foreach (var kv in placements)
            CombineInto(outline, "OutlinePart", OutlineMaterial(kv.Key, palette[kv.Key]), kv.Value);

        var capMesh = HalfCylinderCapMesh();
        foreach (var (capPos, bulge, color) in caps)
        {
            var mat = OutlineMaterial(color, palette[color]);
            var post = new GameObject("OutlineCap");
            post.transform.SetParent(outline, false);
            post.AddComponent<MeshFilter>().sharedMesh = capMesh;
            post.AddComponent<MeshRenderer>().sharedMaterial = mat;
            post.transform.localPosition = capPos;
            post.transform.localRotation = Quaternion.LookRotation(new Vector3(bulge.x, 0f, bulge.y), Vector3.up);
            post.transform.localScale = new Vector3(capRadius, barHeight, capRadius);
        }
    }

    private Mesh _capMesh;

    private Mesh HalfCylinderCapMesh()
    {
        if (_capMesh != null) return _capMesh;

        const int segs = 20;
        var verts = new List<Vector3>();
        var tris = new List<int>();

        var top = new int[segs + 1];
        var bot = new int[segs + 1];
        for (int i = 0; i <= segs; i++)
        {
            float a = Mathf.PI * i / segs;
            float x = Mathf.Cos(a);
            float z = Mathf.Sin(a);
            top[i] = verts.Count; verts.Add(new Vector3(x, 0.5f, z));
            bot[i] = verts.Count; verts.Add(new Vector3(x, -0.5f, z));
        }
        int topC = verts.Count; verts.Add(new Vector3(0f, 0.5f, 0f));
        int botC = verts.Count; verts.Add(new Vector3(0f, -0.5f, 0f));

        for (int i = 0; i < segs; i++)
        {
            tris.Add(topC); tris.Add(top[i + 1]); tris.Add(top[i]);
            tris.Add(botC); tris.Add(bot[i]); tris.Add(bot[i + 1]);
            tris.Add(top[i]); tris.Add(top[i + 1]); tris.Add(bot[i]);
            tris.Add(top[i + 1]); tris.Add(bot[i + 1]); tris.Add(bot[i]);
        }
        tris.Add(top[0]); tris.Add(bot[0]); tris.Add(top[segs]);
        tris.Add(top[segs]); tris.Add(bot[0]); tris.Add(bot[segs]);

        _capMesh = new Mesh { name = "OutlineCornerCap" };
        _capMesh.SetVertices(verts);
        _capMesh.SetTriangles(tris, 0);
        _capMesh.RecalculateNormals();
        _capMesh.RecalculateBounds();
        return _capMesh;
    }
}
