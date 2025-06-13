﻿using System.Reflection;
using UnityEngine;

namespace ONI_MP.Patches.Navigation
{
    public static class NavigatorExtensions
    {
        private static readonly FieldInfo tacticField =
            typeof(Navigator).GetField("tactic", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo targetOffsetsField =
            typeof(Navigator).GetField("<targetOffsets>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo clearReservedCellMethod =
            typeof(Navigator).GetMethod("ClearReservedCell", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Safe alternative to Navigator.GoTo that clients can use.
        /// Avoids Harmony-blocked methods.
        /// </summary>
        public static bool ClientGoTo(this Navigator navigator, KMonoBehaviour target, CellOffset[] offsets, NavTactic tactic = null)
        {
            if (navigator == null || target == null || navigator.smi == null || navigator.smi.sm == null)
                return false;

            if (tactic == null)
            {
                tactic = NavigationTactics.ReduceTravelDistance;
            }

            navigator.smi.GoTo(navigator.smi.sm.normal.moving);
            navigator.smi.sm.moveTarget.Set(target.gameObject, navigator.smi);

            tacticField?.SetValue(navigator, tactic);
            navigator.target = target;
            targetOffsetsField?.SetValue(navigator, offsets);
            clearReservedCellMethod?.Invoke(navigator, null);

            navigator.AdvancePath();

            return navigator.IsMoving();
        }
    }
}
