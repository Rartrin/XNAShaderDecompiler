using System;
using System.IO;

namespace XNAShaderDecompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("XNA Shader Decompiler");
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: <shader.xnb>");
                return;
            }

            string inputFile = args[0];
            if (!File.Exists(inputFile))
            {
                Console.WriteLine("File Not Found: " + inputFile);
                return;
            }
            string outputFile = Path.ChangeExtension(inputFile, ".fxb");

            //try
            //{
                Console.WriteLine("Reading XNB...");
                byte[] effectCode = ContentManager.ReadAsset(inputFile);
                
                //Console.WriteLine("Writing FXB...");
                //File.WriteAllBytes(outputFile, effect.EffectCode);
                
                Console.WriteLine("Parsing FXB...");
                var effect = EffectParser.Parse(effectCode);

                Console.WriteLine("Done!");
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //}
        }
    }
}