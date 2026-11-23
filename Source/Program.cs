using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ChromeAudioRepair;

namespace ChromeVolumeRepair {
 internal static class Program {
  [STAThread]
  static void Main() {
   Application.EnableVisualStyles();
   Application.SetCompatibleTextRenderingDefault(false);
   Application.Run(new RepairWindow());
  }
 }
 internal sealed class RepairWindow : Form {
  readonly Label headline = new Label();
  readonly Label summary = new Label();
  readonly Label status = new Label();
  readonly ListView readings = new ListView();
  readonly CheckBox ready = new CheckBox();
  readonly Button scan = new AccentButton();
  readonly Button repair = new AccentButton();
  readonly Button undo = new Button();
  readonly Button copy = new Button();
  readonly Label badge = new Label();
  readonly Label levels = new Label();
  readonly Label nextSteps = new Label();
  readonly Panel preparation = new Panel();
  readonly Panel details = new Panel();
  readonly Button detailsToggle = new Button();
  SessionInfo[] sessions = new SessionInfo[0];
  readonly List<SessionInfo> previous = new List<SessionInfo>();
  bool busy;
  bool scanValid;
  readonly HelpBubble helpBubble = new HelpBubble();
  readonly Timer helpAnimation = new Timer { Interval=15 };
  readonly Timer helpDismiss = new Timer { Interval=220 };
  Button helpButton;
  bool helpPinned;
  float helpProgress;
  bool helpOpening;
  [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
  protected override void OnHandleCreated(EventArgs e) {
   base.OnHandleCreated(e);
   try {int dark=1;DwmSetWindowAttribute(Handle,20,ref dark,4);int caption=0x00272727;DwmSetWindowAttribute(Handle,35,ref caption,4);int text=0x00F5F1ED;DwmSetWindowAttribute(Handle,36,ref text,4);} catch {}
  }
  void PlaceHelp() {
   if(helpButton==null)return;
   Point anchor=PointToClient(helpButton.PointToScreen(new Point(helpButton.Width,helpButton.Height)));
   helpBubble.Size=new Size(Math.Min(470,ClientSize.Width-44),260);
   helpBubble.Location=new Point(Math.Max(14,anchor.X-helpBubble.Width+7),anchor.Y+10+(int)((1-helpProgress)*9));
  }
  void ShowHelp() {helpDismiss.Stop();helpOpening=true;helpBubble.Visible=true;PlaceHelp();helpBubble.BringToFront();helpAnimation.Start();}
  void HideHelpLater() {helpDismiss.Stop();helpDismiss.Start();}
  void AnimateHelp(object sender,EventArgs e) {
   helpProgress=Math.Max(0,Math.Min(1,helpProgress+(helpOpening?0.11F:-0.15F)));
   helpBubble.Progress=helpProgress;PlaceHelp();helpBubble.Invalidate();
   if(helpProgress==1 || helpProgress==0) {helpAnimation.Stop();if(helpProgress==0)helpBubble.Visible=false;}
  }
  bool OverHelp(Control c) {return c.ClientRectangle.Contains(c.PointToClient(Cursor.Position));}
  protected override void Dispose(bool disposing) {if(disposing){helpAnimation.Dispose();helpDismiss.Dispose();}base.Dispose(disposing);}


  public RepairWindow() {
   Text="Chrome Volume Repair";
   Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
   Font=new Font("Segoe UI",11F);
   BackColor=Color.FromArgb(35,37,42);ForeColor=Color.FromArgb(236,240,247);
   AutoScaleMode=AutoScaleMode.None;ClientSize=new Size(680,450);
   MinimumSize=new Size(680,450);MaximumSize=new Size(1000,700);
   StartPosition=FormStartPosition.CenterScreen;
   var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(30,22,30,26),ColumnCount=1,RowCount=3};
   layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
   var header=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Margin=new Padding(0,0,0,18)};
   header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
   var brand=TextLabel("CHROME VOLUME REPAIR",11F,FontStyle.Bold,Color.FromArgb(136,226,204));brand.Anchor=AnchorStyles.Left;brand.Margin=new Padding(0);header.Controls.Add(brand,0,0);
   var help=new CircleHelpButton {Text="?",AccessibleName="What this tool does",Size=new Size(38,38),Font=new Font("Segoe UI",13F,FontStyle.Bold),Cursor=Cursors.Hand,Margin=new Padding(0),TabIndex=2};
   helpButton=help;
   help.MouseEnter+=delegate {ShowHelp();};help.MouseLeave+=delegate {HideHelpLater();};
   help.Click+=delegate {helpPinned=!helpPinned;if(helpPinned)ShowHelp();else HideHelpLater();};
   help.GotFocus+=delegate {ShowHelp();};help.LostFocus+=delegate {helpPinned=false;HideHelpLater();};
   helpBubble.MouseEnter+=delegate {helpDismiss.Stop();};helpBubble.MouseLeave+=delegate {HideHelpLater();};
   helpAnimation.Tick+=AnimateHelp;
   helpDismiss.Tick+=delegate {if(!helpPinned && !OverHelp(help) && !OverHelp(helpBubble)){helpDismiss.Stop();helpOpening=false;helpAnimation.Start();}};
   helpBubble.Visible=false;Controls.Add(helpBubble);
   KeyPreview=true;KeyDown+=delegate(object sender,KeyEventArgs e) {if(e.KeyCode==Keys.Escape){helpPinned=false;helpOpening=false;helpAnimation.Start();e.Handled=true;}else if(e.KeyCode==Keys.F1){helpPinned=true;ShowHelp();e.Handled=true;}};
   header.Controls.Add(help,1,0);layout.Controls.Add(header,0,0);
   var result=new SoftCard{Dock=DockStyle.Fill,Padding=new Padding(20),Margin=new Padding(0,0,0,20),RowCount=2};result.RowStyles.Add(new RowStyle(SizeType.Percent,65));result.RowStyles.Add(new RowStyle(SizeType.Percent,35));
   headline.Text="Ready";headline.AutoSize=false;headline.Dock=DockStyle.Fill;headline.TextAlign=ContentAlignment.BottomCenter;headline.Font=new Font("Segoe UI",23F,FontStyle.Bold);headline.ForeColor=ForeColor;headline.BackColor=Color.FromArgb(45,48,55);headline.Margin=new Padding(0,0,0,10);result.Controls.Add(headline,0,0);
   levels.AutoSize=false;levels.Dock=DockStyle.Fill;levels.TextAlign=ContentAlignment.TopCenter;levels.Font=new Font("Segoe UI",11F);levels.ForeColor=Color.FromArgb(173,187,207);levels.BackColor=Color.FromArgb(45,48,55);levels.Margin=new Padding(0);levels.Visible=false;levels.AccessibleName="Actual Windows channel volume levels";result.Controls.Add(levels,0,1);layout.Controls.Add(result,0,1);
   var actions=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Margin=new Padding(0)};
   actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
   SetupButton(scan,"Check Chrome",delegate {Scan();});SetupButton(repair,"Fix Chrome",delegate {ConfirmRepair();});
   scan.Dock=repair.Dock=DockStyle.Fill;scan.Margin=new Padding(0,0,7,0);repair.Margin=new Padding(7,0,0,0);actions.Controls.Add(scan,0,0);actions.Controls.Add(repair,1,0);layout.Controls.Add(actions,0,2);
   ready.CheckedChanged+=delegate {UpdateButtons();};Controls.Add(layout);helpBubble.BringToFront();AcceptButton=scan;scan.TabIndex=0;repair.TabIndex=1;
   Shown+=delegate {ActiveControl=scan;helpProgress=0;helpBubble.Visible=false;helpAnimation.Stop();};
   Resize+=delegate {if(helpBubble.Visible)PlaceHelp();};
   scan.MouseDown+=delegate {helpPinned=false;helpOpening=false;helpAnimation.Start();};repair.MouseDown+=delegate {helpPinned=false;helpOpening=false;helpAnimation.Start();};UpdateButtons();
  }
  void ConfirmRepair() {
   if(!scanValid || !sessions.Any(IsLow) || busy)return;
   if(MessageBox.Show(this,"Pause Chrome audio and set any volume boosters back to 100% first.\n\nReady to fix Chrome?","Before fixing",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
   ready.Checked=true;Repair();
  }
  static Label TextLabel(string text,float size,FontStyle style,Color color) {
   var label=new Label{Text=text,AutoSize=true,Font=new Font("Segoe UI",size,style),ForeColor=color,BackColor=Color.Transparent,MaximumSize=new Size(820,0)};return label;
  }
  static void StyleText(Label label,float size,Color color) {
   label.AutoSize=true;label.Font=new Font("Segoe UI",size);label.ForeColor=color;label.BackColor=Color.Transparent;label.MaximumSize=new Size(820,0);
  }
  static void SetupButton(Button button,string text,EventHandler handler) {
   button.Text=text;button.AutoSize=true;button.Padding=new Padding(18,10,18,10);button.MinimumSize=new Size(140,48);button.Margin=new Padding(0,0,12,6);button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=1;button.FlatAppearance.BorderColor=Color.FromArgb(209,218,231);button.BackColor=Color.White;button.ForeColor=Color.FromArgb(41,60,87);button.Cursor=Cursors.Hand;button.Click+=handler;
  }
  static void Primary(Button button,bool primary) {
   var accent=button.Text=="Fix Chrome"?Color.FromArgb(183,166,246):Color.FromArgb(136,226,204);
   button.BackColor=button.Enabled?accent:Color.FromArgb(53,57,65);
   button.ForeColor=button.Enabled?Color.FromArgb(24,34,40):Color.FromArgb(146,153,167);
   button.FlatAppearance.BorderColor=button.Enabled?accent:Color.FromArgb(67,72,83);button.Invalidate();
  }
  static GraphicsPath Rounded(RectangleF r,float radius) {
   var p=new GraphicsPath();float d=radius*2;
   p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
  }
  sealed class AccentButton : Button {
   bool over;
   public AccentButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);MouseEnter+=delegate {over=true;Invalidate();};MouseLeave+=delegate {over=false;Invalidate();};}
   protected override void OnPaint(PaintEventArgs e) {
    e.Graphics.Clear(Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
    Color fill=over && Enabled?ControlPaint.Light(BackColor,0.12F):BackColor;
    using(var path=Rounded(new RectangleF(1,1,Width-3,Height-3),11))using(var brush=new SolidBrush(fill))e.Graphics.FillPath(brush,path);
    TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
    if(Focused && ShowFocusCues){var r=ClientRectangle;r.Inflate(-6,-6);ControlPaint.DrawFocusRectangle(e.Graphics,r,ForeColor,fill);}
   }
  }
  sealed class CircleHelpButton : Button {
   bool over;
   public CircleHelpButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);MouseEnter+=delegate{over=true;Invalidate();};MouseLeave+=delegate{over=false;Invalidate();};}
   protected override void OnPaint(PaintEventArgs e) {
    e.Graphics.Clear(Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
    var circle=new Rectangle(2,2,Width-5,Height-5);
    using(var brush=new SolidBrush(over?Color.FromArgb(58,69,73):Color.FromArgb(43,47,54)))e.Graphics.FillEllipse(brush,circle);
    using(var pen=new Pen(over?Color.FromArgb(136,226,204):Color.FromArgb(107,119,135),1.4F))e.Graphics.DrawEllipse(pen,circle);
    TextRenderer.DrawText(e.Graphics,"?",Font,ClientRectangle,Color.FromArgb(180,233,222),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
    if(Focused && ShowFocusCues){var r=circle;r.Inflate(-4,-4);ControlPaint.DrawFocusRectangle(e.Graphics,r);}
   }
  }
  sealed class HelpBubble : Control {
   float progress;
   public float Progress {get{return progress;}set{progress=value;UpdateShape();}}
   public HelpBubble(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;AccessibleRole=AccessibleRole.ToolTip;AccessibleName="Checks a hidden Windows volume setting. Check Chrome finds low channels; Fix Chrome restores them to 100%. Runs locally and only changes Chrome.";}
   protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);UpdateShape();}
   void UpdateShape(){
    if(Width<40 || Height<40)return;
    float bounce=1+2.70158F*(float)Math.Pow(progress-1,3)+1.70158F*(float)Math.Pow(progress-1,2);
    float scale=0.96F+0.04F*bounce;
    using(var path=Rounded(new RectangleF(3,14,Width-9,Height-22),14))
    using(var pointer=new GraphicsPath())
    using(var transform=new Matrix(scale,0,0,scale,Width*(1-scale),0)){
     pointer.AddPolygon(new[]{new Point(Width-37,15),new Point(Width-24,3),new Point(Width-13,15)});
     var shaped=new Region(path);shaped.Union(pointer);shaped.Transform(transform);
     var old=Region;Region=shaped;if(old!=null)old.Dispose();
    }
   }
   protected override void OnPaintBackground(PaintEventArgs e){}
   static Color Blend(Color a,Color b,float t){return Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));}
   protected override void OnPaint(PaintEventArgs e) {
    e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
    float t=1-(float)Math.Pow(1-Progress,3);
    float bounce=1+2.70158F*(float)Math.Pow(Progress-1,3)+1.70158F*(float)Math.Pow(Progress-1,2);
    float scale=0.96F+0.04F*bounce;e.Graphics.TranslateTransform(Width*(1-scale),0);e.Graphics.ScaleTransform(scale,scale);
    var bg=Color.FromArgb(35,37,42);var surface=Blend(bg,Color.FromArgb(57,62,72),t);
    using(var path=Rounded(new RectangleF(3,14,Width-9,Height-22),14)) {
     using(var brush=new SolidBrush(surface))e.Graphics.FillPath(brush,path);
     using(var pen=new Pen(Blend(bg,Color.FromArgb(91,114,116),t)))e.Graphics.DrawPath(pen,path);
    }
    using(var brush=new SolidBrush(surface))e.Graphics.FillPolygon(brush,new[]{new Point(Width-37,15),new Point(Width-24,3),new Point(Width-13,15)});
    using(var font=new Font("Segoe UI",12F,FontStyle.Bold))using(var brush=new SolidBrush(Blend(bg,Color.FromArgb(153,235,216),t)))e.Graphics.DrawString("A little help for quiet Chrome",font,brush,new RectangleF(23,31,Width-48,32));
    string body="Chrome can be turned down even when Windows shows 100%.\n\nCheck Chrome finds the hidden limit.\nFix Chrome restores normal volume.";
    using(var font=new Font("Segoe UI",10F))using(var brush=new SolidBrush(Blend(bg,Color.FromArgb(231,235,243),t)))e.Graphics.DrawString(body,font,brush,new RectangleF(23,72,Width-48,137));
    using(var font=new Font("Segoe UI",9F))using(var brush=new SolidBrush(Blend(bg,Color.FromArgb(166,178,193),t)))e.Graphics.DrawString("Runs locally. Only changes Chrome.",font,brush,new RectangleF(23,Height-43,Width-48,23));
   }
  }
  sealed class SoftCard : TableLayoutPanel {
   public SoftCard() {AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;ColumnCount=1;ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));BackColor=Color.FromArgb(45,48,55);SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint,true);}
   protected override void OnPaintBackground(PaintEventArgs e) {
    e.Graphics.Clear(Parent==null?Color.FromArgb(35,37,42):Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
    using(var path=Rounded(new RectangleF(0,0,Math.Max(1,Width-1),Math.Max(1,Height-1)),16)) {
     using(var brush=new SolidBrush(BackColor))e.Graphics.FillPath(brush,path);
     using(var pen=new Pen(Color.FromArgb(62,67,77)))e.Graphics.DrawPath(pen,path);
    }
   }
  }
  static bool IsLow(SessionInfo session) { return session.Channels.Any(v=>v<0.9999F); }
  static string Percent(float value) { return (value*100).ToString("0.##")+"%"; }
  static bool Same(SessionInfo a,SessionInfo b) { return a.Endpoint==b.Endpoint && a.SessionId==b.SessionId && a.ProcessId==b.ProcessId; }
  static SessionInfo[] ReadChrome() { return Audio.Inspect(false).Where(s=>s.ProcessName=="chrome" && s.State!=2).ToArray(); }
  void UpdateButtons() {
   scan.Enabled=!busy;
   repair.Enabled=!busy && scanValid && sessions.Any(IsLow);
   undo.Enabled=!busy && previous.Count>0;copy.Enabled=!busy && scanValid;ready.Enabled=!busy;
   Primary(scan,true);Primary(repair,true);AcceptButton=scan;
  }
  void Populate() {
   readings.BeginUpdate();readings.Items.Clear();
   for(int i=0;i<sessions.Length;i++) {
    var s=sessions[i];var item=new ListViewItem("Session "+(i+1));
    item.SubItems.Add(Percent(s.Master)+(s.Muted?" (muted)":""));
    item.SubItems.Add(string.Join(" / ",s.Channels.Select(Percent)));
    item.SubItems.Add(s.State==1?"Playing":"Paused");
    if(IsLow(s))item.ForeColor=Color.FromArgb(158,76,0);
    readings.Items.Add(item);
   }
   readings.EndUpdate();
   levels.Visible=sessions.Length>0;
   if(sessions.Length==1 && sessions[0].Channels.Length==2)levels.Text="Left: "+Percent(sessions[0].Channels[0])+"     Right: "+Percent(sessions[0].Channels[1]);
   else if(sessions.Length>0)levels.Text=string.Join(Environment.NewLine,sessions.Select((s,i)=>(sessions.Length>1?"Connection "+(i+1)+": ":"")+string.Join(" / ",s.Channels.Select(Percent))));
   else levels.Text="";
   levels.AccessibleName=levels.Text;
  }
  void SetStatus(string text,bool error) { status.Text=text; status.AccessibleName=text; status.ForeColor=error?Color.FromArgb(164,48,33):Color.FromArgb(77,92,114); }
  void Scan() {
   busy=true;scanValid=false;ready.Checked=false;UpdateButtons();
   try {
    sessions=ReadChrome();scanValid=true;Populate();headline.ForeColor=Color.FromArgb(236,240,247);
    if(sessions.Length==0) {
     badge.Text="ONE QUICK STEP FIRST";badge.ForeColor=Color.FromArgb(46,100,201);
     headline.Text="Play a video in Chrome first";
     nextSteps.Text="A short video is enough. Once you pause it, come back here.";
     SetStatus("We need Chrome to create an audio connection. Play a video for a few seconds, pause it, then select Check Chrome.",false);
    } else if(sessions.Any(IsLow)) {
     badge.Text="A VOLUME LIMIT WAS FOUND";badge.ForeColor=Color.FromArgb(153,98,12);
     headline.Text="Chrome volume is low";headline.ForeColor=Color.FromArgb(248,198,115);
     nextSteps.Text="The fix returns Chrome’s Windows channels to 100%. It does not raise your speaker volume or change other apps.";
     SetStatus("One or more sound channels are below 100%. This can make Chrome quiet even when its normal volume slider is all the way up.",false);
    } else {
     badge.Text="CHECK COMPLETE  ·  NO CHANGE NEEDED";badge.ForeColor=Color.FromArgb(25,124,91);
     headline.Text="No fix needed";headline.ForeColor=Color.FromArgb(136,226,204);
     nextSteps.Text="Still too quiet? Check the video’s volume, Chrome in Windows Volume Mixer, and your selected speakers or headphones. This app fixes only the channel setting shown above.";
     SetStatus("Chrome’s Windows sound channels are already at 100%. There is nothing to repair in this setting.",false);
    }
   } catch(Exception ex) { sessions=new SessionInfo[0];Populate();badge.Text="COULD NOT COMPLETE CHECK";badge.ForeColor=Color.FromArgb(164,48,33);headline.Text="Could not check. Try again.";nextSteps.Text="Keep Chrome open and choose Check Chrome. Technical details: "+ex.Message;SetStatus("We couldn’t read Chrome’s Windows sound settings. No volume settings were changed.",true); }
   finally {busy=false;UpdateButtons();}
  }
  void Repair() {
   if(busy || !scanValid || !ready.Checked)return;
   busy=true;UpdateButtons();previous.Clear();
   try {
    var current=ReadChrome();
    foreach(var shown in sessions.Where(IsLow)) {
     var target=current.SingleOrDefault(s=>Same(s,shown));
     if(target==null)throw new InvalidOperationException("A Chrome session ended. Check Chrome again.");
     if(!target.Channels.SequenceEqual(shown.Channels))throw new InvalidOperationException("Audio levels changed after the check. Check Chrome again.");
     previous.Add(target);
     var changed=Audio.Inspect(true,target.Endpoint,target.ProcessId,null,target.SessionId).SingleOrDefault(s=>Same(s,target));
     if(changed==null || changed.After==null || changed.After.Any(v=>v!=1F))throw new InvalidOperationException("Windows did not confirm the new levels.");
    }
    sessions=ReadChrome();Populate();
    foreach(var old in previous) {
     var verified=sessions.SingleOrDefault(s=>Same(s,old));
     if(verified==null || verified.Channels.Any(v=>v!=1F))throw new InvalidOperationException("A final verification failed. Check Chrome again.");
    }
    badge.Text="FIX COMPLETE  ·  VERIFIED";badge.ForeColor=Color.FromArgb(25,124,91);
    headline.Text="Fixed";headline.ForeColor=Color.FromArgb(136,226,204);
    nextSteps.Text="Press Play in Chrome and adjust your normal speaker volume to a comfortable level. You can undo this repair while the same Chrome audio connections remain open.";
    SetStatus("Verified: every repaired channel now reads 100%. You’re ready to try your video again.",false);
   } catch(Exception ex) {scanValid=false;badge.Text="FIX NOT COMPLETED";badge.ForeColor=Color.FromArgb(164,48,33);headline.Text="Check Chrome again";nextSteps.Text=ex.Message;SetStatus("Repair stopped. Chrome’s audio connection or settings may have changed. Any completed changes can be undone below.",true);}
   finally {ready.Checked=false;busy=false;UpdateButtons();}
  }
  void Undo() {
   if(busy || previous.Count==0)return;
   if(MessageBox.Show(this,"Restore the previous channel levels? Chrome may become quiet again.","Undo repair",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
   busy=true;UpdateButtons();
   try {
    foreach(var old in previous.ToArray()) {
     var changed=Audio.Inspect(true,old.Endpoint,old.ProcessId,old.Channels,old.SessionId).SingleOrDefault(s=>Same(s,old));
     if(changed==null || changed.After==null || !changed.After.SequenceEqual(old.Channels))throw new InvalidOperationException("A previous Chrome session is no longer available.");
     previous.Remove(old);
    }
    Scan();SetStatus("Previous hidden channel levels restored.",false);
   } catch(Exception ex) {SetStatus("Could not fully undo: "+ex.Message,true);}
   finally {busy=false;UpdateButtons();}
  }
  void CopyResults() {
   try {
    var lines=new List<string> {"Chrome Volume Repair — channel check"};
    for(int i=0;i<sessions.Length;i++) {
     var s=sessions[i];lines.Add("Session "+(i+1)+": app "+Percent(s.Master)+", muted "+s.Muted+", hidden channels "+string.Join(" / ",s.Channels.Select(Percent)));
    }
    lines.Add(status.Text);Clipboard.SetText(string.Join(Environment.NewLine,lines));
    SetStatus("Results copied. They contain only audio levels and repair status.",false);
   } catch(Exception ex) {SetStatus("Could not copy results: "+ex.Message,true);}
  }
 }
}
