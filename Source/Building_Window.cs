using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OpenTheWindows
{
    public class Building_Window : Building
    {
        public static IntVec3 end;
        public static IntVec3 start;
        public LinkDirections Facing;   
        public List<IntVec3> illuminated;
        public bool isFacingSet = false;
        private CompWindow compLight;
        private CompWindow compAir;
        private bool venting = false;
        public override Graphic Graphic
        {
            get
            {
                return compLight.CurrentGraphic;
            }
        }

        public bool Open => this.TryGetComp<CompWindow>().SwitchIsOn;

        //private static int AlignQualityAgainst(IntVec3 c, Map map)
        //{
        //    if (!c.InBounds(map))
        //    {
        //        return 0;
        //    }
        //    if (!c.Walkable(map))
        //    {
        //        return 9;
        //    }
        //    List<Thing> thingList = c.GetThingList(map);
        //    for (int i = 0; i < thingList.Count; i++)
        //    {
        //        Thing thing = thingList[i];
        //        if (typeof(Building_Window).IsAssignableFrom(thing.def.thingClass))
        //        {
        //            return 1;
        //        }
        //        Thing thing2 = thing as Blueprint;
        //        if (thing2 != null)
        //        {
        //            if (thing2.def.entityDefToBuild.passability == Traversability.Impassable)
        //            {
        //                return 9;
        //            }
        //            if (typeof(Building_Window).IsAssignableFrom(thing.def.thingClass))
        //            {
        //                return 1;
        //            }
        //        }
        //    }
        //    return 0;
        //}

        //public static Rot4 WindowRotationAt(IntVec3 loc, Map map)
        //{
        //    int num = 0;
        //    int num2 = 0;
        //    num += AlignQualityAgainst(loc + IntVec3.East, map);
        //    num += AlignQualityAgainst(loc + IntVec3.West, map);
        //    num2 += AlignQualityAgainst(loc + IntVec3.North, map);
        //    num2 += AlignQualityAgainst(loc + IntVec3.South, map);
        //    if (num >= num2)
        //    {
        //        return Rot4.North;
        //    }
        //    return Rot4.East;
        //}

        //public override void Draw()
        //{
        //    base.Rotation = WindowUtility.WindowRotationAt(base.Position, base.Map);
        //float num = Mathf.Clamp01((float)this.visualTicksOpen / (float)this.VisualTicksToOpen);
        //float d = 0.45f * num;
        //for (int i = 0; i < 2; i++)
        //{
        //    Vector3 vector = default(Vector3);
        //    Mesh mesh;
        //    if (i == 0)
        //    {
        //        vector = new Vector3(0f, 0f, -1f);
        //        mesh = MeshPool.plane10;
        //    }
        //    else
        //    {
        //        vector = new Vector3(0f, 0f, 1f);
        //        mesh = MeshPool.plane10Flip;
        //    }
        //    Rot4 rotation = base.Rotation;
        //    rotation.Rotate(RotationDirection.Clockwise);
        //    vector = rotation.AsQuat * vector;
        //    Vector3 vector2 = this.DrawPos;
        //    vector2.y = AltitudeLayer.DoorMoveable.AltitudeFor();
        //    vector2 += vector * d;
        //    Graphics.DrawMesh(mesh, vector2, base.Rotation.AsQuat, this.Graphic.MatAt(base.Rotation, null), 0);
        //}
        //base.Comps_PostDraw();
        //}

        public void CastLight()
        {
            if (Open)
            {
                illuminated = new List<IntVec3>(WindowUtility.CalculateWindowLightCells(def, Position, Rotation, Map).ToList());
            }
            else if (illuminated != null)
            {
                illuminated.Clear();
                isFacingSet = false;
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Map.GetComponent<MapComp_Windows>().DeRegisterWindow(this);
            base.Map.GetComponent<MapComp_Windows>().RegenGrid();
            base.Map.glowGrid.MarkGlowGridDirty(base.Position);
            base.DeSpawn(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            //Scribe_Values.Look<bool>(ref Open, "open", true, false);
            Scribe_Values.Look<bool>(ref isFacingSet, "isFacingSet", true, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (compLight == null) compLight = GetComps<CompWindow>().Where(c => c.Props.signal == "light").FirstOrDefault();
                if (compAir == null) compAir = GetComps<CompWindow>().Where(c => c.Props.signal == "air").FirstOrDefault();
            }
        }

        public Direction8Way FacingCardinal()
        {
            Direction8Way dir = new Direction8Way() { };
            switch (Facing)
            {
                case LinkDirections.Up:
                    dir = Direction8Way.North;
                    break;

                case LinkDirections.Right:
                    dir = Direction8Way.East;
                    break;

                case LinkDirections.Down:
                    dir = Direction8Way.South;
                    break;

                case LinkDirections.Left:
                    dir = Direction8Way.West;
                    break;

                case LinkDirections.None:
                    break;
            }
            return dir;
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            if (!FlickUtility.WantsToBeOn(this))
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.AppendLine();
                }
                stringBuilder.Append("ClosedWindow".Translate());
            }
            if (isFacingSet)
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.AppendLine();
                }
                stringBuilder.Append("Facing " + FacingCardinal() + ".");
            }
            return stringBuilder.ToString();
        }

        public override void SpawnSetup(Map map, bool rsal)
        {
            compLight = GetComps<CompWindow>().Where(c => c.Props.signal == "light").FirstOrDefault();
            compAir = GetComps<CompWindow>().Where(c => c.Props.signal == "air").FirstOrDefault();
            base.SpawnSetup(map, rsal);
            map.GetComponent<MapComp_Windows>().RegisterWindow(this);
            map.GetComponent<MapComp_Windows>().RegenGrid();
            map.glowGrid.MarkGlowGridDirty(Position);
            WindowUtility.FindWindowExternalFacing(this);
        }
        public override void TickRare()
        {
            Map.GetComponent<MapComp_Windows>().RegenGrid();
            if (venting) GenTemperature.EqualizeTemperaturesThroughBuilding(this, 14f, true);
        }

        protected override void ReceiveCompSignal(string signal)
        {
            if (signal == "lightOff")
            {
                def.blockLight = true;
                //def.fillPercent = 1f; //TODO this changes cover  for every one!
            }
            if (signal == "lightOn") //
            {
                def.blockLight = false;
                //def.fillPercent = defaultFillPercent;
            }
            if (signal == "lightOff" || signal == "lightOn" || signal == "ScheduledOn" || signal == "ScheduledOff")
            {
                Map.GetComponent<MapComp_Windows>().RegenGrid();
                Map.glowGrid.MarkGlowGridDirty(base.Position);
            }
            if (signal == "airOn") venting = true;
            if (signal == "airOff") venting = false;
        }
    }
}