using RimWorld;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using Verse;

namespace OpenTheWindows
{
    public class Building_Window : Building
    {
        public IntVec3 end;
        public IntVec3 start;
        public LinkDirections Facing;   
        public List<IntVec3> illuminated = new List<IntVec3>();
        public bool isFacingSet = false;
        private CompWindow mainComp;
        public bool open = true;
        public bool venting = false;
        public bool leaks = false;
        public int size => Math.Max(def.size.x, def.size.z);
        public float ventRate => size * 14f;
        public float closedVentFactor = 0.5f;
        public float leakVentFactor = 0.1f;
        public int adjacentRoofCount;
        public bool updateRequest = false;

        public override Graphic Graphic
        {
            get
            {
                return mainComp.CurrentGraphic;
            }
        }

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
            if (open)
            {
                illuminated = WindowUtility.CalculateWindowLightCells(this).ToList();
            }
            else if (illuminated != null)
            {
                illuminated.Clear();
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mainComp = GetComps<CompWindow>().FirstOrDefault();
            if (mainComp.Props.signal == "both")
            {
                if (!respawningAfterLoad) venting = true;
                leaks = true;
            }
            map.GetComponent<MapComp_Windows>().RegisterWindow(this);
            WindowUtility.FindEnds(this);
            if (!isFacingSet) WindowUtility.FindWindowExternalFacing(this);
            CastLight();
            Map.GetComponent<MapComp_Windows>().RegenGrid();
            Map.glowGrid.MarkGlowGridDirty(Position);
            //just link it!
            if (OpenTheWindowsSettings.LinkWindows)
            {
                map.linkGrid.Notify_LinkerCreatedOrDestroyed(this);
                map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map.GetComponent<MapComp_Windows>().DeRegisterWindow(this);
            Map.GetComponent<MapComp_Windows>().RegenGrid();
            Map.glowGrid.MarkGlowGridDirty(Position);
            //just link it!
            if (OpenTheWindowsSettings.LinkWindows)
            {
                Map.thingGrid.Deregister(this, false);
                Map.linkGrid.Notify_LinkerCreatedOrDestroyed(this);
                Map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
            }
            //
            base.DeSpawn(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref open, "open", true, false);
            Scribe_Values.Look<bool>(ref venting, "venting", false, false);
            Scribe_Values.Look<bool>(ref isFacingSet, "isFacingSet", true, false);
            Scribe_Values.Look<LinkDirections>(ref Facing, "Facing", LinkDirections.None, false);
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
            if (isFacingSet)
            {
                if (!open)
                {
                    if (stringBuilder.Length > 0) stringBuilder.AppendLine();
                    stringBuilder.Append("ClosedWindow".Translate());
                    if (venting) stringBuilder.Append("butVenting".Translate());
                }
                else if (!venting)
                {
                    if (stringBuilder.Length > 0) stringBuilder.AppendLine();
                    stringBuilder.Append("notVenting".Translate());
                }
                if (stringBuilder.Length > 0) stringBuilder.AppendLine();
                stringBuilder.Append("Facing " + FacingCardinal() + ".");
            }
            else
            {
                if (stringBuilder.Length > 0) stringBuilder.AppendLine();
                stringBuilder.Append("cantDetermineSides".Translate());
            }
            return stringBuilder.ToString();
        }

        public override void TickRare()
        {
            if (venting)
            {
                float vent = open ? ventRate : ventRate * closedVentFactor;
                GenTemperature.EqualizeTemperaturesThroughBuilding(this, vent, true);
            } 
            else if (leaks && !open)
            {
                float vent = ventRate * leakVentFactor;
                GenTemperature.EqualizeTemperaturesThroughBuilding(this, vent, true);
            }
        }

        public bool NeedExternalFacingUpdate()
        {
            if (isFacingSet)
            {
                int previousCount = adjacentRoofCount;
                int count = new int();
                foreach (IntVec3 c in GenAdj.CellsAdjacentCardinal(this))
                {
                    if (Map.roofGrid.Roofed(c) && c.Walkable(Map)) count++;
                }
                adjacentRoofCount = count;
                if (count != previousCount) return true;
                else return false;
            }
            return true;
        }

        public bool NeedLightUpdate()
        {
            if (open && isFacingSet) 
            {
                int areacheck = 0;
                if (illuminated != null) areacheck = illuminated.Count();
                CastLight();
                if (illuminated.Count() != areacheck)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        protected override void ReceiveCompSignal(string signal)
        {
            if (signal == "lightOff" || signal == "bothOff")
            {
                def.blockLight = true;
                open = false;
            }
            if (signal == "lightOn" || signal == "bothOn")
            {
                open = true;
                def.blockLight = false;
            }
            if (signal == "lightOff" || signal == "lightOn" || signal == "bothOff" || signal == "bothOn")
            {
                if (!isFacingSet) WindowUtility.FindWindowExternalFacing(this);
                CastLight();
                Map.GetComponent<MapComp_Windows>().RegenGrid();
                Map.glowGrid.MarkGlowGridDirty(Position);
            }
            if (signal == "airOn" || signal == "bothOn") venting = true;
            if (signal == "airOff" || signal == "bothOff") venting = false;
        }
    }
}