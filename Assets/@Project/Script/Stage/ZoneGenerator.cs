using System.Collections.Generic;
using UnityEngine;

// 보드를 랜덤한 모양의 구역으로 나누고 색을 칠한다. 색은 판을 만들 때 한 번만 칠한다.
//
// WHY: 씨앗 여러 개를 동시에 키우되, 매 단계 "가장 작은 구역"만 한 칸 자라게 한다.
// 그냥 번갈아 키우면 먼저 막힌 구역이 쪼그라들고 한 구역이 판을 먹는다.
public static class ZoneGenerator
{
    private static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    public static int[,] Generate(int width, int height, int zoneCount, int minSize, int maxSize)
    {
        int[,] best = null;
        int bestSpread = int.MaxValue;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            var zoneOf = TryGrow(width, height, zoneCount);
            var sizes = new int[zoneCount];
            foreach (var z in zoneOf) sizes[z]++;

            int min = int.MaxValue, max = 0;
            foreach (var s in sizes) { min = Mathf.Min(min, s); max = Mathf.Max(max, s); }
            if (min >= minSize && max <= maxSize) return zoneOf;

            // 조건을 끝내 못 맞추면 크기가 가장 고른 판을 쓴다 — 로딩이 멈추는 것보다 낫다
            if (max - min < bestSpread) { bestSpread = max - min; best = zoneOf; }
        }
        return best;
    }

    private static int[,] TryGrow(int width, int height, int zoneCount)
    {
        var zoneOf = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                zoneOf[x, y] = -1;

        var frontier = new List<List<Vector2Int>>();
        var sizes = new int[zoneCount];
        foreach (var seed in PickSeeds(width, height, zoneCount))
        {
            int id = frontier.Count;
            zoneOf[seed.x, seed.y] = id;
            sizes[id] = 1;
            frontier.Add(new List<Vector2Int>());
            AddNeighbors(frontier[id], seed, width, height);
        }

        int unassigned = width * height - zoneCount;
        while (unassigned > 0)
        {
            int zone = SmallestGrowable(frontier, sizes);
            if (zone < 0) break;

            var list = frontier[zone];
            int pick = Random.Range(0, list.Count);
            var cell = list[pick];
            list.RemoveAt(pick);
            if (zoneOf[cell.x, cell.y] >= 0) continue;

            zoneOf[cell.x, cell.y] = zone;
            sizes[zone]++;
            unassigned--;
            AddNeighbors(list, cell, width, height);
        }
        return zoneOf;
    }

    private static int SmallestGrowable(List<List<Vector2Int>> frontier, int[] sizes)
    {
        int best = -1;
        for (int z = 0; z < frontier.Count; z++)
        {
            if (frontier[z].Count == 0) continue;
            if (best < 0 || sizes[z] < sizes[best] || (sizes[z] == sizes[best] && Random.value < 0.5f)) best = z;
        }
        return best;
    }

    private static void AddNeighbors(List<Vector2Int> list, Vector2Int cell, int width, int height)
    {
        foreach (var d in Dirs)
        {
            var n = cell + d;
            if (n.x >= 0 && n.y >= 0 && n.x < width && n.y < height) list.Add(n);
        }
    }

    // 씨앗끼리 너무 붙으면 한 구역이 다른 구역에 갇혀 가늘어진다
    private static List<Vector2Int> PickSeeds(int width, int height, int count)
    {
        var seeds = new List<Vector2Int>();
        int minDist = Mathf.Max(2, Mathf.RoundToInt(Mathf.Sqrt(width * height / (float)count)) - 1);
        for (int tries = 0; seeds.Count < count && tries < 2000; tries++)
        {
            var c = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
            bool ok = true;
            foreach (var s in seeds)
                if (Mathf.Abs(s.x - c.x) + Mathf.Abs(s.y - c.y) < minDist) { ok = false; break; }
            if (ok) seeds.Add(c);
        }
        // 간격 조건을 못 채우면 남은 수만큼 아무 빈 칸이나 쓴다
        while (seeds.Count < count)
        {
            var c = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
            if (!seeds.Contains(c)) seeds.Add(c);
        }
        return seeds;
    }

    public static List<HashSet<int>> Adjacency(int[,] zoneOf, int zoneCount)
    {
        var adj = new List<HashSet<int>>();
        for (int i = 0; i < zoneCount; i++) adj.Add(new HashSet<int>());
        int w = zoneOf.GetLength(0), h = zoneOf.GetLength(1);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                int a = zoneOf[x, y];
                if (x + 1 < w && zoneOf[x + 1, y] != a) { adj[a].Add(zoneOf[x + 1, y]); adj[zoneOf[x + 1, y]].Add(a); }
                if (y + 1 < h && zoneOf[x, y + 1] != a) { adj[a].Add(zoneOf[x, y + 1]); adj[zoneOf[x, y + 1]].Add(a); }
            }
        return adj;
    }

    // 이웃 구역과 색이 절대 겹치지 않게 칠한다. 같은 색끼리 붙으면 두 구역이 한 구역처럼 보인다.
    //
    // WHY: 하나씩 욕심껏 칠하면 뒤쪽 구역이 막혀서 이웃과 겹친다. 막히면 되돌아가서 다시 칠한다 —
    // 판을 나눈 구역은 평면 지도라 4색이면 항상 답이 있고, 구역이 9개라 금방 끝난다
    public static BrickColor[] Paint(List<HashSet<int>> adj, int colorCount)
    {
        int n = adj.Count;
        var colors = new int[n];
        for (int i = 0; i < n; i++) colors[i] = -1;

        var order = new List<int>();
        for (int i = 0; i < n; i++) order.Add(i);
        Shuffle(order);
        order.Sort((a, b) => adj[b].Count.CompareTo(adj[a].Count));

        if (!Solve(order, 0, adj, colors, colorCount))
        {
            // 색 수가 모자라게 설정된 경우에만 온다 — 겹치더라도 판은 만든다
            Debug.LogWarning($"[ZoneGenerator] {colorCount}색으로 이웃이 안 겹치게 칠할 수 없음");
            for (int i = 0; i < n; i++) if (colors[i] < 0) colors[i] = Random.Range(0, colorCount);
        }
        return ToColors(colors);
    }

    private static bool Solve(List<int> order, int k, List<HashSet<int>> adj, int[] colors, int colorCount)
    {
        if (k >= order.Count) return true;
        int z = order[k];

        var tryColors = new List<int>();
        for (int c = 0; c < colorCount; c++) tryColors.Add(c);
        Shuffle(tryColors);

        foreach (int c in tryColors)
        {
            if (NeighborHas(z, c, adj, colors)) continue;
            colors[z] = c;
            if (Solve(order, k + 1, adj, colors, colorCount)) return true;
            colors[z] = -1;
        }
        return false;
    }

    private static bool NeighborHas(int zone, int color, List<HashSet<int>> adj, int[] colors)
    {
        foreach (int nb in adj[zone])
            if (colors[nb] == color) return true;
        return false;
    }

    private static BrickColor[] ToColors(int[] colors)
    {
        var result = new BrickColor[colors.Length];
        for (int i = 0; i < colors.Length; i++) result[i] = (BrickColor)colors[i];
        return result;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
