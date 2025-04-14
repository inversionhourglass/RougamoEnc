using Rougamo;
using Rougamo.Context;
using Rougamo.Metadatas;

namespace RougamoEnc
{
    internal class Program
    {
        static void Main(string[] args)
        {
            HotReloadService.Initialize();
            Cls.M();
            Console.ReadLine();
            Cls.M();
        }
    }

    public class Cls
    {
        [X]
        public static void M()
        {
            Console.WriteLine(1);
            Console.WriteLine(3);
            Console.WriteLine(5);
        }
    }

    [Advice(Feature.OnEntry)]
    public class XAttribute : MoAttribute
    {
        public override void OnEntry(MethodContext context)
        {
            Console.WriteLine("OnEntry");
        }
    }
}
