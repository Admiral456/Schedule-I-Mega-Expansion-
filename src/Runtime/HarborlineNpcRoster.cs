using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using S1API.Entities;
using S1API.Economy;
using S1API.Map;
using UnityEngine;

namespace ScheduleIMegaExpansion.Runtime
{
    /// <summary>
    /// First native NPC wave for Harborline. These are real S1API NPC types:
    /// S1API supplies the game prefab, appearance components, save/load and FishNet lifecycle.
    /// Coordinates are derived from the exact same clear-ground candidate used by the dockyard builder.
    /// </summary>
    internal static class HarborlineNpcSpawnPoints
    {
        private static readonly Vector2[] LocalSlots =
        {
            new Vector2(-4.5f, 10.0f), // dealer: warehouse west loading bay
            new Vector2(4.5f, 10.0f),  // dealer: warehouse east loading bay
            new Vector2(-17.0f, -9.0f),// customer: market/crate side
            new Vector2(-1.5f, -11.0f),// customer: loading apron
            new Vector2(15.0f, 7.0f)   // customer: access road side
        };

        public static bool TryGet(int slot, out Vector3 position)
        {
            position = Vector3.zero;
            if (slot < 0 || slot >= LocalSlots.Length)
                return false;

            if (!HarborlinePhysicalBuilder.TryGetPlannedNpcSpawnPosition(
                    slot, out float x, out float y, out float z))
                return false;

            position = new Vector3(x, y, z);
            return true;
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

            if (TryGet(slot, out Vector3 position))
                builder.WithSpawnPosition(position);
            else
                MelonLogger.Warning("[Schedule I Mega Expansion] Native NPC prefab '" + id +
                    "' was registered before Harborline ground placement was measurable. " +
                    "The runtime registrar will position its instance after the dockyard is built.");
        }

        public static Vector3 GetRuntimePosition(int slot)
        {
            if (!TryGet(slot, out Vector3 position))
                throw new InvalidOperationException(
                    "Harborline has no verified ground spawn point yet; NPC creation is deferred.");
            return position;
        }
    }

    public sealed class HarborlineDealerMason : NPC
    {
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
                .WithCompletedDealsVariable("harborline_mason_completed_deals"))
                .WithSchedule(plan => plan.EnsureDealSignal());
        }

        protected override void OnCreated()
        {
            base.OnCreated();
            HarborlineNpcLifecycle.InitializePhysicalNpc(this);
        }
    }

    public sealed class HarborlineDealerSierra : NPC
    {
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
                .WithCompletedDealsVariable("harborline_sierra_completed_deals"))
                .WithSchedule(plan => plan.EnsureDealSignal());
        }

        protected override void OnCreated()
        {
            base.OnCreated();
            HarborlineNpcLifecycle.InitializePhysicalNpc(this);
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
                .WithStandards("Moderate")
                .WithCallPoliceChance(0.02f)
                .WithDependence(0.10f, 0.25f));
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
                .WithStandards("High")
                .WithCallPoliceChance(0.04f)
                .WithDependence(0.20f, 0.35f));
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
                .WithStandards("VeryHigh")
                .WithCallPoliceChance(0.07f)
                .WithDependence(0.05f, 0.20f));
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
                npc.Schedule.InitializeActions();
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
                warn("Harborline NPC creation is waiting for a confirmed single-player host. Multiplayer spawning remains disabled until the full host/late-join test.");
                return false;
            }

            if (!HarborlineNpcSpawnPoints.TryGet(0, out _))
            {
                warn("Harborline NPC creation is waiting for the measured dockyard placement.");
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
            // S1API's prefab builder already carries this spawn point. Assign it again on the
            // created wrapper so the first save's instance and subsequent reloads agree.
            npc.Position = HarborlineNpcSpawnPoints.GetRuntimePosition(slot);
            created++;
        }
    }
}
