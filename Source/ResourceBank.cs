using Verse;
using UnityEngine;

namespace OpenTheWindows
{
	[StaticConstructorOnStartup]
	internal static class ResourceBank
	{
		public static readonly Texture2D overlayIcon = ContentFinder<Texture2D>.Get("NaturalLightMap", true);
	}
}