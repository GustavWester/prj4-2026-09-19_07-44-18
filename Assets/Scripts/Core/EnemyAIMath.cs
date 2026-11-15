using System;

/// <summary>
/// Beslutningerne bag fjendernes bevægelse og angrebs-timing.
/// Ren C# (ingen UnityEngine), så den kan unit-testes uden Unity. Bruges af EnemyController.
/// </summary>
public static class EnemyAIMath
{
    /// <summary>Melee- og KeepDistance-fjender stiller sig lidt inden for attackRange.</summary>
    public const float PreferredRangeFactor = 0.8f;

    /// <summary>Korteste ventetid mellem to angreb, uanset hvor negativ jitter er.</summary>
    public const float MinAttackInterval = 0.05f;

    public static float PreferredDistance(float attackRange) => attackRange * PreferredRangeFactor;

    /// <summary>
    /// KeepDistance: 1 = gå mod spilleren, -1 = gå væk, 0 = bliv stående (inden for tolerance).
    /// </summary>
    public static int KeepDistanceStep(float distance, float attackRange, float tolerance)
    {
        float preferred = PreferredDistance(attackRange);
        if (Math.Abs(distance - preferred) <= tolerance) return 0;
        return distance > preferred ? 1 : -1;
    }

    /// <summary>ChasePlayer: om fjenden skal bevæge sig mod spilleren.</summary>
    public static bool ShouldChase(float distance, float detectionRange, bool isMelee, float attackRange)
    {
        if (distance > detectionRange) return false;
        // melee stopper lidt inden for attackRange i stedet for at skubbe ind i spilleren
        if (isMelee && distance <= PreferredDistance(attackRange)) return false;
        return true;
    }

    /// <summary>Ventetid før næste angreb: cooldown + jitter, men aldrig under MinAttackInterval.</summary>
    public static float AttackInterval(float cooldown, float jitter) =>
        Math.Max(MinAttackInterval, cooldown + jitter);

    private static readonly (int dx, int dy)[] Neighbours = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    /// <summary>
    /// BFS fra start-cellen til den nærmeste gangbare celle med udsyn til spilleren.
    /// Returnerer første skridt på den korteste vej dertil (start selv, hvis den har udsyn), eller null hvis ingen kan nås.
    /// hasLos kaldes kun på besøgte celler, så der laves ikke en linecast for hele gitteret.
    /// </summary>
    // ponytail: 4 naboer giver trappe-bevægelse på skrå. 8 naboer (uden at skære hjørner) hvis det ser for kantet ud
    public static (int x, int y)? NextStepTowardLos(bool[,] walkable, int sx, int sy, Func<int, int, bool> hasLos)
    {
        int w = walkable.GetLength(0), h = walkable.GetLength(1);
        if (sx < 0 || sy < 0 || sx >= w || sy >= h) return null;

        var start = (sx, sy);
        var visited = new bool[w, h];
        var parent = new (int x, int y)[w, h];
        var queue = new System.Collections.Generic.Queue<(int x, int y)>();
        visited[sx, sy] = true; // start tjekkes ikke for walkable, fjendens midte kan stå tæt op ad en væg
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (hasLos(c.x, c.y))
            {
                // gå baglæns til cellen lige efter start
                while (c != start && parent[c.x, c.y] != start) c = parent[c.x, c.y];
                return c;
            }
            foreach (var (dx, dy) in Neighbours)
            {
                int nx = c.x + dx, ny = c.y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h || visited[nx, ny] || !walkable[nx, ny]) continue;
                visited[nx, ny] = true;
                parent[nx, ny] = c;
                queue.Enqueue((nx, ny));
            }
        }
        return null;
    }
}
