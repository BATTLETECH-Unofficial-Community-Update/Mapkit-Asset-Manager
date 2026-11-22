using AssetRipper.Assets.Collections;

namespace AssetRipper.Assets;

/// <summary>
/// UCU map kit patch: export GUIDs derived from an asset's identity in the game files instead of <see cref="UnityGuid.NewGuid"/>,
/// so two exports of different level selections give the assets they share the same GUID, and a re-export gives the same GUIDs.
/// </summary>
/// <remarks>
/// Keys, each hashed with <see cref="UnityGuid.Md5Hash(string)"/>:
/// <list type="bullet">
/// <item>an asset read from a game file: <c>file/&lt;file name&gt;/&lt;path ID&gt;</c>;</item>
/// <item>an asset AssetRipper generated (a <see cref="ProcessedAssetCollection"/>, whose path IDs depend on what else was loaded):
/// <c>processed/&lt;collection&gt;/&lt;class ID&gt;/&lt;directory&gt;/&lt;name&gt;</c>;</item>
/// <item>a scene with no GUID of its own: <c>scene/&lt;level file name&gt;</c>.</item>
/// </list>
/// Two different keys that hash to the same GUID in one export (a generated asset with the same directory and name as another)
/// get <c>#n</c> appended; those rows say <c>collision</c> in <c>guid_map.tsv</c>.
/// </remarks>
public static class DeterministicGuids
{
	private static readonly Lock gate = new();
	private static readonly Dictionary<string, UnityGuid> byKey = new();
	private static readonly Dictionary<UnityGuid, (string Kind, string Key)> byGuid = new();
	private static readonly List<string> rows = new();

	public static UnityGuid ForAsset(IUnityObjectBase asset)
	{
		return asset.Collection is ProcessedAssetCollection
			? Issue("processed", $"processed/{asset.Collection.Name}/{asset.ClassID}/{asset.GetBestDirectory()}/{asset.GetBestName()}")
			: Issue("file", $"file/{asset.Collection.Name}/{asset.PathID}");
	}

	public static UnityGuid ForScene(string levelFileName) => Issue("scene", $"scene/{levelFileName}");

	private static UnityGuid Issue(string kind, string key)
	{
		lock (gate)
		{
			if (byKey.TryGetValue(key, out UnityGuid known))
			{
				return known;
			}
			string hashed = key;
			for (int n = 1; byGuid.ContainsKey(UnityGuid.Md5Hash(hashed)); n++)
			{
				hashed = $"{key}#{n}";
				kind = "collision";
			}
			UnityGuid guid = UnityGuid.Md5Hash(hashed);
			byKey.Add(key, guid);
			byGuid.Add(guid, (kind, hashed));
			return guid;
		}
	}

	/// <summary>Records one exported file: its path relative to the project, its GUID and the key the GUID came from.</summary>
	public static void Exported(string relativePath, UnityGuid guid)
	{
		lock (gate)
		{
			(string kind, string key) = byGuid.TryGetValue(guid, out var k) ? k : ("random", "");
			rows.Add($"{relativePath.Replace('\\', '/')}\t{guid}\t{kind}\t{key}");
		}
	}

	/// <summary>One row per exported file: path, GUID, kind (file, processed, scene, collision), key.</summary>
	public static string[] Rows()
	{
		lock (gate)
		{
			return rows.ToArray();
		}
	}
}
