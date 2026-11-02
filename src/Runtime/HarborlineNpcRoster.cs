using System;
using System.Linq;
using MelonLoader;
using System.Reflection;
using S1API.Entities;
using S1API.Economy;
using S1API.Map;
using S1API.Products;

namespace ScheduleIMegaExpansion.Runtime
{
    /// <summary>
    /// First native NPC wave for Harborline. These are real S1API NPC types:
    /// S1API supplies the game prefab, appearance components, save/load and FishNet lifecycle.
    /// Coordinates are derived from the exact same clear-ground candidate used by the dockyard builder.
    /// </summary>
    internal static class HarborlineNpcSpawnPoints
    {
        public static bool TryGet(int slot, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            if (slot < 0 || slot > 4)
                return false;

            return HarborlinePhysicalBuilder.TryGetPlannedNpcSpawnPosition(
                slot, out x, out y, out z);
        }

        private static object CreateNativeVector3(float x, float y, float z)
        {
            PropertyInfo? position = typeof(NPC).GetProperty(
                "Position", BindingFlags.Public | BindingFlags.Instance);
            Type? vectorType = position?.PropertyType;
            if (vectorType == null)
                throw new MissingMemberException("S1API NPC.Position property type could not be resolved.");

            object? value = Activator.CreateInstance(vectorType, new object[] { x, y, z });
            return value ?? throw new InvalidOperationException("Could not construct the native Unity Vector3 value.");
        }

        private static bool TryConfigurePrefabSpawnPosition(
            NPCPrefabBuilder builder, float x, float y, float z)
        {
            try
            {
                MethodInfo? method = typeof(NPCPrefabBuilder).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(candidate => candidate.Name == "WithSpawnPosition"
                        && candidate.GetParameters().Length == 1);
                if (method == null)
                    return false;
                method.Invoke(builder, new[] { CreateNativeVector3(x, y, z) });
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Schedule I Mega Expansion] Could not attach a measured spawn point to the NPC prefab: " + ex.Message);
                return false;
            }
        }

        public static bool TryApplyRuntimePosition(NPC npc, int slot)
        {
            if (!TryGet(slot, out float x, out float y, out float z))
                return false;

            try
            {
                PropertyInfo? position = npc.GetType().GetProperty(
                    "Position", BindingFlags.Public | BindingFlags.Instance);
                if (position?.CanWrite != true)
                    return false;
                position.SetValue(npc, CreateNativeVector3(x, y, z));
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Schedule I Mega Expansion] Could not position NPC '" + npc.ID + "': " + ex.Message);
                return false;
            }
        }

        public static void Configure(
            NPCPrefabBuilder builder,
            string id,
            string firstName,
            string lastName,
            int slot)
        {
            builder.WithIdentity(id, firstName, lastName)
                .WithRegion(Region.Docks);

            if (!TryGet(slot, out float x, out float y, out float z)
                || !TryConfigurePrefabSpawnPosition(builder, x, y, z))
            {
                MelonLogger.Msg("[Schedule I Mega Expansion] NPC '" + id +
                    "' will receive its measured position after the dockyard is generated.");
            }
        }
    }

    public sealed class HarborlineDealerMason : NPC
    {
        private Action? _recruitedHandler;
        private Action? _contractAcceptedHandler;
        private Action? _recommendedHandler;

        public override bool IsPhysical => true;
        public override bool IsDealer => true;

        public HarborlineDealerMason() : base() { }

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            HarborlineNpcSpawnPoints.Configure(builder, "harborline_dealer_mason", "Mason", "Reed", 0);
            builder.WithDealerDefaults(dealer => dealer
                .WithSigningFee(2500f)
                .WithCut(0.18f)
                .WithDealerType(DealerType.PlayerDealer)
                .AllowInsufficientQuality(false)
                .AllowExcessQuality(true)
                .WithCompletedDealsVariable("harborline_mason_completed_deals"));
            builder.WithRelationshipDefaults(relationship => relationship
                .WithDelta(1.5f)
                .SetUnlocked(false)
                .SetUnlockType(NPCRelationship.UnlockType.DirectApproach));
        }

        protected override void OnCreated()
        {
            base.OnCreated();
            HarborlineNpcLifecycle.InitializePhysicalNpc(this);
            WireDealerEvents();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            UnwireDealerEvents();
        }

        private void WireDealerEvents()
        {
            if (Dealer == null)
                return;
            _recruitedHandler ??= () => SendTextMessage("I'm ready to work. Let's get started.");
            _contractAcceptedHandler ??= () => SendTextMessage("I've accepted a new customer contract.");
            _recommendedHandler ??= () => SendTextMessage("A customer sent you my way. Good to meet you.");
            Dealer.OnRecruited -= _recruitedHandler;
            Dealer.OnRecruited += _recruitedHandler;
            Dealer.OnContractAccepted -= _contractAcceptedHandler;
            Dealer.OnContractAccepted += _contractAcceptedHandler;
            Dealer.OnRecommended -= _recommendedHandler;
            Dealer.OnRecommended += _recommendedHandler;
        }

        private void UnwireDealerEvents()
        {
            if (Dealer == null)
                return;
            if (_recruitedHandler != null)
                Dealer.OnRecruited -= _recruitedHandler;
            if (_contractAcceptedHandler != null)
                Dealer.OnContractAccepted -= _contractAcceptedHandler;
            if (_recommendedHandler != null)
                Dealer.OnRecommended -= _recommendedHandler;
        }
    }

    public sealed class HarborlineDealerSierra : NPC
    {
        private Action? _recruitedHandler;
        private Action? _contractAcceptedHandler;
        private Action? _recommendedHandler;

        public override bool IsPhysical => true;
        public override bool IsDealer => true;

        public HarborlineDealerSierra() : base() { }

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            HarborlineNpcSpawnPoints.Configure(builder, "harborline_dealer_sierra", "Sierra", "Voss", 1);
            builder.WithDealerDefaults(dealer => dealer
                .WithSigningFee(4000f)
                .WithCut(0.15f)
                .WithDealerType(DealerType.PlayerDealer)
                .AllowInsufficientQuality(false)
                .AllowExcessQuality(true)
                .WithCompletedDealsVariable("harborline_sierra_completed_deals"));
            builder.WithRelationshipDefaults(relationship => relationship
                .WithDelta(1.5f)
                .SetUnlocked(false)
                .SetUnlockType(NPCRelationship.UnlockType.DirectApproach));
        }

        protected override void OnCreated()
        {
            base.OnCreated();
            HarborlineNpcLifecycle.InitializePhysicalNpc(this);
            WireDealerEvents();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            UnwireDealerEvents();
        }

        private void WireDealerEvents()
        {
            if (Dealer == null)
                return;
            _recruitedHandler ??= () => SendTextMessage("I'm ready to work. Let's get started.");
            _contractAcceptedHandler ??= () => SendTextMessage("I've accepted a new customer contract.");
            _recommendedHandler ??= () => SendTextMessage("A customer sent you my way. Good to meet you.");
            Dealer.OnRecruited -= _recruitedHandler;
            Dealer.OnRecruited += _recruitedHandler;
            Dealer.OnContractAccepted -= _contractAcceptedHandler;
            Dealer.OnContractAccepted += _contractAcceptedHandler;
            Dealer.OnRecommended -= _recommendedHandler;
            Dealer.OnRecommended += _recommendedHandler;
        }

        private void UnwireDealerEvents()
        {
            if (Dealer == null)
                return;
            if (_recruitedHandler != null)
                Dealer.OnRecruited -= _recruitedHandler;
            if (_contractAcceptedHandler != null)
                Dealer.OnContractAccepted -= _contractAcceptedHandler;
            if (_recommendedHandler != null)
                Dealer.OnRecommended -= _recommendedHandler;
        }
    }

    public sealed class HarborlineCustomerCaleb : NPC
    {
        public override bool IsPhysical => true;
        public override bool IsCustomer => true;

        public HarborlineCustomerCaleb() : base() { }

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            HarborlineNpcSpawnPoints.Configure(builder, "harborline_customer_caleb", "Caleb", "Price", 2);
            builder.WithCustomerDefaults(customer => customer
                .WithSpending(180f, 420f)
                .WithOrdersPerWeek(1, 2)
                .WithPreferredOrderDay("Monday")
                .WithOrderTime(630)
                .WithStandards(CustomerStandard.Moderate)
                .AllowDirectApproach(true)
                .WithCallPoliceChance(0.02f)
                .WithDependence(0.10f, 0.25f)
                .WithAffinities(new[]
                {
                    (DrugType.Marijuana, 0.75f),
                    (DrugType.Methamphetamine, -0.25f)
                }));
        }

        protected override void OnCreated()
        {
            base.OnCreated();
            HarborlineNpcLifecycle.InitializePhysicalNpc(this);
        }
    }

    public sealed class HarborlineCustomerNina : NPC
    {
        public override bool IsPhysical => true;
        public override bool IsCustomer => true;

        public HarborlineCustomerNina() : base() { }

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            HarborlineNpcSpawnPoints.Configure(builder, "harborline_customer_nina", "Nina", "Brooks", 3);
            builder.WithCustomerDefaults(customer => customer
                .WithSpending(450f, 850f)
                .WithOrdersPerWeek(2, 3)
                .WithPreferredOrderDay("Wednesday")
                .WithOrderTime(1200)
                .WithStandards(CustomerStandard.High)
                .AllowDirectApproach(true)
                .WithCallPoliceChance(0.04f)
                .WithDependence(0.20f, 0.35f)
                .WithAffinities(new[]
                {
                    (DrugType.Methamphetamine, 0.75f),
                    (DrugType.Marijuana, -0.30f)
                }));
        }

        protected override void OnCreated()
        {
            base.OnCreated();
            HarborlineNpcLifecycle.InitializePhysicalNpc(this);
        }
    }

    public sealed class HarborlineCustomerRafael : NPC
    {
        public override bool IsPhysical => true;
        public override bool IsCustomer => true;

        public HarborlineCustomerRafael() : base() { }

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            HarborlineNpcSpawnPoints.Configure(builder, "harborline_customer_rafael", "Rafael", "Cole", 4);
            builder.WithCustomerDefaults(customer => customer
                .WithSpending(800f, 1600f)
                .WithOrdersPerWeek(1, 3)
                .WithPreferredOrderDay("Friday")
                .WithOrderTime(1930)
                .WithStandards(CustomerStandard.VeryHigh)
                .AllowDirectApproach(true)
                .WithCallPoliceChance(0.07f)
                .WithDependence(0.05f, 0.20f)
                .WithAffinities(new[]
                {
                    (DrugType.Cocaine, 0.70f),
                    (DrugType.Shrooms, 0.35f),
                    (DrugType.Methamphetamine, -0.30f)
                }));
        }

        protected override void OnCreated()
        {
            base.OnCreated();
            HarborlineNpcLifecycle.InitializePhysicalNpc(this);
        }
    }

    internal static class HarborlineNpcLifecycle
    {
        public static void InitializePhysicalNpc(NPC npc)
        {
            try
            {
                // Generate once in OnCreated. The native NPC save pipeline preserves the built appearance;
                // we deliberately do not reroll on OnLoaded or client join.
                npc.Appearance.GenerateRandomAppearance();
                npc.Appearance.Build();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Schedule I Mega Expansion] Native appearance generation failed for " +
                    npc.ID + ": " + ex.Message);
            }

            try
            {
                npc.Schedule.Enable();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Schedule I Mega Expansion] Native schedule initialization failed for " +
                    npc.ID + ": " + ex.Message);
            }
        }
    }

    internal static class HarborlineNpcRegistrar
    {
        private static bool _warnedPlayerGate;
        private static bool _warnedPrefabReadiness;
        private static bool _warnedPlacement;

        private static readonly string[] StableIds =
        {
            "harborline_dealer_mason",
            "harborline_dealer_sierra",
            "harborline_customer_caleb",
            "harborline_customer_nina",
            "harborline_customer_rafael"
        };

        public static bool EnsureInitialWave(Action<string> log, Action<string> warn)
        {
            if (!HarborlinePhysicalBuilder.IsConfirmedSinglePlayerSession())
            {
                if (!_warnedPlayerGate)
                {
                    _warnedPlayerGate = true;
                    warn("Harborline NPC creation is waiting for a confirmed single-player host. Multiplayer spawning remains disabled until the full host/late-join test.");
                }
                return false;
            }

            if (!NPC.CustomNpcsReady)
            {
                if (!_warnedPrefabReadiness)
                {
                    _warnedPrefabReadiness = true;
                    warn("Waiting for S1API to finish registering Harborline NPC prefabs through FishNet.");
                }
                return false;
            }

            if (!HarborlineNpcSpawnPoints.TryGet(0, out _, out _, out _))
            {
                if (!_warnedPlacement)
                {
                    _warnedPlacement = true;
                    warn("Harborline NPC creation is waiting for the measured dockyard placement.");
                }
                return false;
            }

            int created = 0;
            int alreadyPresent = 0;
            try
            {
                // S1API creates real NPC wrappers and native network prefabs. Reuse an existing
                // saved NPC by stable ID; never create a duplicate on reload.
                CreateIfMissing("harborline_dealer_mason", () => new HarborlineDealerMason(), 0, ref created, ref alreadyPresent);
                CreateIfMissing("harborline_dealer_sierra", () => new HarborlineDealerSierra(), 1, ref created, ref alreadyPresent);
                CreateIfMissing("harborline_customer_caleb", () => new HarborlineCustomerCaleb(), 2, ref created, ref alreadyPresent);
                CreateIfMissing("harborline_customer_nina", () => new HarborlineCustomerNina(), 3, ref created, ref alreadyPresent);
                CreateIfMissing("harborline_customer_rafael", () => new HarborlineCustomerRafael(), 4, ref created, ref alreadyPresent);
            }
            catch (Exception ex)
            {
                warn("Native Harborline NPC registration stopped safely: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }

            log("Native Harborline NPC wave ready: " + created + " newly created, " +
                alreadyPresent + " already present. Stable NPC IDs: " + string.Join(", ", StableIds));
            return true;
        }

        public static void ResetForNewSave()
        {
            _warnedPlayerGate = false;
            _warnedPrefabReadiness = false;
            _warnedPlacement = false;
        }

        private static void CreateIfMissing<T>(
            string id,
            Func<T> factory,
            int slot,
            ref int created,
            ref int alreadyPresent) where T : NPC
        {
            if (NPC.Get(id) != null)
            {
                alreadyPresent++;
                return;
            }

            T npc = factory();
            // Prefer the native prefab spawn point; reapply after creation so geometry and NPC
            // share the exact measured coordinates even if S1API scanned prefabs before world load.
            if (!HarborlineNpcSpawnPoints.TryApplyRuntimePosition(npc, slot))
                throw new InvalidOperationException("Native position assignment failed for '" + id + "'.");
            created++;
        }
    }
}
