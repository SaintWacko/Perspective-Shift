using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace PerspectiveShift
{
    [HarmonyPatch(typeof(JoyUtility), nameof(JoyUtility.JoyTickCheckEnd))]
    public static class JoyUtility_JoyTickCheckEnd_Patch
    {
        public static bool Prefix(Pawn pawn, int delta, JoyTickFullJoyAction fullJoyAction, float extraJoyGainFactor, Building joySource, ref bool __result)
        {
            Job curJob = pawn.CurJob;
            if (!pawn.IsAvatar()
                || curJob == null
                || !curJob.playerForced
                || !curJob.ignoreJoyTimeAssignment
                || (curJob.def != JobDefOf.Meditate
                    && curJob.def != JobDefOf.MeditatePray
                    && curJob.def != JobDefOf.Reign))
            {
                return true;
            }

            if (curJob.def.joyKind == null)
            {
                Log.Warning("This method can only be called for jobs with joyKind.");
                __result = false;
                return false;
            }

            if (joySource != null)
            {
                if (joySource.def.building.joyKind != null && curJob.def.joyKind != joySource.def.building.joyKind)
                {
                    Log.ErrorOnce("Joy source joyKind and jobDef.joyKind are not the same. building=" + joySource.ToStringSafe() + ", jobDef=" + curJob.def.ToStringSafe(), joySource.thingIDNumber ^ 0x343FD5CC);
                }
                extraJoyGainFactor *= joySource.GetStatValue(StatDefOf.JoyGainFactor);
            }

            if (pawn.needs.joy == null)
            {
                if (!curJob.doUntilGatheringEnded)
                {
                    pawn.jobs.curDriver.EndJobWith(JobCondition.InterruptForced);
                    __result = false;
                    return false;
                }
            }

            pawn.needs.joy?.GainJoy(extraJoyGainFactor * curJob.def.joyGainRate * 0.36f / 2500f * delta, curJob.def.joyKind);
            if (curJob.def.joySkill != null)
            {
                pawn.skills.GetSkill(curJob.def.joySkill).Learn(curJob.def.joyXpPerTick * delta);
            }

            if (!curJob.ignoreJoyTimeAssignment && !pawn.GetTimeAssignment().allowJoy && !curJob.doUntilGatheringEnded)
            {
                pawn.jobs.curDriver.EndJobWith(JobCondition.InterruptForced);
                __result = true;
                return false;
            }

            __result = false;
            return false;
        }
    }
}
