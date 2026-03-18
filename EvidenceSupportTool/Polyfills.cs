using System;

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// .NET 5.0 未満（.NET Core 3.1 等）で C# 9.0 の record や init プロパティを使用するためのポリフィル。
    /// </summary>
    internal static class IsExternalInit { }

    /// <summary>
    /// .NET 7.0 未満で C# 11.0 の required プロパティを使用するためのポリフィル。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute { }

    /// <summary>
    /// .NET 7.0 未満で C# 11.0 の required プロパティを使用するために必要な属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }
        public string FeatureName { get; }
        public bool IsOptional { get; set; }
        public const string RequiredMembers = nameof(RequiredMembers);
    }

    /// <summary>
    /// .NET 7.0 未満で C# 11.0 の required プロパティをコンストラクタで初期化することを明示するための属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute { }
}
#endif
