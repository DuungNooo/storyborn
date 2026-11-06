using System;
using System.Collections.Generic;
using UnityEngine;

namespace Storyborn
{
    public sealed partial class StoryGame : MonoBehaviour
    {
        enum Mode { Playing, Paused, Dead, Won, Loot, Forge, Routes, Memory }
        enum EnemyPhase { Approach, Tell, Strike, Recover }
        sealed class Foe
        {
            public Vector2 p;
            public int hp, max, type, facing = -1, pattern, combo;
            public float timer, flash, summon, stun, knockback;
            public EnemyPhase phase;
            public SpriteRenderer body, mark;
            public bool hit, summoned;
            public float Width { get { return type == 2 ? 1.8f : type==4?2: type==5?1.3f:.85f; } }
        }
        sealed class Shot
        {
            public Vector2 p, velocity;
            public float life = 4;
            public bool friendly;
            public int damage = 2;
            public float gravity;
            public SpriteRenderer view;
        }
        sealed class Hazard
        {
            public Vector2 p;
            public float until;
            public SpriteRenderer view;
        }
        sealed class Spark
        {
            public Vector2 p, v;
            public float life;
            public SpriteRenderer view;
        }
        [Header("Prototype tuning — not final design")]
        public float moveSpeed = 7.5f;
        public float jumpSpeed = 13;
        public float gravity = 33;
        public float dashSpeed = 23;
        public float dashDuration = .14f;
        public int dashChainLimit = 2;
        public float dashChainWindow = .38f;
        public float dashLockDuration = .5f;
        public float attackReach = 2.1f;
        public float attackCooldown = .24f;

        const float Floor = -3.4f;
        const float Left = -3;
        const float Right = 35;
        Mode mode;
        readonly List<Foe> foes = new List<Foe>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Rect> platforms = new List<Rect>();
        readonly List<Hazard> hazards = new List<Hazard>();
        readonly List<Spark> sparks = new List<Spark>();
        readonly List<GameObject> scenery = new List<GameObject>();
        readonly DashChain chain = new DashChain();
        Camera cam;
        Sprite pixel;
        SpriteRenderer hero, blade, gate, secondGate, lootView;
        Vector2 pos, velocity, dashDirection;
        int facing = 1, hp, maxHp, damage, room, seed, deaths;
        float clock, dashEnd, nextAttack, hurtUntil, slashUntil, coyote;
        float hitStopUntil, trailAt, clearAt, squashUntil;
        bool airJumpUsed;
        readonly ComboChain combo = new ComboChain();
        readonly InputBuffer attackInput = new InputBuffer(.15f);
        readonly InputBuffer dashInput = new InputBuffer(.16f);
        readonly InputBuffer jumpInput = new InputBuffer(.13f);
        readonly InputBuffer[] skillInput = { new InputBuffer(.14f),new InputBuffer(.14f) };
        CombatFeedback feedback;
        Vector3 cameraPosition;
        sealed class Backdrop { public Transform view;public Vector3 home;public float factor; }
        readonly List<Backdrop> backdrops=new List<Backdrop>();
        float shake, bannerUntil;
        MetaState meta;
        RunProgress run;
        readonly float[] skillReady = new float[2];
        bool cleared, tutorial;
        int routeKind, choiceA, choiceB, runCharms;
        const string SaveKey = "Storyborn.Meta.v03";
        readonly string[] routes = { "EQUIPMENT", "GOLD", "CHARM" };
        readonly string[] enemyNames = { "Gingerbread", "Mining dwarf", "The Beast", "Poison apple", "Gingerbread house", "Goblin musician" };
        bool grounded, wasDashing;
        System.Random rng;
        Font uiFont;
        GUIStyle titleStyle, labelStyle, smallStyle, buttonStyle;
        string banner = "";
        readonly string[] roomNames = { "THE RETURNING PATH", "FOREST EDGE", "CANDY TRAIL", "THE OLD RHYTHM", "DEEP WOODS", "RESTING GLADE", "THE BEAST" };
        void Awake()
        {
            Application.targetFrameRate = 120;
            meta=new MetaState {
                tutorialDone=PlayerPrefs.GetInt(SaveKey+".tutorial",0)!=0,
                deaths=PlayerPrefs.GetInt(SaveKey+".deaths",0),memory=PlayerPrefs.GetInt(SaveKey+".memory",0),
                hpUpgrades=PlayerPrefs.GetInt(SaveKey+".hp",0),attackUpgrades=PlayerPrefs.GetInt(SaveKey+".attack",0),
                firstUpgrade=PlayerPrefs.GetInt(SaveKey+".firstUpgrade",0)!=0,
                beastDefeated=PlayerPrefs.GetInt(SaveKey+".beast",0)!=0
            };
            deaths=meta.deaths;
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white); tex.Apply(); tex.filterMode = FilterMode.Point;
            pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one*.5f, 1);
            cam = Camera.main;
            if (cam == null) cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 5.5f;
            cam.backgroundColor = Hex("A9CEC1"); cam.transform.position = new Vector3(8, 0, -10);
            if (FindObjectOfType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hero = Box("Protagonist (placeholder)", Vector2.zero, new Vector2(.7f, 1.3f), Hex("83F4DB"), 6);
            blade = Box("Attack arc", Vector2.zero, Vector2.one, Hex("FFF0B1"), 7);

            feedback=gameObject.AddComponent<CombatFeedback>();feedback.Initialize(cam);
            NewRun();
        }
        SpriteRenderer Box(string name, Vector2 p, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = pixel; sr.color = color;
            sr.sortingOrder = order; go.transform.position = p; go.transform.localScale = size;
            return sr;
        }
        SpriteRenderer Art(string name,string kind,Vector2 p,float height,int order)
        {
            var sr=Box(name,p,Vector2.one,Color.white,order);
            sr.sprite=ForestArt.Get(kind);
            float scale=height/sr.sprite.bounds.size.y;
            sr.transform.localScale=new Vector3(scale,scale,1);
            return sr;
        }
        void SaveMeta()
        {
            PlayerPrefs.SetInt(SaveKey+".tutorial",meta.tutorialDone?1:0);
            PlayerPrefs.SetInt(SaveKey+".deaths",meta.deaths);PlayerPrefs.SetInt(SaveKey+".memory",meta.memory);
            PlayerPrefs.SetInt(SaveKey+".hp",meta.hpUpgrades);PlayerPrefs.SetInt(SaveKey+".attack",meta.attackUpgrades);
            PlayerPrefs.SetInt(SaveKey+".firstUpgrade",meta.firstUpgrade?1:0);PlayerPrefs.SetInt(SaveKey+".beast",meta.beastDefeated?1:0);
            PlayerPrefs.Save();
        }
        void RefreshStats(bool heal=false)
        {
            maxHp=run.MaxHealth+runCharms;damage=run.AttackDamage;
            hp=heal?maxHp:Mathf.Min(hp,maxHp);
        }
        string RoomTitle { get { return tutorial?"LABORATORY / FIRST MEMORY":roomNames[room]; } }
        static Color Hex(string value)
        {
            Color c; ColorUtility.TryParseHtmlString("#"+value, out c); return c;
        }
        void Decor(Vector2 p, Vector2 size, Color color, int layer = 0)
        {
            scenery.Add(Box("Temporary architecture", p, size, color, layer).gameObject);
        }
        void NewRun()
        {
            seed = unchecked((int)DateTime.UtcNow.Ticks); rng = new System.Random(seed);
            run=new RunProgress(meta,seed); runCharms=0;
            // Temporary starter loadout; each item rolls a fixed skill on creation.
            run.Offer(run.NewGear());run.Equip(0);run.Offer(run.NewGear());run.Equip(1);
            RefreshStats(true);room=0;routeKind=0;clock=0;
            dashEnd=nextAttack=hurtUntil=slashUntil=0;
            chain.Limit=dashChainLimit;chain.Window=dashChainWindow;chain.LockDuration=dashLockDuration;
            tutorial=!meta.tutorialDone; BuildRoom();mode=Mode.Playing;
        }
        void ResetChain() { chain.Reset(); }
        void ClearShots()
        { foreach(var s in shots) Destroy(s.view.gameObject);shots.Clear(); }
        void ClearRoomObjects()
        {
            backdrops.Clear();
            foreach(var f in foes) { Destroy(f.body.gameObject);Destroy(f.mark.gameObject); }foes.Clear();
            foreach(var s in sparks) Destroy(s.view.gameObject);sparks.Clear();
            foreach(var h in hazards) Destroy(h.view.gameObject);hazards.Clear();
            foreach(var g in scenery) Destroy(g);scenery.Clear();ClearShots();
            if(gate!=null)Destroy(gate.gameObject);
            if(secondGate!=null)Destroy(secondGate.gameObject);
            if(lootView!=null)Destroy(lootView.gameObject);
        }
        void BuildRoom()
        {
            ClearRoomObjects();
            pos=new Vector2(0,Floor+.65f);velocity=Vector2.zero;facing=1;
            grounded=true;wasDashing=false;airJumpUsed=false;coyote=.1f;
            combo.Reset();attackInput.Clear();dashInput.Clear();jumpInput.Clear();foreach(var input in skillInput)input.Clear();hitStopUntil=0;trailAt=clock;clearAt=-100;squashUntil=clock;
            dashEnd=slashUntil=hurtUntil=nextAttack=clock;ResetChain();skillReady[0]=skillReady[1]=clock;
            cleared=false;cam.backgroundColor=Hex(tutorial?"243647":"A9CEC1");
            Decor(new Vector2(16,Floor-.5f),new Vector2(40,1),Hex(tutorial?"4D6372":"615642"),1);
            Decor(new Vector2(16,Floor+.02f),new Vector2(40,.12f),Hex(tutorial?"8CB1BC":"83B553"),2);
            // Distant canopy moves slowly relative to the camera.
            if(!tutorial)for(int i=0;i<13;i++)
            {
                var distant=Art("Distant canopy","tree",new Vector2(-8+i*4.2f,-.1f),8,-7);
                distant.color=Hex("89B9AD");scenery.Add(distant.gameObject);
                backdrops.Add(new Backdrop{view=distant.transform,home=distant.transform.position,factor=.24f});
            }
            for(int i=0;i<18;i++)
            {
                float x=-5+i*2.7f;float h=4+(float)rng.NextDouble()*3;
                if(tutorial) Decor(new Vector2(x,Floor+h*.5f),new Vector2(.25f,h),Hex("3C5263"),-3);
                else
                {
                    var tree=Art("Forest canopy","tree",new Vector2(x,Floor+h*.5f),h,-3);
                    tree.color=new Color(.7f,.85f,.72f,1);scenery.Add(tree.gameObject);
                    if(i%2==0)scenery.Add(Art("Wildflowers","bush",new Vector2(x,Floor+.35f),.8f,0).gameObject);
                }
            }
            platforms.Clear();
            int layout=tutorial?0:rng.Next(3);
            platforms.Add(new Rect(layout==0?7:layout==1?9:6,-1.25f,3,.18f));
            platforms.Add(new Rect(layout==0?22:layout==1?20:24,-.5f,3,.18f));
            foreach(var platform in platforms)Decor(new Vector2(platform.x,platform.y),new Vector2(platform.width,platform.height),Hex("6E8B54"),2);
            for(int i=0;i<40;i++)scenery.Add(Art("Grass tile","ground",new Vector2(-3+i,Floor-.48f),1,1).gameObject);
            gate=Art("First exit","gate",new Vector2(30,Floor+1.5f),3,3);
            secondGate=Art("Second exit","gate",new Vector2(34,Floor+1.5f),3,3);
            choiceA=rng.Next(3);choiceB=(choiceA+1+rng.Next(2))%3;
            secondGate.enabled=!(tutorial||room==0||room==4||room==5);
            gate.color=secondGate.color=new Color(.35f,.4f,.36f,1);
            if(tutorial)Spawn(17,0);
            else if(room==1){Spawn(11+rng.Next(3),1);Spawn(20,3);Spawn(26,rng.Next(2)==0?0:3);}
            else if(room==2){Spawn(17+rng.Next(3),4);Spawn(11,0);Spawn(26,1);}
            else if(room==3){Spawn(20,5);Spawn(12,3);Spawn(27,0);}
            else if(room==4){Spawn(14,4);Spawn(24,1);Spawn(28,3);}
            else if(room==6)Spawn(23,2);
            banner=tutorial?"W / SPACE jump. LMB attack. RMB + WASD dash. Q / E use gear.":RoomTitle;
            bannerUntil=clock+3;cameraPosition=new Vector3(7,-.35f,-10);cam.transform.position=cameraPosition;
        }
        void RoomCleared()
        {
            cleared=true;clearAt=clock;ClearShots();
            if(room>0&&!tutorial)feedback.ClearRoom();
            foreach(var h in hazards)Destroy(h.view.gameObject);hazards.Clear();
            if(tutorial){banner="You remember this place. Reach the exit and press F.";bannerUntil=clock+8;return;}
            if(room==0 || room==5)return;
            run.CompleteRoom();
            if(room==6){run.AwardBoss();SaveMeta();mode=Mode.Won;return;}
            if(routeKind==0)run.Offer(run.NewGear());
            else if(routeKind==1)run.AwardGold(18);
            else {runCharms+=1;RefreshStats();hp=Mathf.Min(maxHp,hp+2);}
            SaveMeta();
            feedback.Pickup(new Vector2(26,Floor+.8f));
            if(run.Pending!=null)lootView=Art("Rainbow gear / F to inspect","bow",new Vector2(26,Floor+.8f),.7f,5);
            banner=routeKind==0?"A Rainbow remains. Inspect with F, or leave to dismantle it automatically.":routeKind==1?"Gold collected. Choose your next path.":"Story item: Apple Charm. +1 max HP for this run.";
            bannerUntil=clock+5;
        }
        void Advance(int selected)
        {
            int scrapped=run.LeaveRoom();
            if(tutorial){tutorial=false;meta.tutorialDone=true;SaveMeta();room=0;}
            else room++;
            routeKind=selected;mode=Mode.Playing;BuildRoom();
            if(scrapped>0){banner="Unclaimed gear became "+scrapped+" shards.";bannerUntil=clock+4;}
        }
        void Spawn(float x, int type)
        {
            var f = new Foe { p = new Vector2(x,Floor+(type==2 ? 1.1f : (type==4||type==5)?.9f:.7f)), type = type,
                hp = type==2 ? 64 : type==4?14:type==5?22:type==1?8:4, phase = EnemyPhase.Approach, timer = 1 };
            f.max = f.hp;
            string[] kinds={"gingerbread","dwarf","beast","apple","house","dwarf"};
            f.body=Art(enemyNames[type],kinds[type],f.p,type==2?2.2f:type==4?1.8f:type==5?1.8f:1.4f,4);
            f.summon=clock+4;
            f.mark = Box("Attack telegraph",f.p,Vector2.one,Color.clear,3);
            foes.Add(f);
        }
        Color EnemyColor(Foe f) { return Color.white; }
        void Update()
        {
            feedback.Paused=mode!=Mode.Playing;
            if (mode == Mode.Dead || mode == Mode.Won)
            { if (Controls.Down(KeyCode.R)) NewRun(); SyncVisuals(); return; }
            if (Controls.Down(KeyCode.Escape))
            { if(mode==Mode.Playing)mode=Mode.Paused;else mode=Mode.Playing;attackInput.Clear();dashInput.Clear();jumpInput.Clear();foreach(var input in skillInput)input.Clear(); }
            if(mode==Mode.Loot){if(Controls.Down(KeyCode.Alpha1))Equip(0);else if(Controls.Down(KeyCode.Alpha2))Equip(1);else if(Controls.Down(KeyCode.B))Scrap();SyncVisuals();return;}
            if (mode != Mode.Playing) { SyncVisuals(); return; }
            // Collect input even during hit-stop. Buffers use simulation time.
            if(Controls.MouseHeld(0))attackInput.Press(clock);
            if(Controls.MouseDown(1))dashInput.Press(clock);
            if(Controls.Down(KeyCode.W)||Controls.Down(KeyCode.Space))jumpInput.Press(clock);
            if(Controls.Down(KeyCode.Q))skillInput[0].Press(clock);
            if(Controls.Down(KeyCode.E))skillInput[1].Press(clock);
            if(Time.unscaledTime<hitStopUntil){SyncVisuals();return;}
            float dt = Mathf.Min(Time.deltaTime,.033f); clock += dt;
            PlayerStep(dt);
            if (mode == Mode.Playing) EnemyStep(dt);
            if (mode == Mode.Playing) ShotStep(dt);
            for (int i=sparks.Count-1;i>=0;i--)
            {
                Spark s=sparks[i]; s.life-=dt; s.p+=s.v*dt; s.v+=Vector2.down*8*dt;
                if (s.life<=0) { Destroy(s.view.gameObject); sparks.RemoveAt(i); }
                else { s.view.transform.position=s.p; var c=s.view.color;c.a=s.life*2;s.view.color=c; }
            }
            shake = Mathf.Max(0,shake-dt*2);
            float halfWidth = cam.orthographicSize*cam.aspect;
            float targetX = Mathf.Clamp(pos.x+facing*1.7f,Left+halfWidth,Mathf.Max(Left+halfWidth,Right-halfWidth));
            Vector3 target = new Vector3(targetX,-.35f,-10);
            cameraPosition=Vector3.Lerp(cameraPosition,target,1-Mathf.Exp(-8*dt));
            cam.transform.position=cameraPosition+new Vector3(Mathf.Sin(clock*137),Mathf.Cos(clock*113),0)*shake*.12f;
            foreach(var back in backdrops)back.view.position=back.home+Vector3.right*(cameraPosition.x-7)*back.factor;
            if(foes.Count==0 && !cleared)RoomCleared();
            if(mode==Mode.Playing && cleared)
            {
                gate.color=RouteColor(choiceA);secondGate.color=RouteColor(choiceB);
                if(Controls.Down(KeyCode.B))mode=(room==0&&!tutorial)?Mode.Memory:Mode.Forge;
                if(Controls.Down(KeyCode.F))
                {
                    if(run.Pending!=null && Mathf.Abs(pos.x-26)<3)mode=Mode.Loot;
                    else if(NearbyGate()>=0)
                    { if(tutorial||room==0||room==4||room==5)Advance(0);else Advance(NearbyGate()==0?choiceA:choiceB); }
                    else if(room==0&&!tutorial)mode=Mode.Memory;
                    else mode=Mode.Forge;
                }
            }
            for(int i=hazards.Count-1;i>=0;i--)
            {
                var h=hazards[i];
                if(clock>=h.until){Destroy(h.view.gameObject);hazards.RemoveAt(i);}
                else if(Mathf.Abs(pos.x-h.p.x)<1.2f && pos.y<Floor+1.1f)Hurt(1,"Poison on the ground. Jump or dash clear.");
            }
            SyncVisuals();
        }
        Color RouteColor(int kind) { return Hex(kind==0?"AFD7EC":kind==1?"F0CE79":"C5E9AA"); }
        int NearbyGate()
        {
            if(!cleared || (run.Pending!=null && Mathf.Abs(pos.x-26)<3))return -1;
            if(Mathf.Abs(pos.x-30)<1.3f)return 0;
            return secondGate!=null && secondGate.enabled && Mathf.Abs(pos.x-34)<1.3f?1:-1;
        }
        void PlayerStep(float dt)
        {
            bool wasGrounded=grounded;
            float input=(Controls.Held(KeyCode.D)||Controls.Held(KeyCode.RightArrow)?1:0)-(Controls.Held(KeyCode.A)||Controls.Held(KeyCode.LeftArrow)?1:0);

            if(grounded){coyote=.1f;airJumpUsed=false;}else coyote-=dt;
            if(wasDashing && clock>=dashEnd) { velocity.y=0; wasDashing=false; }
            if(clock>=dashEnd)
            {
                if(input!=0) facing=input>0?1:-1;
                velocity.x=input*moveSpeed;
                if(jumpInput.Pending(clock) && (coyote>0||!airJumpUsed))
                {
                    bool airborne=coyote<=0;airJumpUsed=airborne;
                    jumpInput.Consume(clock);velocity.y=jumpSpeed*(airborne?.92f:1);grounded=false;coyote=0;
                    if(airborne)feedback.Land(pos-Vector2.up*.6f);
                }
                bool holdJump=Controls.Held(KeyCode.W)||Controls.Held(KeyCode.Space);
                velocity.y-=gravity*dt*(velocity.y<0?1.18f:!holdJump?2:1);
                if(dashInput.Pending(clock) && chain.TryStart(clock))
                {
                    dashInput.Consume(clock);
                    float up=(Controls.Held(KeyCode.W)||Controls.Held(KeyCode.UpArrow)?1:0)-(Controls.Held(KeyCode.S)||Controls.Held(KeyCode.DownArrow)?1:0);
                    dashDirection=new Vector2(input,up);
                    if(dashDirection.sqrMagnitude<.01f) dashDirection=new Vector2(facing,0);
                    dashDirection.Normalize(); dashEnd=clock+dashDuration;
                    feedback.Dash(pos,facing);trailAt=clock+.055f;
                }
            }
            if(Controls.Down(KeyCode.S) && grounded && pos.y-.65f>Floor+.1f)
            { pos.y-=.25f; velocity.y=-2; grounded=false; coyote=0; }
            wasDashing=clock<dashEnd;
            float oldBottom=pos.y-.65f;
            if(clock<dashEnd)
            {
                velocity=dashDirection*dashSpeed;
                if(clock>=trailAt){feedback.DashTrail(pos,facing);trailAt=clock+.055f;}
            }
            pos+=velocity*dt;
            grounded=false;
            if(velocity.y<=0)
            {
                Land(oldBottom,Floor,Left,Right);
                foreach(var platform in platforms)Land(oldBottom,platform.y+platform.height*.5f,platform.x-platform.width*.5f,platform.x+platform.width*.5f);
            }
            if(pos.y<Floor+.65f) { pos.y=Floor+.65f;velocity.y=0;grounded=true; }
            pos.x=Mathf.Clamp(pos.x,Left+.4f,Right-.4f); pos.y=Mathf.Min(pos.y,5);
            if(!wasGrounded&&grounded){feedback.Land(pos-Vector2.up*.65f);squashUntil=clock+.09f;airJumpUsed=false;}
            if(attackInput.Pending(clock) && clock>=dashEnd && combo.TryAttack(clock,attackCooldown))
            {
                attackInput.Consume(clock);nextAttack=combo.ReadyAt;slashUntil=clock+(combo.Step==2?.17f:.11f);feedback.Swing(combo.Step);
                for(int i=foes.Count-1;i>=0;i--)
                {
                    var f=foes[i];
                    if(!CombatRules.InAttack(pos.x,pos.y,facing,f.p.x,f.p.y,attackReach+f.Width*.5f)) continue;
                    HitFoe(i,combo.Damage(damage),true);
                }
            }
            for(int slot=0;slot<2;slot++)if(skillInput[slot].Pending(clock)&&clock>=skillReady[slot]){skillInput[slot].Consume(clock);UseGear(slot);}
        }
        void UseGear(int slot)
        {
            var g=run.Slots[slot];if(g==null || clock<skillReady[slot])return;
            skillReady[slot]=clock+g.Cooldown;feedback.Swing(1);
            int count=g.ProjectileCount;
            if(g.Variant==0)
                for(int i=0;i<count;i++)Fire(pos+new Vector2(facing*.6f,(i-(count-1)*.5f)*.3f),new Vector2(facing*17,0),true,g.Damage);
            else if(g.Variant==1)
            {
                float center=pos.x+facing*4;
                for(int i=0;i<5+g.Level*2;i++)Fire(new Vector2(center+(i-(4+g.Level*2)*.5f)*.85f,4.5f+(i%2)*.35f),new Vector2(0,-11),true,g.Damage);
            }
            else
                for(int i=0;i<3+g.Level;i++)Fire(pos+new Vector2(facing*.5f,.3f),new Vector2(facing*(6+i*1.8f),9+i*.5f),true,g.Damage,17);
        }
        void Equip(int slot)
        {run.Equip(slot);feedback.Pickup(pos);skillReady[slot]=Mathf.Max(skillReady[slot],clock+.3f);if(run.Pending==null)mode=Mode.Playing;}
        void Scrap()
        {run.ScrapPending();feedback.Pickup(pos);mode=Mode.Playing;if(lootView!=null)lootView.enabled=false;}
        void HitFoe(int index,int hitDamage,bool melee=false)
        {
            var f=foes[index];f.hp-=hitDamage;f.flash=.12f;
            feedback.Hit(f.p,hitDamage,melee&&combo.Step==2);shake=melee&&combo.Step==2?.38f:.18f;
            hitStopUntil=Mathf.Max(hitStopUntil,Time.unscaledTime+(melee?(combo.Step==2?.055f:.028f):.015f));
            if(f.type==0||f.type==1||f.type==3)
            {
                f.stun=melee?.15f:.07f;f.knockback=(pos.x<=f.p.x?1:-1)*(melee&&combo.Step==2?8:4);
                if(f.phase==EnemyPhase.Tell){f.phase=EnemyPhase.Approach;f.timer=.3f;}
            }
            if(f.hp<=0)
            {
                if(!tutorial&&!f.summoned){run.AwardKill(f.type==5);SaveMeta();}
                if(f.type==3)hazards.Add(new Hazard{p=new Vector2(f.p.x,Floor+.1f),until=clock+2.5f,view=Box("Poison",new Vector2(f.p.x,Floor+.1f),new Vector2(2.4f,.2f),Hex("B794CD"),3)});
                Destroy(f.body.gameObject);Destroy(f.mark.gameObject);foes.RemoveAt(index);
            }
        }
        void Land(float oldBottom,float surface,float min,float max)
        {
            if(oldBottom>=surface-.01f && pos.y-.65f<=surface && pos.x>min-.25f && pos.x<max+.25f)
            { pos.y=surface+.65f;velocity.y=0;grounded=true; }
        }
        void EnemyStep(float dt)
        {
            int originalCount=foes.Count;
            for(int k=originalCount-1;k>=0;k--)
            {
                var f=foes[k];f.flash-=dt;
                if(f.stun>0)
                {f.stun-=dt;f.p.x=Mathf.Clamp(f.p.x+f.knockback*dt,Left+1,Right-1);f.knockback=Mathf.MoveTowards(f.knockback,0,30*dt);continue;}
                f.timer-=dt;
                bool enraged=f.type==2 && f.hp<=f.max/2;
                float pace=enraged?1.35f:1;
                if(f.type==4)
                {
                    if(clock>=f.summon && foes.Count<10){Spawn(Mathf.Clamp(f.p.x-2,Left+1,Right-1),0);foes[foes.Count-1].summoned=true;f.summon=clock+4.5f;Burst(f.p,Hex("F6D49B"),5);}
                    continue;
                }
                if(f.phase==EnemyPhase.Approach)
                {
                    f.facing=pos.x>=f.p.x?1:-1;float distance=Mathf.Abs(pos.x-f.p.x);
                    float range=f.type==2?5:f.type==5?5.5f:2.4f;
                    if(distance>range)f.p.x+=f.facing*(f.type==0?3.1f:f.type==3?3.5f:2.1f)*dt*pace;
                    else if(f.timer<=0)
                    {f.phase=EnemyPhase.Tell;f.timer=(f.type==2?.85f:f.type==5?1.05f:.7f)/pace;f.hit=false;f.pattern=f.type==2?(f.pattern+1)%3:f.type==5?1:0;f.combo=f.type==2&&f.pattern==2?(enraged?2:1):0;}
                }
                else if(f.phase==EnemyPhase.Tell && f.timer<=0)
                {f.phase=EnemyPhase.Strike;f.timer=(f.type==2?.34f:.24f);}
                else if(f.phase==EnemyPhase.Strike)
                {
                    bool wave=(f.type==2&&f.pattern==1)||f.type==5;
                    if(wave)
                    {if(!f.hit && pos.y-.65f<Floor+.5f && Mathf.Abs(pos.x-f.p.x)<(f.type==2?7:5))f.hit=Hurt(2,"Ground wave: jump before the warning turns red.");}
                    else
                    {
                        float speed=f.type==2?(f.pattern==2?4:15):f.type==1?2:11;
                        f.p.x+=f.facing*speed*pace*dt;
                        float reach=f.type==2&&f.pattern==2?3:f.Width+.5f;
                        if(!f.hit && CombatRules.Overlap(pos.x,pos.y,.7f,1.3f,f.p.x+f.facing*(reach-f.Width)*.5f,f.p.y,reach,f.type==2?2.2f:1.4f))
                            f.hit=Hurt(f.type==2||f.type==5?2:1,"Lunge / claw strike: dash through or move out before it starts.");
                    }
                    if(f.timer<=0)
                    {
                        if(f.combo>0){f.combo--;f.phase=EnemyPhase.Tell;f.timer=.22f;f.hit=false;}
                        else {f.phase=EnemyPhase.Recover;f.timer=(f.type==2?1.1f:.85f)/pace;}
                    }
                }
                else if(f.phase==EnemyPhase.Recover && f.timer<=0){f.phase=EnemyPhase.Approach;f.timer=.3f/pace;}
                f.p.x=Mathf.Clamp(f.p.x,Left+1,Right-1);
                if(mode!=Mode.Playing)break;
            }
        }
        void Fire(Vector2 p,Vector2 v,bool friendly=false,int hitDamage=2,float projectileGravity=0)
        {
            shots.Add(new Shot { p=p, velocity=v, friendly=friendly, damage=hitDamage,gravity=projectileGravity, view=Box(friendly?"E ink bolt":"Echo projectile",p,new Vector2(.35f,.16f),Hex(friendly?"92D0EF":"F4B77F"),5) });
        }
        void ShotStep(float dt)
        {
            for(int i=shots.Count-1;i>=0;i--)
            {
                var s=shots[i];s.velocity+=Vector2.down*s.gravity*dt;s.p+=s.velocity*dt;s.life-=dt;s.view.transform.position=s.p;
                bool hit=false;
                if(s.friendly)
                {
                    for(int j=foes.Count-1;j>=0;j--)
                        if(CombatRules.Overlap(foes[j].p.x,foes[j].p.y,foes[j].Width,foes[j].type==2?2.2f:1.4f,s.p.x,s.p.y,.3f,.3f))
                        { HitFoe(j,s.damage);hit=true;break; }
                }
                else
                {
                    hit=CombatRules.Overlap(pos.x,pos.y,.7f,1.3f,s.p.x,s.p.y,.3f,.3f);
                    if(hit) Hurt(1,"An echo reached you. Watch its line, then dodge or use a platform.");
                }
                if(hit || s.life<=0 || s.p.y<Floor || s.p.x<Left-2 || s.p.x>Right+2)
                { Destroy(s.view.gameObject);shots.RemoveAt(i); }
            }
        }
        bool Hurt(int amount,string cause)
        {
            if(clock<dashEnd || clock<hurtUntil || mode!=Mode.Playing) return false;
            hp=Mathf.Max(0,hp-amount);hurtUntil=clock+.8f;shake=.6f;feedback.Hurt(pos);hitStopUntil=Time.unscaledTime+.065f;
            banner=cause;bannerUntil=clock+3;
            if(hp<=0)
            {
                mode=Mode.Dead;meta.deaths++;deaths=meta.deaths;SaveMeta();
            }
            return true;
        }
        void Burst(Vector2 p,Color color,int count)
        {
            for(int i=0;i<count;i++)
            {
                Vector2 v=new Vector2((float)rng.NextDouble()*8-4,(float)rng.NextDouble()*6);
                sparks.Add(new Spark {p=p,v=v,life=.5f,view=Box("Ink fragment",p,new Vector2(.09f,.09f),color,8)});
            }
        }
        void SyncVisuals()
        {
            string pose=clock<slashUntil?"hero_attack":!grounded?"hero_jump":Mathf.Abs(velocity.x)>.1f?(facing<0?"hero_left":"hero_right"):"hero_idle";
            hero.sprite=ForestArt.Get(pose);hero.flipX=facing<0 && pose!="hero_left";
            float scale=1.5f/hero.sprite.bounds.size.y;
            float squash=clock<squashUntil?.9f:1;
            hero.transform.localScale=new Vector3(scale/squash,scale*squash,1);
            float bob=grounded&&Mathf.Abs(velocity.x)>.1f?Mathf.Abs(Mathf.Sin(clock*18))*.035f:0;
            hero.transform.position=pos+Vector2.up*(.1f+bob);
            hero.color=clock<dashEnd?new Color(.6f,1,1,.65f):clock<hurtUntil&&((int)(clock*18)%2==0)?Hex("FF9B9B"):Color.white;
            if(lootView!=null){lootView.enabled=run.Pending!=null;lootView.transform.position=new Vector2(26,Floor+.9f+Mathf.Sin(clock*3)*.1f);}
            blade.enabled=clock<slashUntil;
            blade.transform.position=pos+new Vector2(facing*attackReach*.55f,.1f);
            blade.transform.localScale=new Vector3(attackReach,combo.Step==2?.38f:.18f,1);
            blade.transform.rotation=Quaternion.Euler(0,0,facing*(combo.Step==1?-20:20));
            blade.color=combo.Step==2?Hex("FFD386"):Hex("EFFFE4");
            foreach(var f in foes)
            {
                f.body.transform.position=f.p+(f.type==3?Vector2.up*Mathf.Abs(Mathf.Sin(clock*5))*.25f:Vector2.zero);f.body.flipX=f.facing<0;
                f.body.color=f.flash>0?Color.white:f.phase==EnemyPhase.Tell?Hex("FFD488"):f.phase==EnemyPhase.Strike?Hex("FF526E"):f.phase==EnemyPhase.Recover?Hex("AF91D1"):EnemyColor(f);
                f.mark.enabled=f.phase==EnemyPhase.Tell || f.phase==EnemyPhase.Strike;
                Color c=f.phase==EnemyPhase.Tell?Hex("F0BD71"):Hex("FF526E");c.a=f.phase==EnemyPhase.Tell?.22f+Mathf.PingPong(clock*2,.3f):.58f;f.mark.color=c;
                if((f.type==2 && f.pattern==1)||f.type==5)
                { f.mark.transform.position=new Vector2(f.p.x,Floor+.15f);f.mark.transform.localScale=new Vector3(f.type==2?14:10,.3f,1); }
                else
                { float length=f.type==2?(f.pattern==2?3:6):f.type==1?1.4f:3.5f;
                  f.mark.transform.position=f.p+new Vector2(f.facing*length*.5f,0);
                  f.mark.transform.localScale=new Vector3(length,.16f,1); }
            }
        }
        void OnDestroy()
        {
            if(pixel!=null) { Destroy(pixel.texture);Destroy(pixel); }
        }
    }
}
