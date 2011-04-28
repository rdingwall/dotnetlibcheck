using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LibCheck
{
    class Program
    {
        // Todo: refactor, put on github
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: libcheck <lib directory> <pattern> [pattern ...n]");
                Console.WriteLine(@"Example: libcheck C:\upp\trunk\lib Foo.* Bar.Service");
            }
            try
            {
                var assemblies = Directory.GetFiles(args[0], "*.dll", SearchOption.AllDirectories)
                    .Select(fileName => TryLoadAssembly(fileName))
                    .Where(assembly => assembly != null)
                    .OrderBy(a => a.FullName)
                    .ToList();

                var patterns = args.Skip(1).Select(p => WildcardToRegex(p));

                var assemblyNames = assemblies.Select(assembly => assembly.GetName());

                int errors = 0;

                var interestedAssemblies = assemblies.Where(a => patterns.Any(re => re.IsMatch(a.GetName().Name)));

                var machines = new Dictionary<Module, ImageFileMachine>();
                var peKinds = new Dictionary<Module, PortableExecutableKinds>();

                foreach (Assembly assembly in interestedAssemblies)
                {
                    var assemblyName = assembly.GetName();

                    WriteLine(ConsoleColor.Gray, "{0} {1}", assembly.ManifestModule.Name, assemblyName.Version);

                    foreach (AssemblyName reference in assembly.GetReferencedAssemblies().Where(IsUserAssembly))
                    {
                        if (assemblyNames.Select(a => a.FullName).Contains(reference.FullName))
                            continue;

                        errors++;

                        var nearest = assemblyNames.FirstOrDefault(a => a.Name.Equals(reference.Name));

                        if (nearest == null)
                        {
                            WriteError("   Missing: {0} {1}", reference.Name, reference.Version);
                            continue;
                        }

                        if (nearest.Version < reference.Version)
                            WriteError("   Wrong version: {0} {2} (I need NEWER version {1})", reference.Name,
                                       GetSignificantDifference(nearest.Version, reference.Version), nearest.Version);
                        else
                            WriteError("   Wrong version: {0} {2} (I need OLDER version {1})", reference.Name,
                                       GetSignificantDifference(nearest.Version, reference.Version), nearest.Version);
                    }

                    PortableExecutableKinds peKind;
                    ImageFileMachine machine;
                    assembly.ManifestModule.GetPEKind(out peKind, out machine);

                    peKinds[assembly.ManifestModule] = peKind;
                    machines[assembly.ManifestModule] = machine;

                    if (peKind != PortableExecutableKinds.ILOnly)
                        WriteWarning("   Warning: specific PortableExecutableKind {0} {1} {2}",
                                  assembly.ManifestModule.Name, peKind, machine);

                }

                var imageFileTypes = machines.Select(p => p.Value).Distinct();
                if (imageFileTypes.Count() > 1)
                {
                    WriteWarning("Warning: different ImageFileMachine types detected:");

                    foreach (var imageFileType in imageFileTypes)
                    {
                        WriteLine(ConsoleColor.Gray, "{0}", imageFileType);
                        machines
                            .Where(p => p.Value.Equals(imageFileType))
                            .Select(p => p.Key)
                            .ToList()
                            .ForEach(m => WriteLine(ConsoleColor.Gray, "   {0} {1} {2}", m.Name, peKinds[m], imageFileType));
                    }
                }

                if (errors > 0)
                    WriteError("{0} errors found", errors);
            }
            catch (Exception e)
            {
                WriteError(e.ToString());
            }

            if (System.Diagnostics.Debugger.IsAttached)
                Console.ReadKey();
        }

        static string GetSignificantDifference(Version available, Version required)
        {
            if (available.Major != required.Major)
                return required.ToString();
            if (available.Minor != required.Minor)
                return String.Format("x.{0}.{1}.{2}", required.Minor, required.Build, required.Revision);
            if (available.Build != required.Build)
                return String.Format("x.x.{0}.{1}", required.Build, required.Revision);
            else
                return String.Format("x.x.x.{0}", required.Revision);
        }

        static bool IsUserAssembly(AssemblyName assemblyName)
        {
            var systemPrefixes = new[]
                                     {
                                         "Microsoft",
                                         "System",
                                         "mscorlib",
                                         "PresentationFramework",
                                         "WindowsBase",
                                         "PresentationCore"
                                     };

            return !systemPrefixes.Any(s => assemblyName.Name.StartsWith(s, 
                StringComparison.CurrentCultureIgnoreCase));
        }

        static Assembly TryLoadAssembly(string fileName)
        {
            try
            {
                return Assembly.ReflectionOnlyLoadFrom(fileName);
            }
            catch (Exception e)
            {
                WriteLine(ConsoleColor.DarkGray, "Ignored: {0} : {1}", fileName, e.Message);
                return null;
            }
        }

        static void WriteLine(ConsoleColor color, string format, params object[] args)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(format, args);
            Console.ResetColor();
        }

        static void WriteWarning(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(format, args);
            Console.ResetColor();
        }

        static void WriteError(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(format, args);
            Console.ResetColor();
        }

        public static Regex WildcardToRegex(string pattern)
        {
            var re = "^.*" + Regex.Escape(pattern)
                             .Replace("\\*", ".*")
                             .Replace("\\?", ".") + ".*$";

            return new Regex(re, RegexOptions.IgnoreCase);
        }
    }
}
