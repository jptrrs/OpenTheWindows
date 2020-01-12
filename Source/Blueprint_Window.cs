using RimWorld;
using Verse;

namespace OpenTheWindows
{
	public class Blueprint_Window : Blueprint_Build
	{
		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			Log.Message("Blueprint!");
			base.Rotation = WindowUtility.WindowRotationAt(base.Position, base.Map);
		}

		//public override void Draw()
		//{
		//	Log.Message("Blueprint!");
		//	base.Rotation = WindowUtility.WindowRotationAt(base.Position, base.Map);
		//	base.Draw();
		//}

	}
}