using System;

namespace ONI_MP.Networking.Compatibility
{
    public class ModInfo
    {
        public string StaticID { get; set; }
        public string Version { get; set; }
        public string Title { get; set; }
        public bool IsRequired { get; set; }
        public bool AllowVersionMismatch { get; set; }

        public ModInfo()
        {
        }

        public ModInfo(string staticId, string version = "unknown", string title = null, bool isRequired = true)
        {
            StaticID = staticId;
            Version = version;
            Title = title ?? staticId;
            IsRequired = isRequired;
            AllowVersionMismatch = false;
        }

        public bool Matches(ModInfo other)
        {
            if (StaticID != other.StaticID)
                return false;

            if (!AllowVersionMismatch && Version != other.Version)
                return false;

            return true;
        }

        public bool HasVersionMismatch(ModInfo other)
        {
            return StaticID == other.StaticID && Version != other.Version;
        }

        public override string ToString()
        {
            return $"{StaticID} v{Version}";
        }

        public override bool Equals(object obj)
        {
            if (obj is ModInfo other)
            {
                return StaticID == other.StaticID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return StaticID?.GetHashCode() ?? 0;
        }

        // Mods conhecidos que não são críticos para compatibilidade
        public static readonly string[] NON_CRITICAL_MODS = new string[]
        {
            "Local-Testing-Mod",
            "Debug-Mod",
            "DevTool"
        };

        public bool IsCritical()
        {
            foreach (var nonCritical in NON_CRITICAL_MODS)
            {
                if (StaticID.Contains(nonCritical))
                {
                    return false;
                }
            }
            return true;
        }
    }
}