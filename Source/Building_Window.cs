using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    using static WindowUtility;
    public class Building_Window : Building_Door
    {
        public LinkDirections Facing;
        public bool isFacingSet,
            open = true,
            venting,
            needsUpdate = true,
            autoVent,
            alarmReact = OpenTheWindowsSettings.AlarmReactDefault, 
            emergencyShut,
            leaks,
            badTemperatureOnce,
            badTemperatureRecently,
            niceOutside,
            compsToTick;
        public IntVec3 start, end;
        public List<IntVec3> view = new List<IntVec3>(), illuminated = new List<IntVec3>();
        float closedVentFactor = 0.5f, leakVentFactor = 0.1f, VentRate;
        CompWindow mainComp, ventComp;
        List<ScanLine> scanLines = new List<ScanLine>();
        int skippedTempChecks, maxTempChecks = 4, rareTicks, windowSize, hashInterval;
        CellRect occupiedRect;
        Room temperatureReference;

        public override Graphic Graphic
        {
            get {  return mainComp.CurrentGraphic; }
        }
        public Building_Window()
        {
            MapUpdateWatcher.MapUpdate += MapUpdateHandler;
            if (HarmonyPatcher.BetterPawnControl) AlertManager_LoadState.Alarm += EmergencyShutOff; // register with an event, handler must match template signature

            void MapUpdateHandler(object sender, MapUpdateWatcher.MapUpdateInfo info)
            {
                if (info.map != Map) return;
                IntVec3 cell = info.center;
                bool removed = info.removed;
                bool roof = sender is RoofGrid;
                if (isFacingSet && roof && cell.IsInterior(this))
                {
                    bool contains = false;
                    foreach (var item in GenAdj.CellsAdjacentCardinal(this))
                    {
                        if (item == cell)
                        {
                            contains = true;
                            break;
                        }
                    }
                    if (!contains) return;
                }
                bool unsureFace = false;
                var length = scanLines.Count;
                for (int i = 0; i < length; i++)
                {
                    var scanLine = scanLines[i];
                    if (scanLine.Affected(cell))
                    {
                        IntVec3 motivator = roof ? IntVec3.Zero : cell;
                        var line = scanLine;
                        bool before = line.facingSet;
                        line.FindObstruction(motivator, removed, info.map);
                        unsureFace |= line.facingSet != before;
                        needsUpdate = true;
                    }
                }
                if (unsureFace) isFacingSet = false;
            }

            void EmergencyShutOff(object sender, bool danger) // event handler
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
        }

        #region vanilla overrides
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            //We need to manually clean up the districts and rooms since it's based on vanilla doors which are only designed for 1x1 size
            Map map = Map;
            foreach (var cell in this.OccupiedRect().Cells)
            {
                if (cell != this.Position && cell.GetEdifice(map) == this)
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
                    defaultDesc = mainComp.Props.automated ? "CommandCloseOnEmergencyDesc".Translate() + "ManualCommandNote".Translate() : "CommandCloseOnEmergencyDesc".Translate(),
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

            string SelfDiagnotics()
            {
                var report = new StringBuilder();
                report.Append($"[Windows] Diagnostics for {this}:");
                bool hasRoom = AttachedRoom != null;
                report.AppendLine($"AttachedRoom? {hasRoom.ToStringYesNo()}");
                if (hasRoom) report.AppendWithComma(AttachedRoomName());
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
                    var scanLinesCount = scanLines.Count;
                    for (int i = 0; i < scanLinesCount; i++)
                    {
                        var line = scanLines[i];
                        string facing = line.facingSet ? line.facing.ToString() : "indetermined";
                        //report.AppendLine($"ScanLine {i+1}: {line.clearLineReport} clear cells, facing {facing}");
                    }
                }
                return report.ToString();
            }

            bool GizmoInhibitor(Gizmo gizmo)
            {
                return
                    (gizmo is Command_Toggle toggle && toggle.icon == TexCommand.HoldOpen) ||
                    (HarmonyPatcher.LocksType != null && gizmo.GetType() == HarmonyPatcher.LocksType);
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
                    if (!AttachedRoom().UsesOutdoorTemperature)
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

            Direction8Way FacingCardinal()
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

            //Cache variables
            occupiedRect = GenAdj.OccupiedRect(Position, Rotation, def.size);
            windowSize = Math.Max(def.size.x, def.size.z);
            VentRate = windowSize * 14f;

            hashInterval = this.thingIDNumber % 200;

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
            start = FindEnd(Position, Rotation, def.size, false);
            end = FindEnd(Position, Rotation, def.size, true);
            SetScanLines();

            //Cache whether or not there are comps to tick
            compsToTick = this.comps?.Any(x => x is not CompWindow) ?? false;
        }
        public override void Notify_ColorChanged()
        {
            mainComp.offGraphic = null; //Let it regenerate with the new color
            base.Notify_ColorChanged();
        }
        public override void Tick()
        {
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
            if (Current.gameInt.tickManager.ticksGameInt % 250 == hashInterval) TickRare();

            void CheckFacing()
            {
                bool before = isFacingSet;
                LinkDirections fwdDir;
                LinkDirections bwdDir;
                if (Rotation.IsHorizontal)
                {
                    fwdDir = LinkDirections.Right;
                    bwdDir = LinkDirections.Left;
                }
                else
                {
                    fwdDir = LinkDirections.Up;
                    bwdDir = LinkDirections.Down;
                }
                int fwdVotes = 0, bwdVotes = 0;

                for (int i = scanLines.Count; i-- > 0;)
                {
                    var line = scanLines[i];
                    if (!line.facingSet) continue;
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
                    //Reset
                    for (int j = scanLines.Count; j-- > 0;)
                    {
                        var line = scanLines[j];
                        line.clearCells = new int[] { };
                        line.facingSet = false;
                    }
                }
                if (!needsUpdate && isFacingSet != before) needsUpdate = true;
            }
        }
        public override void TickRare()
        {
            if (Spawned)
            {
                UpdateWindowTemperature();
                if (venting)
                {
                    float vent = open ? VentRate : VentRate * closedVentFactor;
                    GenTemperature.EqualizeTemperaturesThroughBuilding(this, vent, true);
                }
                else if (leaks && !open)
                {
                    GenTemperature.EqualizeTemperaturesThroughBuilding(this, VentRate * leakVentFactor, true);
                }
                if (autoVent && !emergencyShut && isFacingSet && (!AttachedRoom()?.UsesOutdoorTemperature ?? false))
                {
                    AutoVentControl();
                }
            }
            if (compsToTick)
			{
                var compsList = this.comps;
                for (int i = compsList.Count; i-- > 0;) compsList[i].CompTickRare();
			}

            void AutoVentControl()
            {
                if (rareTicks-- == 0 && !badTemperatureOnce) {
                    rareTicks = 3;
                    return; //Checks only each 12,5s or if bad temperature on last cycle.
                }
                float insideTemp = AttachedRoom().Temperature;
                float outsideTemp = GenTemperature.GetTemperatureForCell(OutsideCell(), Map);
                if (OpenTheWindowsSettings.ComfortTemp.Includes(insideTemp)) //Stand down if temperature is right.
                {
                    if (ventComp.WantsFlick()) //If temperature is right but a command is still pending, cancel it.
                    {
                        ventComp.FlickFor(venting);
                    }
                    if (leaks && !badTemperatureOnce && !open && OpenTheWindowsSettings.ComfortTemp.Includes(outsideTemp)) //If its a simple window, check if should re-open next cycle.
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
                
                IntVec3 OutsideCell()
                {
                    switch (Facing)
                    {
                        case LinkDirections.Up: return Position + IntVec3.North;
                        case LinkDirections.Right: return Position + IntVec3.East;
                        case LinkDirections.Down: return Position + IntVec3.South;
                        case LinkDirections.Left: return Position + IntVec3.West;
                        case LinkDirections.None: return IntVec3.Zero;
                        default: return IntVec3.Zero;
                    }
                }

                void ShouldReOpen()
                {
                    if (niceOutside)
                    {
                        mainComp.FlickFor(true);
                        niceOutside = false;
                        if (Prefs.LogVerbose)
                        {
                            Log.Message($"[Windows] {this} @ {AttachedRoomName()} decided to re-open because outside doesn't look so bad. Comfortable temperature range is {OpenTheWindowsSettings.ComfortTemp}.");
                        }
                        return;
                    }
                    niceOutside = true;
                }

                void ReactToTemperature(float insideTemp, float outsideTemp)
                {
                    bool
                        colderOutside = insideTemp > outsideTemp,
                        colderInside = insideTemp < outsideTemp,
                        tooHotInside = insideTemp > OpenTheWindowsSettings.ComfortTemp.max,
                        tooColdInside = insideTemp < OpenTheWindowsSettings.ComfortTemp.min,
                        tooHotOutside = outsideTemp > OpenTheWindowsSettings.ComfortTemp.max,
                        tooColdOutside = outsideTemp < OpenTheWindowsSettings.ComfortTemp.min,
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
                            Log.Message($"[Windows] {this} @ {AttachedRoomName()} decided to {action} because it's too {reason}. Comfortable temperature range is {OpenTheWindowsSettings.ComfortTemp}.");
                        }
                    }
                }
            }
        }
        #endregion

        public override bool PawnCanOpen(Pawn p)
        {
            return false;
        }

        bool GetTemperatureReference(out Room room)
        {
            Map map = Map;
            room = null;
            foreach (var cell in GenAdj.CellsAdjacentCardinal(this))
            {
                if (occupiedRect.Contains(cell)) continue;
                room = cell.GetRoom(map);
                if (room == null) continue;
                break;
            }
            return room != null;
        }
        
        void UpdateWindowTemperature()
        {
            if (temperatureReference == null && !GetTemperatureReference(out temperatureReference)) return;
            Map map = Map;
            
            foreach (IntVec3 c in occupiedRect)
            {
                var room = c.GetRoom(map);
                if (room != null) room.Temperature = temperatureReference.Temperature;
            }
        }

        IntVec3 InsideCell()
        {
            switch (Facing)
            {
                case LinkDirections.Up: return Position + IntVec3.South;
                case LinkDirections.Right: return Position + IntVec3.West;
                case LinkDirections.Down: return Position + IntVec3.North;
                case LinkDirections.Left: return Position + IntVec3.East;
                case LinkDirections.None: return IntVec3.Zero;
                default: return IntVec3.Zero;
            }
        }
        string AttachedRoomName()
        {
            return AttachedRoom()?.Role?.label ?? "undefined";
        }
        public Room AttachedRoom()
        {
            var cell = InsideCell();
            return (isFacingSet & cell != IntVec3.Zero) ? cell.GetRoom(Map) : null;
        }
        void CastLight()
        {
            DarkenCellsCarefully();
            view.Clear();
            if (open && isFacingSet)
            {
                for (int i = scanLines.Count; i-- > 0;)
                {
                    scanLines[i].CastLight();
                }
            }
        }
        void DarkenCellsCarefully()
        {
            if (!illuminated.NullOrEmpty())
            {
                List<IntVec3> affected = new List<IntVec3>(illuminated);
                var neighbors = Neighbors();
                if (neighbors != null)
                {
                    for (int i = neighbors.Count; i-- > 0;)
                    {
                        var item = neighbors[i];
                        if (item != this && item.open && item.illuminated != null)
                        {
                            var illuminated = item.illuminated;
                            for (int j = illuminated.Count; j-- > 0;)
                            {
                                affected.Remove(illuminated[j]);
                            }
                        }
                    }
                }
                
                //This actually updates the glowgrids
                Map.GetComponent<MapComp_Windows>().ExcludeTileRange(affected);

                //Update internal data
                illuminated.Clear();
            }

            List<Building_Window> Neighbors()
            {
                if (!isFacingSet) return null;
                Region region = Map.regionGrid.GetValidRegionAt(InsideCell());
                if (region == null) return null;

                List<Building_Window> result = new List<Building_Window>();
                FindAffectedWindows(result, region, this.GetRegion());
                return result;
            }
        }
        void SetScanLines()
        {
            scanLines.Clear();
            foreach (IntVec3 c in occupiedRect)
            {
                scanLines.Add(new ScanLine(this, c));
            }
        }
    
        class ScanLine
        {
            public LinkDirections facing;
            public bool horizontal, bleeds, facingSet;
            public int[] clearCells = new int[] { };
            Map map;
            int maxreach;
            Building_Window parent;
            IntVec3 position, bleedDirection, toRight, toLeft;
            List<IntVec3> scanLine = new List<IntVec3>();

            public ScanLine(Building_Window window, IntVec3 pos)
            {
                position = pos;
                parent = window;
                map = parent.Map;
                maxreach = (parent.windowSize / 2 + 1) + WindowUtility.deep;
                horizontal = window.Rotation.IsHorizontal;
                bleeds = ShouldBleed();
                SetScanLine();
                FindObstruction(IntVec3.Zero);
            }
            public bool Affected(IntVec3 cell)
            {
                return scanLine.Contains(cell);
            }
            public void CastLight()
            {
                if (!facingSet) return;
                
                int length = scanLine.Count;
                for (int i = 0; i < length; i++)
                {
                    var item = scanLine[i];
                    if (Array.Exists(clearCells, x => x == i))
                    {
                        var relevant = item.IsInterior(position, parent.Facing) ? parent.illuminated : parent.view;
                        relevant.AddDistinct(item);
                        if (bleeds) AddBleed(item, relevant);
                    }
                }
            }
            public void FindObstruction(IntVec3 motivator, bool removed = false, Map updatedMap = null)
            {
                if (updatedMap != null) map = updatedMap;
                var actualClearCells = Unobstructed(motivator, removed);
                
                //Compile clearCells array
                List<int> workingList = new List<int>();
                var length = scanLine.Count;
                for (int i = 0; i < length; i++)
                {
                    var item = scanLine[i];
                    if (actualClearCells.Contains(item)) workingList.Add(i);
                }
                clearCells = workingList.ToArray();
                
            }
            void SetScanLine()
            {
                int cellx = position.x;
                int cellz = position.z;
                scanLine.Clear();
                for (int i = 1; i <= +maxreach; i++)
                {
                    IntVec3 targetA = horizontal ? new IntVec3(cellx + i, 0, cellz) : new IntVec3(cellx, 0, cellz + i);
                    IntVec3 targetB = horizontal ? new IntVec3(Math.Max(0, cellx - i), 0, cellz) : new IntVec3(cellx, 0, Math.Max(0, cellz - i)); 
                    if (targetA.InBounds(map)) scanLine.Add(targetA);
                    if (targetB.InBounds(map)) scanLine.Add(targetB);
                }
            }
            List<IntVec3> Unobstructed(IntVec3 motivator, bool removed = false)
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
                if (!facingSet) return new List<IntVec3>(); //escape if unable to determine sides
                bool southward = facing == LinkDirections.Down || facing == LinkDirections.Left;

                //3. Determine clearance and max reach on each side
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
                
                return new List<IntVec3>(cleared.Keys);
            }
            void AddBleed(IntVec3 c, List<IntVec3> list)
            {
                if (parent.windowSize > 1)
                {
                    //Are the siblings clear?
                    if(!
                    (
                        (parent.scanLines.Find(x => x.position == toLeft)?.facingSet ?? false) || 
                        (parent.scanLines.Find(x => x.position == toRight)?.facingSet ?? false))
                    ) return;

                    IntVec3 b = c + bleedDirection;
                    if (b.CanBeSeenOverFast(map)) list.AddDistinct(b);
                }
                else
                {
                    var leftEdge = c + (horizontal ? IntVec3.South : IntVec3.West);
                    if (leftEdge.CanBeSeenOverFast(map)) list.AddDistinct(leftEdge);
                    
                    var rightEdge = c + (horizontal ? IntVec3.North : IntVec3.East);
                    if (rightEdge.CanBeSeenOverFast(map)) list.AddDistinct(rightEdge);
                }
            }
            bool IsClear(IntVec3 c, bool inside)
            {
                return Affected(c) && c.CanBeSeenOverFast(map) && (inside || !map.roofGrid.Roofed(c) || c.IsTransparentRoof(map));
            }
            bool SetFacing(bool overhangFwd, bool overhangBwd)
            {
                if (overhangFwd != overhangBwd)
                {
                    if (overhangFwd) facing = horizontal ? LinkDirections.Left : LinkDirections.Down;
                    if (overhangBwd) facing = horizontal ? LinkDirections.Right : LinkDirections.Up;
                    return true;
                }
                return false;
            }
            bool ShouldBleed()
            {
                if (parent.windowSize == 1) return true;
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
    }
}