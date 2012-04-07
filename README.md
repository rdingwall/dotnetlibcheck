.NET libcheck
=============

.NET libcheck is a quick command-line tool for helping track down .NET assembly 
compatibility issues. 

    System.IO.FileLoadException: Could not load file or assembly {0} or one of its dependencies. 
    The located assembly's manifest definition does not match the assembly reference.

If you've ever seen this error before, you need libcheck!

### How to use

To use, simply point libcheck at a directory of assemblies. 

```
libcheck.exe C:\dev\yourapp\lib
```

You can (optionally) provide a list of wildcard patterns to limit the assemblies
you are interested in.

```
libcheck.exe C:\dev\yourapp\lib *NHibernate* *Castle*
```

And it will give you a report like this:

![Libcheck](http://richarddingwall.name/wp-content/uploads/2010/05/libcheck.gif)

### What does it check for?

Libcheck issues warnings for:

 * Assemblies that are referenced, but missing
 * Assemblies that are referenced, but the wrong version
 * Assemblies that are specific PE kinds (32 bit/64 bit only)
 * Assemblies that are non-matching platforms (e.g. i386 vs AMD64)
 
You can read the original libcheck blog [announcement here](http://richarddingwall.name/2010/05/06/libcheck-quick-and-dirty-assembly-compatibility-debugging/).

