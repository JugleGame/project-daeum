namespace Daeume.Core
{
    public static class StringTable
    {
        public static string Get(string key)
        {
            return key ?? string.Empty;
        }
    }
}
