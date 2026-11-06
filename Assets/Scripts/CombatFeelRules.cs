using System;
namespace Storyborn
{
    // Simulation-time rules: pausing and hit-stop do not advance combo recovery.
    public sealed class ComboChain
    {
        public int Step { get; private set; }
        public float ReadyAt { get; private set; }
        float lastAttack = float.NegativeInfinity;
        bool started;
        public bool TryAttack(float now, float baseCooldown)
        {
            if(now < ReadyAt) return false;
            Step = !started || now-lastAttack > .75f ? 0 : (Step+1)%3;
            started=true;
            lastAttack=now;
            ReadyAt=now+Math.Max(.01f,baseCooldown)*(Step==2?1.35f:1f);
            return true;
        }
        public int Damage(int baseDamage) { return Math.Max(0,baseDamage)+(Step==2?1:0); }
        public void Reset()
        { Step=0;ReadyAt=0;lastAttack=float.NegativeInfinity;started=false; }
    }

    // A buffered action is consumed exactly once. Caller supplies gameplay clock.
    public sealed class InputBuffer
    {
        readonly float lifetime;
        float expiresAt;
        bool pressed;
        public InputBuffer(float lifetime) { this.lifetime=Math.Max(0,lifetime); }
        public void Press(float now) { pressed=true;expiresAt=now+lifetime; }
        public bool Pending(float now) { return pressed && now<=expiresAt; }
        public bool Consume(float now)
        {
            if(!Pending(now)) { pressed=false;return false; }
            pressed=false;return true;
        }
        public void Clear() { pressed=false;expiresAt=0; }
    }
}
