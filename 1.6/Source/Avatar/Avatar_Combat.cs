using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace PerspectiveShift
{
    public partial class Avatar
    {
        public static bool IsAvatarLeftClick = false;
        private static Texture2D _reticleTex;
        public static Texture2D ReticleTex => _reticleTex ??= ContentFinder<Texture2D>.Get("UI/Reticle");
        private static Texture2D _reticleCooldownTex;
        public static Texture2D ReticleCooldownTex => _reticleCooldownTex ??= ContentFinder<Texture2D>.Get("UI/ReticleCooldown");
        private static Texture2D _reticleNoLOSTex;
        public static Texture2D ReticleNoLOSTex => _reticleNoLOSTex ??= ContentFinder<Texture2D>.Get("UI/ReticleNoLOS");

        private static Texture2D _dropCursorTex;
        public static Texture2D DropCursorTex => _dropCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Drop");

        private static Texture2D _mineCursorTex;
        public static Texture2D MineCursorTex => _mineCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Mine");

        private static Texture2D _buildCursorTex;
        public static Texture2D BuildCursorTex => _buildCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Build");

        private static Texture2D _chopCursorTex;
        public static Texture2D ChopCursorTex => _chopCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Chop");

        private static Texture2D _harvestCursorTex;
        public static Texture2D HarvestCursorTex => _harvestCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Harvest");

        private static Texture2D _cutCursorTex;
        public static Texture2D CutCursorTex => _cutCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Scythe");

        private static Texture2D _arrowsCursorTex;
        public static Texture2D ArrowsCursorTex => _arrowsCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Arrows");

        private static Texture2D _ammoCursorTex;
        public static Texture2D AmmoCursorTex => _ammoCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Ammo");

        private static Texture2D _chargeCursorTex;
        public static Texture2D ChargeCursorTex => _chargeCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Charge");

        private static Texture2D _traverseCursorTex;
        public static Texture2D TraverseCursorTex => _traverseCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Traverse");

        private static Texture2D _openCursorTex;
        public static Texture2D OpenCursorTex => _openCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Open");

        private static Texture2D _recreationCursorTex;
        public static Texture2D RecreationCursorTex => _recreationCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Recreation");

        private static Texture2D _sleepCursorTex;
        public static Texture2D SleepCursorTex => _sleepCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Sleep");

        private static Texture2D _researchCursorTex;
        public static Texture2D ResearchCursorTex => _researchCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Research");

        private static Texture2D _roofCursorTex;
        public static Texture2D RoofCursorTex => _roofCursorTex ??= ContentFinder<Texture2D>.Get("UI/CustomCursors/Roof");

        public bool HandleSelectorClick()
        {
            if (Find.Targeter.IsTargeting) return false;
            if (pawn.Downed) return false;
            if (pawn.InMentalState || passedOut) return false;

            if (ModCompatibility.IsPawnInVehicle(pawn, out Pawn veh, out bool isDriver, out bool isGunner))
            {
                if (isGunner && Event.current.type == EventType.MouseDown)
                {
                    if (Event.current.button == 0)
                    {
                        ModCompatibility.FireVehicleWeapons(veh, pawn, UI.MouseMapPosition());
                        Event.current.Use();
                        return true;
                    }
                    else if (Event.current.button == 1)
                    {
                        ModCompatibility.ClearVehicleWeapons(veh, pawn);
                        Event.current.Use();
                        return true;
                    }
                }
                return false;
            }

            if (Find.TickManager.Paused) return false;
            if (State.CameraLockPosition.HasValue) return false;

            if (IsMouseOverUI() || IsMouseOverColonistBar()) return false;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (pawn.Drafted)
                {
                    if (MouseIsOverPawn()) return false;

                    HandleFiring();
                    return true;
                }
                else
                {
                    return HandleLeftClick();
                }
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                if (pawn.carryTracker?.CarriedThing != null && pawn.inventory != null && pawn.carryTracker.CarriedThing is not (Pawn or Corpse))
                {
                    var carried = pawn.carryTracker.CarriedThing;
                    int count = carried.stackCount;

                    if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, carried, count))
                    {
                        var maxCount = MassUtility.CountToPickUpUntilOverEncumbered(pawn, carried);
                        if (maxCount <= 0)
                        {
                            Messages.Message("PS_CannotCarryMoreWeight".Translate(), MessageTypeDefOf.RejectInput, false);
                            Event.current.Use();
                            return true;
                        }
                        count = maxCount;
                    }

                    var transferred = pawn.carryTracker.innerContainer.TryTransferToContainer(carried, pawn.inventory.innerContainer, count);
                    if (transferred > 0)
                    {
                        DefsOf.PS_PackInventory.PlayOneShotOnCamera();
                        Event.current.Use();
                        return true;
                    }
                }

                if (pawn.Drafted)
                {
                    var otherPawnsSelected = Find.Selector.SelectedObjects
                        .Any(o => o is Pawn p && p != pawn);
                    if (otherPawnsSelected)
                        return false;

                    if (pawn.jobs?.curJob != null && pawn.jobs.curJob.def.playerInterruptible)
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);

                    Event.current.Use();
                    return true;
                }

                if (pawn.jobs?.curJob != null && pawn.jobs.curJob.def.playerInterruptible)
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
            return false;
        }

        private void HandleFiring()
        {
            if (pawn.stances.curStance is Stance_Busy) return;
            if (IsAbilityCastJob()) return;
            var verb = GetActiveVerb();
            if (verb == null) return;

            if (pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                Messages.Message("IsIncapableOfViolence".Translate(pawn.LabelShort, pawn), MessageTypeDefOf.RejectInput);
                return;
            }

            if (!verb.verbProps.IsMeleeAttack && pawn.WorkTagIsDisabled(WorkTags.Shooting))
            {
                Messages.Message("IsIncapableOfShooting".Translate(pawn), MessageTypeDefOf.RejectInput);
                return;
            }

            var targetCell = UI.MouseCell();
            if (!targetCell.InBounds(pawn.Map)) return;

            var target = GetBestTarget(targetCell);
            Vector3 targetPos = target.Thing != null ? target.Thing.DrawPos : targetCell.ToVector3Shifted();
            Vector3 toTarget = targetPos - pawn.DrawPos;
            if (toTarget.sqrMagnitude > 0.01f)
                pawn.Rotation = Rot4.FromAngleFlat(NormAngle(Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg));

            if (pawn.Position.DistanceTo(targetCell) <= ShootTuning.MeleeRange)
            {
                if (target.Thing != null)
                    pawn.meleeVerbs.TryMeleeAttack(target.Thing);
            }
            else if (verb.CanHitTarget(target))
            {
                verb.TryStartCastOn(target, false, true);
            }
        }

        private void HandleCombatStance()
        {
            if (!pawn.Drafted || pawn.stances.curStance == null) return;

            bool isMoving = IsMoving;

            if (isMoving)
            {
                var rgActive = ModCompatibility.IsRunAndGunActiveFor(pawn, out string rgReason);
                if (rgActive)
                {
                    var stance = pawn.stances.curStance;
                    if (stance is Stance_Warmup warmup && stance.GetType() != ModCompatibility.stanceRunAndGunType)
                    {
                        ModCompatibility.ConvertToRunAndGunStance(pawn, warmup);
                    }
                    else if (stance is Stance_Cooldown cooldown && stance.GetType() != ModCompatibility.stanceRunAndGunCooldownType)
                    {
                        ModCompatibility.ConvertToRunAndGunCooldownStance(pawn, cooldown);
                    }
                    return;
                }

            }
            else
            {
                if (ModCompatibility.IsRunAndGunActiveFor(pawn))
                {
                    var stance = pawn.stances.curStance;
                    if (ModCompatibility.stanceRunAndGunType != null && stance.GetType() == ModCompatibility.stanceRunAndGunType && stance is Stance_Warmup warmup)
                    {
                        ModCompatibility.ConvertToVanillaWarmupStance(pawn, warmup);
                    }
                    else if (ModCompatibility.stanceRunAndGunCooldownType != null && stance.GetType() == ModCompatibility.stanceRunAndGunCooldownType && stance is Stance_Cooldown cooldown)
                    {
                        ModCompatibility.ConvertToVanillaCooldownStance(pawn, cooldown);
                    }
                }
            }
        }

        private void DrawReticle(Vector2 center)
        {
            if (Event.current.type != EventType.Repaint) return;

            if (pawn.stances.curStance is Stance_Busy)
            {
                bool isCooldown = pawn.stances.curStance is Stance_Cooldown;
                Color prev = GUI.color;
                GUI.color = isCooldown ? new Color(1f, 0.65f, 0f) : Color.white;
                float size = 32f;
                var rect = new Rect(center.x - size / 2f, center.y - size / 2f, size, size);
                var tex = isCooldown ? ReticleCooldownTex : ReticleTex;
                if (tex != null) GUI.DrawTexture(rect, tex);
                GUI.color = prev;
                return;
            }

            Color color = Color.green;
            Texture2D reticleTex = ReticleTex;

            var verb = GetActiveVerb();
            if (verb != null)
            {
                var targetCell = UI.MouseCell();
                if (!targetCell.InBounds(pawn.Map)) { LeanTarget = Vector3.zero; return; }

                var target = GetBestTarget(targetCell);
                if (!verb.CanHitTarget(target))
                {
                    color = Color.red;
                    reticleTex = ReticleNoLOSTex;
                    if (!IsMoving) LeanTarget = Vector3.zero;
                }
                else if (!IsMoving)
                {
                    UpdateLeanTarget(targetCell);
                }
            }

            Color prevColor = GUI.color;
            GUI.color = color;
            float sz = 32f;
            var r = new Rect(center.x - sz / 2f, center.y - sz / 2f, sz, sz);
            if (reticleTex != null) GUI.DrawTexture(r, reticleTex);
            GUI.color = prevColor;
        }

        private void UpdateLeanTarget(IntVec3 targetCell)
        {
            var leanSources = new List<IntVec3>();
            ShootLeanUtility.LeanShootingSourcesFromTo(pawn.Position, targetCell, pawn.Map, leanSources);
            var best = leanSources
                .Where(s => s != pawn.Position && s.IsValid && s != IntVec3.Zero
                            && GenSight.LineOfSight(s, targetCell, pawn.Map, skipFirstCell: true))
                .OrderBy(s => s.DistanceToSquared(targetCell))
                .FirstOrDefault();
            LeanTarget = (best != IntVec3.Zero && best != pawn.Position)
                ? (best - pawn.Position).ToVector3()
                : Vector3.zero;
        }

        private Verb GetActiveVerb()
        {
            var verb = pawn.equipment?.PrimaryEq?.PrimaryVerb;
            if (verb == null || verb.verbProps.IsMeleeAttack)
                verb = pawn.VerbTracker?.AllVerbs?.FirstOrDefault(v => v is Verb_MeleeAttack && v.Available());
            return verb;
        }

        private bool IsAbilityCastJob()
        {
            if (pawn.CurJob?.ability != null)
                return true;
            if (ModCompatibility.IsVEFAbilityCast(pawn))
                return true;
            return false;
        }

        private LocalTargetInfo GetBestTarget(IntVec3 targetCell)
        {
            var things = targetCell.GetThingList(pawn.Map);
            Thing best = things.FirstOrDefault(t => t is Pawn && t != pawn)
                ?? things.FirstOrDefault(t => t.def.category == ThingCategory.Building || t.def.category == ThingCategory.Item);
            return best != null ? new LocalTargetInfo(best) : new LocalTargetInfo(targetCell);
        }

        private void HandleHoldToFire(bool mouseOverGizmo, bool mouseOverUI)
        {

            if (Event.current.type == EventType.Repaint
                && pawn.Drafted
                && PerspectiveShiftMod.settings.holdToFire
                && Input.GetMouseButton(0)
                && !mouseOverUI && !mouseOverGizmo
                && !State.ControlsFrozen
                && !Find.Targeter.IsTargeting
                && !Find.TickManager.Paused)
            {
                HandleFiring();
            }
        }

        private void UpdateCursorAndReticle(bool mouseOverGizmo, bool mouseOverUI)
        {
            bool drafted = pawn.Drafted && !pawn.InMentalState;

            if (drafted && !Find.TickManager.Paused && Find.Selector.IsSelected(pawn) && !Find.Targeter.IsTargeting)
            {
                if (!PerspectiveShiftMod.settings.disableCustomGizmos)
                    Find.Selector.Deselect(pawn);
            }

            bool cursorBlocked = mouseOverUI || mouseOverGizmo || State.ControlsFrozen || Find.Targeter.IsTargeting
                || WorldRendererUtility.WorldSelected;

            if (PerspectiveShiftMod.settings.haulingCursor && CarriedThing != null && !pawn.InMentalState && !cursorBlocked)
            {
                if (drafted && !IsMoving) LeanTarget = Vector3.zero;
                Cursor.visible = false;
                DrawDropCursor(UI.MousePositionOnUIInverted);
                return;
            }

            if (!drafted && !cursorBlocked)
            {
                var hint = MouseOverJobTarget();
                if (hint != CursorJobHint.None)
                {
                    Cursor.visible = false;
                    DrawJobCursor(UI.MousePositionOnUIInverted, CursorTexFor(hint));
                    return;
                }
            }

            if (drafted && !cursorBlocked && !Find.TickManager.Paused)
            {
                Cursor.visible = false;
                DrawReticle(UI.MousePositionOnUIInverted);
                return;
            }

            Cursor.visible = true;
        }

        private enum CursorJobHint
        {
            None,
            Mine,
            Build,
            Chop,
            Harvest,
            Cut,
            Traverse,
            Open,
            ReloadArrow,
            ReloadAmmo,
            ReloadCharge,
            Sleep,
            Recreation,
            Research,
            Roof,
        }

        private static Texture2D CursorTexFor(CursorJobHint hint)
        {
            switch (hint)
            {
                case CursorJobHint.Build: return BuildCursorTex;
                case CursorJobHint.Chop: return ChopCursorTex;
                case CursorJobHint.Harvest: return HarvestCursorTex;
                case CursorJobHint.Cut: return CutCursorTex;
                case CursorJobHint.Traverse: return TraverseCursorTex;
                case CursorJobHint.Open: return OpenCursorTex;
                case CursorJobHint.ReloadArrow: return ArrowsCursorTex;
                case CursorJobHint.ReloadAmmo: return AmmoCursorTex;
                case CursorJobHint.ReloadCharge: return ChargeCursorTex;
                case CursorJobHint.Sleep: return SleepCursorTex;
                case CursorJobHint.Recreation: return RecreationCursorTex;
                case CursorJobHint.Research: return ResearchCursorTex;
                case CursorJobHint.Roof: return RoofCursorTex;
                default: return MineCursorTex;
            }
        }

        private const float JobCursorRefresh = 0.25f;

        private IntVec3 jobCursorCell = IntVec3.Invalid;
        private IntVec3 jobCursorPawnCell = IntVec3.Invalid;
        private CursorJobHint jobCursorHint;
        private Thing jobCursorTarget;
        private float jobCursorStaleAt;

        private CursorJobHint MouseOverJobTarget()
        {
            if (CarriedThing != null || pawn.InMentalState || pawn.Map == null) return CursorJobHint.None;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return CursorJobHint.None;

            var cell = UI.MouseCell();
            bool targetGone = jobCursorTarget != null && (jobCursorTarget.Destroyed || !jobCursorTarget.Spawned);

            if (!targetGone
                && cell == jobCursorCell
                && pawn.Position == jobCursorPawnCell
                && Time.realtimeSinceStartup < jobCursorStaleAt)
            {
                return jobCursorHint;
            }

            jobCursorCell = cell;
            jobCursorPawnCell = pawn.Position;
            jobCursorStaleAt = Time.realtimeSinceStartup + JobCursorRefresh;
            jobCursorHint = EvaluateJobTarget(cell, out jobCursorTarget);
            return jobCursorHint;
        }

        private CursorJobHint EvaluateJobTarget(IntVec3 cell, out Thing target)
        {
            var hint = EvaluateJobTargetInt(cell, out target);
            JobFailReason.Clear();
            return hint;
        }

        private CursorJobHint EvaluateJobTargetInt(IntVec3 cell, out Thing target)
        {
            target = null;
            var things = cell.GetThingList(pawn.Map);
            var settings = PerspectiveShiftMod.settings;

            if (pawn.Position.DistanceTo(cell) > settings.grabRange)
            {
                if (!settings.recreationCursor) return CursorJobHint.None;

                for (int i = 0; i < things.Count; i++)
                {
                    if (!IsWithinInteractionRange(things[i]) || !CanRecreateAt(things[i])) continue;

                    target = things[i];
                    return CursorJobHint.Recreation;
                }
                return CursorJobHint.None;
            }

            if (settings.buildCursor && !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Construction))
            {
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is not Frame frame || !frame.IsCompleted()) continue;
                    if (!GenConstruct.CanConstruct(frame, pawn, true, true)) continue;

                    target = frame;
                    return CursorJobHint.Build;
                }

                for (int i = 0; i < things.Count; i++)
                {
                    if (pawn.Map.designationManager.DesignationOn(things[i], DesignationDefOf.Deconstruct) == null) continue;
                    if (!pawn.CanReserve(things[i])) continue;

                    target = things[i];
                    return CursorJobHint.Build;
                }

                for (int i = 0; i < things.Count; i++)
                {
                    if (!CanRepair(things[i])) continue;

                    target = things[i];
                    return CursorJobHint.Build;
                }
            }

            if (settings.traverseCursor && ModCompatibility.AsAboveSoBelowAvailable)
            {
                for (int i = 0; i < things.Count; i++)
                {
                    if (!ModCompatibility.IsLevelLink(things[i])) continue;
                    if (things[i] is not Building_Door door || !door.PawnCanOpen(pawn)) continue;

                    target = things[i];
                    return CursorJobHint.Traverse;
                }
            }

            if (settings.reloadCursors && ModCompatibility.ProgressionAmmunitionAvailable)
            {
                for (int i = 0; i < things.Count; i++)
                {
                    if (!ModCompatibility.TryGetAmmoRechargerType(things[i], pawn, out string ammoType)) continue;
                    if (!pawn.CanReserve(things[i], 1, -1, null, true)) continue;

                    target = things[i];
                    return ReloadCursorHint(ammoType);
                }
            }

            if (settings.openCursor)
            {
                for (int i = 0; i < things.Count; i++)
                {
                    if (!CanOpenNow(things[i])) continue;

                    target = things[i];
                    return CursorJobHint.Open;
                }
            }

            if (settings.chopCursor || settings.harvestCursor || settings.cutCursor)
            {
                var plant = cell.GetPlant(pawn.Map);
                if (plant != null)
                {
                    if (settings.cutCursor && CanCutPlantNow(plant))
                    {
                        target = plant;
                        return CursorJobHint.Cut;
                    }

                    if (CanHarvestNow(plant))
                    {
                        bool isTree = plant.def.plant.IsTree;
                        if (isTree ? settings.chopCursor : settings.harvestCursor)
                        {
                            target = plant;
                            return isTree ? CursorJobHint.Chop : CursorJobHint.Harvest;
                        }
                    }
                }
            }

            if (settings.mineCursor && !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Mining))
            {
                var mineable = cell.GetFirstMineable(pawn.Map);
                if (mineable != null && pawn.CanReserve(mineable))
                {
                    target = mineable;
                    return CursorJobHint.Mine;
                }
            }

            if (settings.roofCursor && CanBuildRoofAt(cell)) return CursorJobHint.Roof;

            if (!settings.researchCursor && !settings.sleepCursor && !settings.recreationCursor) return CursorJobHint.None;

            for (int i = 0; i < things.Count; i++)
            {
                if (CanResearchAt(things[i]))
                {
                    target = things[i];
                    return CursorJobHint.Research;
                }
                if (CanSleepIn(things[i]))
                {
                    target = things[i];
                    return CursorJobHint.Sleep;
                }
                if (CanRecreateAt(things[i]))
                {
                    target = things[i];
                    return CursorJobHint.Recreation;
                }
            }

            return CursorJobHint.None;
        }

        private static CursorJobHint ReloadCursorHint(string ammoType)
        {
            switch (ammoType)
            {
                case "Arrow": return CursorJobHint.ReloadArrow;
                case "Charge": return CursorJobHint.ReloadCharge;
                default: return CursorJobHint.ReloadAmmo;
            }
        }

        private bool CanCutPlantNow(Plant plant)
        {
            if (plant.def.plant.IsTree) return false;
            if (pawn.Map.designationManager.DesignationOn(plant, DesignationDefOf.CutPlant) == null) return false;
            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.PlantCutting)) return false;
            if (!PlantUtility.PawnWillingToCutPlant_Job(plant, pawn)) return false;

            return pawn.CanReserve(plant, 1, -1, null, true);
        }

        private bool CanOpenNow(Thing thing)
        {
            if (thing is not IOpenable { CanOpen: true }) return false;
            if (thing.def.category == ThingCategory.Item) return false;
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) return false;
            if (!pawn.CanReach(thing, PathEndMode.OnCell, Danger.Deadly)) return false;

            return pawn.CanReserve(thing, 1, -1, null, true);
        }

        private bool CanRepair(Thing thing)
        {
            if (thing is not Building building) return false;
            if (!RepairUtility.PawnCanRepairNow(pawn, building)) return false;
            if (pawn.Faction == Faction.OfPlayer && !pawn.Map.areaManager.Home[building.Position]) return false;
            if (building.IsBurning()) return false;

            var designations = pawn.Map.designationManager;
            if (designations.DesignationOn(building, DesignationDefOf.Deconstruct) != null) return false;
            if (building.def.mineable && designations.DesignationAt(building.Position, DesignationDefOf.Mine) != null) return false;
            if (building.def.mineable && designations.DesignationAt(building.Position, DesignationDefOf.MineVein) != null) return false;

            return pawn.CanReserve(building, 1, -1, null, true);
        }

        private bool CanBuildRoofAt(IntVec3 cell)
        {
            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Construction)) return false;

            var map = pawn.Map;
            var buildRoof = map.areaManager.BuildRoof;
            if (buildRoof.TrueCount == 0 || !buildRoof[cell]) return false;
            if (cell.Roofed(map)) return false;
            if (!pawn.CanReserve(cell, 1, -1, ReservationLayerDefOf.Ceiling, true)) return false;
            if (!RoofCollapseUtility.WithinRangeOfRoofHolder(cell, map)) return false;
            if (!RoofCollapseUtility.ConnectedToRoofHolder(cell, map, true)) return false;

            return RoofUtility.FirstBlockingThing(cell, map) == null;
        }

        private bool CanResearchAt(Thing thing)
        {
            if (thing is not Building_ResearchBench bench) return false;
            if (!PerspectiveShiftMod.settings.researchCursor) return false;
            if (pawn.CurJobDef == JobDefOf.Research) return false;
            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Research)) return false;

            var project = Find.ResearchManager.GetProject();
            if (project == null || !project.CanBeResearchedAt(bench, false)) return false;
            if (!pawn.CanReserve(bench, 1, -1, null, true)) return false;

            return !bench.def.hasInteractionCell || pawn.CanReserveSittableOrSpot(bench.InteractionCell, true);
        }

        private bool CanSleepIn(Thing thing)
        {
            if (thing is not Building_Bed bed) return false;
            if (!PerspectiveShiftMod.settings.sleepCursor) return false;
            if (!pawn.Awake() || pawn.InBed()) return false;
            if (bed.ForPrisoners || bed.Medical || pawn.needs?.rest == null) return false;
            if (!RestUtility.CanUseBedEver(pawn, bed.def)) return false;

            return pawn.CanReserveAndReach(bed, PathEndMode.OnCell, Danger.Deadly, bed.SleepingSlotsCount, 0);
        }

        private bool CanRecreateAt(Thing thing)
        {
            if (!PerspectiveShiftMod.settings.recreationCursor) return false;
            if (thing is not Building || pawn.needs?.joy == null) return false;
            if (pawn.CurJob != null && pawn.CurJob.targetA.Thing == thing && pawn.CurJobDef.joyKind != null) return false;
            if (thing.TryGetComp<CompPowerTrader>() is { PowerOn: false }) return false;

            var joyGivers = DefDatabase<JoyGiverDef>.AllDefsListForReading;
            for (int i = 0; i < joyGivers.Count; i++)
            {
                var joyGiver = joyGivers[i];
                if (joyGiver.thingDefs == null || !joyGiver.thingDefs.Contains(thing.def)) continue;

                Job job;
                if (joyGiver.Worker is JoyGiver_WatchBuilding)
                {
                    if (!WatchBuildingUtility.CalculateWatchCells(thing.def, thing.Position, thing.Rotation, pawn.Map).Contains(pawn.Position)) continue;
                    job = JobMaker.MakeJob(joyGiver.jobDef, thing, pawn.Position);
                }
                else if (joyGiver.Worker is JoyGiver_InteractBuilding worker)
                {
                    job = worker.TryGivePlayJob(pawn, thing);
                    if (job == null) continue;
                }
                else continue;

                bool startable = JobTargetsInRange(job) && !job.def.HasModExtension<DisableLeftClickExtension>();
                JobMaker.ReturnToPool(job);
                if (startable) return true;
            }
            return false;
        }

        private static void DrawJobCursor(Vector2 center, Texture2D tex)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (tex == null) return;

            const float size = 22f;
            GUI.DrawTexture(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), tex);
        }

        private void DrawDropCursor(Vector2 center)
        {
            if (Event.current.type != EventType.Repaint) return;

            var tex = DropCursorTex;
            if (tex == null) return;

            const float size = 22f;
            GUI.DrawTexture(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), tex);
        }
    }
}
