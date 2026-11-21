/// Where swarm monsters spawn (GAME_DESIGN.md section 4). An empty side list falls back to all four sides.
public enum SwarmSpawn
{
    /// A random point on any map edge.
    Edge,
    /// Only sides without a convoy this loop.
    FreeSides,
    /// Only the convoy sides.
    ConvoySides
}
