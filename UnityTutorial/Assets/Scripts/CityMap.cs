using System.Collections.Generic;
using UnityEngine;

// Grade da cidade: cada caractere é uma célula de 1x1 unidade.
// '.' = rua, '#' = prédio, 'G' = posto, 'P' = parque (tudo que não é rua é obstáculo).
// Precisa ser igual ao ROWS de Tools/generate_assets.py, que desenha o City.png.
public static class CityMap
{
    public static readonly string[] Rows =
    {
        "......................",
        "......................",
        "..###..###..GGG..###..",
        "..###..###..GGG..###..",
        "..###..###..GGG..###..",
        "......................",
        "......................",
        "..###..PPP..###..###..",
        "..###..PPP..###..###..",
        "..###..PPP..###..###..",
        "......................",
        "......................",
    };

    public static int Width => Rows[0].Length;
    public static int Height => Rows.Length;

    // Canto inferior esquerdo do mapa em coordenadas do mundo
    public static readonly Vector2 Origin = new Vector2(-11f, -6.6f);
    public static Vector2 Center => Origin + new Vector2(Width / 2f, Height / 2f);

    // y = 0 é a linha de baixo
    public static char CellAt(int x, int y) => Rows[Height - 1 - y][x];

    public static bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;

    public static bool IsRoad(Vector2Int c) => InBounds(c) && CellAt(c.x, c.y) == '.';

    public static Vector2 CellToWorld(Vector2Int c) => Origin + new Vector2(c.x + 0.5f, c.y + 0.5f);

    public static Vector2Int WorldToCell(Vector2 p)
    {
        Vector2 local = p - Origin;
        return new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
    }

    public static Vector2Int NearestRoad(Vector2 p)
    {
        Vector2Int c = WorldToCell(p);
        if (IsRoad(c)) return c;
        Vector2Int best = c;
        float bestDist = float.MaxValue;
        for (int r = 1; r <= 4; r++)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    var n = new Vector2Int(c.x + dx, c.y + dy);
                    if (!IsRoad(n)) continue;
                    float d = (CellToWorld(n) - p).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; best = n; }
                }
            if (bestDist < float.MaxValue) break;
        }
        return best;
    }

    // Verifica se um objeto de raio "radius" anda em linha reta de a até b só pela rua
    public static bool LineClear(Vector2 a, Vector2 b, float radius)
    {
        float dist = Vector2.Distance(a, b);
        int steps = Mathf.Max(1, Mathf.CeilToInt(dist / 0.2f));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
            if (!IsRoad(WorldToCell(p + new Vector2(radius, radius))) ||
                !IsRoad(WorldToCell(p + new Vector2(-radius, radius))) ||
                !IsRoad(WorldToCell(p + new Vector2(radius, -radius))) ||
                !IsRoad(WorldToCell(p + new Vector2(-radius, -radius))))
                return false;
        }
        return true;
    }

    static int[] cameFrom;
    static readonly Queue<int> queue = new Queue<int>();
    static readonly Vector2Int[] directions =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
    };

    // Busca em largura (BFS) pela grade, com diagonais sem "cortar" quinas de prédio.
    // Preenche "path" com as células do início até o destino.
    public static bool FindPath(Vector2Int start, Vector2Int goal, List<Vector2Int> path)
    {
        path.Clear();
        if (!IsRoad(start) || !IsRoad(goal)) return false;

        int size = Width * Height;
        if (cameFrom == null || cameFrom.Length != size) cameFrom = new int[size];
        for (int i = 0; i < size; i++) cameFrom[i] = -1;

        int startIndex = start.y * Width + start.x;
        int goalIndex = goal.y * Width + goal.x;
        cameFrom[startIndex] = startIndex;
        queue.Clear();
        queue.Enqueue(startIndex);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (current == goalIndex) break;
            var c = new Vector2Int(current % Width, current / Width);
            foreach (var d in directions)
            {
                var n = c + d;
                if (!IsRoad(n)) continue;
                if (d.x != 0 && d.y != 0 && (!IsRoad(new Vector2Int(c.x + d.x, c.y)) || !IsRoad(new Vector2Int(c.x, c.y + d.y))))
                    continue;
                int ni = n.y * Width + n.x;
                if (cameFrom[ni] != -1) continue;
                cameFrom[ni] = current;
                queue.Enqueue(ni);
            }
        }

        if (cameFrom[goalIndex] == -1) return false;
        for (int i = goalIndex; ; i = cameFrom[i])
        {
            path.Add(new Vector2Int(i % Width, i / Width));
            if (i == startIndex) break;
        }
        path.Reverse();
        return true;
    }
}
