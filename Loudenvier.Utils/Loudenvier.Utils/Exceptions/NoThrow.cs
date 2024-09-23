using System;

namespace Loudenvier.Utils;

public static class NoThrow
{
    public static void Do(Action action) {
        try {
            action();
        } catch { }
    }
}
