using System.IO;

namespace WoW.Utility
{
    public static class Extensions
    {
        //neat trick from: http://stackoverflow.com/a/5434325
        public static Stream ToStream(this string str)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
