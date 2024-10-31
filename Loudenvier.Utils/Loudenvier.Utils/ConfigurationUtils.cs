using System;
using System.Configuration;
using System.ComponentModel;
using System.Collections.Specialized;

namespace Loudenvier.Utils;

// adds some depencies

/*    public static class ConfigurationUtils
{
    /// <summary>
    /// Gets the value associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <typeparam name="T">Type of the parameter to be read</typeparam>
    /// <param name="settingsCollection">The AppSettings collection (or any <see cref="NameValueCollection"/></param>
    /// <param name="key">The key to the desired value</param>
    /// <param name="def">A default value to be returned in case of a missing key (defults to <typeparamref name="T"/> default)</param>
    /// <returns>The value associeted with the specified key or the specified default value if the key does not exist</returns>
    public static T GetValue<T>(this NameValueCollection settingsCollection, string key, T def = default) {
        var value = settingsCollection[key];
        if (string.IsNullOrWhiteSpace(value))
            return def;
        TypeConverter tc = TypeDescriptor.GetConverter(typeof(T));
        if (tc != null)
            return (T)tc.ConvertFromString(null, System.Globalization.CultureInfo.InvariantCulture, value);
        return def;
    }
    /// <summary>
    /// Gets the value associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <typeparam name="T">Type of the parameter to be read</typeparam>
    /// <param name="settingsCollection">The AppSettings collection (or any <see cref="NameValueCollection"/></param>
    /// <param name="key">The key to the desired value</param>
    /// <param name="def">A default value to be returned in case of a missing key (defults to <typeparamref name="T"/> default)</param>
    /// <returns>The value associeted with the specified key or the specified default value if the key does not exist</returns>
    public static T Val<T>(this NameValueCollection settingsCollection, string key, T def = default) => GetValue(settingsCollection, key, def);

    public static string Get(this ConnectionStringSettingsCollection conns, string key, string defaultConn = null) {
        var conn = conns[key];
        return conn != null ? conn.ConnectionString : defaultConn;
    }

    public static string Get(string configKey, string _default = null)
         => ConfigurationManager.AppSettings.Get(configKey) ?? _default;
    public static TimeSpan GetTime(string configKey, TimeSpan _default = default) 
        => Get(configKey).ToNullableTimeSpan() ?? _default;

    public static T Get<T>(string configKey, T _default = default)
        => ConfigurationManager.AppSettings.GetValue<T>(configKey, _default);
}*/
