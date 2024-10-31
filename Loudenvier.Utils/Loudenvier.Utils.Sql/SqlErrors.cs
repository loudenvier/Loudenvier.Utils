using System;
using System.Collections.Generic;
using System.Text;

namespace Loudenvier.Utils;

public static class SqlErrors
{
    public const int PRIMARY_KEY_VIOLATION = 2627;
    public const int UNIQUE_CONSTRAINT_VIOLATION = 2601;

    public static readonly int[] DuplicateKeyViolations = [PRIMARY_KEY_VIOLATION, UNIQUE_CONSTRAINT_VIOLATION];
}
