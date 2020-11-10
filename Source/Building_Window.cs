using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OpenTheWindows
{
    public class Building_Window : Building
    {
        public IntVec3 start, end;
        public LinkDirections Facing;
        public List<IntVec3> illuminated = new List<IntVec3>();
        private CompWindow mainComp, ventComp;
        public bool 
            isFacingSet = false,
            open = true,
            venting = false,
            leaks = false,
            updateRequest = false;
        public int size => Math.Max(def.size.x, def.size.z);
        public float ventRate => size * 14f;
        public float 
            closedVentFactor = 0.5f,
            leakVentFactor = 0.1f;
        public int 
            adjacentRoofCount,
            nextToleranceCheckTick,
            ToleranceCheckInterval = 1000;
        public FloatRange targetTemp = new FloatRange(ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin), ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax));

        public IntVec3 inside
        {
            get
            {
                switch (Facing)
                {
                    case LinkDirections.Up:
                        return Position + IntVec3.North;

                    case LinkDirections.Right:
                        return Position + IntVec3.East;

                    case LinkDirections.Down:
                        return Position + IntVec3.South;

                    case LinkDirections.Left:
                        return Position + IntVec3.West;

                    case LinkDirections.None:
                        return IntVec3.Zero;
                }
                return IntVec3.Zero;
            }
        }

        public IntVec3 outside
        {
            get
            {
                switch (Facing)
                {
                    case LinkDirections.Up:
                        return Position + IntVec3.South;

                    case LinkDirections.Right:
                        return Position + IntVec3.West;

                    case LinkDirections.Down:
                        return Position + IntVec3.North;

                    case LinkDirections.Left:
                        return Position + IntVec3.East;

                    case LinkDirections.None:
                        return IntVec3.Zero;
                }
                return IntVec3.Zero;
            }
        }

        public override Graphic Graphic
        {
            get
            {
                return mainComp.CurrentGraphic;
            }
        }

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
                ventComp = mainComp;
            }
            else ventComp = GetComps<CompWindow>().Where(x => x.Props.signal == "air").FirstOrDefault();
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
            //test
            if (isFacingSet && inside.GetRoomGroup(Map) != null)
            {
                float insideTemp = GenTemperature.GetTemperatureForCell(inside, Map);
                if (!targetTemp.Includes(insideTemp))
                {
                    int ticksGame = Find.TickManager.TicksGame;
                    if (nextToleranceCheckTick == 0 || ticksGame >= nextToleranceCheckTick)
                    {
                        float outsideTemp = GenTemperature.GetTemperatureForCell(outside, Map);
                        bool doFlick = false;
                        if (!venting) //open if...
                        {
                            doFlick =   /*...too hot inside*/ (insideTemp > targetTemp.max && insideTemp > outsideTemp) ||
                                        /*...too cold inside*/ (insideTemp < targetTemp.min && insideTemp < outsideTemp);
                        } 
                        else //close if...
                        {
                            doFlick =   /*...too hot inside*/ insideTemp > targetTemp.max ||
                                        /*...too cold inside*/ insideTemp < targetTemp.min;
                        }
                        if (doFlick) ventComp.ForceFlick();
                        nextToleranceCheckTick = ticksGame + ToleranceCheckInterval;
                    }
                }
            }
            base.TickRare();
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

        public override void ReceiveCompSignal(string signal)
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

        //public override IEnumerable<Gizmo> GetGizmos()
        //{
        //    foreach (Gizmo gizmo in base.GetGizmos())
        //    {
        //        yield return gizmo;
        //    }
        //    Command_Action unmake = new Command_Action
        //    {
        //        defaultLabel = "AutoTemp",//Props.commandLabelKey.Translate(),
        //        defaultDesc = "test",//Props.commandDescKey.Translate(),
        //                             //icon = LoadedBedding.uiIcon,
        //                             //iconAngle = LoadedBedding.uiIconAngle,
        //                             //iconOffset = LoadedBedding.uiIconOffset,
        //                             //iconDrawScale = GenUI.IconDrawScale(LoadedBedding),
        //        action = delegate ()
        //        {
        //            wantSwitchOn = !wantSwitchOn;
        //            FlickUtility.UpdateFlickDesignation(parent);
        //        }
        //    };
        //    yield return unmake;

        }
    }