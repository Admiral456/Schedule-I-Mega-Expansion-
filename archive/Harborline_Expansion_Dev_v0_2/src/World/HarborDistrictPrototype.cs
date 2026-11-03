using System;
using S1MAPI.Building;
using S1MAPI.Building.Config;
using S1MAPI.ProceduralMesh;
using UnityEngine;
using HarborlineExpansion.Config;

namespace HarborlineExpansion.World
{
    /// <summary>
    /// Visual greybox only. Location is configured after a safe open-world coordinate survey.
    /// This intentionally does not spawn NPCs, alter save data, or claim multiplayer sync yet.
    /// </summary>
    public static class HarborDistrictPrototype
    {
        public static void Spawn(ExpansionConfig config, Action<string> log)
        {
            Vector3 o = config.Origin;

            // A broad reclaimed harbor platform. This is a prototype slab, not final terrain.
            CreateBox("Harborline_Ground", o + new Vector3(0f, -1f, 0f),
                new Vector3(220f, 2f, 150f), new Color(0.28f, 0.31f, 0.30f));

            // Main access road running north-south into the new district.
            CreateBox("Harborline_AccessRoad", o + new Vector3(0f, 0.05f, -90f),
                new Vector3(12f, 0.15f, 85f), new Color(0.13f, 0.14f, 0.15f));

            // Road edge / sidewalks.
            CreateBox("Harborline_Sidewalk_W", o + new Vector3(-8f, 0.12f, -90f),
                new Vector3(3f, 0.25f, 85f), new Color(0.50f, 0.50f, 0.47f));
            CreateBox("Harborline_Sidewalk_E", o + new Vector3(8f, 0.12f, -90f),
                new Vector3(3f, 0.25f, 85f), new Color(0.50f, 0.50f, 0.47f));

            // Portside Supply storefront.
            GameObject shop = new BuildingBuilder("Harborline_PortsideSupply")
                .WithConfig(BuildingConfig.Large)
                .AddFloor()
                .AddCeiling()
                .AddWalls(southDoor: true, eastWindow: true)
                .AddRoofTrim()
                .AddLights()
                .Build();

            shop.name = "Harborline_PortsideSupply";
            shop.transform.position = o + new Vector3(-34f, 0f, -36f);

            // Warehouse shell.
            GameObject warehouse = new BuildingBuilder("Harborline_FreightWarehouse")
                .WithConfig(BuildingConfig.Large)
                .AddFloor()
                .AddCeiling()
                .AddWalls(southDoor: true, eastWindow: false)
                .AddRoofTrim()
                .AddLights()
                .Build();

            warehouse.name = "Harborline_FreightWarehouse";
            warehouse.transform.position = o + new Vector3(38f, 0f, -34f);

            // Interior greybox props: counter and shelving.
            CreateBox("Portside_Counter_Greybox", o + new Vector3(-34f, 0.65f, -39f),
                new Vector3(5f, 1.3f, 1.0f), new Color(0.30f, 0.20f, 0.13f));
            CreateBox("Portside_Shelf_Left_Greybox", o + new Vector3(-42f, 1.0f, -27f),
                new Vector3(1.0f, 2.0f, 4.0f), new Color(0.35f, 0.30f, 0.23f));
            CreateBox("Portside_Shelf_Right_Greybox", o + new Vector3(-26f, 1.0f, -27f),
                new Vector3(1.0f, 2.0f, 4.0f), new Color(0.35f, 0.30f, 0.23f));

            // Parking / loading area.
            CreateBox("Harborline_LoadingYard", o + new Vector3(40f, 0.02f, -82f),
                new Vector3(55f, 0.1f, 28f), new Color(0.18f, 0.19f, 0.18f));

            log("Harborline visual greybox spawned at " + o + ". No NPC/shop interactions are enabled yet.");
        }

        private static GameObject CreateBox(string name, Vector3 worldPosition, Vector3 size, Color color)
        {
            GameObject go = new ProceduralMeshBuilder(name)
                .AddBox(Vector3.zero, size)
                .SetColor(color)
                .Build();

            go.name = name;
            go.transform.position = worldPosition;

            // Add a collider to the generated mesh if S1MAPI produced a readable mesh filter.
            MeshFilter? filter = go.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null && go.GetComponent<Collider>() == null)
            {
                MeshCollider collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }

            return go;
        }
    }
}