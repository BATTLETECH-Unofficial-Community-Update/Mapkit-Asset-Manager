using AsmResolver.DotNet;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.IO.Endian;
using AssetRipper.IO.Files;
using AssetRipper.IO.Files.SerializedFiles;
using AssetRipper.SerializationLogic;
using AssetRipper.SourceGenerated.Classes.ClassID_115;
using AssetRipper.SourceGenerated.Subclasses.Hash128;
using System.Buffers.Binary;

namespace AssetRipper.Import.Structure.Assembly;

public static class MonoScriptExtensions
{
	/// <summary>
	/// Before 2018.2 or Release or after 2022.2
	/// </summary>
	public static bool HasAssemblyName(this IMonoScript monoScript)
	{
		return monoScript.Collection.Flags.IsRelease() || !monoScript.IsReleaseOnly_AssemblyName();
	}

	public static string GetValidAssemblyName(this IMonoScript monoScript)
	{
		string name = SpecialFileNames.FixAssemblyName(monoScript.AssemblyName);
		return string.IsNullOrEmpty(name) ? "Assembly-CSharp" : name;
	}

	/// <summary>
	/// Apply <see cref="SpecialFileNames.FixAssemblyName(string)"/> to <see cref="IMonoScript.AssemblyName"/>.
	/// </summary>
	/// <param name="monoScript">The relevant MonoScript.</param>
	/// <returns></returns>
	public static string GetAssemblyNameFixed(this IMonoScript monoScript)
	{
		return SpecialFileNames.FixAssemblyName(monoScript.AssemblyName);
	}

	public static SerializableType? GetBehaviourType(this IMonoScript monoScript, IAssemblyManager assemblyManager)
	{
		return monoScript.GetBehaviourType(assemblyManager, out _);
	}

	public static SerializableType? GetBehaviourType(this IMonoScript monoScript, IAssemblyManager assemblyManager, out string? failureReason)
	{
		// Note: We use the MonoScript collection version here, not the MonoBehaviour version.
		// It probably doesn't matter because MonoScript and MonoBehaviour versions are usually the same,
		// and even when they are different, they still have the same major and minor version.
		// Also, Unity doesn't change serialization very often at all.

		ScriptIdentifier scriptID = assemblyManager.GetScriptID(monoScript.GetAssemblyNameFixed(), monoScript.Namespace, monoScript.ClassName_R);

		// Fallback: when the exact assembly+namespace+name lookup fails, look the class up by name in the same assembly,
		// nested types included. A type matches on ClassName_R, ClassName, or ClassName_R ignoring case; the first match in
		// enumeration order wins. Handles MonoScript names that do not resolve through namespace+name (nested, renamed types).
		if (assemblyManager.IsSet && !assemblyManager.IsValid(scriptID))
		{
			TypeDefinition? foundType = null;
			string asmName = monoScript.GetAssemblyNameFixed();
			foreach (AssemblyDefinition asm in assemblyManager.GetAssemblies())
			{
				string asmNameStr = asm.Name?.ToString() ?? "";
				if (!string.Equals(asmNameStr, asmName, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				foreach (ModuleDefinition module in asm.Modules)
				{
					foreach (TypeDefinition type in module.GetAllTypes())
					{
						// one test per type: any of the three name matches ends the search
						string typeName = type.Name?.ToString() ?? "";
						if (typeName == monoScript.ClassName_R?.ToString() ||
							typeName == monoScript.ClassName?.ToString() ||
							string.Equals(typeName, monoScript.ClassName_R?.ToString(), StringComparison.OrdinalIgnoreCase))
						{
							foundType = type;
							break;
						}
					}
					if (foundType != null) break;
				}
				if (foundType != null) break;
			}

			if (foundType != null)
			{
				scriptID = new ScriptIdentifier(asmName, foundType.Namespace?.ToString() ?? "", foundType.Name?.ToString() ?? "");
			}
		}

		if (!assemblyManager.IsSet)
		{
			failureReason = null;
		}
		else if (!assemblyManager.IsValid(scriptID))
		{
			failureReason = "Script ID is invalid";
		}
		else if (assemblyManager.TryGetSerializableType(scriptID, monoScript.Collection.Version, out SerializableType? result, out failureReason))
		{
			return result;
		}
		return null;
	}

	public static string GetFullName(this IMonoScript monoScript)
	{
		if (string.IsNullOrEmpty(monoScript.Namespace))
		{
			return monoScript.ClassName_R;
		}
		else
		{
			return $"{monoScript.Namespace}.{monoScript.ClassName_R}";
		}
	}

	public static ScriptIdentifier GetScriptID(this IMonoScript monoScript, IAssemblyManager assemblyManager)
	{
		return assemblyManager.GetScriptID(monoScript.GetAssemblyNameFixed(), monoScript.Namespace, monoScript.ClassName_R);
	}

	public static TypeDefinition GetTypeDefinition(this IMonoScript monoScript, IAssemblyManager assemblyManager)
	{
		ScriptIdentifier scriptID = monoScript.GetScriptID(assemblyManager);
		return assemblyManager.GetTypeDefinition(scriptID);
	}

	public static bool IsScriptPresents(this IMonoScript monoScript, IAssemblyManager assemblyManager)
	{
		ScriptIdentifier scriptID = monoScript.GetScriptID(assemblyManager);
		return assemblyManager.IsPresent(scriptID);
	}

	public static Hash128_5 GetPropertiesHash(this IMonoScript monoScript)
	{
		if (monoScript.Has_PropertiesHash_Hash128_5())
		{
			return monoScript.PropertiesHash_Hash128_5;
		}
		else
		{
			Span<byte> hash = stackalloc byte[4];
			//I have reason to believe that this depends on the endianness of the file containing the MonoScript,
			//but most platforms are little endian, so I don't really know for sure.
			if (monoScript.Collection.EndianType == EndianType.BigEndian)
			{
				BinaryPrimitives.WriteUInt32BigEndian(hash, monoScript.PropertiesHash_UInt32);
			}
			else
			{
				BinaryPrimitives.WriteUInt32LittleEndian(hash, monoScript.PropertiesHash_UInt32);
			}
			return new()
			{
				Bytes__0 = hash[0],
				Bytes__1 = hash[1],
				Bytes__2 = hash[2],
				Bytes__3 = hash[3],
			};
		}
	}
}
