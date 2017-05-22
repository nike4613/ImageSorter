using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Utilites
{
    public static class Utils
    {
        public static string SFormat(this string s, params object[] args)
        {
            return String.Format(s, args);
        }

        public static string UrlDecode(this string s)
        {
            return HttpUtility.UrlDecode(s);
        }

        public static uint SetBits(this long n)
        {
            uint count = 0;
            while (n != 0)
            {
                n &= (n - 1);
                count++;
            }
            return count;
        }

        public static string GetRelativePath(string fullPath, string basePath)
        {
            // Require trailing backslash for path
            if (!basePath.EndsWith("\\"))
                basePath += "\\";

            Uri baseUri = new Uri(basePath);
            Uri fullUri = new Uri(fullPath);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);

            // Uri's use forward slashes so convert back to backward slashes
            return relativeUri.ToString().Replace("/", "\\");

        }

        public static Process ProcessCommandLine(string exec, params string[] args)
        {
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                FileName = exec,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = string.Join(" ", args)
            };
            Process exeProcess = new Process()
            {
                StartInfo = startInfo
            };
            return exeProcess;
        }

        private delegate TResult Func<T1, TResult>(T1 arg1);
        private static Dictionary<Type, Delegate> _cachedIL = new Dictionary<Type, Delegate>();

        public static T ILClone<T>(this T myObject)
        {
            if (!_cachedIL.TryGetValue(typeof(T), out Delegate myExec))
            {
                // Create ILGenerator
                DynamicMethod dymMethod = new DynamicMethod("DoClone", typeof(T), new Type[] { typeof(T) }, true);
                ConstructorInfo cInfo = myObject.GetType().GetConstructor(new Type[] { });

                ILGenerator generator = dymMethod.GetILGenerator();

                LocalBuilder lbf = generator.DeclareLocal(typeof(T));
                //lbf.SetLocalSymInfo("_temp");

                generator.Emit(OpCodes.Newobj, cInfo);
                generator.Emit(OpCodes.Stloc_0);
                foreach (FieldInfo field in myObject.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    // Load the new object on the eval stack... (currently 1 item on eval stack)
                    generator.Emit(OpCodes.Ldloc_0);
                    // Load initial object (parameter)          (currently 2 items on eval stack)
                    generator.Emit(OpCodes.Ldarg_0);
                    // Replace value by field value             (still currently 2 items on eval stack)
                    generator.Emit(OpCodes.Ldfld, field);
                    // Store the value of the top on the eval stack into the object underneath that value on the value stack.
                    //  (0 items on eval stack)
                    generator.Emit(OpCodes.Stfld, field);
                }

                // Load new constructed obj on eval stack -> 1 item on stack
                generator.Emit(OpCodes.Ldloc_0);
                // Return constructed object.   --> 0 items on stack
                generator.Emit(OpCodes.Ret);

                myExec = dymMethod.CreateDelegate(typeof(Func<T, T>));
                _cachedIL.Add(typeof(T), myExec);
            }
            return ((Func<T, T>)myExec)(myObject);
        }

        public static bool ULCompare(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length) return false;

            var longSize = (int)Math.Floor(a1.Length / 8.0);
            var long1 = Unsafe.As<long[]>(a1);
            var long2 = Unsafe.As<long[]>(a2);

            for (var i = 0; i < longSize; i++)
            {
                if (long1[i] != long2[i]) return false;
            }

            for (var i = longSize * 8; i < a1.Length; i++)
            {
                if (a1[i] != a2[i]) return false;
            }

            return true;
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateHardLink(
              string lpFileName,
              string lpExistingFileName,
              IntPtr lpSecurityAttributes
              );

        public static bool CreateHardLink(string lpFileName, string lpExistingFileName)
        {
            return CreateHardLink(lpFileName, lpExistingFileName, IntPtr.Zero);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateSymbolicLink(
            string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        public enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }
    }

}
