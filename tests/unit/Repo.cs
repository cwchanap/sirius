namespace Hoshikawa.Tests;

static class Repo
{
    public static readonly string Root=Find(AppContext.BaseDirectory);
    public static string Path(string relative)=>System.IO.Path.Combine(Root,relative);
    static string Find(string from)
    {
        for(var dir=new DirectoryInfo(from);dir!=null;dir=dir.Parent)if(File.Exists(System.IO.Path.Combine(dir.FullName,"project.godot")))return dir.FullName;
        throw new InvalidOperationException("no project.godot above "+from);
    }
}
