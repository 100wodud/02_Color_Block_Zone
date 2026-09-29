using System.Collections.Generic;
using UnityEngine;

// 트레이에 나올 조각 모양을 뽑는다. 조각은 색이 없다 — 판에 놓을 때 구역 색을 받는다.
public static class PieceGenerator
{
    private struct Shape
    {
        public Vector2Int[] Cells;
        public int Weight;
        public Shape(int weight, params (int x, int y)[] cells)
        {
            Weight = weight;
            Cells = new Vector2Int[cells.Length];
            for (int i = 0; i < cells.Length; i++) Cells[i] = new Vector2Int(cells[i].x, cells[i].y);
        }
    }

    // WHY: 구역 하나가 7칸 안팎이라 5칸 이상 조각은 넣지 않는다 — 한 조각이 구역을 거의 다 먹으면
    // 한 구역 안에 맞춰 넣을 여지가 사라진다. 작은 조각 가중치를 높여 구역 끝자리를 메울 수 있게 한다
    private static readonly Shape[] Shapes =
    {
        new(3, (0, 0)),                                  // 1칸
        new(5, (0, 0), (1, 0)),                          // 2칸
        new(4, (0, 0), (1, 0), (2, 0)),                  // I3
        new(5, (0, 0), (1, 0), (0, 1)),                  // L3
        new(2, (0, 0), (1, 0), (2, 0), (3, 0)),          // I4
        new(3, (0, 0), (1, 0), (0, 1), (1, 1)),          // O
        new(3, (0, 0), (1, 0), (2, 0), (0, 1)),          // L4
        new(2, (0, 0), (1, 0), (2, 0), (2, 1)),          // J4
        new(3, (0, 0), (1, 0), (2, 0), (1, 1)),          // T
        new(2, (0, 0), (1, 0), (1, 1), (2, 1)),          // S
        new(2, (1, 0), (2, 0), (0, 1), (1, 1)),          // Z
    };

    public static List<Vector2Int> RandomShape()
    {
        int total = 0;
        foreach (var s in Shapes) total += s.Weight;
        int roll = Random.Range(0, total);
        Shape pick = Shapes[0];
        foreach (var s in Shapes)
        {
            if (roll < s.Weight) { pick = s; break; }
            roll -= s.Weight;
        }
        return Rotate(pick.Cells, Random.Range(0, 4));
    }

    public static List<Vector2Int> Rotate(IReadOnlyList<Vector2Int> cells, int turns)
    {
        var list = new List<Vector2Int>(cells.Count);
        foreach (var c in cells)
        {
            var r = c;
            for (int t = 0; t < turns; t++) r = new Vector2Int(-r.y, r.x);
            list.Add(r);
        }
        return Normalize(list);
    }

    public static List<Vector2Int> Normalize(List<Vector2Int> cells)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var c in cells) { minX = Mathf.Min(minX, c.x); minY = Mathf.Min(minY, c.y); }
        var list = new List<Vector2Int>(cells.Count);
        foreach (var c in cells) list.Add(new Vector2Int(c.x - minX, c.y - minY));
        return list;
    }

    public static Vector2 Center(IReadOnlyList<Vector2Int> cells)
    {
        int maxX = 0, maxY = 0;
        foreach (var c in cells) { maxX = Mathf.Max(maxX, c.x); maxY = Mathf.Max(maxY, c.y); }
        return new Vector2(maxX * 0.5f, maxY * 0.5f);
    }
}
