using System;

namespace Storyborn
{
    [Serializable]
    public sealed class MetaState
    {
        public bool tutorialDone;
        public int deaths;
        public int memory;
        public int hpUpgrades;
        public int attackUpgrades;
        public bool firstUpgrade;
        public bool beastDefeated;

        // Save data is local and may come from older prototypes. Keep purchases bounded.
        public void Sanitize()
        {
            deaths = Math.Max(0, deaths);
            memory = Math.Max(0, memory);
            hpUpgrades = Math.Max(0, Math.Min(RunProgress.MaxPermanentLevel, hpUpgrades));
            attackUpgrades = Math.Max(0, Math.Min(RunProgress.MaxPermanentLevel, attackUpgrades));
        }
    }

    [Serializable]
    public sealed class Gear
    {
        // 0: straight shot, 1: arrow rain, 2: arcing spread. Assigned once on acquisition.
        public int Variant;
        public int Level;
        public int Id;
        public Gear(int variant, int id)
        {
            if (variant < 0 || variant > 2) throw new ArgumentOutOfRangeException("variant");
            Variant = variant;
            Id = id;
        }
        public string Name { get { return "RAINBOW"; } }
        public string SkillName { get { return Variant == 0 ? "Straight shot" : Variant == 1 ? "Arrow rain" : "Arcing volley"; } }
        public int Damage { get { return 2 + Level; } }
        public float Cooldown { get { return Variant == 0 ? 1.6f : Variant == 1 ? 4f : 3f; } }
        public int ProjectileCount { get { return 1 + Level; } }
        public int ScrapValue { get { return 3 + Level * 2; } }
    }

    // Independent of Unity: all economy and item exchanges are atomic and testable.
    public sealed class RunProgress
    {
        public const int MaxGearLevel = 3;
        public const int MaxPermanentLevel = 5;
        public readonly MetaState Meta;
        public readonly Gear[] Slots = new Gear[2];
        public Gear Pending { get; private set; }
        public int Gold { get; private set; }
        public int Shards { get; private set; }
        public int Memory { get { return Meta.memory; } }
        public int MaxHealth { get { return 8 + Meta.hpUpgrades * 2 + (Meta.firstUpgrade ? 1 : 0); } }
        public int AttackDamage { get { return 1 + Meta.attackUpgrades + (Meta.beastDefeated ? 1 : 0); } }
        public int HealthCost { get { return Meta.hpUpgrades >= MaxPermanentLevel ? -1 : 5 + Meta.hpUpgrades * 5; } }
        public int AttackCost { get { return Meta.attackUpgrades >= MaxPermanentLevel ? -1 : 8 + Meta.attackUpgrades * 8; } }
        readonly Random random;
        int nextId;

        public RunProgress(MetaState meta, int seed)
        {
            Meta = meta ?? new MetaState();
            Meta.Sanitize();
            random = new Random(seed);
            ResetRun();
        }

        public Gear NewGear() { return new Gear(random.Next(3), ++nextId); }

        public int Offer(Gear gear)
        {
            if (gear == null) return 0;
            if (Object.ReferenceEquals(Pending, gear)) return 0;
            if (Object.ReferenceEquals(Slots[0], gear) || Object.ReferenceEquals(Slots[1], gear))
                throw new InvalidOperationException("An equipped item cannot also be a ground pickup.");
            int gained = ScrapPending();
            Pending = gear;
            return gained;
        }

        // The old item is left on the ground; it can be equipped in the other slot or scrapped.
        // There is never a reserve inventory and no item is duplicated by a swap.
        public Gear Equip(int slot)
        {
            if (!ValidSlot(slot) || Pending == null) return null;
            Gear displaced = Slots[slot];
            Slots[slot] = Pending;
            Pending = displaced;
            return displaced;
        }

        public int ScrapPending()
        {
            if (Pending == null) return 0;
            int amount = Pending.ScrapValue;
            Shards += amount;
            Pending = null;
            return amount;
        }
        // Dismantling an equipped skill item frees that slot; the reward cannot be claimed twice.
        public int ScrapSlot(int slot)
        {
            if (!ValidSlot(slot) || Slots[slot] == null) return 0;
            int amount = Slots[slot].ScrapValue;
            Slots[slot] = null;
            Shards += amount;
            return amount;
        }
        public int LeaveRoom() { return ScrapPending(); }

        // -1 means unavailable: empty/invalid slot or maximum level.
        public int UpgradeCost(int slot)
        {
            if (!ValidSlot(slot) || Slots[slot] == null || Slots[slot].Level >= MaxGearLevel) return -1;
            return 3 + Slots[slot].Level * 3;
        }
        public bool Upgrade(int slot)
        {
            int price = UpgradeCost(slot);
            if (price < 0 || Shards < price) return false;
            Shards -= price;
            Slots[slot].Level++;
            Meta.firstUpgrade = true; // Achievement unlock is permanent; level is not.
            return true;
        }
        public bool BuyHealth()
        {
            int price = HealthCost;
            if (price < 0 || Meta.memory < price) return false;
            Meta.memory -= price;
            Meta.hpUpgrades++;
            return true;
        }
        public bool BuyAttack()
        {
            int price = AttackCost;
            if (price < 0 || Meta.memory < price) return false;
            Meta.memory -= price;
            Meta.attackUpgrades++;
            return true;
        }
        public bool TrySpendGold(int amount)
        {
            if (amount < 0 || Gold < amount) return false;
            Gold -= amount;
            return true;
        }
        public void AwardGold(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException("amount");
            Gold += amount;
        }
        public void AwardKill(bool elite)
        {
            Gold += elite ? 8 : 2;
            if (elite) Meta.memory++;
        }
        public void CompleteRoom()
        {
            Gold += 5;
            Meta.memory++;
        }
        public void AwardBoss()
        {
            Gold += 20;
            Meta.memory += 5;
            Meta.beastDefeated = true;
        }
        public void ResetRun()
        {
            Gold = 0;
            Shards = 0;
            Slots[0] = Slots[1] = null;
            Pending = null;
        }
        static bool ValidSlot(int slot) { return slot >= 0 && slot < 2; }
    }
}
