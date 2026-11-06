using UnityEngine;
namespace Storyborn
{
    public sealed partial class StoryGame
    {
        bool showHelp=true;
        GUIStyle hudSmall, hudValue, centered, centeredSmall, cardTitle;
        static readonly Color PanelInk=new Color(.055f,.074f,.10f,.96f);
        static readonly Color PanelSoft=new Color(.085f,.11f,.14f,.95f);
        static readonly Color Trim=new Color(.62f,.54f,.37f,1);
        static readonly Color Mint=new Color(.57f,.85f,.70f,1);
        static readonly Color Paper=new Color(.91f,.88f,.78f,1);
        bool uiWasClear;
        int uiLastRoom=-99;
        float uiClearStamp=-100;

        void InitGUI()
        {
            if(titleStyle!=null)return;
            titleStyle=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=34,fontStyle=FontStyle.Bold,wordWrap=true};titleStyle.normal.textColor=Paper;
            labelStyle=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=18,wordWrap=true};labelStyle.normal.textColor=Hex("D6DDD9");
            smallStyle=new GUIStyle(labelStyle){fontSize=14};
            hudSmall=new GUIStyle(smallStyle){fontSize=12};hudSmall.normal.textColor=Hex("A3B6B4");
            hudValue=new GUIStyle(labelStyle){fontSize=20,fontStyle=FontStyle.Bold};hudValue.normal.textColor=Paper;
            centered=new GUIStyle(hudValue){alignment=TextAnchor.MiddleCenter};
            centeredSmall=new GUIStyle(smallStyle){alignment=TextAnchor.MiddleCenter};
            cardTitle=new GUIStyle(labelStyle){fontSize=20,fontStyle=FontStyle.Bold};cardTitle.normal.textColor=Paper;
            buttonStyle=new GUIStyle(GUI.skin.button){font=uiFont,fontSize=16,fontStyle=FontStyle.Bold,wordWrap=true,alignment=TextAnchor.MiddleCenter};
            buttonStyle.normal.background=null;buttonStyle.hover.background=null;buttonStyle.active.background=null;
            buttonStyle.focused.background=null;buttonStyle.normal.textColor=Paper;buttonStyle.hover.textColor=Color.white;buttonStyle.active.textColor=Mint;
        }
        void Fill(Rect rect,Color color)
        {Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        void Panel(Rect r,Color accent,float thickness=2)
        {
            Fill(r,accent);Fill(new Rect(r.x+thickness,r.y+thickness,r.width-thickness*2,r.height-thickness*2),PanelInk);
            Fill(new Rect(r.x+7,r.y+7,r.width-14,1),new Color(accent.r,accent.g,accent.b,.22f));
        }
        void Label(float x,float y,float w,float h,string text,GUIStyle style=null)
        {GUI.Label(new Rect(x,y,w,h),text,style??labelStyle);}
        bool Button(float x,float y,float w,float h,string text)
        {
            Rect r=new Rect(x,y,w,h);Panel(r,GUI.enabled?Trim:Hex("394247"));
            return GUI.Button(r,text,buttonStyle);
        }
        bool ActionButton(float x,float y,float w,float h,string text,bool available=true)
        {
            bool previous=GUI.enabled;GUI.enabled=previous&&available;
            bool clicked=Button(x,y,w,h,text);GUI.enabled=previous;return clicked;
        }
        void Overlay(){Fill(new Rect(0,0,1280,720),new Color(.015f,.025f,.035f,.72f));}
        void Icon(string kind,Rect rect,Color tint)
        {
            Sprite sprite=ForestArt.Get(kind);if(sprite==null)return;
            Color previous=GUI.color;GUI.color=tint;
            Rect t=sprite.textureRect;Texture tex=sprite.texture;
            GUI.DrawTextureWithTexCoords(rect,tex,new Rect(t.x/tex.width,t.y/tex.height,t.width/tex.width,t.height/tex.height));
            GUI.color=previous;
        }
        string GearText(int slot)
        {var g=run.Slots[slot];return g==null?"EMPTY SLOT":g.SkillName+"  +"+g.Level;}
        string VariantDescription(Gear g)
        {
            if(g==null)return "Find a story weapon to fill this slot.";
            return g.Variant==0?"A quick, straight arrow. Extra arrows join the shot as it grows.":g.Variant==1?"Arrows fall over the space ahead. Upgrades widen the rain.":"A spreading volley follows a curved path. More arrows with each level.";
        }
        void KeyBadge(float x,float y,string key,Color accent)
        {Panel(new Rect(x,y,28,26),accent,1);Label(x,y,28,26,key,centeredSmall);}
        void Currency(float y,string label,int value,Color color,int symbol)
        {
            Rect r=new Rect(1064,y,192,29);Fill(r,new Color(.04f,.07f,.085f,.87f));
            if(symbol==0){Fill(new Rect(1074,y+8,12,12),color);Fill(new Rect(1078,y+5,4,18),color);}
            else if(symbol==1){Fill(new Rect(1078,y+5,5,18),color);Fill(new Rect(1075,y+9,11,10),color);}
            else {Fill(new Rect(1074,y+7,14,14),color);Fill(new Rect(1078,y+11,6,6),PanelInk);}
            Label(1096,y+2,90,25,label,hudSmall);Label(1188,y,58,27,value.ToString(),hudValue);
        }
        void SkillHUD(int slot,float x)
        {
            var g=run.Slots[slot];float remaining=Mathf.Max(0,skillReady[slot]-clock);
            Rect r=new Rect(x,620,176,80);Color accent=g==null?Hex("47535C"):remaining>0?Hex("667483"):Mint;
            Panel(r,accent);Icon("bow",new Rect(x+11,632,48,48),g==null?new Color(.3f,.34f,.35f,1):Color.white);
            if(g!=null && remaining>0)
            {
                float ratio=Mathf.Clamp01(remaining/g.Cooldown);
                Fill(new Rect(x+8,628,56,64*ratio),new Color(.02f,.03f,.05f,.8f));
                Label(x+8,636,56,35,remaining.ToString("0.0"),centered);
            }
            KeyBadge(x+7,610,slot==0?"Q":"E",accent);
            Label(x+69,631,102,19,g==null?"EMPTY":"RAINBOW +"+g.Level,hudSmall);
            Label(x+69,651,99,38,g==null?"Find gear":g.SkillName,smallStyle);
            if(g!=null && remaining<=0)Fill(new Rect(x+8,693,160,2),Mint);
        }
        void DrawHUD()
        {
            // Leave the middle of the arena open; health and skills sit below combat.
            Panel(new Rect(24,620,300,80),Trim);
            Icon("hero_idle",new Rect(34,628,44,64),Color.white);
            Label(89,631,145,24,"THE UNWRITTEN",hudSmall);
            Label(229,627,85,27,hp+" / "+maxHp,hudValue);
            Fill(new Rect(89,657,220,15),Hex("3D2B35"));
            Fill(new Rect(89,657,220*Mathf.Clamp01((float)hp/maxHp),15),hp<=maxHp*.25f?Hex("DB6D6D"):Mint);
            Fill(new Rect(89,657,220*Mathf.Clamp01((float)hp/maxHp),3),Hex("D4E6CE"));
            Label(89,678,120,18,"ATK "+damage,hudSmall);
            float locked=chain.Remaining(clock);
            Label(190,676,119,20,locked>0?"DASH "+locked.ToString("0.0")+"s":"DASH READY",hudSmall);
            SkillHUD(0,346);SkillHUD(1,534);
            Label(1010,681,246,23,"ESC  /  PAUSE & CONTROLS",hudSmall);
            Currency(18,"GOLD",run.Gold,Hex("E4BB67"),0);
            Currency(50,"SHARDS",run.Shards,Hex("91CDDF"),1);
            Currency(82,"MEMORY",run.Memory,Hex("C2A7E9"),2);
            Panel(new Rect(405,18,470,45),Trim,1);Label(416,22,448,30,RoomTitle,centeredSmall);
            for(int i=0;i<7;i++)
            {
                float x=535+i*31;Color color=i==room?Paper:i<room?Mint:Hex("52605F");
                Fill(new Rect(x,72,i==room?12:8,i==room?12:8),color);
                if(i<6)Fill(new Rect(x+14,76,14,1),Hex("52605F"));
            }
            Label(24,22,260,25,"STORYBORN",cardTitle);
            Label(25,49,230,20,"FOREST / TELLING "+(deaths+1),hudSmall);
            Foe boss=null;
            foreach(var f in foes)
            {
                if(f.type==2){boss=f;continue;}
                if(f.hp>=f.max && f.flash<=0 && f.type!=5)continue;
                Vector3 v=cam.WorldToViewportPoint(f.p+Vector2.up*(f.type==5?1.25f:1.0f));if(v.x<0||v.x>1)continue;
                float x=v.x*1280-28,y=(1-v.y)*720;
                Fill(new Rect(x-1,y-1,58,6),PanelInk);Fill(new Rect(x,y,56*(float)f.hp/f.max,4),f.type==5?Hex("D8AA69"):Hex("D5827E"));
            }
            if(boss!=null)
            {
                Label(360,103,560,25,boss.hp<=boss.max/2?"THE BEAST  /  ENRAGED":"THE BEAST",centeredSmall);
                Fill(new Rect(338,132,604,14),Trim);Fill(new Rect(340,134,600,10),Hex("3A2631"));
                Fill(new Rect(340,134,600*(float)boss.hp/boss.max,10),Hex("C57C79"));
                Label(947,126,80,26,boss.hp+" / "+boss.max,hudSmall);
            }
            if(clock<bannerUntil && mode==Mode.Playing)
            {
                float y=boss==null?113:162;
                Panel(new Rect(252,y,776,52),Hex("516760"),1);
                Label(269,y+7,742,39,banner,centeredSmall);
            }
        }
        void WorldTag(Vector2 world,string title,string subtitle,bool selected)
        {
            Vector3 v=cam.WorldToViewportPoint(world);if(v.x<-.08f||v.x>1.08f)return;
            float x=Mathf.Clamp(v.x*1280-69,14,1128),y=Mathf.Clamp((1-v.y)*720,170,500);
            Panel(new Rect(x,y,138,53),selected?Paper:Trim,1);
            Label(x+4,y+4,130,22,title,centeredSmall);
            Label(x+4,y+26,130,20,subtitle,hudSmall);
        }
        void ContextHints()
        {
            if(!cleared || mode!=Mode.Playing)return;
            bool single=tutorial||room==0||room==4||room==5;
            int nearbyGate=NearbyGate();
            string destination=tutorial?"RETURNING PATH":room==0?"ENTER FOREST":room==4?"RESTING GLADE":room==5?"THE BEAST":"";
            if(single)WorldTag(new Vector2(30,Floor+3.2f),destination,"F  ENTER",nearbyGate==0);
            else
            {
                WorldTag(new Vector2(30,Floor+3.2f),routes[choiceA],"F  "+(room==2?"ELITE PATH":"BATTLE PATH"),nearbyGate==0);
                WorldTag(new Vector2(34,Floor+3.2f),routes[choiceB],"F  "+(room==2?"ELITE PATH":"BATTLE PATH"),nearbyGate==1);
            }
            if(run.Pending!=null)WorldTag(new Vector2(26,Floor+2),"RAINBOW +"+run.Pending.Level,"F  INSPECT",Mathf.Abs(pos.x-26)<3);
            string prompt;
            if(run.Pending!=null && Mathf.Abs(pos.x-26)<3)prompt="F  Inspect Rainbow  /  1 or 2 equip  /  B dismantle inside item view";
            else if(nearbyGate>=0)prompt="F  Enter this path   /   Unclaimed gear becomes shards";
            else prompt=room==0&&!tutorial?"F / B  Remember  /  Spend permanent memory":room==5?"F / B  Forge  /  Recover and prepare":"B  Forge  /  Spend your shards before the next battle";
            Panel(new Rect(280,561,720,34),Hex("43574F"),1);Label(292,565,696,25,prompt,centeredSmall);
        }
        void GearCard(Rect r,Gear g,string caption,Color accent,Gear compare=null)
        {
            Panel(r,accent);Label(r.x+20,r.y+14,r.width-40,24,caption,hudSmall);
            Icon("bow",new Rect(r.x+20,r.y+53,64,64),g==null?new Color(.3f,.3f,.3f,1):Color.white);
            Label(r.x+99,r.y+54,r.width-119,28,g==null?"EMPTY":"RAINBOW +"+g.Level,cardTitle);
            Label(r.x+99,r.y+85,r.width-119,42,g==null?"No skill equipped":g.SkillName,smallStyle);
            Fill(new Rect(r.x+20,r.y+134,r.width-40,1),Hex("35444A"));
            if(g==null){Label(r.x+20,r.y+151,r.width-40,72,"Equip the discovered item here.",smallStyle);return;}
            Label(r.x+20,r.y+151,115,22,"DAMAGE",hudSmall);Label(r.x+144,r.y+145,110,30,g.Damage.ToString(),hudValue);
            Label(r.x+20,r.y+181,115,22,"COOLDOWN",hudSmall);Label(r.x+144,r.y+176,110,30,g.Cooldown.ToString("0.0")+"s",hudValue);
            Label(r.x+20,r.y+215,r.width-40,57,VariantDescription(g),smallStyle);
            if(compare!=null)
            {
                int delta=compare.Damage-g.Damage;
                Label(r.x+20,r.y+280,r.width-40,27,"Incoming damage "+(delta>=0?"+":"")+delta,smallStyle);
            }
        }
        void MenuFrame(string title,string subtitle)
        {
            Overlay();Panel(new Rect(70,55,1140,600),Trim);
            Label(98,77,1084,48,title,titleStyle);Label(100,128,1080,38,subtitle,smallStyle);
            Fill(new Rect(100,171,1080,1),Hex("39474A"));
        }
        void DrawLoot()
        {
            var incoming=run.Pending;if(incoming==null){mode=Mode.Playing;return;}
            MenuFrame("A story, given shape.","Choose its place. Replaced gear stays on the ground; there is no reserve inventory.");
            GearCard(new Rect(100,193,338,328),incoming,"DISCOVERED",Mint);
            GearCard(new Rect(469,193,338,328),run.Slots[0],"Q / EQUIPPED",Trim,incoming);
            GearCard(new Rect(838,193,338,328),run.Slots[1],"E / EQUIPPED",Trim,incoming);
            if(Button(100,538,338,49,"B  DISMANTLE / +"+incoming.ScrapValue+" SHARDS")){Scrap();return;}
            if(Button(469,538,338,49,"1  EQUIP TO Q")){Equip(0);return;}
            if(Button(838,538,338,49,"2  EQUIP TO E")){Equip(1);return;}
            Label(102,609,735,26,"Leave it behind to receive its shards when entering the next room.",hudSmall);
            if(Button(915,604,260,34,"ESC  KEEP WALKING"))mode=Mode.Playing;
        }
        void DrawForge()
        {
            MenuFrame("The forge of fragments.","This run only. Shards strengthen a skill; gold restores your health.");
            for(int i=0;i<2;i++)
            {
                float x=100+i*550;var g=run.Slots[i];int cost=run.UpgradeCost(i);
                GearCard(new Rect(x,193,514,291),g,i==0?"Q / EQUIPPED":"E / EQUIPPED",Trim);
                bool canUpgrade=cost>=0&&run.Shards>=cost;
                string upgradeText=g==null?"EMPTY SLOT":cost<0?"MAXIMUM LEVEL":"UPGRADE / "+cost+" SHARDS";
                if(ActionButton(x,499,310,45,upgradeText,canUpgrade))
                    if(run.Upgrade(i)){SaveMeta();RefreshStats();}
                if(ActionButton(x+324,499,190,45,g==null?"DISMANTLE":"SCRAP / +"+g.ScrapValue,g!=null))run.ScrapSlot(i);
            }
            if(ActionButton(100,561,514,43,"RECOVER 4 HP / 12 GOLD",hp<maxHp&&run.Gold>=12)&&run.TrySpendGold(12))hp=Mathf.Min(maxHp,hp+4);
            Label(650,566,510,36,"SHARDS "+run.Shards+"     GOLD "+run.Gold+"     HP "+hp+" / "+maxHp,hudValue);
            if(Button(915,612,260,31,"ESC  CONTINUE"))mode=Mode.Playing;
        }
        void DrawMemory()
        {
            MenuFrame("What time cannot take.","Permanent growth remains through death. You remember what once existed.");
            float[] xs={100,650};
            for(int i=0;i<2;i++)
            {
                float x=xs[i];int level=i==0?meta.hpUpgrades:meta.attackUpgrades;int cost=i==0?run.HealthCost:run.AttackCost;
                Panel(new Rect(x,193,514,250),Trim);Label(x+24,214,466,35,i==0?"LASTING MEMORY":"SHARPENED PURPOSE",cardTitle);
                Label(x+24,265,466,60,i==0?"Maximum health +2":"Basic attack damage +1",labelStyle);
                for(int j=0;j<RunProgress.MaxPermanentLevel;j++)Fill(new Rect(x+24+j*47,330,36,7),j<level?Mint:Hex("34474C"));
                Label(x+24,355,466,28,"LEVEL "+level+" / "+RunProgress.MaxPermanentLevel,smallStyle);
                if(ActionButton(x+24,391,466,38,cost<0?"COMPLETED":"REMEMBER / "+cost+" MEMORY",cost>=0&&run.Memory>=cost))
                {bool bought=i==0?run.BuyHealth():run.BuyAttack();if(bought){SaveMeta();RefreshStats(true);}}
            }
            Panel(new Rect(100,466,1064,110),Hex("445753"),1);
            Label(122,479,1020,24,"DEEDS THAT REMAIN",hudSmall);
            Label(122,510,500,43,(meta.firstUpgrade?"[KEPT] ":"[LOCKED] ")+"First upgrade / permanent HP +1",smallStyle);
            Label(646,510,494,43,(meta.beastDefeated?"[KEPT] ":"[LOCKED] ")+"The Beast / permanent attack +1",smallStyle);
            Label(102,611,510,31,"MEMORY "+run.Memory,hudValue);
            if(Button(915,604,260,34,"ESC  RETURN"))mode=Mode.Playing;
        }
        void DrawRoutes()
        {
            MenuFrame("Which story comes next?","Each sign describes the next room's reward. Leftover gear becomes shards.");
            if(room==4)
            {
                Panel(new Rect(100,198,1064,324),Trim);Label(126,241,1010,60,"RESTING GLADE",titleStyle);
                Label(126,326,1010,50,"Recover and strengthen your equipment before facing the Beast.");
                if(Button(126,424,1010,64,"ENTER THE GLADE"))Advance(0);
            }
            else for(int i=0;i<2;i++)
            {
                int choice=i==0?choiceA:choiceB;float x=100+i*550;
                Panel(new Rect(x,198,514,324),i==0?Mint:Trim);
                Label(x+25,226,464,45,routes[choice],titleStyle);
                Label(x+25,287,464,26,room==2?"ELITE ENCOUNTER":"FOREST BATTLE",hudSmall);
                Label(x+25,333,464,60,choice==0?"Find a skill item. Equip it or turn it into shards.":choice==1?"Earn gold to recover before the next challenge.":"Receive a story item that lasts for this run.");
                if(Button(x+25,426,464,64,"TAKE THIS PATH")){Advance(choice);return;}
            }
            if(Button(915,604,260,34,"ESC  BACK"))mode=Mode.Playing;
        }
        void DrawPause()
        {
            MenuFrame("The forest waits.","Take a breath. Time and cooldowns are paused.");
            Label(103,200,1000,40,"MOVEMENT",hudSmall);
            Label(103,235,1000,64,"A / D  Move     W / SPACE  Double jump (hold to rise)     S  Drop\nRIGHT CLICK + WASD  Invincible dash",labelStyle);
            Label(103,326,1000,40,"COMBAT & INTERACTION",hudSmall);
            Label(103,361,1000,84,"HOLD LEFT CLICK  3-hit combo     Q / E  Equipped skills\nF  Interact / enter a gate     B  Forge or permanent growth\nAmber warning  >  Red attack  >  Purple recovery",labelStyle);
            if(Button(100,474,336,55,showHelp?"CONTROL HINTS / ON":"CONTROL HINTS / OFF"))showHelp=!showHelp;
            if(Button(464,474,336,55,feedback.Muted?"SOUND / OFF":"SOUND / ON"))feedback.SetMuted(!feedback.Muted);
            if(Button(828,474,336,55,"RESUME / ESC"))mode=Mode.Playing;
            if(Button(650,558,514,55,"RESTART RUN / KEEP PERMANENT GROWTH"))NewRun();
            Label(103,568,510,49,"Restarting clears gold, shards and equipment.",smallStyle);
        }
        void DrawEnd()
        {
            bool dead=mode==Mode.Dead;
            MenuFrame(dead?"Time returns. You remember.":"A beast becomes a memory.",dead?"Your story continues at the returning path.":"The first chapter is complete. Another telling awaits.");
            Panel(new Rect(100,198,1064,120),dead?Hex("92645F"):Mint,1);
            Label(125,219,1010,82,dead?banner:"The Beast falls. The deed you earned remains through the next return.");
            Label(105,345,1020,41,"MEMORY KEPT  "+run.Memory,titleStyle);
            Label(105,405,1020,64,"Permanent purchases and remembered deeds stay.\nGold, shards, equipment and temporary upgrades begin anew.");
            Label(105,504,1020,29,"TIME "+clock.ToString("0.0")+"s    /    TELLING "+(deaths+1)+"    /    SEED "+seed,hudSmall);
            if(Button(100,565,1064,66,"R  RETURN TO THE STARTING POINT"))NewRun();
        }
        void OnGUI()
        {
            if(run==null)return;
            InitGUI();Matrix4x4 previous=GUI.matrix;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            DrawHUD();ContextHints();
            if(uiLastRoom!=room){uiLastRoom=room;uiWasClear=false;}
            if(cleared&&!uiWasClear)uiClearStamp=Time.unscaledTime;
            uiWasClear=cleared;
            if(cleared&&room>0&&room<5&&mode==Mode.Playing&&Time.unscaledTime-uiClearStamp<2.2f)
            {
                Label(420,242,440,60,"ROOM CLEAR",new GUIStyle(titleStyle){alignment=TextAnchor.MiddleCenter});
            }
            if(showHelp&&mode==Mode.Playing&&tutorial)
            {Panel(new Rect(300,510,680,39),Trim,1);Label(310,515,660,28,"LMB attack  /  RMB dash  /  Q E skills  /  W jump  /  F interact",centeredSmall);}
            if(mode==Mode.Loot)DrawLoot();
            else if(mode==Mode.Forge)DrawForge();
            else if(mode==Mode.Memory)DrawMemory();
            else if(mode==Mode.Routes)DrawRoutes();
            else if(mode==Mode.Paused)DrawPause();
            else if(mode==Mode.Dead||mode==Mode.Won)DrawEnd();
            GUI.matrix=previous;
        }
    }
}
