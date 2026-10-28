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
  Console.WriteLine(count+" checks passed.");
 }
}
