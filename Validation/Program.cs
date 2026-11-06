using System;
using Storyborn;
class Program
{
 static int count;
 static void Check(bool result,string name) { if(!result) throw new Exception(name);count++;Console.WriteLine("PASS "+name); }
 static void Main()
 {
  var d=new DashChain();
  Check(d.TryStart(0),"first dash accepted");
  Check(d.TryStart(.2f),"second chained dash accepted");
  Check(!d.TryStart(.3f),"third dash locked");
  Check(Math.Abs(d.Remaining(.3f)-.4f)<.001f,"lock countdown accurate");
  Check(!d.TryStart(.69f),"lock lasts for configured duration");
  Check(d.TryStart(.71f),"dash returns after lock");
  d.Reset();Check(d.TryStart(0) && d.TryStart(.5f) && d.Count==1,"spaced inputs reset chain");
  d.Reset();Check(d.Remaining(0)==0 && d.Count==0 && d.TryStart(0),"new run clears old lock and history");
  var one=new DashChain{Limit=1};Check(one.TryStart(0)&&!one.TryStart(.1f),"one dash limit supported");
  var three=new DashChain{Limit=3};Check(three.TryStart(0)&&three.TryStart(.1f)&&three.TryStart(.2f)&&!three.TryStart(.3f),"three dash limit supported");
  Check(CombatRules.InAttack(0,0,1,1.5f,0,2),"forward target hit");
  Check(!CombatRules.InAttack(0,0,1,-1.5f,0,2),"rear target missed");
  Check(CombatRules.InAttack(0,0,-1,-1.5f,0,2),"left facing target hit");
  Check(!CombatRules.InAttack(0,0,1,1,3,2),"high target missed");
  Check(!CombatRules.InAttack(0,0,1,3,0,2),"distant target missed");
  Check(CombatRules.Overlap(0,0,1,1,.9f,0,1,1),"bodies overlap");
  Check(!CombatRules.Overlap(0,0,1,1,2,0,1,1),"separate bodies do not overlap");
  Check(!CombatRules.Overlap(0,0,1,1,0,2,1,1),"vertical separation respected");
  var skill=new SkillCooldown(4);
  Check(skill.TryUse(0),"skill initially ready");
  Check(!skill.TryUse(.1f),"skill rejects repeated input");
  Check(Math.Abs(skill.Remaining(1)-3)<.001f,"skill cooldown displayed accurately");
  Check(!skill.TryUse(3.99f) && skill.TryUse(4),"skill returns exactly at cooldown boundary");
  skill.Reset();Check(skill.TryUse(0),"new room or run resets skill cooldown");
  ProgressChecks();
  FeelChecks();
  Console.WriteLine(count+" checks passed.");
 }
 static void ProgressChecks()
 {
  var meta=new MetaState{memory=40,tutorialDone=true};
  var run=new RunProgress(meta,42);
  var first=new Gear(0,101);run.Offer(first);run.Equip(0);
  Check(object.ReferenceEquals(run.Slots[0],first) && run.Pending==null,"equip into empty slot consumes pickup");
  var second=new Gear(1,102);run.Offer(second);var displaced=run.Equip(0);
  Check(object.ReferenceEquals(displaced,first) && object.ReferenceEquals(run.Pending,first) && object.ReferenceEquals(run.Slots[0],second),"swap leaves displaced item on ground");
  run.Equip(1);
  Check(object.ReferenceEquals(run.Slots[1],first) && run.Pending==null,"displaced item can enter other slot without duplication");
  Check(run.ScrapPending()==0 && run.Shards==0,"empty pickup gives no shards");
  Check(!run.Upgrade(0) && second.Level==0 && run.Shards==0,"unaffordable gear upgrade changes nothing");
  run.Offer(new Gear(2,103));Check(run.LeaveRoom()==3 && run.Shards==3 && run.Pending==null,"unclaimed ground gear automatically converts to shards at exit");
  Check(run.LeaveRoom()==0 && run.Shards==3,"room exit cannot scrap same item twice");
  Check(run.Upgrade(0) && second.Level==1 && second.Damage==3 && second.ProjectileCount==2 && run.Shards==0,"upgrade spends exact cost and improves same rolled skill");
  Check(second.Variant==1 && meta.firstUpgrade && run.MaxHealth==9,"first upgrade records permanent achievement without rerolling skill");
  run.Offer(new Gear(0,104));run.Offer(new Gear(1,105));
  Check(run.Shards==3 && run.Pending.Id==105,"new offer resolves old unclaimed pickup without loss");
  int before=run.Shards;run.Offer(run.Pending);
  Check(run.Shards==before,"reoffering same pending item cannot duplicate shards");
  bool threw=false;try{run.Offer(run.Slots[0]);}catch(InvalidOperationException){threw=true;}
  Check(threw && object.ReferenceEquals(run.Slots[0],second),"equipped item cannot also be offered on ground");
  Check(run.BuyHealth() && meta.memory==35 && meta.hpUpgrades==1 && run.MaxHealth==11,"permanent health purchase spends persistent currency");
  Check(run.BuyAttack() && meta.memory==27 && meta.attackUpgrades==1 && run.AttackDamage==2,"permanent attack purchase spends persistent currency");
  run.AwardKill(false);run.AwardKill(true);run.CompleteRoom();
  Check(run.Gold==15 && meta.memory==29,"enemy and room rewards distinguish gold and memory");
  Check(!run.TrySpendGold(16) && !run.TrySpendGold(-1) && run.Gold==15,"gold overspend and negative purchase rejected");
  Check(run.TrySpendGold(5) && run.Gold==10,"gold spending is atomic");
  run.AwardBoss();int atk=run.AttackDamage;run.AwardBoss();
  Check(meta.beastDefeated && atk==3 && run.AttackDamage==3,"boss achievement bonus only applies once");
  int memory=meta.memory;run.ResetRun();
  Check(run.Gold==0 && run.Shards==0 && run.Pending==null && run.Slots[0]==null && run.Slots[1]==null,"rewind clears all run currencies and all gear");
  Check(meta.memory==memory && meta.tutorialDone && meta.firstUpgrade && meta.beastDefeated && run.MaxHealth==11 && run.AttackDamage==3,"rewind retains memories tutorial purchases and achievements");
  Check(!run.Upgrade(-1) && !run.Upgrade(2) && run.UpgradeCost(0)==-1,"invalid or empty gear slot rejected");
  var poor=new RunProgress(new MetaState(),1);
  Check(!poor.BuyHealth() && !poor.BuyAttack() && poor.Memory==0,"insufficient permanent currency does not mutate upgrades");
  var capped=new RunProgress(new MetaState{memory=999,hpUpgrades=5,attackUpgrades=5},2);
  Check(!capped.BuyHealth() && !capped.BuyAttack() && capped.Memory==999,"permanent upgrades cap without consuming currency");
  poor.Offer(new Gear(2,201){Level=3});poor.Equip(0);
  Check(poor.UpgradeCost(0)==-1 && !poor.Upgrade(0),"maximum gear level cannot upgrade");
  var a=new RunProgress(new MetaState(),314);var b=new RunProgress(new MetaState(),314);
  bool stable=true;for(int i=0;i<30;i++){var ga=a.NewGear();var gb=b.NewGear();stable&=ga.Variant==gb.Variant && ga.Id==gb.Id && ga.Variant>=0 && ga.Variant<=2;}
  Check(stable,"seed reproduces gear rolls while all variants stay valid");
  var dismantle=new RunProgress(new MetaState(),73);
  Check(dismantle.ScrapSlot(0)==0 && dismantle.ScrapSlot(-1)==0 && dismantle.ScrapSlot(2)==0 && dismantle.Shards==0,"empty and invalid equipped-slot scraps give no reward");
  var upgraded=new Gear(1,301){Level=2};dismantle.Offer(upgraded);dismantle.Equip(1);
  var untouched=new Gear(0,302);dismantle.Offer(untouched);
  Check(dismantle.ScrapSlot(1)==7 && dismantle.Shards==7 && dismantle.Slots[1]==null && object.ReferenceEquals(dismantle.Pending,untouched),"equipped scrap pays level value and preserves separate ground item");
  Check(dismantle.ScrapSlot(1)==0 && dismantle.Shards==7,"equipped item cannot be scrapped twice");
  var corrupted=new RunProgress(new MetaState{memory=-8,deaths=-1,hpUpgrades=500,attackUpgrades=-5},0);
  Check(corrupted.Memory==0 && corrupted.Meta.deaths==0 && corrupted.Meta.hpUpgrades==5 && corrupted.Meta.attackUpgrades==0,"save normalization bounds invalid values");
 }
 static void FeelChecks()
 {
  var combo=new ComboChain();
  Check(combo.TryAttack(0,.2f) && combo.Step==0 && combo.Damage(2)==2,"combo begins with normal first hit");
  Check(!combo.TryAttack(.1f,.2f) && combo.Step==0,"early repeated input cannot advance combo");
  Check(combo.TryAttack(.2f,.2f) && combo.Step==1 && combo.Damage(2)==2,"second hit follows recovery");
  Check(combo.TryAttack(.4f,.2f) && combo.Step==2 && combo.Damage(2)==3,"third hit gains finisher damage");
  Check(!combo.TryAttack(.65f,.2f) && combo.TryAttack(.68f,.2f) && combo.Step==0,"finisher has longer recovery then wraps combo");
  Check(combo.TryAttack(1.5f,.2f) && combo.Step==0,"idle interval resets combo to opener");
  combo.Reset();Check(combo.TryAttack(0,.2f) && combo.Step==0,"new room clears combo history and recovery");
  var buffer=new InputBuffer(.12f);
  Check(!buffer.Pending(0) && !buffer.Consume(0),"unpressed buffer cannot fire");
  buffer.Press(1);Check(buffer.Pending(1.1f) && buffer.Consume(1.1f) && !buffer.Consume(1.1f),"buffer fires at most once");
  buffer.Press(2);Check(!buffer.Consume(2.13f),"expired press cannot trigger late action");
  buffer.Press(3);Check(buffer.Pending(3) && buffer.Pending(3) && buffer.Consume(3),"frozen simulation preserves input across hit-stop");
  buffer.Press(4);buffer.Clear();Check(!buffer.Pending(4),"room transition clears queued input");
  buffer.Press(5);buffer.Press(5.1f);Check(buffer.Consume(5.2f) && !buffer.Consume(5.2f),"held input refreshes expiry without accumulating attacks");
 }

}
