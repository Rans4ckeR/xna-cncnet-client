﻿using System;
using System.Collections.Generic;
using System.IO;
#if NETFRAMEWORK
using System.Linq;
#else
using System.Runtime.Loader;
#endif

using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace DTAClient;

internal static class Program
{
    private static readonly string COMMON_LIBRARY_PATH;

    static Program()
    {
        /* We have different binaries depending on build platform, but for simplicity
         * the target projects (DTA, TI, MO, YR) supply them all in a single download.
         * To avoid DLL hell, we load the binaries from different directories
         * depending on the build platform.
         *
         * For .NET 6 Release mode we split up the DXMainClient dll from the AppHost executable.
         * The AppHost is located in the root, as is the case for the .NET 4.8 executables.
         * The actual DXMainClient dll is 2 directories up in Application.StartupPath\Binaries\<WindowsGL,OpenGL,XNA> */

#if DEBUG || NETFRAMEWORK
        string startupPath = Application.StartupPath;
#elif !NETFRAMEWORK
        string startupPath = Path.GetFullPath(Path.Combine(Application.StartupPath, "..\\..\\"));
#endif

#if DEBUG
        COMMON_LIBRARY_PATH = startupPath;
#else
        COMMON_LIBRARY_PATH = Path.Combine(startupPath, "Binaries");
#endif

#if XNA && DEBUG
        SPECIFIC_LIBRARY_PATH = startupPath;
#elif XNA
        SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "XNA");
#elif WINDOWSGL && DEBUG
        SPECIFIC_LIBRARY_PATH = startupPath;
#elif WINDOWSGL
        SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "OpenGL");
#elif DEBUG
        SPECIFIC_LIBRARY_PATH = startupPath;
#else
        SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "Windows");
#endif

        // Set up DLL load paths as early as possible
#if NETFRAMEWORK
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
#else
        AssemblyLoadContext.Default.Resolving += DefaultAssemblyLoadContextOnResolving;
#endif

#if !DEBUG
        Environment.CurrentDirectory = Directory.GetParent(startupPath.Replace('\\', '/')).FullName;
#else
        Environment.CurrentDirectory = startupPath.Replace('\\', '/');
#endif
    }

    private static readonly string SPECIFIC_LIBRARY_PATH;

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        bool noAudio = false;
        bool multipleInstanceMode = false;
        List<string> unknownStartupParams = new();

        for (int arg = 0; arg < args.Length; arg++)
        {
            string argument = args[arg].ToUpperInvariant();

            switch (argument)
            {
                case "-NOAUDIO":
                    noAudio = true;
                    break;
                case "-MULTIPLEINSTANCE":
                    multipleInstanceMode = true;
                    break;
                default:
                    unknownStartupParams.Add(argument);
                    break;
            }
        }

        StartupParams parameters = new(noAudio, multipleInstanceMode, unknownStartupParams);

        if (multipleInstanceMode)
        {
            // Proceed to client startup
            PreStartup.Initialize(parameters);
            return;
        }

        // We're a single instance application!
        // http://stackoverflow.com/questions/229565/what-is-a-good-pattern-for-using-a-global-mutex-in-c/229567

        // Global prefix means that the mutex is global to the machine
        string mutexId = string.Format("Global/{{{0}}}", Guid.Parse("1CC9F8E7-9F69-4BBC-B045-E734204027A9"));

#if NETFRAMEWORK
        System.Security.AccessControl.MutexAccessRule allowEveryoneRule = new(
            new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null),
            System.Security.AccessControl.MutexRights.FullControl,
            System.Security.AccessControl.AccessControlType.Allow);
        System.Security.AccessControl.MutexSecurity securitySettings = new();
        securitySettings.AddAccessRule(allowEveryoneRule);

        using Mutex mutex = new(false, mutexId, out bool _, securitySettings);
#else
        using var mutex = new Mutex(false, mutexId, out _);
#endif
        bool hasHandle = false;
        try
        {
            try
            {
                hasHandle = mutex.WaitOne(8000, false);
                if (hasHandle == false)
                    throw new TimeoutException("Timeout waiting for exclusive access");
            }
            catch (AbandonedMutexException)
            {
                hasHandle = true;
            }
            catch (TimeoutException)
            {
                return;
            }

            // Proceed to client startup
            PreStartup.Initialize(parameters);
        }
        finally
        {
            if (hasHandle)
                mutex.ReleaseMutex();
        }
    }

#if NETFRAMEWORK
    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        string unresolvedAssemblyName = args.Name.Split(',').First();

        if (unresolvedAssemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
            return null;

        FileInfo commonFileInfo = new(FormattableString.Invariant($"{Path.Combine(COMMON_LIBRARY_PATH, unresolvedAssemblyName)}.dll"));

        if (commonFileInfo.Exists)
            return Assembly.Load(AssemblyName.GetAssemblyName(commonFileInfo.FullName));

        FileInfo specificFileInfo = new(FormattableString.Invariant($"{Path.Combine(SPECIFIC_LIBRARY_PATH, unresolvedAssemblyName)}.dll"));

        if (specificFileInfo.Exists)
            return Assembly.Load(AssemblyName.GetAssemblyName(specificFileInfo.FullName));

        return null;
    }
#else
    private static Assembly DefaultAssemblyLoadContextOnResolving(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
    {
        if (assemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
            return null;

        var commonFileInfo = new FileInfo(FormattableString.Invariant($"{Path.Combine(COMMON_LIBRARY_PATH, assemblyName.Name)}.dll"));

        if (commonFileInfo.Exists)
            return assemblyLoadContext.LoadFromAssemblyPath(commonFileInfo.FullName);

        var specificFileInfo = new FileInfo(FormattableString.Invariant($"{Path.Combine(SPECIFIC_LIBRARY_PATH, assemblyName.Name)}.dll"));

        if (specificFileInfo.Exists)
            return assemblyLoadContext.LoadFromAssemblyPath(specificFileInfo.FullName);

        return null;
    }
#endif
}