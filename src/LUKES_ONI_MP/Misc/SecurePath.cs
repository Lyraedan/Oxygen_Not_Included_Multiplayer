using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Misc
{
    public static class SecurePath
    {
        public static string Combine(string root, params string[] paths)
        {
            var absoluteRoot = Path.GetFullPath(root);
            var relativePath = Path.Combine(absoluteRoot, Path.Combine(paths));
            var absolutePath = Path.GetFullPath(relativePath);
            if (absolutePath == absoluteRoot || absolutePath.StartsWith(EnsureEndsWithSeparator(absoluteRoot)))
                return absolutePath;

            throw new IOException($"Unable to access \"{relativePath}\" as it's outside of \"{absoluteRoot}\"");
        }

        private static string EnsureEndsWithSeparator(string value) =>
            value.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

    }
}
