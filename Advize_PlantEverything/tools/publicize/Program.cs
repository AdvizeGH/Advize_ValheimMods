using System;
using System.IO;
using Mono.Cecil;

class P {
  static void Main(string[] a) {
    string src=a[0], dst=a[1], probe=a.Length>2?a[2]:null;
    var res=new DefaultAssemblyResolver();
    if(probe!=null) res.AddSearchDirectory(probe);
    res.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(src)));
    var rp=new ReaderParameters{ReadSymbols=false, AssemblyResolver=res};
    var asm=AssemblyDefinition.ReadAssembly(src, rp);
    foreach(var mod in asm.Modules)
    foreach(var t in mod.GetTypes()){
      if(!t.IsPublic && !t.IsNestedPublic) t.IsPublic=true;
      foreach(var f in t.Fields) if(!f.IsPublic) f.IsPublic=true;
      foreach(var p in t.Properties){
        var g=p.GetMethod; if(g!=null && !g.IsPublic) g.IsPublic=true;
        var s=p.SetMethod; if(s!=null && !s.IsPublic) s.IsPublic=true;
      }
      foreach(var m in t.Methods) if(!m.IsPublic) m.IsPublic=true;
    }
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dst)));
    asm.Write(dst);
    Console.WriteLine("publicized -> "+dst);
  }
}
