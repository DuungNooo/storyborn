using System;
namespace Storyborn
{
    // Pure C# rules, shared by runtime and editor tests.
    public sealed class DashChain
    {
        public int Limit = 2;
        public float Window = .38f;
        public float LockDuration = .5f;
        public int Count { get; private set; }
        public float LockedUntil { get; private set; }
        float last = -100f;
        public bool TryStart(float now)
        {
            if (now < LockedUntil) return false;
            Count = now - last <= Window ? Count + 1 : 1;
            last = now;
            if (Count >= Limit) { LockedUntil = now + LockDuration; Count = 0; last = -100f; }
            return true;
        }
        public void Reset() { Count = 0; LockedUntil = 0; last = -100f; }
        public float Remaining(float now) { return Math.Max(0, LockedUntil - now); }
    }
    public sealed class SkillCooldown
    {
        readonly float duration;
        float readyAt;
        public SkillCooldown(float duration) { this.duration=duration; }
        public bool TryUse(float now)
        { if(now<readyAt) return false; readyAt=now+duration; return true; }
        public float Remaining(float now) { return Math.Max(0,readyAt-now); }
        public void Reset() { readyAt=0; }
    }
    public static class CombatRules
    {
        public static bool Overlap(float ax, float ay, float aw, float ah,
            float bx, float by, float bw, float bh)
        {
            return Math.Abs(ax-bx) < (aw+bw)*.5f && Math.Abs(ay-by) < (ah+bh)*.5f;
        }
        public static bool InAttack(float playerX, float playerY, int facing,
            float targetX, float targetY, float reach)
        {
            float dx = targetX-playerX;
            return dx*facing > -.3f && Math.Abs(dx) < reach && Math.Abs(targetY-playerY) < 1.7f;
        }
    }
}
