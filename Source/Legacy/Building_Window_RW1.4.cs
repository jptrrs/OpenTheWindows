using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Verse.Noise;

namespace OpenTheWindows
{
    //Changed in RW 1.5
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

        public IntVec3
            start,
            end;

        public List<int>
            view = new List<int>(),
            illuminated = new List<int>();

        private const int
            tickRareInterval = 250,
            maxTempChecks = 4; // 12,5 seconds each.
        private float
            closedVentFactor = 0.5f,
            leakVentFactor = 0.1f,
            ventRate;

        private bool
            leaks = false,
            badTemperatureOnce = false,
            badTemperatureRecently = false,
            niceOutside = false,
            compsToTick;

        private CompWindow
            mainComp,
            ventComp;

        private List<ScanLine> scanLines = new List<ScanLine>();

        private int
            skippedTempChecks = 0,
            size,
            hashInterval;

        #endregion variables

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

        public string AttachedRoomName => AttachedRoom?.Role?.label ?? "undefined";
        public List<IntVec3> EffectArea => isFacingSet ? illuminated.Concat(view).Select(x => Map.cellIndices.IndexToCell(x)).ToList() : new List<IntVec3>();

        public override Graphic Graphic
        {
            get
            {
                return mainComp.CurrentGraphic;
            }
        }

        public bool Large => Size > 1;
        public int PositionIdx => Map.cellIndices.CellToIndex(Position);
        public int Reach => Size / 2 + 1;

        private int HashInterval
        {
            get
            {
                if (hashInterval == 0) hashInterval = thingIDNumber % 200;
                return hashInterval;
            }
        }

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

        private int Size
        {
            get
            {
                if (size == 0) size = Math.Max(def.size.x, def.size.z);
                return size;
            }
        }

        private IntRange TargetTemp => OpenTheWindowsSettings.ComfortTemp;

        private float VentRate
        {
            get
            {
                if (isFacingSet && ventRate == 0) ventRate = Size * 14f;
                return ventRate;
            }
        }

        #endregion properties

        public Building_Window()
        {
            MapUpdateWatcher.MapUpdate += MapUpdateHandler;
            if (HarmonyPatcher.BetterPawnControl) AlertManager_LoadState.Alarm += EmergencyShutOff; // register with an event, handler must match template signature
        }

        #region vanilla overrides

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            //fetched from Owlchemist's
            //We need to manually clean up the districts and rooms since it's based on vanilla doors which are only designed for 1x1 size
            Map map = Map;
            foreach (var cell in this.OccupiedRect().Cells)
            {
                if (cell != Position && cell.GetEdifice(map) == this)
                {
                    map.regionGrid.allRooms.Remove(cell.GetRoom(map));
                    cell.GetDistrict(map)?.RemoveRegion(cell.GetRegion(map));
                }
            }
            map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();

            //Remove light the window was once casting
            DarkenCellsCarefully();

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
                            mainComp.FlickFor(false);
                            ventComp.FlickFor(false);
                        }
                        if (!alarmReact && emergencyShut)
                        {
                            emergencyShut = false;
                        }
                    }
                };
            }
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Diagnostics",
                    action = delegate ()
                    {
                        Log.Warning(SelfDiagnotics());
                        Log.TryOpenLogWindow();
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Reset",
                    action = delegate ()
                    {
                        SetScanLines();
                        CastLight();
                        needsUpdate = true;
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
                        autoVentStatus = badTemperatureRecently ? "WaitingTemperatureNormalization".Translate() : "TrackingTemperature".Translate();
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
                    break;

                case "airOff":
                    venting = false;
                    break;

                case "bothOn":
                    open = true;
                    def.blockLight = false;
                    needsUpdate = true;
                    venting = true;
                    break;

                case "bothOff":
                    def.blockLight = true;
                    open = false;
                    needsUpdate = true;
                    venting = false;
                    break;

                default:
                    break;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            //light & ventilation comps:
            var listOfComps = GetComps<CompWindow>();
            if (listOfComps != null)
            {
                foreach (var comp in listOfComps)
                {
                    //Assign the main comp, which is always the first
                    if (mainComp == null)
                    {
                        mainComp = comp;
                        if (mainComp.Props.signal == CompProperties_Window.Signal.both)
                        {
                            if (!respawningAfterLoad) venting = true;
                            leaks = true;
                            ventComp = mainComp;
                        }
                    }
                    //Check for the vent comp
                    if (comp.Props.signal == CompProperties_Window.Signal.air) ventComp = comp;
                }
            }

            //basic functionality
            map.GetComponent<MapComp_Windows>().RegisterWindow(this);
            WindowUtility.FindEnds(this);
            SetScanLines();

            //wall linking
            //if (OpenTheWindowsSettings.LinkWindows)
            //{
            //    map.linkGrid.Notify_LinkerCreatedOrDestroyed(this);
            //      map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
            //}

            //Cache whether or not there are comps to tick
            compsToTick = comps?.Any(x => !(x is CompWindow)) ?? false;
        }

        public override void Tick()
        {
            //base.Tick();
            if (compsToTick)
            {
                var compsList = this.comps;
                for (int i = compsList.Count; i-- > 0;) compsList[i].CompTick();
            }
            if (needsUpdate)
            {
                if (!isFacingSet) CheckFacing();
                CastLight();
                Map.GetComponent<MapComp_Windows>().IncludeTileRange(illuminated);
                needsUpdate = false;
            }
            if (Current.gameInt.tickManager.ticksGameInt % tickRareInterval == HashInterval) TickRare();
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
                if (autoVent && !emergencyShut && isFacingSet && (!AttachedRoom?.UsesOutdoorTemperature ?? false)) AutoVentControl();
            }
            base.TickRare();
        }

        #endregion vanilla overrides

        #region adapting as door

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
            //return open && p.HostileTo(this); //This makes the window traversable for hostiles! Useful?
            return false;
        }

        #endregion adapting as door

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
            int ticksGame = Find.TickManager.TicksGame;
            if ((ticksGame % (tickRareInterval * 3) != 0) && !badTemperatureOnce) return; //Checks only each 12,5s or if bad temperature on last cycle.
            float insideTemp = AttachedRoom.Temperature;
            float outsideTemp = GenTemperature.GetTemperatureForCell(Outside, Map);
            if (TargetTemp.Includes(insideTemp)) //Stand down if temperature is right.
            {
                if (ventComp.WantsFlick()) //If temperature is right but a command is still pending, cancel it.
                {
                    ventComp.FlickFor(venting);
                }
                if (leaks && !badTemperatureOnce && !open && TargetTemp.Includes(outsideTemp)) //If its a simple window, check if should re-open next cycle.
                {
                    ShouldReOpen();
                }
                badTemperatureRecently = badTemperatureOnce = false;
                skippedTempChecks = 0;
                return;
            }
            if (badTemperatureRecently) //Escape if acted recently.
            {
                if (skippedTempChecks <= maxTempChecks)
                {
                    skippedTempChecks++;
                    return;
                }
                badTemperatureRecently = false;
                skippedTempChecks = 0;
            }
            if (badTemperatureOnce) //React only if bad temperature persists from last tickRare.
            {
                badTemperatureOnce = false;
                ReactToTemperature(insideTemp, outsideTemp); //Actually evaluating the situation.
                return;
            }
            badTemperatureOnce = true;
        }

        //fetched from Owlchemist's
        private void DarkenCellsCarefully()
        {
            if (!illuminated.NullOrEmpty())
            {
                List<int> affected = new List<int>(illuminated);
                foreach (var neighbor in Neighbors())
                {
                    if (neighbor != this && neighbor.open && !neighbor.illuminated.NullOrEmpty())
                    {
                        affected.RemoveAll(x => neighbor.illuminated.Contains(x));
                    }
                }

                //This actually updates the glowgrids
                Map.GetComponent<MapComp_Windows>().ExcludeTileRange(affected);

                //Update internal data
                illuminated.Clear();
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
            if (info.map != Map) return;
            var cell = info.center;
            int cellIdx = info.map.cellIndices.CellToIndex(cell);
            bool removed = info.removed;
            bool roof = sender is RoofGrid;
            if (isFacingSet && roof && cellIdx.IsInterior(this) && !GenAdj.CellsAdjacentCardinal(this).Contains(cell)) return;
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

        private void ReactToTemperature(float insideTemp, float outsideTemp)
        {
            bool
                colderOutside = insideTemp > outsideTemp,
                colderInside = insideTemp < outsideTemp,
                tooHotInside = insideTemp > TargetTemp.max,
                tooColdInside = insideTemp < TargetTemp.min,
                tooHotOutside = outsideTemp > TargetTemp.max,
                tooColdOutside = outsideTemp < TargetTemp.min,
                doFlick = false,
                intent;
            if (niceOutside && (tooColdOutside || tooColdInside)) niceOutside = false;
            if (!venting) //open if...
            {
                doFlick = (tooHotInside && colderOutside) || (tooColdInside && colderInside && !tooColdOutside);
                intent = true;
            }
            else //close if...
            {
                doFlick = (tooHotInside && tooHotOutside) || (tooColdInside && tooColdOutside);
                intent = false;
            }
            if (doFlick)
            {
                badTemperatureRecently = true;
                ventComp.FlickFor(intent);
                if (Prefs.LogVerbose)
                {
                    string action = intent ? "open" : "close";
                    string inside = tooHotInside ? "HOT" : "COLD";
                    string outside = tooHotOutside ? "HOT" : "COLD";
                    string reason = intent ? $"{inside} inside and outside is better" : $"{outside} outside";
                    Log.Message($"[OpenTheWindows] {this} @ {AttachedRoomName} decided to {action} because it's too {reason}. Comfortable temperature range is {TargetTemp}.");
                }
            }
        }

        private string SelfDiagnotics()
        {
            var report = new StringBuilder();
            report.Append($"[OpenTheWindows] Diagnostics for {this}:\n");
            bool hasRoom = AttachedRoom != null;
            string roomName = hasRoom ? $" ({AttachedRoomName})" : "";
            report.AppendLine($"AttachedRoom? {hasRoom.ToStringYesNo()}{roomName}");
            report.AppendLine($"isFacingSet? {isFacingSet.ToStringYesNo()}");
            report.AppendLine($"open? {open.ToStringYesNo()}");
            report.AppendLine($"venting? {venting.ToStringYesNo()}");
            report.AppendLine($"needsUpdate? {needsUpdate.ToStringYesNo()}");
            if (scanLines.NullOrEmpty())
            {
                report.AppendLine($"No scanLines!");
            }
            else
            {
                var scanLinesCount = scanLines.Count();
                for (int i = 0; i < scanLinesCount; i++)
                {
                    var line = scanLines[i];
                    string facing = line.facingSet ? line.facing.ToString() : "indetermined";
                    report.AppendLine($"ScanLine {i + 1}: {line.clearLineReport} clear cells, facing {facing}");
                }
            }
            return report.ToString();
        }

        private void SetScanLines()
        {
            scanLines.Clear();
            foreach (IntVec3 c in GenAdj.OccupiedRect(Position, Rotation, def.size))
            {
                scanLines.Add(new ScanLine(this, c));
            }
        }

        private void ShouldReOpen()
        {
            if (niceOutside)
            {
                mainComp.FlickFor(true);
                niceOutside = false;
                if (Prefs.LogVerbose)
                {
                    Log.Message($"[OpenTheWindows] {this} @ {AttachedRoomName} decided to re-open because outside doesn't look so bad. Comfortable temperature range is {TargetTemp}.");
                }
                return;
            }
            niceOutside = true;
        }

        #endregion custom

        #region adapting to Better Pawn Control

        public void EmergencyShutOff(object sender, bool danger) // event handler
        {
            bool mustclose = danger && alarmReact && open;
            bool mustopen = !danger && emergencyShut;
            if (mustclose || mustopen)
            {
                mainComp.FlickFor(!open);
                ventComp.FlickFor(!venting);
                emergencyShut = !emergencyShut;
                return;
            }
            if (!alarmReact) return;
            if (mainComp.WantsFlick() && mainComp.wantSwitchOn == danger) mainComp.FlickFor(!danger);
            if (ventComp.WantsFlick() && ventComp.wantSwitchOn == danger) ventComp.FlickFor(!danger);
        }

        #endregion adapting to Better Pawn Control

        #region nested

        private class ScanLine
        {
            public LinkDirections facing;

            public bool
                horizontal = false,
                bleeds = false,
                facingSet = false;

            private List<int>
                clearLine = new List<int>(),
                scanLine = new List<int>();

            private Map map;
            private int maxreach;
            private Building_Window parent;

            private IntVec3
                position,
                bleedDirection,
                toRight,
                toLeft;

            public ScanLine(Building_Window window, IntVec3 pos)
            {
                position = pos;
                parent = window;
                map = parent.Map;
                maxreach = parent.Reach + WindowUtility.deep;
                horizontal = window.Rotation.IsHorizontal;
                bleeds = ShouldBleed();
                SetScanLine();
                FindObstruction(IntVec3.Zero);
            }

            public string clearLineReport => clearLine.Count().ToString();
            private bool LeftIsSet => SiblingLeft.facingSet;
            private int PositionIdx => map.cellIndices.CellToIndex(position);
            private bool RightIsSet => SiblingRight.facingSet;
            private bool SiblingClear => (SiblingLeft != null && LeftIsSet) || (SiblingRight != null && RightIsSet);
            private ScanLine SiblingLeft => parent.scanLines.Find(x => x.position == toLeft);
            private ScanLine SiblingRight => parent.scanLines.Find(x => x.position == toRight);

            public bool Affected(IntVec3 cell)
            {
                return scanLine.Contains(map.cellIndices.CellToIndex(cell));
            }

            public void CastLight()
            {
                if (!facingSet) return;
                foreach (var pos in clearLine)
                {
                    var relevant = pos.IsInterior(PositionIdx, parent.Facing) ? parent.illuminated : parent.view;
                    relevant.AddDistinct(pos);
                    if (bleeds) AddBleed(pos, relevant);
                }
            }

            public void FindObstruction(IntVec3 motivator, bool removed = false, Map updatedMap = null)
            {
                if (updatedMap != null) map = updatedMap;
                clearLine = Unobstructed(motivator, removed);
            }

            public void Reset()
            {
                clearLine.Clear();
                facingSet = false;
            }

            public void SetScanLine()
            {
                int cellx = position.x;
                int cellz = position.z;
                scanLine.Clear();
                for (int i = 1; i <= +maxreach; i++)
                {
                    //fetched from Owlchemist's
                    IntVec3 targetA = horizontal ? new IntVec3(cellx + i, 0, cellz) : new IntVec3(cellx, 0, cellz + i);
                    IntVec3 targetB = horizontal ? new IntVec3(Math.Max(0, cellx - i), 0, cellz) : new IntVec3(cellx, 0, Math.Max(0, cellz - i));
                    if (targetA.InBounds(map)) scanLine.Add(map.cellIndices.CellToIndex(targetA));
                    if (targetB.InBounds(map)) scanLine.Add(map.cellIndices.CellToIndex(targetB));
                }
            }

            public List<int> Unobstructed(IntVec3 motivator, bool removed = false)
            {
                //1. What is the test?
                bool lazy = motivator == IntVec3.Zero;
                cellTest motivated = (target, inside) => target == motivator ? removed : IsClear(target, inside);
                cellTest clear = lazy ? IsClear : motivated;

                //2. Determine facing
                IntVec3 dummy;
                bool overhangFwd = !ClearForward(position, horizontal, clear, false, 1, out dummy); //sets facing Down/Left
                bool overhangBwd = !ClearBackward(position, horizontal, clear, false, 1, out dummy); //sets facing Up/Right
                facingSet = SetFacing(overhangFwd, overhangBwd);
                if (!facingSet) return new List<int>(); //escape if unable to determine sides
                bool southward = facing == LinkDirections.Down || facing == LinkDirections.Left;

                //3. Determine clearance and max reach on each side
                Dictionary<int, int> cleared = new Dictionary<int, int>();
                int reachFwd = 0;
                int reachBwd = 0;
                //forward (walks North/East)
                for (int i = 1; i <= maxreach; i++)
                {
                    IntVec3 clearedFwd;
                    if (ClearForward(position, horizontal, clear, southward, i, out clearedFwd))
                    {
                        if (clearedFwd.InBounds(map)) cleared.Add(map.cellIndices.CellToIndex(clearedFwd), i);
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
                        if (clearedBwd.InBounds(map)) cleared.Add(map.cellIndices.CellToIndex(clearedBwd), i);
                        reachBwd++;
                    }
                    else break;
                }

                //5. Apply clearance.
                int obstructed = southward ? reachBwd : reachFwd;
                cleared.RemoveAll(x => x.Key.IsInterior(PositionIdx, facing) && x.Value > obstructed);
                return cleared.Keys.ToList();
            }

            private void AddBleed(int index, List<int> list)
            {
                var cell = map.cellIndices.IndexToCell(index);
                if (parent.Large)
                {
                    if (!SiblingClear) return;
                    var spill = cell + bleedDirection;
                    if (spill.CanBeSeenOverFast(map)) list.AddDistinct(map.cellIndices.CellToIndex(spill));
                }
                else
                {
                    var left = horizontal ? IntVec3.South : IntVec3.West;
                    var right = horizontal ? IntVec3.North : IntVec3.East;
                    var leftEdge = cell + left;
                    var rightEdge = cell + right;
                    if (leftEdge.CanBeSeenOverFast(map)) list.AddDistinct(map.cellIndices.CellToIndex(leftEdge));
                    if (rightEdge.CanBeSeenOverFast(map)) list.AddDistinct(map.cellIndices.CellToIndex(rightEdge));
                }
            }

            private bool IsClear(IntVec3 c, bool inside)
            {
                return Affected(c) && c.CanBeSeenOverFast(map) && (inside || !map.roofGrid.Roofed(c) || c.IsTransparentRoof(map));
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
                if (!parent.Large) return true;
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

        #endregion nested
    }
}