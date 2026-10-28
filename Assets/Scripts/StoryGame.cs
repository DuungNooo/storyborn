using System;
using System.Collections.Generic;
using UnityEngine;

namespace Storyborn
{
    public sealed class StoryGame : MonoBehaviour
    {
        enum Mode { Title, Playing, Reward, Paused, Dead, Won }
        enum EnemyPhase { Approach, Tell, Strike, Recover }
        sealed class Foe
        {
            public Vector2 p;
            public int hp, max, type, facing = -1, pattern;
            public float timer, flash;
            public EnemyPhase phase;
            public SpriteRenderer body, mark;
            public bool hit;
            public float Width { get { return type == 2 ? 1.5f : .85f; } }
        }
        sealed class Shot
        {
            public Vector2 p, velocity;
            public float life = 4;
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
        public float attackCooldown = .3f;

        const float Floor = -3.4f;
        const float Left = -3;
        const float Right = 35;
        Mode mode;
        readonly List<Foe> foes = new List<Foe>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Spark> sparks = new List<Spark>();
        readonly List<GameObject> scenery = new List<GameObject>();
        readonly DashChain chain = new DashChain();
        Camera cam;
        Sprite pixel;
        SpriteRenderer hero, blade, gate;
        Vector2 pos, velocity, dashDirection;
        int facing = 1, hp, maxHp, damage, room, seed, deaths;
        float clock, dashEnd, nextAttack, hurtUntil, slashUntil, coyote, jumpBuffer;
        float shake, bannerUntil;
        bool grounded, rewardTaken, wasDashing;
        int[] offers = new int[3];
        System.Random rng;
        Font uiFont;
        GUIStyle titleStyle, labelStyle, smallStyle, buttonStyle;
        string banner = "";
        readonly string[] roomNames = { "01 / THE UNWRITTEN GATE", "02 / FRACTURED ARCHIVE", "03 / THE LAST WITNESS" };
        readonly string[] rewardNames = { "SHARPENED WORD", "LASTING MEMORY", "LIGHT FOOTSTEPS", "LONG SENTENCE", "SECOND BREATH" };
        readonly string[] rewardText = { "Attack damage +1", "Max health +2; recover 2", "Movement speed +12%", "Attack reach +20%", "Recover all health" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindObjectOfType<StoryGame>() == null) new GameObject("Storyborn — Runtime").AddComponent<StoryGame>();
        }
        void Awake()
        {
            Application.targetFrameRate = 120;
            deaths = PlayerPrefs.GetInt("Storyborn.Deaths", 0);
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white); tex.Apply(); tex.filterMode = FilterMode.Point;
            pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one*.5f, 1);
            cam = Camera.main;
            if (cam == null) cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 6.1f;
            cam.backgroundColor = Hex("10141F"); cam.transform.position = new Vector3(8, 0, -10);
            if (FindObjectOfType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hero = Box("Protagonist (placeholder)", Vector2.zero, new Vector2(.7f, 1.3f), Hex("83F4DB"), 6);
            blade = Box("Attack arc", Vector2.zero, Vector2.one, Hex("FFF0B1"), 7);
            NewRun(); mode = Mode.Title;
        }
        SpriteRenderer Box(string name, Vector2 p, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = pixel; sr.color = color;
            sr.sortingOrder = order; go.transform.position = p; go.transform.localScale = size;
            return sr;
        }
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
            maxHp = hp = 8; damage = 1; room = 0; moveSpeed = 7.5f; attackReach = 2.1f;
            clock = 0; dashEnd = nextAttack = hurtUntil = slashUntil = 0;
            chain.Limit = dashChainLimit; chain.Window = dashChainWindow; chain.LockDuration = dashLockDuration;
            ResetChain();
            BuildRoom(); mode = Mode.Playing;
        }
        void ResetChain() { chain.Reset(); }
        void ClearShots()
        {
            foreach (var s in shots) Destroy(s.view.gameObject); shots.Clear();
        }
        void BuildRoom()
        {
            foreach (var f in foes) { Destroy(f.body.gameObject); Destroy(f.mark.gameObject); } foes.Clear();
            foreach (var s in sparks) Destroy(s.view.gameObject); sparks.Clear();
            foreach (var g in scenery) Destroy(g); scenery.Clear(); ClearShots();
            if (gate != null) Destroy(gate.gameObject);
            pos = new Vector2(0, Floor+.65f); velocity = Vector2.zero; facing = 1;
            grounded = true; wasDashing = false; coyote = .1f; jumpBuffer = 0; dashEnd = slashUntil = hurtUntil = nextAttack = clock;
            ResetChain(); rewardTaken = false;
            Decor(new Vector2(16, Floor-.4f), new Vector2(40,.8f), Hex("333D50"), 1);
            Decor(new Vector2(16, Floor+.02f), new Vector2(40,.06f), Hex("7F979F"), 2);
            for (int i = 0; i < 24; i++)
            {
                float x = -5+i*2;
                float h = 2+(float)rng.NextDouble()*4;
                Decor(new Vector2(x,Floor+h*.5f), new Vector2(.9f,h), Hex(i%2==0 ? "1A2232" : "20283A"), -3);
                Decor(new Vector2(x,Floor+h+.1f), new Vector2(1.25f,.16f), Hex("30384B"), -2);
            }
            for (int i = 0; i < 38; i++)
                Decor(new Vector2(-4+(float)rng.NextDouble()*44, -2+(float)rng.NextDouble()*7), new Vector2(.04f,.04f), Hex("526578"), -1);
            // Platforms are one-way surfaces: jump through, land from above.
            Decor(new Vector2(7,-1.25f), new Vector2(3,.18f), Hex("44526A"), 2);
            Decor(new Vector2(22,-.5f), new Vector2(3,.18f), Hex("44526A"), 2);
            gate = Box("Exit — E when clear", new Vector2(33, Floor+1.5f), new Vector2(.4f,3), Hex("79536B"), 3);
            if (room == 0) { Spawn(11,0); Spawn(23,0); }
            if (room == 1) { Spawn(10,1); Spawn(19,0); Spawn(27,1); }
            if (room == 2) Spawn(24,2);
            banner = roomNames[room]; bannerUntil = clock+3;
            cam.transform.position = new Vector3(7,0,-10);
        }
        void Spawn(float x, int type)
        {
            var f = new Foe { p = new Vector2(x,Floor+(type==2 ? 1.1f : .7f)), type = type,
                hp = type==2 ? 26 : type==1 ? 4 : 5, phase = EnemyPhase.Approach, timer = 1 };
            f.max = f.hp;
            f.body = Box(type==2 ? "Witness (test boss)" : type==1 ? "Echo (test ranged enemy)" : "Fragment (test melee enemy)", f.p,
                new Vector2(f.Width,type==2 ? 2.2f : 1.4f), EnemyColor(f), 4);
            f.mark = Box("Attack telegraph",f.p,Vector2.one,Color.clear,3);
            foes.Add(f);
        }
        Color EnemyColor(Foe f) { return Hex(f.type==2 ? "C293E5" : f.type==1 ? "E9B76A" : "D78689"); }
        void Update()
        {
            if (mode == Mode.Title) { if (Input.GetKeyDown(KeyCode.Return)) mode=Mode.Playing; SyncVisuals(); return; }
            if (mode == Mode.Dead || mode == Mode.Won)
            { if (Input.GetKeyDown(KeyCode.R)) NewRun(); SyncVisuals(); return; }
            if (Input.GetKeyDown(KeyCode.Escape))
            { if (mode==Mode.Playing) mode=Mode.Paused; else if(mode==Mode.Paused) mode=Mode.Playing; }
            if (mode == Mode.Reward)
            {
                for (int i=0;i<3;i++) if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))) TakeReward(i);
                SyncVisuals(); return;
            }
            if (mode != Mode.Playing) { SyncVisuals(); return; }
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
            Vector3 target = new Vector3(targetX,0,-10);
            cam.transform.position = Vector3.Lerp(cam.transform.position,target,1-Mathf.Exp(-7*dt));
            if (shake>0) cam.transform.position += new Vector3(Mathf.Sin(clock*137),Mathf.Cos(clock*113),0)*shake*.15f;
            if(foes.Count==0)
            {
                gate.color=Hex("83F4DB");
                if (!rewardTaken && room<2) { RollRewards(); mode=Mode.Reward; }
                else if (Mathf.Abs(pos.x-33)<1.8f && Input.GetKeyDown(KeyCode.E))
                { if(room==2) mode=Mode.Won; else { room++; BuildRoom(); } }
            }
            SyncVisuals();
        }
        void PlayerStep(float dt)
        {
            float input=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
            if(Input.GetKeyDown(KeyCode.Space)) jumpBuffer=.12f; else jumpBuffer-=dt;
            if(grounded) coyote=.1f; else coyote-=dt;
            if(wasDashing && clock>=dashEnd) { velocity.y=0; wasDashing=false; }
            if(clock>=dashEnd)
            {
                if(input!=0) facing=input>0?1:-1;
                velocity.x=input*moveSpeed;
                if(jumpBuffer>0 && coyote>0) { velocity.y=jumpSpeed;grounded=false;coyote=0;jumpBuffer=0; }
                velocity.y-=gravity*dt;
                if((Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.K)) && chain.TryStart(clock))
                {
                    float up=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0);
                    dashDirection=new Vector2(input,up);
                    if(dashDirection.sqrMagnitude<.01f) dashDirection=new Vector2(facing,0);
                    dashDirection.Normalize(); dashEnd=clock+dashDuration;
                    Burst(pos,Hex("83F4DB"),5);
                }
            }
            wasDashing=clock<dashEnd;
            float oldBottom=pos.y-.65f;
            if(clock<dashEnd) { velocity=dashDirection*dashSpeed; }
            pos+=velocity*dt;
            grounded=false;
            if(velocity.y<=0)
            {
                Land(oldBottom,Floor,Left,Right);
                Land(oldBottom,-1.16f,5.5f,8.5f);
                Land(oldBottom,-.41f,20.5f,23.5f);
            }
            if(pos.y<Floor+.65f) { pos.y=Floor+.65f;velocity.y=0;grounded=true; }
            pos.x=Mathf.Clamp(pos.x,Left+.4f,Right-.4f); pos.y=Mathf.Min(pos.y,5);
            if(clock>=nextAttack && (Input.GetKeyDown(KeyCode.J)||Input.GetMouseButtonDown(0)))
            {
                nextAttack=clock+attackCooldown; slashUntil=clock+.12f;
                for(int i=foes.Count-1;i>=0;i--)
                {
                    var f=foes[i];
                    if(!CombatRules.InAttack(pos.x,pos.y,facing,f.p.x,f.p.y,attackReach+f.Width*.5f)) continue;
                    f.hp-=damage; f.flash=.12f; Burst(f.p,Hex("FFF0B1"),7); shake=.2f;
                    if(f.hp<=0) { Destroy(f.body.gameObject);Destroy(f.mark.gameObject);foes.RemoveAt(i); }
                }
            }
        }
        void Land(float oldBottom,float surface,float min,float max)
        {
            if(oldBottom>=surface-.01f && pos.y-.65f<=surface && pos.x>min-.25f && pos.x<max+.25f)
            { pos.y=surface+.65f;velocity.y=0;grounded=true; }
        }
        void EnemyStep(float dt)
        {
            foreach(var f in foes)
            {
                f.flash-=dt; f.timer-=dt;
                if(f.phase==EnemyPhase.Approach)
                {
                    f.facing=pos.x>=f.p.x?1:-1;
                    float distance=Mathf.Abs(pos.x-f.p.x);
                    float range=f.type==1?9:f.type==2?5:2.8f;
                    if(distance>range) f.p.x+=f.facing*(f.type==2?2.3f:2)*dt;
                    else if(f.timer<=0)
                    {
                        f.phase=EnemyPhase.Tell;f.timer=f.type==2?.85f:.7f;f.hit=false;
                        f.pattern=f.type==2?(f.pattern+1)%3:0;
                    }
                }
                else if(f.phase==EnemyPhase.Tell && f.timer<=0)
                {
                    f.phase=EnemyPhase.Strike;f.timer=f.type==2?.34f:.22f;
                    if(f.type==1) Fire(f.p,new Vector2(f.facing*9,0));
                    if(f.type==2 && f.pattern==2)
                    { Fire(f.p,new Vector2(f.facing*10,0));Fire(f.p,new Vector2(f.facing*8,4));Fire(f.p,new Vector2(f.facing*8,-4)); }
                }
                else if(f.phase==EnemyPhase.Strike)
                {
                    if(f.type==0 || (f.type==2 && f.pattern==0))
                    {
                        f.p.x+=f.facing*(f.type==2?16:12)*dt;
                        if(!f.hit && CombatRules.Overlap(pos.x,pos.y,.7f,1.3f,f.p.x,f.p.y,f.Width+.5f,f.type==2?2.2f:1.4f))
                            f.hit=Hurt(f.type==2?2:1,"You were caught in the lunge. Dash through, or jump away.");
                    }
                    if(f.type==2 && f.pattern==1 && !f.hit && pos.y-.65f<Floor+.5f && Mathf.Abs(pos.x-f.p.x)<7)
                        f.hit=Hurt(2,"The ground wave hits low. Jump before it turns red.");
                    if(f.timer<=0) { f.phase=EnemyPhase.Recover;f.timer=f.type==2?1.05f:.85f; }
                }
                else if(f.phase==EnemyPhase.Recover && f.timer<=0)
                { f.phase=EnemyPhase.Approach;f.timer=.3f; }
                f.p.x=Mathf.Clamp(f.p.x,Left+1,Right-1);
                if(mode!=Mode.Playing) break;
            }
        }
        void Fire(Vector2 p,Vector2 v)
        {
            shots.Add(new Shot { p=p, velocity=v, view=Box("Echo projectile",p,new Vector2(.3f,.3f),Hex("F4B77F"),5) });
        }
        void ShotStep(float dt)
        {
            for(int i=shots.Count-1;i>=0;i--)
            {
                var s=shots[i];s.p+=s.velocity*dt;s.life-=dt;s.view.transform.position=s.p;
                bool hit=CombatRules.Overlap(pos.x,pos.y,.7f,1.3f,s.p.x,s.p.y,.3f,.3f);
                if(hit) Hurt(1,"An echo reached you. Watch its line, then dodge or use a platform.");
                if(hit || s.life<=0 || s.p.y<Floor || s.p.x<Left-2 || s.p.x>Right+2)
                { Destroy(s.view.gameObject);shots.RemoveAt(i); }
            }
        }
        bool Hurt(int amount,string cause)
        {
            if(clock<dashEnd || clock<hurtUntil || mode!=Mode.Playing) return false;
            hp=Mathf.Max(0,hp-amount);hurtUntil=clock+.8f;shake=.6f;Burst(pos,Hex("F08794"),12);
            banner=cause;bannerUntil=clock+3;
            if(hp<=0)
            {
                mode=Mode.Dead;deaths++;PlayerPrefs.SetInt("Storyborn.Deaths",deaths);PlayerPrefs.Save();
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
        void RollRewards()
        {
            var bag=new List<int>{0,1,2,3,4};
            for(int i=0;i<3;i++) { int n=rng.Next(bag.Count);offers[i]=bag[n];bag.RemoveAt(n); }
            ClearShots(); banner="The room is quiet. Choose what you remember."; bannerUntil=clock+3;
        }
        void TakeReward(int index)
        {
            int id=offers[index];
            if(id==0) damage++;
            if(id==1) { maxHp+=2;hp=Mathf.Min(maxHp,hp+2); }
            if(id==2) moveSpeed*=1.12f;
            if(id==3) attackReach*=1.2f;
            if(id==4) hp=maxHp;
            rewardTaken=true;mode=Mode.Playing;banner="Memory kept. Reach the gate and press E.";bannerUntil=clock+4;
        }
        void SyncVisuals()
        {
            hero.transform.position=pos;
            hero.color=clock<dashEnd?Hex("FFFFFF") : clock<hurtUntil && ((int)(clock*18)%2==0)?Hex("ED8196"):Hex("83F4DB");
            hero.transform.localScale=new Vector3(clock<dashEnd?1.1f:.7f,clock<dashEnd?.8f:1.3f,1);
            blade.enabled=clock<slashUntil;
            blade.transform.position=pos+new Vector2(facing*attackReach*.55f,.1f);
            blade.transform.localScale=new Vector3(attackReach,.24f,1);
            foreach(var f in foes)
            {
                f.body.transform.position=f.p;
                f.body.color=f.flash>0?Color.white:f.phase==EnemyPhase.Tell?Hex("FFD488"):f.phase==EnemyPhase.Strike?Hex("FF526E"):f.phase==EnemyPhase.Recover?Hex("AF91D1"):EnemyColor(f);
                f.mark.enabled=f.phase==EnemyPhase.Tell || f.phase==EnemyPhase.Strike;
                Color c=f.phase==EnemyPhase.Tell?Hex("F0BD71"):Hex("FF526E");c.a=.35f;f.mark.color=c;
                if(f.type==2 && f.pattern==1)
                { f.mark.transform.position=new Vector2(f.p.x,Floor+.15f);f.mark.transform.localScale=new Vector3(14,.3f,1); }
                else
                { float length=f.type==1 || (f.type==2&&f.pattern==2)?10:f.type==2?6:3.5f;
                  f.mark.transform.position=f.p+new Vector2(f.facing*length*.5f,0);
                  f.mark.transform.localScale=new Vector3(length,.16f,1); }
            }
        }
        void InitGUI()
        {
            if(titleStyle!=null) return;
            titleStyle=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=48,fontStyle=FontStyle.Bold}; titleStyle.normal.textColor=Hex("E5ECEF");
            labelStyle=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=20,wordWrap=true};labelStyle.normal.textColor=Hex("D0DCE4");
            smallStyle=new GUIStyle(labelStyle){fontSize=14};
            buttonStyle=new GUIStyle(GUI.skin.button){font=uiFont,fontSize=18,wordWrap=true};
            buttonStyle.normal.textColor=Hex("C2F7E9");
        }
        void Fill(Rect rect,Color color) { Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old; }
        void Label(float x,float y,float w,float h,string text,GUIStyle style=null) { GUI.Label(new Rect(x,y,w,h),text,style??labelStyle); }
        bool Button(float x,float y,float w,float h,string text) { return GUI.Button(new Rect(x,y,w,h),text,buttonStyle); }
        void Overlay()
        { Fill(new Rect(0,0,1280,720),new Color(.03f,.05f,.09f,.94f)); }
        void OnGUI()
        {
            InitGUI();GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            if(mode==Mode.Title)
            {
                Overlay();Label(80,60,600,30,"STORYBORN / COMBAT PROTOTYPE 0.1",smallStyle);
                Label(80,128,1000,90,"A story that refuses to fade.",titleStyle);
                Label(83,235,860,80,"Civilization fell. Its stories survived.\nRead the warning. Choose your position. Write the next line.");
                Label(83,350,900,95,"A / D  Move       SPACE  Jump       J / Left click  Attack\nSHIFT / K  Dash  ·  Hold a direction for an aerial dash\nTwo quick dashes trigger a 0.5s lock. No parry.");
                if(Button(83,500,340,62,"ENTER  /  BEGIN A NEW STORY")) mode=Mode.Playing;
                Label(83,610,900,50,"Placeholder shapes only. Weapons, enemies and upgrades are prototype choices.",smallStyle);return;
            }
            Fill(new Rect(0,0,1280,96),new Color(.035f,.05f,.08f,.93f));
            Label(28,16,410,25,"STORYBORN  /  "+roomNames[room],smallStyle);
            Fill(new Rect(28,52,240,13),Hex("3B2C3D"));Fill(new Rect(28,52,240*(float)hp/maxHp,13),Hex("83F4DB"));
            Label(280,44,180,30,hp+" / "+maxHp+"   HP",smallStyle);
            float lockLeft=chain.Remaining(clock);
            Label(450,20,420,28,lockLeft>0?"DASH LOCK  "+lockLeft.ToString("0.00")+"s":"DASH READY  /  "+(dashChainLimit-chain.Count)+" in chain",smallStyle);
            Label(450,49,420,25,"ATK "+damage+"   /   ENEMIES "+foes.Count+"   /   SEED "+seed,smallStyle);
            Label(1000,20,260,50,"ESC  Pause\nAttempt "+(deaths+1),smallStyle);
            Fill(new Rect(0,672,1280,48),new Color(.035f,.05f,.08f,.94f));
            Label(28,682,1200,30,"A/D  Move     SPACE  Jump     J  Attack     SHIFT/K  Dash     E  Gate     |     Amber = warning  ·  Red = active  ·  Purple = recovery",smallStyle);
            if(clock<bannerUntil) Label(250,112,850,60,banner);
            if(foes.Count==0 && mode==Mode.Playing) Label(450,580,600,50,"ROOM CLEAR  /  Reach the gate and press E");
            foreach(var f in foes)
            {
                Vector3 v=cam.WorldToViewportPoint(f.p+Vector2.up*(f.type==2?1.5f:1.1f));
                if(v.x<0 || v.x>1) continue;
                float x=v.x*1280-35,y=(1-v.y)*720;
                Fill(new Rect(x,y,70,5),Hex("382B3F"));Fill(new Rect(x,y,70*(float)f.hp/f.max,5),Hex("E5B4E6"));
                if(f.type==2) Label(x-35,y-28,220,30,"THE LAST WITNESS",smallStyle);
            }
            if(mode==Mode.Reward)
            {
                Overlay();Label(80,100,1100,70,"Choose a memory.",titleStyle);
                Label(83,190,1000,60,"One fragment survives with you, until this run ends.");
                for(int i=0;i<3;i++)
                {
                    float x=83+i*375; Fill(new Rect(x,290,345,240),Hex("202C3B"));
                    Label(x+24,320,300,30,"0"+(i+1)+"  /  "+rewardNames[offers[i]],smallStyle);
                    Label(x+24,375,290,65,rewardText[offers[i]]);
                    if(Button(x+24,460,297,45,"KEEP  ["+(i+1)+"]")) TakeReward(i);
                }
            }
            if(mode==Mode.Paused)
            {
                Overlay();Label(420,180,600,80,"A moment of silence.",titleStyle);
                if(Button(420,320,440,60,"RESUME  /  ESC")) mode=Mode.Playing;
                if(Button(420,410,440,60,"ABANDON RUN & RESTART")) NewRun();
            }
            if(mode==Mode.Dead || mode==Mode.Won)
            {
                Overlay();Label(85,150,1120,100,mode==Mode.Dead?"This telling has ended.":"Someone will remember.",titleStyle);
                Label(88,280,1020,100,mode==Mode.Dead?banner:"You crossed the archive and defeated the last witness.\nThe next story is still unwritten.");
                Label(88,410,1000,50,"Room "+(room+1)+" / 3   ·   Run time "+clock.ToString("0.0")+"s   ·   Seed "+seed,smallStyle);
                if(Button(88,510,410,65,"R  /  WRITE ANOTHER STORY")) NewRun();
            }
        }
        void OnDestroy()
        {
            if(pixel!=null) { Destroy(pixel.texture);Destroy(pixel); }
        }
    }
}
