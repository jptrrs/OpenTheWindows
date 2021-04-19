using Verse;

namespace OpenTheWindows
{
    static class Extensions
    {
        public static bool Includes(this IntRange range, float num)
        {
            return num <= range.max && num >= range.max;
        }
    }
}
