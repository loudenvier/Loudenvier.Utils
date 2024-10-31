using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Loudenvier.Utils;

public class DuplicateKeyData
{
    static bool TryParseMessage(string errorMessage, out IEnumerable<string> data) {
        data = errorMessage.GrabErrorInfo();
        return data != null && data.Count() >= 3;
    }
    static IEnumerable<string> ParseMessage(string errorMessage) {
        var result = errorMessage.GrabErrorInfo(); 
        if (result is null || result.Count() < 3) {
            throw new ArgumentOutOfRangeException(nameof(errorMessage), errorMessage,
                "The error message does not contain all the needed fields of a PRIMARY KEY or duplicate CONSTRAINT violation. " +
                "Are you sure you passed the correct error message? If your SQL language is not English this may fail even " +
                "with a proper error message due to different formatting. It's also not guaranteed to work on every version of SQL Server.");
        }
        return result;
    }

    readonly record struct DataIdx(int Object, int Index, int Value);
    static readonly DataIdx PKIndexes = new(Object: 1, Index: 0, Value: 2);
    static readonly DataIdx DKIndexes = new(Object: 0, Index: 1, Value: 2);

    protected DuplicateKeyData(int errorCode, IEnumerable<string> errorInfo) {
        ErrorCode = errorCode;
        var idx = IsPrimaryKeyViolation ? PKIndexes : DKIndexes;
        IndexOrConstraint = errorInfo.ElementAt(idx.Index);
        Object = errorInfo.ElementAt(idx.Object);
        Value = errorInfo.ElementAt(idx.Value);
    }
    public DuplicateKeyData(int errorCode, string errorMessage) : this(errorCode, ParseMessage(errorMessage)) { }

    public int ErrorCode { get; }
    public string Object { get; }
    public string IndexOrConstraint { get; }
    public string Value { get; }
    public bool IsPrimaryKeyViolation => ErrorCode == SqlErrors.PRIMARY_KEY_VIOLATION;

    static readonly DuplicateKeyData Dummy = new(0, new string[] {"","",""});
    public static bool TryParse(int errorCode, string errorMessage, out DuplicateKeyData data) {
        if (!TryParseMessage(errorMessage, out var errorInfo)) { 
            data = Dummy;
            return false;
        }
        data = new(errorCode, errorInfo); 
        return true;
    }
    public static DuplicateKeyData? FromException(Exception ex) {
        var error = ex.FindFirstError(SqlErrors.DuplicateKeyViolations);
        if (error is null) return null;
        if (TryParse(error.Number, error.Message, out var result))
            return result;
        return null;
    }
}
