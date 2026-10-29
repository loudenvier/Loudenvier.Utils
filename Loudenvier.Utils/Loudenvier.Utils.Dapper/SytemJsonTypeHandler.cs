using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Dapper;
using Loudenvier.Utils.Json;

namespace Loudenvier.Utils.Dapper;

/// <summary>
/// Marks a concrete CLR type whose values are mapped to SQL Server NVARCHAR(MAX)
/// JSON columns. Registration also covers arrays (T[]) and List&lt;T&gt;.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class JsonContentAttribute : Attribute { }

/// <summary>
/// Shared JSON configuration. A contract modifier normalizes non-nullable
/// List&lt;T&gt;/T[] properties after JSON deserialization (missing or explicit null).
/// Reflection and delegate compilation take place when STJ creates/caches the
/// contract, never on each deserialized object's hot path.
/// </summary>
internal static class JsonColumnSerializer
{
    internal static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions() {
        var options = new JsonSerializerOptions(JsonOptions.Compact);

        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(ConfigureCollectionNormalization);

        options.TypeInfoResolver = resolver;
        options.MakeReadOnly();
        return options;
    }

    private static void ConfigureCollectionNormalization(JsonTypeInfo typeInfo) {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        var rules = new List<CollectionRule>();
        var statements = new List<Expression>();
        var instance = Expression.Parameter(typeof(object), "instance");
        bool canCompileDirect = !typeInfo.Type.IsValueType; // Boxed structs need contract setters.

        foreach (JsonPropertyInfo property in typeInfo.Properties) {
            Type collectionType = property.PropertyType;
            bool isArray = collectionType.IsArray && collectionType.GetArrayRank() == 1;
            bool isList = collectionType.IsGenericType &&
                          collectionType.GetGenericTypeDefinition() == typeof(List<>);

            if ((!isArray && !isList) || property.Get == null || property.Set == null)
                continue;

            // The annotations are encoded by the C# compiler using
            // NullableAttribute / NullableContextAttribute. Decode them
            // through CustomAttributeData (also works on net472/netstandard2.0).
            MemberInfo? member = property.AttributeProvider as MemberInfo;
            if (member == null || !NullableMetadata.IsNonNullable(member))
                continue;

            rules.Add(new CollectionRule(property.Get, property.Set, collectionType, isArray));

            if (!canCompileDirect)
                continue;

            // Prefer one JIT-compiled direct getter/assignment routine per
            // contract. For uncommon metadata/visibility/init-only cases,
            // use the cached STJ getter/setter delegates as a safe fallback.
            try {
                Expression obj = Expression.Convert(instance, typeInfo.Type);
                Expression access;
                PropertyInfo? pi = member as PropertyInfo;
                FieldInfo? fi = member as FieldInfo;

                if (pi != null && pi.GetIndexParameters().Length == 0 &&
                    pi.GetGetMethod(true) != null && pi.GetSetMethod(true) != null &&
                    !IsInitOnly(pi.GetSetMethod(true)!))
                    access = Expression.Property(obj, pi);
                else if (fi != null && !fi.IsInitOnly && !fi.IsLiteral)
                    access = Expression.Field(obj, fi);
                else
                    throw new NotSupportedException("Member requires JSON-contract accessor.");

                if (access.Type != collectionType)
                    throw new NotSupportedException("Member type differs from JSON contract.");

                Expression empty = isArray
                    ? Expression.Call(
                        typeof(Array).GetMethod("Empty", BindingFlags.Public | BindingFlags.Static)!
                            .MakeGenericMethod(collectionType.GetElementType()!))
                    : Expression.New(collectionType);

                statements.Add(Expression.IfThen(
                    Expression.Equal(access, Expression.Constant(null, collectionType)),
                    Expression.Assign(access, empty)));
            } catch (Exception ex) when (ex is ArgumentException ||
                                         ex is InvalidOperationException ||
                                         ex is NotSupportedException) {
                canCompileDirect = false;
                statements.Clear();
            }
        }

        if (rules.Count == 0)
            return;

        Action<object> normalizer;
        if (canCompileDirect && statements.Count == rules.Count) {
            try {
                // With all setters available, this performs plain null
                // checks + assignments: no reflection and no allocations
                // unless a List<T> actually needs to be created.
                normalizer = Expression.Lambda<Action<object>>(
                    Expression.Block(statements), instance).Compile();
            } catch (Exception ex) when (ex is PlatformNotSupportedException ||
                                         ex is NotSupportedException ||
                                         ex is MemberAccessException ||
                                         ex is InvalidOperationException) {
                normalizer = BuildFallback(rules);
            }
        } else {
            normalizer = BuildFallback(rules);
        }

        // Preserve IJsonOnDeserialized or any callback already registered.
        Action<object>? previous = typeInfo.OnDeserialized;
        typeInfo.OnDeserialized = previous == null
            ? normalizer
            : obj => { normalizer(obj); previous(obj); };
    }

    private static bool IsInitOnly(MethodInfo setter) {
        // An init accessor should be handled via STJ's cached contract setter
        // instead of emitting a normal assignment to an init-only property.
        foreach (Type mod in setter.ReturnParameter.GetRequiredCustomModifiers())
            if (mod.FullName == "System.Runtime.CompilerServices.IsExternalInit")
                return true;
        return false;
    }

    private static Action<object> BuildFallback(List<CollectionRule> rules) {
        var prepared = new PreparedRule[rules.Count];
        for (int i = 0; i < rules.Count; i++) {
            CollectionRule rule = rules[i];
            Func<object> makeEmpty;

            if (rule.IsArray) {
                // Array.Empty<T>() is invoked only once at contract creation.
                object singleton = typeof(Array)
                    .GetMethod("Empty", BindingFlags.Public | BindingFlags.Static)!
                    .MakeGenericMethod(rule.Type.GetElementType()!)
                    .Invoke(null, null)!;
                makeEmpty = () => singleton;
            } else {
                Type listType = rule.Type;
                // Only called when null. Creating List<T> is unavoidable.
                makeEmpty = () => Activator.CreateInstance(listType)!;
            }

            prepared[i] = new PreparedRule(rule.Get, rule.Set, makeEmpty);
        }

        return obj => {
            for (int i = 0; i < prepared.Length; i++) {
                PreparedRule rule = prepared[i];
                if (rule.Get(obj) == null)
                    rule.Set(obj, rule.CreateEmpty());
            }
        };
    }

    private sealed class CollectionRule
    {
        internal readonly Func<object, object?> Get;
        internal readonly Action<object, object?> Set;
        internal readonly Type Type;
        internal readonly bool IsArray;

        internal CollectionRule(Func<object, object?> get, Action<object, object?> set,
            Type type, bool isArray) {
            Get = get;
            Set = set;
            Type = type;
            IsArray = isArray;
        }
    }

    private sealed class PreparedRule
    {
        internal readonly Func<object, object?> Get;
        internal readonly Action<object, object?> Set;
        internal readonly Func<object> CreateEmpty;

        internal PreparedRule(Func<object, object?> get, Action<object, object?> set,
            Func<object> createEmpty) {
            Get = get;
            Set = set;
            CreateEmpty = createEmpty;
        }
    }
}

/// <summary>
/// Reads compiler-generated nullable metadata without using
/// NullabilityInfoContext (which is unavailable in all the target TFMs).
/// Runs ONLY when a System.Text.Json contract is created.
/// Unknown/oblivious annotations are conservatively left unchanged.
/// </summary>
internal static class NullableMetadata
{
    private const string NullableAttributeName =
        "System.Runtime.CompilerServices.NullableAttribute";
    private const string ContextAttributeName =
        "System.Runtime.CompilerServices.NullableContextAttribute";

    internal static bool IsNonNullable(MemberInfo member) {
        // Explicit annotations override any enclosing nullable context,
        // including the explicitly oblivious (0) state.
        IList<CustomAttributeData> annotations = member.GetCustomAttributesData();
        byte? flag = ReadFlag(annotations, NullableAttributeName);
        if (flag.HasValue)
            return flag.Value == 1;

        flag = ReadFlag(annotations, ContextAttributeName);
        if (flag.HasValue)
            return flag.Value == 1;

        Type? owner = member.DeclaringType;
        while (owner != null) {
            flag = ReadFlag(owner.GetCustomAttributesData(), ContextAttributeName);
            if (flag.HasValue)
                return flag.Value == 1;
            owner = owner.DeclaringType;
        }

        // No reliable nullable information: do not change the value.
        return false;
    }

    private static byte? ReadFlag(IList<CustomAttributeData> attributes, string fullName) {
        foreach (CustomAttributeData attribute in attributes) {
            if (attribute.AttributeType.FullName != fullName ||
                attribute.ConstructorArguments.Count == 0)
                continue;

            CustomAttributeTypedArgument first = attribute.ConstructorArguments[0];
            if (first.Value is byte v)
                return v;

            // NullableAttribute(byte[]) encodes generic/nested nullability;
            // the first element indicates the nullability of the member itself.
            if (first.Value is IList<CustomAttributeTypedArgument> flags && flags.Count != 0 && flags[0].Value is byte)
                return (byte)flags[0].Value!;
        }
        return null;
    }
}

/// <summary>
/// Dapper handler for a single value stored as NVARCHAR(MAX) JSON.
/// The handler is registered globally by CLR type, not per property.
/// </summary>
public sealed class SystemJsonTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    private static readonly JsonSerializerOptions Options = JsonColumnSerializer.Options;

    public override T? Parse(object value) {
        if (value == null || value is DBNull)
            return default;

        if (value is not string json)
            throw new DataException(
                "Expected JSON string for " + typeof(T).FullName +
                ", received " + value.GetType().FullName + ".");

        return JsonSerializer.Deserialize<T>(json, Options);
    }

    public override void SetValue(IDbDataParameter parameter, T? value) {
        if (parameter == null)
            throw new ArgumentNullException(nameof(parameter));

        parameter.DbType = DbType.String; // SQL Server Unicode NVARCHAR
        parameter.Size = -1;             // NVARCHAR(MAX)
        parameter.Value = value is null
            ? (object)DBNull.Value
            : JsonSerializer.Serialize(value, Options);
    }
}

/// <summary>
/// Discovers [JsonContent] types and registers handlers for T, T[] and
/// List&lt;T&gt;. Invoke once at startup, before executing Dapper queries.
/// </summary>
public static class DapperJsonRegistration
{
    private static readonly object Sync = new object();

    public static void Register(params Assembly[] assemblies) {
        if (assemblies == null)
            throw new ArgumentNullException(nameof(assemblies));

        lock (Sync) {
            foreach (Assembly assembly in assemblies.Distinct()) {
                if (assembly == null)
                    continue;

                foreach (Type type in GetLoadableTypes(assembly)) {
                    if (!type.IsDefined(typeof(JsonContentAttribute), false))
                        continue;

                    if (type.ContainsGenericParameters || type.IsAbstract || type.IsInterface)
                        throw new InvalidOperationException(
                            "[JsonContent] requires a concrete closed type: " + type.FullName);

                    RegisterHandler(type);
                    RegisterHandler(type.MakeArrayType());
                    RegisterHandler(typeof(List<>).MakeGenericType(type));
                }
            }
        }
    }

    public static void RegisterFromAssemblyContaining<TMarker>() {
        Register(typeof(TMarker).Assembly);
    }

    private static void RegisterHandler(Type type) {
        if (SqlMapper.HasTypeHandler(type))
            return; // Preserve existing explicitly configured handlers.

        Type handlerType = typeof(SystemJsonTypeHandler<>).MakeGenericType(type);
        var handler = (SqlMapper.ITypeHandler)Activator.CreateInstance(handlerType)!;
        SqlMapper.AddTypeHandler(type, handler);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException ex) {
            return ex.Types.Where(type => type != null).Select(type => type!);
        }
    }
}
