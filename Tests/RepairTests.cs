using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ChromeAudioRepair;
using ChromeVolumeRepair;
namespace ChromeAudioRepair {
 public class SessionInfo {
  public string Endpoint;public string SessionId;public uint ProcessId;public string ProcessName;public int State;public float Master;public bool Muted;public float[] Channels;public float[] After;
 }
 public static class Audio {
  public static SessionInfo[] State; public static int Writes;
  public static SessionInfo[] Inspect(bool repair,string endpoint=null,uint pid=0,float[] levels=null,string session=null) {
   if(repair)foreach(var s in State.Where(x=>x.ProcessName=="chrome" && x.Endpoint==endpoint && x.ProcessId==pid && x.SessionId==session)) {s.Channels=levels??s.Channels.Select(x=>1F).ToArray();s.After=s.Channels;Writes++;}
   return State.Select(s=>new SessionInfo{Endpoint=s.Endpoint,SessionId=s.SessionId,ProcessId=s.ProcessId,ProcessName=s.ProcessName,State=s.State,Master=s.Master,Muted=s.Muted,Channels=(float[])s.Channels.Clone(),After=s.After}).ToArray();
  }
 }
}
class RepairTests {
 static object Field(object form,string name) {return form.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);}
 static void Call(object form,string method) {form.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,null);}
 static void Assert(bool value,string message) {if(!value)throw new Exception(message);}
 static SessionInfo Chrome(float level) {return new SessionInfo{Endpoint="e",SessionId="s",ProcessId=100,ProcessName="chrome",Channels=new[]{level,level},Master=1};}
 [STAThread]static int Main() {
  Application.EnableVisualStyles();
  Audio.State=new[]{Chrome(0.01584893F),new SessionInfo{Endpoint="e",SessionId="other",ProcessId=200,ProcessName="music",Channels=new[]{0.5F,0.5F},Master=1}};
  using(var f=new RepairWindow()) {
   Call(f,"Scan");Assert(((Button)Field(f,"repair")).Enabled,"Low channels should offer Fix Chrome");
   Call(f,"Repair");Assert(Audio.Writes==0,"Unacknowledged repair wrote levels");
   ((CheckBox)Field(f,"ready")).Checked=true;Assert(((Button)Field(f,"repair")).Enabled,"Low levels should enable repair");
   Call(f,"Repair");Assert(Audio.State[0].Channels.All(x=>x==1),"Chrome not repaired");Assert(Audio.State[1].Channels.All(x=>x==0.5F),"Other app changed");
   Assert(!((CheckBox)Field(f,"ready")).Checked,"Acknowledgement not cleared");Assert(((Button)Field(f,"undo")).Enabled,"Undo snapshot missing");
   Assert(((Label)Field(f,"status")).Text.Contains("Verified"),"No verified result");
  }
  Audio.State=new[]{Chrome(1F)};
  using(var f=new RepairWindow()) {Call(f,"Scan");((CheckBox)Field(f,"ready")).Checked=true;Assert(!((Button)Field(f,"repair")).Enabled,"Healthy channels must not enable repair");}
  Audio.State=new SessionInfo[0];
  using(var f=new RepairWindow()) {Call(f,"Scan");Assert(((Label)Field(f,"headline")).Text.Contains("Play a video"),"No-session instructions missing");}
  Audio.State=new[]{Chrome(0.1F)};
  using(var f=new RepairWindow()) {Call(f,"Scan");Audio.State[0].Channels=new[]{0.2F,0.2F};((CheckBox)Field(f,"ready")).Checked=true;int count=Audio.Writes;Call(f,"Repair");Assert(Audio.Writes==count,"Stale scan caused a write");Assert(((Label)Field(f,"status")).Text.Contains("stopped"),"Stale-state error missing");}
  Console.WriteLine("PASS: acknowledgement gate, targeted repair/readback, other-app preservation, undo snapshot, normal/no-session states, stale-scan protection.");return 0;
 }
}


