using SpookilySharp;
using System.Text.Json;

namespace Loudenvier.Utils.Dapper;  

public class CacheIdentity: IEquatable<CacheIdentity>
{
    public CacheIdentity(string sql, object? param = null) {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentNullException(nameof(sql));
        Sql = sql;
        this.Param = param;
        CalculateHash();
    }
    public object? Param { get; }
    public string Sql { get; }

    private void CalculateHash() {
        // TODO: Make Hashcode independent from Param field order!
        var s = string.Concat(Sql, Serialize(Param));
        Key = s.SpookyHash128();
    }
    public HashCode128 Key { get; private set; }
    public override string ToString() => Key.ToString();
    public static implicit operator string(CacheIdentity id) => id.ToString();

    #region (In)Equality

    public static bool operator ==(CacheIdentity left, CacheIdentity right) => left.Equals(right);
    public static bool operator !=(CacheIdentity left, CacheIdentity right) => !(left == right);
    public bool Equals(CacheIdentity? o) {
        if (o is null) return false;
        return ReferenceEquals(this, o) || Key.Equals(o.Key) && Sql.Equals(o.Sql) && (Param?.Equals(Param) ?? true);
    }
    public override bool Equals(object? o) => Equals(o as CacheIdentity);
    public override int GetHashCode() => unchecked((int)Key.Hash1);

    #endregion

    static string Serialize(object? o) => JsonSerializer.Serialize(o);
}