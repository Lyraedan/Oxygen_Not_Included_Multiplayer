using System.Collections.Generic;

namespace ONI_MP.Networking.Compatibility
{
    public class CompatibilityResult
    {
        public bool IsCompatible { get; set; }
        public string RejectReason { get; set; }
        public List<string> MissingMods { get; set; }
        public List<string> ExtraMods { get; set; }
        public List<string> VersionMismatches { get; set; }
        public List<string> Warnings { get; set; }

        public CompatibilityResult()
        {
            IsCompatible = false;
            RejectReason = "";
            MissingMods = new List<string>();
            ExtraMods = new List<string>();
            VersionMismatches = new List<string>();
            Warnings = new List<string>();
        }

        public static CompatibilityResult CreateApproved()
        {
            return new CompatibilityResult
            {
                IsCompatible = true,
                RejectReason = MP_STRINGS.UI.MODCOMPATIBILITY.COMPATIBILITYRESULT.APPROVED
            };
        }

        public static CompatibilityResult CreateRejected(string reason)
        {
            return new CompatibilityResult
            {
                IsCompatible = false,
                RejectReason = reason
            };
        }

        public void AddMissingMod(string modId, string modName = null)
        {
            string displayName = string.IsNullOrEmpty(modName) ? modId : modName;
            if (!MissingMods.Contains(displayName))
            {
                MissingMods.Add(displayName);
            }
        }

        public void AddExtraMod(string modId, string modName = null)
        {
            string displayName = string.IsNullOrEmpty(modName) ? modId : modName;
            if (!ExtraMods.Contains(displayName))
            {
                ExtraMods.Add(displayName);
            }
        }

        public void AddVersionMismatch(string modId, string modName = null)
        {
            string displayName = string.IsNullOrEmpty(modName) ? modId : modName;
            if (!VersionMismatches.Contains(displayName))
            {
                VersionMismatches.Add(displayName);
            }
        }

        public void AddWarning(string warning)
        {
            if (!Warnings.Contains(warning))
            {
                Warnings.Add(warning);
            }
        }

        public bool HasIssues()
        {
            return MissingMods.Count > 0 || ExtraMods.Count > 0 || VersionMismatches.Count > 0;
        }

        public override string ToString()
        {
            if (IsCompatible)
            {
                return string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.COMPATIBILITYRESULT.COMPATIBILITY_ISCOMPATIBLE, RejectReason);
            }
            else
            {
                var issues = new List<string>();
                if (MissingMods.Count > 0) issues.Add(string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.COMPATIBILITYRESULT.COMPATIBILITY_MISSING, MissingMods.Count));
                if (ExtraMods.Count > 0) issues.Add(string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.COMPATIBILITYRESULT.COMPATIBILITY_EXTRA, ExtraMods.Count));
                if (VersionMismatches.Count > 0) issues.Add(string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.COMPATIBILITYRESULT.COMPATIBILITY_MISMATCH, VersionMismatches.Count));

                return string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.COMPATIBILITYRESULT.COMPATIBILITY_INCOMPATIBLE, RejectReason, string.Join(", ", issues));
            }
        }
    }
}