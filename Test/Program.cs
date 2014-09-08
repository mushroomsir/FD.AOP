using System;
using FD.AOP;
namespace Test
{   
    public class Program
    {
        public static void Main(string[] args)
        {          
            try
            {
                var y = new Program();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.Read();
        }
        [FuncLog(Order=1)]
        public Program TestMethod1(int i, int j, Program c)
        {

            Console.WriteLine("ok");
            return new Program();
        }
    }
}
