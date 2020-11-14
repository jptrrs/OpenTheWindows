using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    public class Building_Window : Building
    {
        public bool autoVent = true;
        public List<IntVec3> effectArea = new List<IntVec3>();
        public LinkDirections Facing;
        public List<IntVec3> illuminated = new List<IntVec3>();
        public bool isFacingSet = false;
        public bool open = true;
        public IntVec3 start, end;
        public bool updateRequest = false;
        public bool venting = false;
        private static float maxNeighborDistance = 20f; //Radius to search for other windows overlapping areas. Should change if we're using windows that reach more than 6 cells deep.
        private int adjacentRoofCount;
        private float closedVentFactor = 0.5f;
        private int intervalMultiplierAfterAttempts = 4;
        private bool leaks = false;
        private float leakVentFactor = 0.1f;
        private CompWindow mainComp, ventComp;
        private int nextToleranceCheckTick;
        private int reach;
        private bool recentlyOperated = false;
        private FloatRange targetTemp = new FloatRange(ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin), ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax));
        private int toleranceCheckInterval = 1000;

        public Room attachedRoom
        {
            get
            {
                if (isFacingSet & inside != IntVec3.Zero)
                {
                    return inside.GetRoom(Map);
                }
                return null;
            }
        }

        public override Graphic Graphic
        {
            get
            {
                return mainComp.CurrentGraphic;
            }
        }

        public int Reach
        {
            get
            {
                if (reach == 0) reach = WindowUtility.CalculateWindowReach(def.Size);
                return reach;
            }
        }
        private int size => Math.Max(def.size.x, def.size.z);
        private IntVec3 inside
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

        private IntVec3 outside
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

        private float ventRate => size * 14f;
        public void CastLight()
        {
            DarkenCellsCarefully();
            if (open)
            {
                var lightCells = WindowUtility.CalculateWindowLightCells(this);
                effectArea = lightCells.Keys.ToList();
                foreach (IntVec3 c in effectArea)
                {
                    bool interior = false;
                    switch (Facing)
                    {
                        case LinkDirections.Up:
                            if (c.z < Position.z) interior = true;
                            break;
                        case LinkDirections.Right:
                            if (c.x < Position.x) interior = true;
                            break;

                        case LinkDirections.Down:
                            if (c.z > Position.z) interior = true;
                            break;

                        case LinkDirections.Left:
                            if (c.x > Position.x) interior = true;
                            break;

                        case LinkDirections.None:
                            break;
                    }
                    if (interior)
                    {
                        illuminated.Add(c);
                        Map.GetComponent<MapComp_Windows>().IncludeTile(c, LightLevelFallOff(lightCells[c]));
                    }
                }
            }
            else
            {
                effectArea.Clear();
            }
        }

        public void DarkenCellsCarefully()
        {
            if (!illuminated.EnumerableNullOrEmpty())
            {
                IEnumerable<Building_Window> neighbors = GenRadial.RadialDistinctThingsAround(Position, Map, maxNeighborDistance, false).Where(x => x is Building_Window && x != this).Cast<Building_Window>().Where(x => x.open && !x.illuminated.EnumerableNullOrEmpty());
                IEnumerable<IntVec3> overlap = neighbors.EnumerableNullOrEmpty() ? null : neighbors.SelectMany(x => x.illuminated);//   .Select(x => x.illuminated.Keys).Aggregate((l, r) => l.Union(r));
                //IEnumerable<IntVec3> overlap = neighbors.EnumerableNullOrEmpty() ? null : neighbors.SelectMany(x => x.illuminated.Keys);
                List<IntVec3> affected = overlap.EnumerableNullOrEmpty() ? illuminated : illuminated.Intersect(overlap).ToList();
                int count = 0;
                foreach (IntVec3 c in affected)
                {
                    Map.GetComponent<MapComp_Windows>().ExcludeTile(c);
                    count++;
                }
                illuminated.Clear();
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map.GetComponent<MapComp_Windows>().DeRegisterWindow(this);
            DarkenCellsCarefully();
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

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            yield return new Command_Toggle
            {
                icon = ContentFinder<Texture2D>.Get("UI/AutoVentIcon_" + ventComp.Props.signal, true),
                defaultLabel = "AutoVentilation".Translate(),
                defaultDesc = "AutoVentilationDesc".Translate(),
                isActive = (() => autoVent),
                toggleAction = delegate ()
                {
                    autoVent = !autoVent;
                }
            };
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
                if (autoVent)
                {
                    if (stringBuilder.Length > 0) stringBuilder.AppendLine();
                    string autoVentStatus = "";
                    if (!attachedRoom.UsesOutdoorTemperature)
                    {
                        autoVentStatus = recentlyOperated ? "WaitingTemperatureNormalization".Translate() : "TrackingTemperature".Translate();
                    }
                    else autoVentStatus = "CantTrackTemperature".Translate();
                    stringBuilder.Append(autoVentStatus);
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
                if (effectArea != null) areacheck = effectArea.Count();
                CastLight();
                if (effectArea.Count() != areacheck)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        public override void ReceiveCompSignal(string signal)
        {
            bool needsupdate = false;
            if (signal == "lightOff" || signal == "bothOff")
            {
                def.blockLight = true;
                open = false;
                needsupdate = true;
            }
            if (signal == "lightOn" || signal == "bothOn")
            {
                open = true;
                def.blockLight = false;
                needsupdate = true;
            }
            if (needsupdate)
            {
                if (!isFacingSet) WindowUtility.FindWindowExternalFacing(this);
                CastLight();
            }
            if (signal == "airOn" || signal == "bothOn")
            {
                venting = true;
                recentlyOperated = true;
            }
            if (signal == "airOff" || signal == "bothOff")
            {
                venting = false;
                recentlyOperated = true;
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

            //just link it!
            if (OpenTheWindowsSettings.LinkWindows)
            {
                map.linkGrid.Notify_LinkerCreatedOrDestroyed(this);
                map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
            }
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
            if (autoVent && isFacingSet && attachedRoom != null && !attachedRoom.UsesOutdoorTemperature)
            {
                float insideTemp = attachedRoom.Temperature;
                int ticksGame = Find.TickManager.TicksGame;
                if (!targetTemp.Includes(insideTemp) && !recentlyOperated)
                {
                    //Log.Message(this + " found inside temperature is out of range (" + insideTemp.ToStringTemperature());
                    if (nextToleranceCheckTick == 0 || ticksGame >= nextToleranceCheckTick)
                    {
                        float outsideTemp = GenTemperature.GetTemperatureForCell(outside, Map);
                        bool
                            colderOutside = insideTemp > outsideTemp,
                            colderInside = insideTemp < outsideTemp,
                            tooHotInside = insideTemp > targetTemp.max,
                            tooColdInside = insideTemp < targetTemp.min,
                            tooHotOutside = outsideTemp > targetTemp.max,
                            tooColdOutside = outsideTemp < targetTemp.min;
                        bool doFlick = false;
                        if (!venting) //open if...
                        {
                            doFlick = (tooHotInside && colderOutside) || (tooColdInside && colderInside && !tooColdOutside);
                        }
                        else //close if...
                        {
                            doFlick = (tooHotInside && tooHotOutside) || (tooColdInside && tooColdOutside);
                        }
                        if (doFlick)
                        {
                            recentlyOperated = true;
                            ventComp.AutoFlickRequest();
                        }
                        nextToleranceCheckTick = ticksGame + toleranceCheckInterval;
                    }
                }
                else if (ticksGame >= nextToleranceCheckTick + (toleranceCheckInterval * intervalMultiplierAfterAttempts) || targetTemp.Includes(insideTemp))
                {

                    if (recentlyOperated) recentlyOperated = false;
                }
            }
            base.TickRare();
        }

        private float LightLevelFallOff(int step)
        {
            int y = 12 / Reach;
            double result = 1 / Math.Pow(y,step-1);
            Log.Message("DEBUG falloff(" + step + ") for reach " + Reach + " = " + result);
            return (float)result;
        }
    }
}