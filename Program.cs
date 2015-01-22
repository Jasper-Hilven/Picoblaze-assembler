using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
namespace PicoBlazeCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            
            //TEST REGION
            testForm();
            return;

            //END TEST REGION


            if (args.Length != 2)
            {
                Console.WriteLine("syntax:");
                Console.WriteLine("pbc.exe input.psm output.vhd");
                return;
            }
            string form = System.IO.File.ReadAllText("ROM_form.vhd");
            string code = System.IO.File.ReadAllText(args[0]);


            var compiler = new Compiler(code, form);
            try
            {
                string useless;
                string output = compiler.Compile(out useless);
                System.IO.File.WriteAllText(args[1], output);
                Console.WriteLine("Compiled successfully.\n");
            }
            catch (CompilerException e)
            {
                Console.WriteLine("Compile error on line " + e.line + ": " + e.message + ".");
            }
#if DEBUG
            Console.WriteLine("Press enter to continue ...\n");
            Console.ReadLine();
#endif
        }


        static void testForm()
        {
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                TestForm dazForm = new TestForm();
                Application.Run(dazForm);
            }

        }
    }
}

