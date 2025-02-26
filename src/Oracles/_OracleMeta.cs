namespace VoidTemplate.Oracles;

internal static class _OracleMeta
{
    public static void Hook()
    {
        OracleHooks.Hook();
        SLOracle.Hook();

    }
}
