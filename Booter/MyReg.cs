using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;

namespace Booter
{
    public class RegNode 
    {
        public RegNode Parent = null;
        public List<RegNode> Nodes = new List<RegNode>();
        public string Name = "";
        public string Path  = "";
        public object Value = "";

        public void Add(RegNode son) 
        {
            Nodes.Add(son);
            son.Parent = this;
        }
    }

    /// <summary>
    /// 提供注册表的访问的一些方法
    /// </summary>
    /// 
    public class MyReg
    {
        /// <summary>
        /// 获取没有重定向的注册表对象
        /// </summary>
        /// <param name="key">原始注册表对象</param>
        /// <returns></returns>
        public static RegistryKey GetNonRedirectionKey(RegistryKey key)
        {
            SafeRegistryHandle handle = key.Handle;
            RegistryKey result;
            if (Environment.Is64BitOperatingSystem)
            {
                result = RegistryKey.FromHandle(handle, RegistryView.Registry64);
            }
            else
            {
                result = RegistryKey.FromHandle(handle, RegistryView.Registry32);
            }
            return result;
        }

        /// <summary>
        /// 获取键值对的列表
        /// </summary>
        /// <param name="root">根注册表项</param>
        /// <param name="path">子键名</param>
        /// <returns></returns>
        public static Dictionary<string, object> GetValues(RegistryKey root, string path)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            RegistryKey dest;
            try
            {
                RegistryKey rk = MyReg.GetNonRedirectionKey(root);
                dest = rk.OpenSubKey(path);
            }
            catch { return result; }
            if (dest == null)
            {
                return result;
            }
            foreach (string name in dest.GetValueNames())
            {
                try
                {
                    result.Add(name, dest.GetValue(name));
                }
                catch {}
            }
            return result;
        }

        /// <summary>
        /// 获取子键名列表
        /// </summary>
        /// <param name="root">根注册表项</param>
        /// <param name="path">子键名</param>
        /// <returns></returns>
        public static string[] GetSubKeyNames(RegistryKey root, string path) 
        {
            string[] result = { };
            RegistryKey dest;
            try
            {
                RegistryKey rk = MyReg.GetNonRedirectionKey(root);
                dest = rk.OpenSubKey(path);
                result = dest.GetSubKeyNames();
            }
            catch { }
            return result;
        }

        public static List<RegNode> FindAllCOMFolders(RegistryKey root, string path, ref RegNode tr) 
        {
            List<RegNode> result = new List<RegNode>();
            tr.Path = root.Name + @"\" + path;
            tr.Name = path.Substring(path.LastIndexOf('\\') + 1);
            MyReg._FindAllCOMFolders(root, path, ref result, ref tr);
            return result;
        }

        private static void _FindAllCOMFolders(RegistryKey root, string path, ref List<RegNode> result, ref RegNode tr)
        {
            RegistryKey dest = null;
            try
            {
                RegistryKey rk = MyReg.GetNonRedirectionKey(root);
                dest = rk.OpenSubKey(path);
            }
            catch { }
            if (dest == null) { return; }
            foreach (string subkeyname in dest.GetSubKeyNames())
            {
                RegNode node = new RegNode();
                node.Path = tr.Name + @"\" + subkeyname;
                node.Name = subkeyname;
                tr.Add(node);
                if (MyFileHelper.regs[3].Match(subkeyname.Trim()).Success) 
                {
                    try
                    {
                        Guid _t = new Guid(subkeyname.Trim());
                        result.Add(node);
                    }
                    catch { }
                }
                MyReg._FindAllCOMFolders(dest, subkeyname, ref result, ref node);
            }
        }

        public static List<RegNode> FindAllValues(RegistryKey root, string path, string[] keynames, ref RegNode tr)
        {
            List<RegNode> result = new List<RegNode>();
            tr.Path = root.Name + @"\" + path;
            tr.Name = path.Substring(path.LastIndexOf('\\') + 1);
            MyReg._FindAllValues(root, path, ref result, keynames, ref tr);
            return result;
        }

        private static void _FindAllValues(RegistryKey root, string path, ref List<RegNode> result, string[] keynames, ref RegNode tr)
        {
            RegistryKey dest = null;
            try
            {
                RegistryKey rk = MyReg.GetNonRedirectionKey(root);
                dest = rk.OpenSubKey(path);
            }
            catch { }
            if (dest == null) { return; }
            foreach (string name in dest.GetValueNames())
            {
                if (keynames != null)
                {
                    foreach (string key in keynames)
                    {
                        try
                        {
                            RegNode node = new RegNode();
                            node.Path = tr.Name + @"\" + name;
                            node.Name = name;
                            node.Value = dest.GetValue(name);
                            if (name.ToLower() == key.ToLower())
                            {
                                result.Add(node);
                            }
                            tr.Add(node);
                        }
                        catch { }
                    }
                }
                else 
                {
                    try
                    {
                        RegNode node = new RegNode();
                        node.Path = tr.Name + @"\" + name;
                        node.Name = name;
                        node.Value = dest.GetValue(name);
                        result.Add(node);
                        tr.Add(node);
                    }
                    catch { }
                }
            }
            foreach (string subkeyname in dest.GetSubKeyNames())
            {
                RegNode node = new RegNode();
                node.Path = tr.Name + @"\" + subkeyname;
                node.Name = subkeyname;
                tr.Add(node);
                MyReg._FindAllValues(dest, subkeyname, ref result, keynames, ref node);
            }
        }
    }

}
