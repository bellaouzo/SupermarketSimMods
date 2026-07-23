using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: IgnoresAccessChecksTo("Assembly-CSharp")]
[assembly: IgnoresAccessChecksTo("Assembly-CSharp-firstpass")]
[assembly: AssemblyCompany("Tsuteto / IL2CPP port")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyDescription("Employee Training Program for Supermarket Simulator (IL2CPP)")]
[assembly: AssemblyFileVersion("2.6.7.1")]
[assembly: AssemblyInformationalVersion("2.6.7.1-il2cpp")]
[assembly: AssemblyProduct("EmployeeTrainingProgram")]
[assembly: AssemblyTitle("EmployeeTrainingProgram")]
[assembly: AssemblyVersion("2.6.7.1")]

namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class IgnoresAccessChecksToAttribute : Attribute
{
	public IgnoresAccessChecksToAttribute(string assemblyName)
	{
	}
}
