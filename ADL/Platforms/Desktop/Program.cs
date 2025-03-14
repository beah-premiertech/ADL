using Uno.UI.Runtime.Skia;

namespace ADL;
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ArgsHelper.Args = args;
        var host = SkiaHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWindows()
            .Build();

        host.Run();
    }
}
