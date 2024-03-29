﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Lithnet.CredentialProvider.RegistrationTool
{
    public static class RegistrationServices
    {
        private static RegistryKey GetLocalMachine64()
        {
            return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        }

        private static RegistryKey GetClassesRoot64()
        {
            return RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);
        }

        public static bool IsManagedAssembly(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var peReader = new PEReader(fs))
                {
                    if (!peReader.HasMetadata)
                    {
                        return false;
                    }

                    MetadataReader reader = peReader.GetMetadataReader();
                    return reader.IsAssembly;
                }
            }
        }

        public static IEnumerable<CredentialProviderRegistrationData> GetCredentalProviders()
        {
            var cpKeys = GetLocalMachine64().OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers");
            foreach (var clsid in cpKeys.GetSubKeyNames())
            {
                if (Guid.TryParse(clsid, out Guid result))
                {
                    yield return GetCredentialProvider(result);
                }
            }
        }

        public static CredentialProviderRegistrationData GetCredentialProvider(Type type)
        {
            var comGuid = GetComGuid(type);
            var progId = GetComProgId(type);

            var item = GetCredentialProvider(comGuid);
            item.ProgId ??= progId;
            item.CredentialProviderName ??= GetTypeFullName(type);
            item.DllPath ??= GetTypeAssemblyLocation(type);
            if (item.DllType == DllType.Unknown)
            {
                item.DllType = IsFrameworkType(type) ? DllType.NetFramework : DllType.NetCore;
            }

            return item;
        }

        public static CredentialProviderRegistrationData GetCredentialProvider(string progId)
        {
            var clsid = GetClsidFromProgId(progId);
            return GetCredentialProvider(clsid);
        }

        public static CredentialProviderRegistrationData GetCredentialProvider(Guid clsid)
        {
            CredentialProviderRegistrationData data = new CredentialProviderRegistrationData();

            data.Clsid = clsid;

            var clsidKey = GetClassesRoot64().OpenSubKey($@"CLSID\{clsid:B}");
            if (clsidKey != null)
            {
                var inprocKey = clsidKey.OpenSubKey("InprocServer32");
                data.IsComRegistered = inprocKey != null;

                if (data.IsComRegistered)
                {
                    var coreLib = inprocKey.GetValue(string.Empty) as string;

                    if (string.IsNullOrWhiteSpace(coreLib))
                    {
                        data.IsComRegistered = false;
                    }
                    else
                    {
                        if (string.Equals(coreLib, "mscoree.dll", StringComparison.OrdinalIgnoreCase))
                        {
                            data.DllType = DllType.NetFramework;
                            data.DllPath = inprocKey.GetValue("CodeBase") as string;
                        }
                        else if (coreLib.EndsWith(".comhost.dll", StringComparison.OrdinalIgnoreCase))
                        {
                            data.DllType = DllType.NetCore;
                            var i = coreLib.IndexOf(".comhost.dll", StringComparison.OrdinalIgnoreCase);
                            data.DllPath = coreLib.Substring(0, i) + ".dll";
                        }
                        else
                        {
                            data.DllType = DllType.Native;
                            data.DllPath = coreLib;
                        }
                    }
                }
            }

            data.ProgId = GetClassesRoot64().OpenSubKey($@"CLSID\{clsid:B}\ProgId")?.GetValue(string.Empty) as string;

            var cpkey = GetLocalMachine64().OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\{clsid:B}");
            data.IsCredentialProviderRegistered = cpkey != null;

            if (data.IsCredentialProviderRegistered)
            {
                int? disabled = cpkey.GetValue("Disabled", 0) as int?;
                data.IsCredentalProviderEnabled = disabled == null || disabled == 0;
                data.CredentialProviderName = cpkey.GetValue(string.Empty) as string;
            }

            return data;
        }

        public static void UnregisterCredentialProvider(Type type, bool unregisterCom)
        {
            DeleteCredentialProviderRegistration(type);

            if (unregisterCom)
            {
                if (IsFrameworkType(type))
                {
                    UnregisterFrameworkAssembly(type);
                }
                else
                {
                    UnregisterNetCoreAssembly(type);
                }
            }
        }

        public static void UnregisterCredentialProvider(Guid clsid, bool unregisterCom)
        {
            DeleteCredentialProviderRegistration(clsid);

            if (unregisterCom)
            {
                UnregisterClass(clsid);

            }
        }

        public static void RegisterCredentialProvider(Type type)
        {
            CreateCredentialProviderRegistration(type);

            if (IsFrameworkType(type))
            {
                RegisterFrameworkAssembly(type);
            }
            else
            {
                RegisterNetCoreAssembly(type);
            }
        }

        public static void DisableCredentialProvider(Type type)
        {
            var comGuid = GetComGuid(type);
            DisableCredentialProvider(comGuid);
        }

        public static void DisableCredentialProvider(Guid comGuid)
        {
            var key = GetLocalMachine64().OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\{comGuid:B}", true);
            key?.SetValue("Disabled", 1);
        }

        public static void EnableCredentialProvider(string progId)
        {
            var clsid = GetClsidFromProgId(progId);
            EnableCredentialProvider(clsid);
        }

        public static void DisableCredentialProvider(string progId)
        {
            var clsid = GetClsidFromProgId(progId);
            DisableCredentialProvider(clsid);
        }

        public static void EnableCredentialProvider(Guid comGuid)
        {
            var key = GetLocalMachine64().OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\{comGuid:B}", true);
            key?.SetValue("Disabled", 0);
        }

        public static void EnableCredentialProvider(Type type)
        {
            var comGuid = GetComGuid(type);
            EnableCredentialProvider(comGuid);
        }

        private static void CreateCredentialProviderRegistration(Type t)
        {
            var comGuid = GetComGuid(t);
            var typeName = GetTypeFullName(t);

            var key = GetLocalMachine64().CreateSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\{comGuid:B}", true);
            key.SetValue(null, typeName);
        }

        private static void DeleteCredentialProviderRegistration(Type t)
        {
            var comGuid = GetComGuid(t);
            DeleteCredentialProviderRegistration(comGuid);
        }

        private static void DeleteCredentialProviderRegistration(Guid clsid)
        {
            var reg = GetLocalMachine64().OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers", true);
            reg.DeleteSubKeyTree($"{clsid:B}", false);
        }

        private static void RegisterNetCoreAssembly(Type t)
        {
            var comGuid = GetComGuid(t);
            var typeName = GetTypeFullName(t);
            var progId = GetComProgId(t);
            var assemblyLocation = GetTypeAssemblyLocation(t);

            var dir = Path.GetDirectoryName(assemblyLocation);
            var assemblyFile = Path.GetFileNameWithoutExtension(assemblyLocation);
            var comHostLocation = Path.Combine(dir, assemblyFile + ".comhost.dll");

            var rootClsid = GetLocalMachine64().CreateSubKey($@"Software\Classes\CLSID\{comGuid:B}", true);
            rootClsid.SetValue(null, "CoreCLR COMHost Server");

            var inprocKey = rootClsid.CreateSubKey("InprocServer32", true);
            inprocKey.SetValue(null, comHostLocation);
            inprocKey.SetValue("ThreadingModel", "Both");

            var progIdKey = rootClsid.CreateSubKey("ProgId", true);
            progIdKey.SetValue(null, progId);

            var progIdRoot = GetLocalMachine64().CreateSubKey($@"Software\Classes\{progId}", true);
            progIdRoot.SetValue(null, typeName);
            var progIdSubKey = progIdRoot.CreateSubKey("CLSID");
            progIdSubKey.SetValue(null, comGuid.ToString("B"));
        }

        private static void UnregisterNetCoreAssembly(Type t)
        {
            var comGuid = GetComGuid(t);
            var progId = GetComProgId(t);

            UnregisterClass(comGuid, progId);
        }

        private static void UnregisterClass(Guid? clsid, string progId)
        {
            var reg = GetLocalMachine64().OpenSubKey(@"Software\Classes", true);

            if (clsid != null)
            {
                reg.DeleteSubKeyTree($@"CLSID\{clsid:B}", false);
            }

            if (!string.IsNullOrWhiteSpace(progId))
            {
                reg.DeleteSubKeyTree(progId, false);
            }
        }

        private static void RegisterFrameworkAssembly(Type t)
        {
            var comGuid = GetComGuid(t);
            var typeName = GetTypeFullName(t);
            var progId = GetComProgId(t);

            var rootReg = GetLocalMachine64().CreateSubKey(@"Software\Classes");

            var rootClsid = rootReg.CreateSubKey($@"CLSID\{comGuid:B}", true);
            rootClsid.SetValue(null, typeName);

            rootClsid.CreateSubKey("Implemented Categories");
            rootClsid.CreateSubKey(@"Implemented Categories\{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}");

            var inprocKey = rootClsid.CreateSubKey("InprocServer32", true);
            inprocKey.SetValue(null, "mscoree.dll");
            inprocKey.SetValue("ThreadingModel", "Both");
            inprocKey.SetValue("Class", typeName);
            inprocKey.SetValue("RuntimeVersion", "v4.0.30319");
            inprocKey.SetValue("Assembly", GetTypeAssemblyName(t));
            inprocKey.SetValue("CodeBase", GetTypeAssemblyLocation(t));

            var progIdKey = rootClsid.CreateSubKey("ProgId", true);
            progIdKey.SetValue(null, progId);

            var progIdRoot = rootReg.CreateSubKey(progId, true);
            progIdRoot.SetValue(null, typeName);
            var progIdSubKey = progIdRoot.CreateSubKey("CLSID");
            progIdSubKey.SetValue(null, comGuid.ToString("B"));
        }

        private static void UnregisterFrameworkAssembly(Type t)
        {
            var comGuid = GetComGuid(t);
            var progId = GetComProgId(t);

            UnregisterClass(comGuid, progId);
        }

        public static void UnregisterClass(Guid clsid)
        {
            string progid = null;

            try
            {
                progid = GetProgIdFromClasid(clsid);
            }
            catch (NotFoundException) { }

            UnregisterClass(clsid, progid);
        }

        public static void UnregisterClass(string progId)
        {
            Guid? clsid = null;
            try
            {
                clsid = GetClsidFromProgId(progId);
            }
            catch (NotFoundException) { }

            UnregisterClass(clsid, progId);
        }

        public static Guid GetClsidFromProgId(string progId)
        {
            var value = GetClassesRoot64().OpenSubKey($@"{progId}\CLSID")?.GetValue(string.Empty) as string;

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ClsidNotFoundException($"The clsid for ProgId was not found {progId}");
            }

            return Guid.Parse(value);
        }

        public static string GetProgIdFromClasid(Guid clsid)
        {
            var value = GetClassesRoot64().OpenSubKey($@"CLSID\{clsid:B}\ProgId")?.GetValue(string.Empty) as string;

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ProgIdNotFoundException($"The ProgId for clsid was not found {clsid:B}");
            }

            return value;
        }

        private static string GetTypeAssemblyLocation(Type type)
        {
            return type.Assembly.Location;
        }

        private static string GetTypeAssemblyName(Type type)
        {
            return type.Assembly.FullName;
        }

        private static string GetTypeClassName(Type type)
        {
            return type.Name;
        }

        private static string GetTypeFullName(Type type)
        {
            return type.FullName;
        }

        private static Guid GetComGuid(Type type)
        {
            var typeId = type.GetCustomAttributeValue("GuidAttribute");

            if (typeId == null)
            {
                throw new ArgumentException($"The type {type.Name} does not have the Guid attribute present");
            }

            return new Guid(typeId);
        }

        private static string GetComProgId(Type type)
        {
            var typeId = type.GetCustomAttributeValue("ProgIdAttribute");

            if (typeId == null)
            {
                throw new ArgumentException($"The type {type.Name} does not have the ProgId attribute present");
            }

            return typeId;
        }

        private static bool IsFrameworkType(Type type)
        {
            return IsFrameworkAssembly(type.Assembly);
        }

        private static bool IsFrameworkAssembly(Assembly assembly)
        {
            var framework = assembly.GetCustomAttributeValue("TargetFrameworkAttribute");
            return framework.StartsWith(".NETFramework");
        }


        private static string GetCustomAttributeValue(this Type type, string attributeName)
        {
            var cads = type.GetCustomAttributesData();
            foreach (CustomAttributeData cad in cads.Where(a => a.AttributeType.Name == attributeName))
            {
                return cad.ConstructorArguments.FirstOrDefault().Value as string;
            }

            return String.Empty;
        }

        private static string GetCustomAttributeValue(this Assembly assembly, string attributeName)
        {
            foreach (CustomAttributeData cad in assembly.GetCustomAttributesData().Where(a => a.AttributeType.Name == attributeName))
            {
                return cad.ConstructorArguments.FirstOrDefault().Value as string;
            }

            return String.Empty;
        }

        public static IEnumerable<Type> GetCredentialProviders(Assembly assembly)
        {
            List<Type> types = new List<Type>();

            foreach (var type in assembly.GetExportedTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                try
                {
                    foreach (var i in type.GetInterfaces())
                    {
                        if (i.Name == "ICredentialProvider")
                        {
                            types.Add(type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Type {type} failed with {ex.ToString()}");
                }
            }

            return types;
        }

        public static AssemblyFromMetadataLoadContext LoadAssembly(string assemblyPath)
        {
            string assemblyBasePath = Path.GetDirectoryName(assemblyPath);

            List<string> paths = new List<string>();
            paths.AddRange(Directory.GetFiles(assemblyBasePath));
            paths.Add(typeof(object).Assembly.Location);

            paths.AddRange(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));

            var resolver = new PathAssemblyResolver(paths);
            MetadataLoadContext mlc = new MetadataLoadContext(resolver);
            return new AssemblyFromMetadataLoadContext(mlc.LoadFromAssemblyPath(assemblyPath), mlc);
        }
    }
}
