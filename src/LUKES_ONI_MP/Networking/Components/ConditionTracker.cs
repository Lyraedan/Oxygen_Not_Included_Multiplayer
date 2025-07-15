using System;
using UnityEngine;
using Klei.AI;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.DuplicantActions;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking.Components
{
    public class ConditionTracker : KMonoBehaviour
    {
        private MinionIdentity minion;

        // Public properties for tracking condition
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public float Calories { get; private set; }
        public float Stress { get; private set; }
        public float Breath { get; private set; }
        public float Bladder { get; private set; }
        public float Stamina { get; private set; }
        public float BodyTemperature { get; private set; }
        public float Morale { get; private set; }

        private float nextSyncTime = 0f;
        private const float SyncInterval = 1f;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            minion = GetComponent<MinionIdentity>();
            if (minion == null)
            {
                DebugConsole.LogWarning("[ConditionTracker] Missing MinionIdentity.");
                return;
            }

            SubscribeToHealth();
            SubscribeToAmounts();
            SubscribeToAttributes();

            DebugConsole.Log($"[ConditionTracker] Tracking started for {minion.name}");
        }

        private void Update()
        {
            if (!MultiplayerSession.IsHost)
                return;

            if (Time.time >= nextSyncTime)
            {
                nextSyncTime = Time.time + SyncInterval;
                SendConditionPacket();
            }
        }

        // Called on host to push values into the game
        public void ApplyHealth(float health, float maxHealth)
        {
            var component = GetComponent<Health>();
            if (component != null)
            {
                component.hitPoints = health;
                var maxHpField = typeof(Health).GetField("maxHitPoints", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (maxHpField != null)
                {
                    maxHpField.SetValue(component, maxHealth);
                }
                else
                {
                    //DebugConsole.LogWarning("[ConditionTracker] Could not reflect maxHitPoints field.");
                }

                Health = health;
                MaxHealth = maxHealth;
            }
        }

        public void ApplyAmounts(
            float calories,
            float stress,
            float breath,
            float bladder,
            float stamina,
            float bodyTemperature)
        {
            var db = Db.Get();
            var go = gameObject;

            Calories = calories;
            Stress = stress;
            Breath = breath;
            Bladder = bladder;
            Stamina = stamina;
            BodyTemperature = bodyTemperature;

            db.Amounts.Calories.Lookup(go)?.SetValue(calories);
            db.Amounts.Stress.Lookup(go)?.SetValue(stress);
            db.Amounts.Breath.Lookup(go)?.SetValue(breath);
            db.Amounts.Bladder.Lookup(go)?.SetValue(bladder);
            db.Amounts.Stamina.Lookup(go)?.SetValue(stamina);
            db.Amounts.Temperature.Lookup(go)?.SetValue(bodyTemperature);
        }

        public void ApplyAttributes(float morale)
        {
            Morale = morale;

            var moraleAttr = Db.Get().Attributes.QualityOfLife.Lookup(gameObject);
            if (moraleAttr != null)
            {
                // Use reflection to set private baseValue inside AttributeInstance.Attribute
                var attributeField = typeof(AttributeInstance)
                    .GetField("attribute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var attribute = attributeField?.GetValue(moraleAttr);
                if (attribute != null)
                {
                    var baseValueField = attribute.GetType()
                        .GetField("BaseValue", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                    if (baseValueField != null)
                    {
                        baseValueField.SetValue(attribute, morale);
                    }
                    else
                    {
                        //DebugConsole.LogWarning("[ConditionTracker] Failed to reflect BaseValue on Attribute.");
                    }
                }
                else
                {
                    //DebugConsole.LogWarning("[ConditionTracker] Failed to reflect attribute field on AttributeInstance.");
                }
            }
        }


        public void ApplyAll(
            float health, float maxHealth,
            float calories, float stress, float breath, float bladder,
            float stamina, float bodyTemp,
            float morale)
        {
            ApplyHealth(health, maxHealth);
            ApplyAmounts(calories, stress, breath, bladder, stamina, bodyTemp);
            ApplyAttributes(morale);
        }

        private void SubscribeToHealth()
        {
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.GetAmountInstance.OnDelta += (dt) =>
                {
                    Health = health.hitPoints;
                    MaxHealth = health.maxHitPoints;
                    //DebugConsole.Log($"[Logger] {minion.name} - Health updated: {Health}/{MaxHealth}");
                };

                Health = health.hitPoints;
                MaxHealth = health.maxHitPoints;
            }
        }

        private void SubscribeToAmounts()
        {
            var db = Db.Get();
            var go = gameObject;

            void HookAmount(Amount amount, Action<float> setter)
            {
                var instance = amount.Lookup(go);
                if (instance == null) return;

                instance.OnDelta += delta =>
                {
                    setter(instance.value);
                    //DebugConsole.Log($"[Logger] {minion.name} - {amount.Id} changed: {instance.value}");
                };

                setter(instance.value); // set initial
            }

            HookAmount(db.Amounts.Calories, v => Calories = v);
            HookAmount(db.Amounts.Stress, v => Stress = v);
            HookAmount(db.Amounts.Breath, v => Breath = v);
            HookAmount(db.Amounts.Bladder, v => Bladder = v);
            HookAmount(db.Amounts.Stamina, v => Stamina = v);
            HookAmount(db.Amounts.Temperature, v => BodyTemperature = v);
        }

        private void SubscribeToAttributes()
        {
            var attributes = GetComponent<Attributes>();
            if (attributes != null)
            {
                foreach (var attrInstance in attributes)
                {
                    if (attrInstance.Id == "Morale")
                    {
                        attrInstance.OnDirty += () =>
                        {
                            Morale = attrInstance.GetTotalValue();
                            //DebugConsole.Log($"[Logger] {minion.name} - Morale updated: {Morale}");
                        };

                        Morale = attrInstance.GetTotalValue();
                    }
                }
            }
        }

        public void SendConditionPacket()
        {
            if (!MultiplayerSession.IsHost)
                return;

            if (!TryGetComponent(out NetworkIdentity identity))
                return;

            var packet = new DuplicantConditionPacket
            {
                NetId = identity.NetId,
                Health = Health,
                MaxHealth = MaxHealth,
                Calories = Calories,
                Stress = Stress,
                Breath = Breath,
                Bladder = Bladder,
                Stamina = Stamina,
                BodyTemperature = BodyTemperature,
                Morale = Morale
            };

            PacketSender.SendToAllClients(packet);
        }

    }
}
