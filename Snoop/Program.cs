namespace Snoop
{
    using System;

    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            var app = new App();
            return app.Run();
        }
    }
}