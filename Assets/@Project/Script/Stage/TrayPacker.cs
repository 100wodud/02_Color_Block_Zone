using System.Collections.Generic;
using UnityEngine;

// 새 조각들을 트레이 그리드에 겹치지 않게 배치한다.
//
// WHY: 세로 I4 는 3줄짜리 트레이에 안 들어가서 돌려서라도 넣어야 한다. 조각끼리 붙어 있으면
// 어디까지가 한 조각인지 헷갈리므로 먼저 한 칸씩 띄워 보고, 안 되면 붙여서 넣는다
public static class TrayPacker
{
    public static List<(List<Vector2Int> cells, Vector2Int anchor)> Pack(List<List<Vector2Int>> shapes, int cols, int rows)
    {
        foreach (bool gap in new[] { true, false })
        {
            var occupied = new HashSet<Vector2Int>();
            var result = new List<(List<Vector2Int>, Vector2Int)>();
            if (Place(shapes, 0, cols, rows, gap, occupied, result))
                return Center(result, cols);
        }
        return null;
    }

    private static bool Place(List<List<Vector2Int>> shapes, int index, int cols, int rows, bool gap,
        HashSet<Vector2Int> occupied, List<(List<Vector2Int>, Vector2Int)> result)
    {
        if (index >= shapes.Count) return true;

        // 뽑힌 방향을 먼저 써 보고, 안 들어가면 돌린다
        for (int turn = 0; turn < 4; turn++)
        {
            var cells = turn == 0 ? shapes[index] : PieceGenerator.Rotate(shapes[index], turn);
            for (int col = 0; col < cols; col++)
                for (int row = rows - 1; row >= 0; row--)
                {
                    var anchor = new Vector2Int(col, row);
                    if (!Fits(cells, anchor, cols, rows, gap, occupied)) continue;

                    foreach (var c in cells) occupied.Add(c + anchor);
                    result.Add((cells, anchor));
                    if (Place(shapes, index + 1, cols, rows, gap, occupied, result)) return true;
                    result.RemoveAt(result.Count - 1);
                    foreach (var c in cells) occupied.Remove(c + anchor);
                }
        }
        return false;
    }

    private static bool Fits(List<Vector2Int> cells, Vector2Int anchor, int cols, int rows, bool gap, HashSet<Vector2Int> occupied)
    {
        foreach (var lc in cells)
        {
            var c = lc + anchor;
            if (c.x < 0 || c.y < 0 || c.x >= cols || c.y >= rows) return false;
            if (occupied.Contains(c)) return false;
            if (!gap) continue;
            if (occupied.Contains(c + Vector2Int.left) || occupied.Contains(c + Vector2Int.right)
                || occupied.Contains(c + Vector2Int.up) || occupied.Contains(c + Vector2Int.down)) return false;
        }
        return true;
    }

    // 왼쪽부터 채워서 한쪽으로 몰리므로 남는 칸을 좌우로 나눠 가운데로 옮긴다
    private static List<(List<Vector2Int> cells, Vector2Int anchor)> Center(List<(List<Vector2Int>, Vector2Int)> placed, int cols)
    {
        int maxX = 0;
        foreach (var (cells, anchor) in placed)
            foreach (var c in cells) maxX = Mathf.Max(maxX, c.x + anchor.x);
        int shift = (cols - 1 - maxX) / 2;

        var list = new List<(List<Vector2Int>, Vector2Int)>();
        foreach (var (cells, anchor) in placed) list.Add((cells, anchor + new Vector2Int(shift, 0)));
        return list;
    }
}
