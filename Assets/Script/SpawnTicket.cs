public static class SpawnTicket
{
    // 传送前写入，场景加载后一次性读取并清空
    public static string NextAnchorId;

    public static string Consume()
    {
        var v = NextAnchorId;
        NextAnchorId = null;
        return v;
    }
}
