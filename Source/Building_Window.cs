using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    using static WindowUtility;
    public class Building_Window : Building_Door
    {
        #region variables
        public LinkDirections Facing;

        public bool
            isFacingSet = false,
            open = true,
            venting = false,
            needsUpdate = true,
            autoVent = false,
            alarmReact = OpenTheWindowsSettings.AlarmReactDefault,
            emergencyShut = false;

        public IntVec3 start, end;

        public List<IntVec3>
            view = new List<IntVec3>(),
            illuminated = new List<IntVec3>();
        private float
            closedVentFactor = 0.5f,
            leakVentFactor = 0.1f;

        private bool
            leaks = false,
            recentlyOperated = false;

        private CompWindow
            mainComp,
            ventComp;

        private int
            nextToleranceCheckTick,
            toleranceCheckInterval = 1000,
            intervalMultiplierAfterAttempts = 4;

        private List<ScanLine> scanLines = new List<ScanLine>();
        #endregion

        #region properties
        public Room AttachedRoom
        {
            get
            {
                if (isFacingSet & Inside != IntVec3.Zero)
                {
                    return Inside.GetRoom(Map);
                }
                return null;
            }
        }
        public List<IntVec3> EffectArea => isFacingSet ? illuminated.Concat(view).ToList() : new List<IntVec3>();
        public override Graphic Graphic
        {
            get
            {
                return mainComp.CurrentGraphic;
            }
        }
        public bool large => Size > 1;
        public int reach => Size / 2 + 1;
        private IntVec3 Inside
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

        private int MaxNeighborDistance => 2 * (reach + WindowUtility.deep + 1);
        private IntVec3 Outside
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

        private int Size => Math.Max(def.size.x, def.size.z);
        private IntRange TargetTemp => OpenTheWindowsSettings.ComfortTemp;
        private float VentRate => Size * 14f;
        #endregion

        public Building_Window()
        {
            MapUpdateWatcher.MapUpdate += MapUpdateHandler;
            if (HarmonyPatcher.BetterPawnControl) AlertManager_LoadState.Alarm += EmergencyShutOff; // register with an event, handler must match template signature
        }

        #region vanilla overrides
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
            Scribe_Values.Look<bool>(ref autoVent, "autoVent", false, false);
            Scribe_Values.Look<LinkDirections>(ref Facing, "Facing", LinkDirections.None, false);
            if (HarmonyPatcher.BetterPawnControl)
            {
                Scribe_Values.Look<bool>(ref emergencyShut, "emergencyShut", false, false);
                Scribe_Values.Look<bool>(ref alarmReact, "alarmReact", OpenTheWindowsSettings.AlarmReactDefault, false);
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                if (!GizmoInhibitor(gizmo)) yield return gizmo;
            }
            yield return new Command_Toggle
            {
                icon = ContentFinder<Texture2D>.Get("UI/AutoVentIcon_" + ventComp.Props.signal, true),
                defaultLabel = "CommandAutoVentilation".Translate(),
                defaultDesc = "CommandAutoVentilationDesc".Translate(),
                isActive = () => autoVent,
                disabled = alarmReact && AlertManagerProxy.onAlert,
                disabledReason = "DisabledByEmergency".Translate(),
                toggleAction = delegate ()
                {
                    autoVent = !autoVent;
                }
            };
            if (HarmonyPatcher.BetterPawnControl)
            {
                yield return new Command_Toggle
                {
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/EmergencyOn", true),
                    iconDrawScale = 0.75f,
                    defaultLabel = "CommandCloseOnEmergency".Translate(),
                    defaultDesc = "CommandCloseOnEmergencyDesc".Translate() + mainComp.ManualNote,
                    isActive = () => alarmReact,
                    toggleAction = delegate ()
                    {
                        alarmReact = !alarmReact;
                        if (AlertManagerProxy.onAlert)
                        {
                            mainComp.FlickFor(!open);
                        }
                        if (!alarmReact && emergencyShut)
                        {
                            emergencyShut = false;
                        }
                    }
                };
            }
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
                    string closeNote = emergencyShut ? "EmergencyClosed".Translate() : "ClosedWindow".Translate();
                    stringBuilder.Append(closeNote);
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
                    if (!AttachedRoom.UsesOutdoorTemperature)
                    {
                        autoVentStatus = recentlyOperated ? "WaitingTemperatureNormalization".Translate() : "TrackingTemperature".Translate();
                    }
                    else autoVentStatus = "CantTrackTemperature".Translate();
                    stringBuilder.Append(autoVentStatus);
                }
                if (stringBuilder.Length > 0) stringBuilder.AppendLine();
                stringBuilder.Append($"Facing {FacingCardinal()}.");
            }
            else
            {
                if (stringBuilder.Length > 0) stringBuilder.AppendLine();
                stringBuilder.Append("cantDetermineSides".Translate());
            }
            return stringBuilder.ToString();
        }

        public override void ReceiveCompSignal(string signal)
        {
            switch (signal)
            {
                case "lightOn":
                    open = true;
                    def.blockLight = false;
                    needsUpdate = true;
                    break;
                case "lightOff":
                    def.blockLight = true;
                    open = false;
                    needsUpdate = true;
                    break;
                case "airOn":
                    venting = true;
                    recentlyOperated = true;
                    break;
                case "airOff":
                    venting = false;
                    recentlyOperated = true;
                    break;
                case "bothOn":
                    open = true;
                    def.blockLight = false;
                    needsUpdate = true;
                    venting = true;
                    recentlyOperated = true;
                    break;
                case "bothOff":
                    def.blockLight = true;
                    open = false;
                    needsUpdate = true;
                    venting = false;
                    recentlyOperated = true;
                    break;
                default:
                    break;
            }
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            //light & ventilation comps:
            mainComp = GetComps<CompWindow>().FirstOrDefault();
            if (mainComp.Props.signal == "both")
            {
                if (!respawningAfterLoad) venting = true;
                leaks = true;
                ventComp = mainComp;
            }
            else ventComp = GetComps<CompWindow>().Where(x => x.Props.signal == "air").FirstOrDefault();

            //basic functionality
            map.GetComponent<MapComp_Windows>().RegisterWindow(this);
            WindowUtility.FindEnds(this);
            SetScanLines();

            //wall linking
            if (OpenTheWindowsSettings.LinkWindows)
            {
                map.linkGrid.Notify_LinkerCreatedOrDestroyed(this);
                map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
            }
        }
        public override void Tick()
        {
            base.Tick();
            if (needsUpdate)
            {
                if (!isFacingSet) CheckFacing();
                CastLight();
                Map.GetComponent<MapComp_Windows>().IncludeTileRange(illuminated);
                needsUpdate = false;
            }
            if (Find.TickManager.TicksGame % 250 == 0)
            {
                TickRare();
            }
        }
        public override void TickRare()
        {
            if (Spawned)
            {
                if (venting)
                {
                    float vent = open ? VentRate : VentRate * closedVentFactor;
                    GenTemperature.EqualizeTemperaturesThroughBuilding(this, vent, true);
                }
                else if (leaks && !open)
                {
                    float vent = VentRate * leakVentFactor;
                    GenTemperature.EqualizeTemperaturesThroughBuilding(this, vent, true);
                }
                if (autoVent && !emergencyShut && isFacingSet && AttachedRoom != null && !AttachedRoom.UsesOutdoorTemperature)
                {
                    AutoVentControl();
                }
            }
            base.TickRare();
        }
        #endregion

        #region adapting as door
        public new bool openInt = false;
        public override void Draw()
        {
            if (Size == 1)
            {
                Rot4 current = Rotation;
                base.Draw();
                if (current != Rotation)
                {
                    SetScanLines();
                    isFacingSet = false;
                    needsUpdate = true;
                }
            }
            else Comps_PostDraw();
        }
        public override bool PawnCanOpen(Pawn p)
        {
            return false;
        }
        #endregion

        #region custom
        public void CastLight()
        {
            DarkenCellsCarefully();
            view.Clear();
            if (open && isFacingSet)
            {
                foreach (var line in scanLines)
                {
                    line.CastLight();
                }
            }
        }

        public void CheckFacing()
        {
            bool before = isFacingSet;
            LinkDirections fwdDir = Rotation.IsHorizontal ? LinkDirections.Right : LinkDirections.Up;
            LinkDirections bwdDir = Rotation.IsHorizontal ? LinkDirections.Left : LinkDirections.Down;
            int fwdVotes = 0;
            int bwdVotes = 0;
            foreach (var line in scanLines.Where(x => x.facingSet))
            {
                if (line.facing == fwdDir) fwdVotes++;
                if (line.facing == bwdDir) bwdVotes++;
            }
            if (fwdVotes != bwdVotes)
            {
                Facing = fwdVotes > bwdVotes ? fwdDir : bwdDir;
                isFacingSet = true;
            }
            else
            {
                isFacingSet = false;
                foreach (var line in scanLines) line.Reset();
            }
            if (!needsUpdate && isFacingSet != before) needsUpdate = true;
        }

        public void DarkenCellsCarefully()
        {
            if (!illuminated.NullOrEmpty())
            {
                var neighbors = Neighbors()?.Where(x => x != this && x.open && !x.illuminated.EnumerableNullOrEmpty());
                IEnumerable<IntVec3> competing = neighbors.EnumerableNullOrEmpty() ? null : neighbors.Select(x => x.illuminated).Aggregate((l, r) => l.Union(r).ToList());
                List<IntVec3> affected = competing.EnumerableNullOrEmpty() ? illuminated : illuminated.Except(competing).ToList();
                Map.GetComponent<MapComp_Windows>().ExcludeTileRange(affected);
                //Log.Message($"{this} darkened {affected.Count()} cells from {illuminated.Count()} that were illuminated.\n" +
                //    $"There were {competing.EnumerableCount()} competing cells from {neighbors.EnumerableCount()} neighbors.");
                illuminated.Clear();
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

        public List<Building_Window> Neighbors()
        {
            if (!isFacingSet) return null;
            var region = Map.regionGrid.GetValidRegionAt(Inside);
            if (region == null) return null;
            List<Building_Window> result = new List<Building_Window>();
            FindAffectedWindows(result, region, this.GetRegion());
            //if (result.Count() > 3) result.RemoveAll(x => x.Position.DistanceToSquared(Position) > MaxNeighborDistance); //Unreliable, for some reason. But not tremendously needed.
            return result;
        }

        private void AutoVentControl() 
        {
            float insideTemp = AttachedRoom.Temperature;
            int ticksGame = Find.TickManager.TicksGame;
            if (!TargetTemp.Includes(insideTemp) && !recentlyOperated)
            {
                if (nextToleranceCheckTick == 0 || ticksGame >= nextToleranceCheckTick)
                {
                    float outsideTemp = GenTemperature.GetTemperatureForCell(Outside, Map);
                    bool
                        colderOutside = insideTemp > outsideTemp,
                        colderInside = insideTemp < outsideTemp,
                        tooHotInside = insideTemp > TargetTemp.max,
                        tooColdInside = insideTemp < TargetTemp.min,
                        tooHotOutside = outsideTemp > TargetTemp.max,
                        tooColdOutside = outsideTemp < TargetTemp.min;
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
            else if (ticksGame >= nextToleranceCheckTick + (toleranceCheckInterval * intervalMultiplierAfterAttempts) || TargetTemp.Includes(insideTemp))
            {
                if (recentlyOperated) recentlyOperated = false;
            }
        }

        private bool GizmoInhibitor(Gizmo gizmo)
        {
            return
                (gizmo is Command_Toggle toggle && toggle.icon == TexCommand.HoldOpen) ||
                (HarmonyPatcher.LocksType != null && gizmo.GetType() == HarmonyPatcher.LocksType);
        }

        private void MapUpdateHandler(object sender, MapUpdateWatcher.MapUpdateInfo info)
        {
            var cell = info.center;
            bool removed = info.removed;
            bool roof = sender is RoofGrid;
            if (isFacingSet && roof && cell.IsInterior(this) && !GenAdj.CellsAdjacentCardinal(this).Contains(cell)) return;
            bool unsureFace = false;
            for (int i = 0; i < scanLines.Count(); i++)
            {
                if (scanLines[i].Affected(cell))
                {
                    IntVec3 motivator = roof ? IntVec3.Zero : cell;
                    var line = scanLines[i];
                    bool before = line.facingSet;
                    line.FindObstruction(motivator, removed, info.map);
                    unsureFace |= line.facingSet != before;
                    needsUpdate = true;
                }
            }
            if (unsureFace) isFacingSet = false;
        }

        private void SetScanLines()
        {
            scanLines.Clear();
            foreach (IntVec3 c in GenAdj.OccupiedRect(Position, Rotation, def.size))
            {
                scanLines.Add(new ScanLine(this, c));
            }
        }
        #endregion

        #region adapting to Better Pawn Control
        public void EmergencyShutOff(object sender, bool active) // event handler
        {
            bool mustclose = active && alarmReact && open;
            bool mustopen = !active && emergencyShut;
            if (mustclose || mustopen)
            {
                mainComp.AutoFlickRequest();
                ventComp.AutoFlickRequest();
                emergencyShut = !emergencyShut;
            }
        }
        #endregion

        #region nested
        private class ScanLine
        {
            public LinkDirections facing;
            public bool
                horizontal = false,
                bleeds = false,
                facingSet = false;
            int[] clearCells = new int[] { };
            Map map;
            int maxreach;
            Building_Window parent;
            IntVec3 
                position,
                bleedDirection,
                toRight,
                toLeft;
            List<IntVec3> scanLine = new List<IntVec3>();

            public ScanLine(Building_Window window, IntVec3 pos)
            {
                position = pos;
                parent = window;
                map = parent.Map;
                maxreach = parent.reach + WindowUtility.deep;
                horizontal = window.Rotation.IsHorizontal;
                bleeds = ShouldBleed();
                SetScanLine();
                FindObstruction(IntVec3.Zero);
            }

            List<IntVec3> clearLine => scanLine.FindAll(x => clearCells.Contains(scanLine.IndexOf(x)));
            ScanLine siblingRight => parent.scanLines.Find(x => x.position == toRight);
            ScanLine siblingLeft => parent.scanLines.Find(x => x.position == toLeft);
            private bool siblingClear => (siblingLeft != null && leftIsSet) || (siblingRight != null && rightIsSet);
            bool rightIsSet => siblingRight.facingSet;
            bool leftIsSet => siblingLeft.facingSet;

            public bool Affected(IntVec3 cell)
            {
                return scanLine.Contains(cell);
            }

            public void CastLight()
            {
                if (!facingSet) return;
                foreach (IntVec3 cell in clearLine)
                {
                    var relevant = cell.IsInterior(position, parent.Facing) ? parent.illuminated : parent.view;
                    relevant.AddDistinct(cell);
                    if (bleeds) AddBleed(cell, relevant);
                }
                //Log.Message($"DEBUG @{position} CastLight: {clearCells.Count()} clearCells / {count} interior.");
            }

            public void FindObstruction(IntVec3 motivator, bool removed = false, Map updatedMap = null)
            {
                if (updatedMap != null) map = updatedMap;
                var actualClearCells = Unobstructed(motivator, removed);
                clearCells = scanLine.Where(x => actualClearCells.Contains(x)).Select(x => scanLine.IndexOf(x)).ToArray();
                //Log.Message($"DEBUG @{position} FindObstruction: {clearCells.Count()} clearCells from {replaced.Count()} before.");
            }

            public void LineAdjust(int[] old, int[] @new)
            {
                var delta = old.Except(@new);
            }

            public void SetScanLine()
            {
                int cellx = position.x;
                int cellz = position.z;
                scanLine.Clear();
                for (int i = 1; i <= +maxreach; i++)
                {
                    if (horizontal)
                    {
                        IntVec3 targetA = new IntVec3(cellx + i, 0, cellz);
                        if (targetA.InBounds(map)) scanLine.Add(targetA);
                        IntVec3 targetB = new IntVec3(Math.Max(0, cellx - i), 0, cellz);
                        if (targetB.InBounds(map)) scanLine.Add(targetB);
                    }
                    else
                    {
                        IntVec3 targetA = new IntVec3(cellx, 0, cellz + i);
                        if (targetA.InBounds(map)) scanLine.Add(targetA); ;
                        IntVec3 targetB = new IntVec3(cellx, 0, Math.Max(0, cellz - i));
                        if (targetB.InBounds(map)) scanLine.Add(targetB);
                    }
                }
            }

            public List<IntVec3> Unobstructed(IntVec3 motivator, bool removed = false)
            {
                //1. What is the test is?
                bool lazy = motivator == IntVec3.Zero;
                cellTest motivated = (target, inside) => target == motivator ? removed : IsClear(target, inside);
                cellTest clear = lazy ? IsClear : motivated;

                //2. Determine facing
                IntVec3 dummy;
                bool overhangFwd = !ClearForward(position, horizontal, clear, false, 1, out dummy); //sets facing Down/Left
                bool overhangBwd = !ClearBackward(position, horizontal, clear, false, 1, out dummy); //sets facing Up/Right
                facingSet = SetFacing(overhangFwd, overhangBwd);
                if (!facingSet) return new List<IntVec3>(); //escape if unable to determine sides
                bool southward = facing == LinkDirections.Down || facing == LinkDirections.Left;

                //3. Determine clearence and max reach on each side
                Dictionary<IntVec3, int> cleared = new Dictionary<IntVec3, int>();
                int reachFwd = 0;
                int reachBwd = 0;
                //forward (walks North/East)
                for (int i = 1; i <= maxreach; i++)
                {
                    IntVec3 clearedFwd;
                    if (ClearForward(position, horizontal, clear, southward, i, out clearedFwd))
                    {
                        cleared.Add(clearedFwd,i);
                        reachFwd++;
                    }
                    else break;
                }
                //backward (walks South/West)
                for (int i = 1; i <= maxreach; i++)
                {
                    IntVec3 clearedBwd;
                    if (ClearBackward(position, horizontal, clear, !southward, i, out clearedBwd))
                    {
                        cleared.Add(clearedBwd, i);
                        reachBwd++;
                    }
                    else break;
                }

                //5. Apply clearance.
                int obstructed = southward ? reachBwd : reachFwd;
                cleared.RemoveAll(x => x.Key.IsInterior(position, facing) && x.Value > obstructed);
                return cleared.Keys.ToList();
            }

            private void AddBleed(IntVec3 c, List<IntVec3> list)
            {
                if (parent.large)
                {
                    if (!siblingClear) return;
                    var b = c + bleedDirection;
                    if (b.Walkable(map)) list.AddDistinct(b);
                }
                else
                {
                    var left = horizontal ? IntVec3.South : IntVec3.West;
                    var right = horizontal ? IntVec3.North : IntVec3.East;
                    var leftEdge = c + left;
                    var rightEdge = c + right;
                    if (leftEdge.Walkable(map)) list.AddDistinct(leftEdge);
                    if (rightEdge.Walkable(map)) list.AddDistinct(rightEdge);
                }
            }

            public void Reset()
            {
                clearCells = new int[] { };
                facingSet = false;
            }

            private bool IsClear(IntVec3 c, bool inside)
            {
                bool result = Affected(c) && c.Walkable(map) && (inside || !map.roofGrid.Roofed(c) || HarmonyPatcher.TransparentRoofsList.Contains(map.roofGrid.RoofAt(c)));
                return result;
            }

            private bool SetFacing(bool overhangFwd, bool overhangBwd)
            {
                if (overhangFwd != overhangBwd)
                {
                    if (overhangFwd) facing = horizontal ? LinkDirections.Left : LinkDirections.Down;
                    if (overhangBwd) facing = horizontal ? LinkDirections.Right : LinkDirections.Up;
                    return true;
                }
                return false;
            }

            private bool ShouldBleed()
            {
                if (!parent.large) return true;
                var startDir = horizontal ? IntVec3.North : IntVec3.East;
                var endDir = horizontal ? IntVec3.South : IntVec3.West;
                toRight = position + startDir;
                toLeft = position + endDir;
                bool isStart = toRight == parent.start;
                bool isEnd = toLeft == parent.end;
                bleedDirection = isStart ? startDir : endDir;
                return isStart || isEnd;
            }
        }
        #endregion
    }
}