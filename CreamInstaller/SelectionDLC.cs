using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Windows.Forms;

namespace CreamInstaller;

public enum DLCType
{
    None = 0,
    Steam,
    SteamHidden,
    Epic,
    EpicEntitlement
}

internal sealed class SelectionDLC : IEquatable<SelectionDLC>
{
    internal static readonly ConcurrentDictionary<SelectionDLC, byte> All = new();

    internal readonly string Id;
    internal readonly string Name;
    internal readonly TreeNode TreeNode;
    internal readonly DLCType Type;
    internal readonly string GameId;
    internal string Icon;
    internal string Product;
    internal string Publisher;
    private Selection selection;

    private SelectionDLC(DLCType type, string gameId, string id, string name)
    {
        Type = type;
        GameId = gameId;
        Id = id;
        Name = name;
        TreeNode = new() { Tag = Type, Name = Id, Text = Name };
        _ = All.TryAdd(this, 0);
    }

    internal bool Enabled
    {
        get => TreeNode.Checked;
        set => TreeNode.Checked = value;
    }

    internal Selection Selection
    {
        get => selection;
        set
        {
            if (ReferenceEquals(selection, value))
                return;
            selection = value;
            if (value is null)
            {
                _ = All.TryRemove(this, out _);
                TreeNode.Remove();
            }
            else
            {
                _ = All.TryAdd(this, default);
                _ = value.TreeNode.Nodes.Add(TreeNode);
                Enabled = Name != "Unknown" && value.Enabled;
            }
        }
    }

    public bool Equals(SelectionDLC other)
        => other is not null && (ReferenceEquals(this, other) ||
                                 Type == other.Type && GameId == other.GameId && Id == other.Id);

    internal static SelectionDLC GetOrCreate(DLCType type, string gameId, string id, string name)
        => FromId(type, gameId, id) ?? new SelectionDLC(type, gameId, id, name);

    internal static SelectionDLC FromId(DLCType type, string gameId, string dlcId)
        => All.Keys.FirstOrDefault(dlc => dlc.Type == type && dlc.GameId == gameId && dlc.Id == dlcId);

    public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is SelectionDLC other && Equals(other);

    public override int GetHashCode() => HashCode.Combine((int)Type, GameId, Id);
}