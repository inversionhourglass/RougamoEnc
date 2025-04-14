using System;
using System.Reflection;
using MonoMod.Core;
using MonoMod.Utils;
using System.Collections.Generic;
using System.Linq;

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(RougamoEnc.HotReloadService))]
namespace RougamoEnc
{
    public static class HotReloadService
    {
        // 保存原始方法的信息
        private static MethodBase? _originalMethod;
        
        // 保存第一次拷贝的方法 (M1)
        private static MethodInfo? _copiedMethodM1;
        
        // 保存热重载后拷贝的方法 (M2)
        private static MethodInfo? _copiedMethodM2;
        
        // 保存方法重定向的Detour
        private static ICoreDetour? _methodDetour;
        private static ICoreDetour? _rougamoMethodDetour;
        
        // 保存方法的签名信息用于比较
        private static string? _methodSignature;

        internal static void Initialize()
        {
            try
            {
                // 获取 Cls.M 方法
                Type clsType = typeof(Cls);
                _originalMethod = clsType.GetMethod("M", BindingFlags.Public | BindingFlags.Static);
                
                if (_originalMethod == null)
                {
                    Console.WriteLine("无法找到 Cls.M 方法");
                    return;
                }
                
                // 保存方法签名信息用于后续比较
                _methodSignature = GetMethodSignature(_originalMethod);
                
                // 使用 DynamicMethodDefinition 拷贝方法 (M1)
                _copiedMethodM1 = DynamicMethodHelper.CreateCopy(_originalMethod);
                
                Console.WriteLine("初始化完成：已保存 Cls.M 方法的副本");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化错误: {ex.Message}");
            }
        }

        internal static void ClearCache(Type[]? types) { }

        internal static void UpdateApplication(Type[]? types)
        {
            try
            {
                if (types == null || types.Length == 0)
                {
                    return;
                }
                
                // 检查是否包含 Cls 类型
                Type? clsType = types.FirstOrDefault(t => t.Name == "Cls");
                if (clsType == null)
                {
                    return;
                }
                
                // 获取热重载后的 Cls.M 方法
                MethodBase? updatedMethod = clsType.GetMethod("M", BindingFlags.Public | BindingFlags.Static);
                if (updatedMethod == null)
                {
                    Console.WriteLine("热重载后无法找到 Cls.M 方法");
                    return;
                }
                
                // 获取热重载后方法的签名
                string updatedMethodSignature = GetMethodSignature(updatedMethod);
                
                // 检查方法是否发生变化
                if (_methodSignature == updatedMethodSignature)
                {
                    Console.WriteLine("方法未发生变化，不需要更新");
                    return;
                }
                
                Console.WriteLine("检测到方法变化，开始更新...");
                
                // 更新方法签名
                _methodSignature = updatedMethodSignature;
                
                // 清除之前的Detour
                _methodDetour?.Dispose();
                _rougamoMethodDetour?.Dispose();
                
                // 拷贝更新后的方法 (M2)
                _copiedMethodM2 = DynamicMethodHelper.CreateCopy(updatedMethod);
                
                // 获取 Rougamo 生成的方法
                MethodBase? rougamoMethod = clsType.GetMethod("$Rougamo_M", BindingFlags.NonPublic | BindingFlags.Static);
                if (rougamoMethod == null)
                {
                    Console.WriteLine("无法找到 $Rougamo_M 方法");
                    return;
                }
                
                // 创建方法重定向
                // 将 Cls.M 重定向到 M1
                _methodDetour = DetourFactory.Current.CreateDetour(updatedMethod, _copiedMethodM1);
                
                // 将 Cls.$Rougamo_M 重定向到 M2
                _rougamoMethodDetour = DetourFactory.Current.CreateDetour(rougamoMethod, _copiedMethodM2);
                
                Console.WriteLine("热重载更新完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"热重载更新错误: {ex.Message}");
            }
        }
        
        // 获取方法签名用于比较方法是否发生变化
        private static string GetMethodSignature(MethodBase method)
        {
            // 使用 IL 指令作为方法签名
            try
            {
                return method.GetMethodBody()?.GetILAsByteArray()?.ToHexString() ?? string.Empty;
            }
            catch
            {
                // 如果无法获取 IL，则使用方法的哈希码
                return method.GetHashCode().ToString();
            }
        }
    }
    
    // 辅助类，用于创建方法的副本
    internal static class DynamicMethodHelper
    {
        public static MethodInfo CreateCopy(MethodBase original)
        {
            // 使用 DynamicMethodDefinition 创建方法副本
            using var dmd = new DynamicMethodDefinition(original);
            return dmd.Generate();
        }
    }
    
    // 扩展方法，用于将字节数组转换为十六进制字符串
    internal static class ByteArrayExtensions
    {
        public static string ToHexString(this byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }
}
