public static class SpawnTicket
{
    // ����ǰд�룬�������غ�һ���Զ�ȡ�����
    public static string NextAnchorId;

    public static string Consume()
    {
        var v = NextAnchorId;
        NextAnchorId = null;
        return v;
    }
}
