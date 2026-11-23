using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ChromeAudioRepair {
[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class Enumerator {}
[ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IDevices {
 [PreserveSig]int EnumAudioEndpoints(int flow,uint mask,out IDeviceCollection devices);
 [PreserveSig]int GetDefaultAudioEndpoint(int flow,int role,out IDevice device);
 [PreserveSig]int GetDevice([MarshalAs(UnmanagedType.LPWStr)]string id,out IDevice device);
 [PreserveSig]int RegisterEndpointNotificationCallback(IntPtr p); [PreserveSig]int UnregisterEndpointNotificationCallback(IntPtr p);
}
[ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IDeviceCollection {
 [PreserveSig]int GetCount(out uint count); [PreserveSig]int Item(uint index,out IDevice device);
}
[ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IDevice {
 [PreserveSig]int Activate(ref Guid iid,uint context,IntPtr activation,[MarshalAs(UnmanagedType.IUnknown)]out object result);
 [PreserveSig]int OpenPropertyStore(uint access,out IntPtr store);
 [PreserveSig]int GetId([MarshalAs(UnmanagedType.LPWStr)]out string id);
 [PreserveSig]int GetState(out uint state);
}
[ComImport, Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IManager {
 [PreserveSig]int GetAudioSessionControl(ref Guid session,uint flags,out IntPtr control);
 [PreserveSig]int GetSimpleAudioVolume(ref Guid session,uint flags,out IntPtr volume);
 [PreserveSig]int GetSessionEnumerator(out ISessions sessions);
}
[ComImport, Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface ISessions {
 [PreserveSig]int GetCount(out int count);
 [PreserveSig]int GetSession(int index,[MarshalAs(UnmanagedType.IUnknown)]out object session);
}
[ComImport, Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IControl {
 [PreserveSig]int GetState(out int state);
 [PreserveSig]int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)]out string name);
 [PreserveSig]int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string name,ref Guid context);
 [PreserveSig]int GetIconPath([MarshalAs(UnmanagedType.LPWStr)]out string path);
 [PreserveSig]int SetIconPath([MarshalAs(UnmanagedType.LPWStr)]string path,ref Guid context);
 [PreserveSig]int GetGroupingParam(out Guid grouping);
 [PreserveSig]int SetGroupingParam(ref Guid grouping,ref Guid context);
 [PreserveSig]int RegisterAudioSessionNotification(IntPtr p);
 [PreserveSig]int UnregisterAudioSessionNotification(IntPtr p);
 [PreserveSig]int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)]out string id);
 [PreserveSig]int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)]out string id);
 [PreserveSig]int GetProcessId(out uint pid);
 [PreserveSig]int IsSystemSoundsSession();
 [PreserveSig]int SetDuckingPreference([MarshalAs(UnmanagedType.Bool)]bool optout);
}
[ComImport, Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface ISimpleVolume {
 [PreserveSig]int SetMasterVolume(float level,ref Guid context);
 [PreserveSig]int GetMasterVolume(out float level);
 [PreserveSig]int SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,ref Guid context);
 [PreserveSig]int GetMute([MarshalAs(UnmanagedType.Bool)]out bool mute);
}
[ComImport, Guid("1C158861-B533-4B30-B1CF-E853E51C59B8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IChannels {
 [PreserveSig]int GetChannelCount(out uint count);
 [PreserveSig]int SetChannelVolume(uint index,float level,ref Guid context);
 [PreserveSig]int GetChannelVolume(uint index,out float level);
 [PreserveSig]int SetAllVolumes(uint count,[In,MarshalAs(UnmanagedType.LPArray,SizeParamIndex=0)]float[] levels,ref Guid context);
 [PreserveSig]int GetAllVolumes(uint count,[Out,MarshalAs(UnmanagedType.LPArray,SizeParamIndex=0)]float[] levels);
}
public class SessionInfo {
 public string Endpoint; public string SessionId; public uint ProcessId; public string ProcessName; public int State;
 public float Master; public bool Muted; public float[] Channels; public float[] After;
}
public static class Audio {
 static void Check(int hr) { Marshal.ThrowExceptionForHR(hr); }
 public static SessionInfo[] Inspect(bool repairChrome, string targetEndpoint=null, uint targetPid=0, float[] requestedLevels=null, string targetSession=null) {
  var rows=new List<SessionInfo>(); var enumerator=(IDevices)new Enumerator(); IDeviceCollection devices;
  Check(enumerator.EnumAudioEndpoints(0,1,out devices)); uint deviceCount; Check(devices.GetCount(out deviceCount));
  for(uint d=0;d<deviceCount;d++) {
   IDevice device; Check(devices.Item(d,out device)); string endpoint; Check(device.GetId(out endpoint));
   object managerObj; Guid managerId=typeof(IManager).GUID; Check(device.Activate(ref managerId,23,IntPtr.Zero,out managerObj));
   var manager=(IManager)managerObj; ISessions sessions; Check(manager.GetSessionEnumerator(out sessions)); int count; Check(sessions.GetCount(out count));
   for(int i=0;i<count;i++) {
    object obj; Check(sessions.GetSession(i,out obj));
    try {
     var control=(IControl)obj; uint pid; Check(control.GetProcessId(out pid)); int state; Check(control.GetState(out state));
     string name="System"; try {name=Process.GetProcessById((int)pid).ProcessName;} catch {}
     var volume=(ISimpleVolume)obj; float master; bool muted; Check(volume.GetMasterVolume(out master)); Check(volume.GetMute(out muted));
     var channel=(IChannels)obj; uint n; Check(channel.GetChannelCount(out n)); var values=new float[n]; Check(channel.GetAllVolumes(n,values));
     string sessionId; Check(control.GetSessionInstanceIdentifier(out sessionId));
     var row=new SessionInfo {Endpoint=endpoint,SessionId=sessionId,ProcessId=pid,ProcessName=name,State=state,Master=master,Muted=muted,Channels=values};
     if(repairChrome && name.Equals("chrome",StringComparison.OrdinalIgnoreCase) && (targetEndpoint==null || (targetEndpoint==endpoint && targetPid==pid && (targetSession==null || targetSession==sessionId)))) {
      var unity=new float[n]; for(int j=0;j<unity.Length;j++)unity[j]=1f;
      if(requestedLevels!=null) {
       if(requestedLevels.Length!=n) throw new ArgumentException("Channel count changed. Refresh and try again.");
       foreach(float v in requestedLevels) if(float.IsNaN(v)||float.IsInfinity(v)||v<0||v>1) throw new ArgumentException("Channel levels must be between 0 and 100%.");
       unity=requestedLevels;
      }
      Guid context=new Guid("53e26c67-33c1-42cf-84be-9a5291bb4aa2"); Check(channel.SetAllVolumes(n,unity,ref context));
      row.After=new float[n]; Check(channel.GetAllVolumes(n,row.After));
     }
     rows.Add(row);
    } finally {Marshal.ReleaseComObject(obj);}
   }
   Marshal.ReleaseComObject(sessions); Marshal.ReleaseComObject(managerObj); Marshal.ReleaseComObject(device);
  }
  Marshal.ReleaseComObject(devices); Marshal.ReleaseComObject(enumerator); return rows.ToArray();
 }
}
}
