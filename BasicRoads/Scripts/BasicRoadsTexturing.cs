// Project:         BasicRoads mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut
// Contributors:    Interkarma

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using System.IO;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using System;

namespace BasicRoads
{
    /// <summary>
    /// Generates texture tiles for terrains and uses marching squares for tile transitions.
    /// Paints roads onto terrain.
    /// </summary>
    public class BasicRoadsTexturing : DefaultTerrainTexturing
    {
        public const byte water = 0;
        public const byte dirt = 1;
        public const byte grass = 2;
        public const byte stone = 3;

        public const byte road = 46;
        public const byte road_grass = 55;
        public const byte road_dirt = 47;

        public const byte N  = 0b_1000_0000;
        public const byte NE = 0b_0100_0000;
        public const byte E  = 0b_0010_0000;
        public const byte SE = 0b_0001_0000;
        public const byte S  = 0b_0000_1000;
        public const byte SW = 0b_0000_0100;
        public const byte W  = 0b_0000_0010;
        public const byte NW = 0b_0000_0001;

        public const string RoadDataFilename = "roadData.bytes";

        static byte[] roadData;

        public BasicRoadsTexturing()
        {
            // Read in road data.
            TextAsset dataAsset;
            string fileName = Path.Combine(WorldDataReplacement.WorldDataPath, RoadDataFilename);

            if (ModManager.Instance.TryGetAsset(RoadDataFilename, false, out dataAsset))
            {
                roadData = dataAsset.bytes;
            }
            if (roadData == null || roadData.Length != MapsFile.MaxMapPixelX * MapsFile.MaxMapPixelY)
            {
                Debug.LogWarning("BasicRoads: Unable to load road data, starting with blank path data.");
                roadData = new byte[MapsFile.MaxMapPixelX * MapsFile.MaxMapPixelY];
            }
        }

        internal byte[] GetRoadData()
        {
            byte[] data = new byte[MapsFile.MaxMapPixelX * MapsFile.MaxMapPixelY];
            roadData.CopyTo(data, 0);
            return data;
        }

        internal void UpdateRoadData(byte[] data)
        {
            if (data != null && data.Length == roadData.Length)
                Array.Copy(data, roadData, data.Length);
        }

        public override JobHandle ScheduleAssignTilesJob(ITerrainSampler terrainSampler, ref MapPixelData mapData, JobHandle dependencies, bool march = true)
        {
            // Cache tile data to minimise noise sampling during march.
            NativeArray<byte> tileData = new NativeArray<byte>(tileDataDim * tileDataDim, Allocator.TempJob);
            GenerateTileDataJob tileDataJob = new GenerateTileDataJob
            {
                heightmapData = mapData.heightmapData,
                tileData = tileData,
                tdDim = tileDataDim,
                hDim = terrainSampler.HeightmapDimension,
                maxTerrainHeight = terrainSampler.MaxTerrainHeight,
                oceanElevation = terrainSampler.OceanElevation,
                beachElevation = terrainSampler.BeachElevation,
                mapPixelX = mapData.mapPixelX,
                mapPixelY = mapData.mapPixelY,
            };
            JobHandle tileDataHandle = tileDataJob.Schedule(tileDataDim * tileDataDim, 64, dependencies);

            // Assign tile data to terrain, painting roads in the process
            NativeArray<byte> lookupData = new NativeArray<byte>(lookupTable, Allocator.TempJob);
            AssignTilesWithRoadsJob assignTilesJob = new AssignTilesWithRoadsJob
            {
                lookupTable = lookupData,
                tileData = tileData,
                tilemapData = mapData.tilemapData,
                tdDim = tileDataDim,
                tDim = assignTilesDim,
                hDim = terrainSampler.HeightmapDimension,
                march = march,
                locationRect = mapData.locationRect,
                roadDataPt = roadData[mapData.mapPixelX + (mapData.mapPixelY * MapsFile.MaxMapPixelX)],
            };
            JobHandle assignTilesHandle = assignTilesJob.Schedule(assignTilesDim * assignTilesDim, 64, tileDataHandle);

            SmoothRoadTerrainJob smoothRoadTerrainJob = new SmoothRoadTerrainJob()
            {
                heightmapData = mapData.heightmapData,
                tilemapData = mapData.tilemapData,
                hDim = DaggerfallUnity.Instance.TerrainSampler.HeightmapDimension,
                tDim = assignTilesDim,
                locationRect = mapData.locationRect,
            };
            JobHandle smoothRoadHandle = smoothRoadTerrainJob.Schedule(assignTilesHandle);

            // Add both working native arrays to disposal list.
            mapData.nativeArrayList.Add(tileData);
            mapData.nativeArrayList.Add(lookupData);

            return smoothRoadHandle;
        }

        // Very basic marching squares from default texturer, with road painting added.
        struct AssignTilesWithRoadsJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<byte> tileData;
            [ReadOnly]
            public NativeArray<byte> lookupTable;

            public NativeArray<byte> tilemapData;

            public int tdDim;
            public int tDim;
            public int hDim;
            public bool march;
            public Rect locationRect;
            public byte roadDataPt;

            public void Execute(int index)
            {
                int x = JobA.Row(index, tDim);
                int y = JobA.Col(index, tDim);

                // Do nothing if in location rect as texture already set, to 0xFF if zero
                if (tilemapData[index] != 0)
                    return;

                if (PaintRoad(x, y, index, roadDataPt))
                    return;

                // Assign tile texture
                if (march)
                {
                    // Get sample points
                    int tdIdx = JobA.Idx(x, y, tdDim);
                    int b0 = tileData[tdIdx];               // tileData[x, y]
                    int b1 = tileData[tdIdx + 1];           // tileData[x + 1, y]
                    int b2 = tileData[tdIdx + tdDim];       // tileData[x, y + 1]
                    int b3 = tileData[tdIdx + tdDim + 1];   // tileData[x + 1, y + 1]

                    int shape = (b0 & 1) | (b1 & 1) << 1 | (b2 & 1) << 2 | (b3 & 1) << 3;
                    int ring = (b0 + b1 + b2 + b3) >> 2;
                    int tileID = shape | ring << 4;

                    tilemapData[index] = lookupTable[tileID];
                }
                else
                {
                    tilemapData[index] = tileData[JobA.Idx(x, y, tdDim)];
                }
            }

            private bool PaintRoad(int x, int y, int index, byte roadDataPt)
            {
                bool hasRoad = false;

                if (roadDataPt != 0)
                {
                    // Paint road around locations
                    if (x > locationRect.xMin && x < locationRect.xMax && y > locationRect.yMin && y < locationRect.yMax)
                    {
                        tilemapData[index] = road;
                        return true;
                    }

                    // N-S
                    if (((roadDataPt & N) > 0 && (x == 63 || x == 64) && y > 63) || ((roadDataPt & S) > 0 && (x == 63 || x == 64) && y < 64))
                    {
                        tilemapData[index] = road;
                        hasRoad = true;
                    }
                    if (((roadDataPt & N) > 0 && (x == 63 || x == 64) && y == 63) || ((roadDataPt & S) > 0 && (x == 63 || x == 64) && y == 64))
                    {
                        PaintHalfRoad(x, y, index, x == y, x == 64);
                        hasRoad = true;
                    }
                    // E-W
                    if (((roadDataPt & E) > 0 && (y == 63 || y == 64) && x > 63) || ((roadDataPt & W) > 0 && (y == 63 || y == 64) && x < 64))
                    {
                        tilemapData[index] = road;
                        hasRoad = true;
                    }
                    if (((roadDataPt & E) > 0 && (y == 63 || y == 64) && x == 63) || ((roadDataPt & W) > 0 && (y == 63 || y == 64) && x == 64))
                    {
                        PaintHalfRoad(x, y, index, x == y, x == 64);
                        hasRoad = true;
                    }
                    // NE-SW
                    if (((roadDataPt & NE) > 0 && x == y && x > 63) || ((roadDataPt & SW) > 0 && x == y && x < 64))
                    {
                        tilemapData[index] = road;
                        hasRoad = true;
                    }
                    if (((roadDataPt & NE) > 0 && x == y && x == 63) || ((roadDataPt & SW) > 0 && x == y && x == 64))
                    {
                        PaintHalfRoad(x, y, index, true, x == 64);
                        hasRoad = true;
                    }
                    if (((roadDataPt & NE) > 0 && ((x == y + 1 && x > 63) || (x + 1 == y && y > 63))) || ((roadDataPt & SW) > 0 && ((x == y + 1 && x <= 64) || (x + 1 == y && y <= 64))))
                    {
                        PaintHalfRoad(x, y, index, false, (x == y + 1));
                        hasRoad = true;
                    }
                    // NW-SE
                    int _x = 127 - x;
                    if (((roadDataPt & NW) > 0 && _x == y && x < 64) || ((roadDataPt & SE) > 0 && _x == y && x > 63))
                    {
                        tilemapData[index] = road;
                        hasRoad = true;
                    }
                    if (((roadDataPt & NW) > 0 && _x == y && x == 64) || ((roadDataPt & SE) > 0 && _x == y && x == 63))
                    {
                        PaintHalfRoad(x, y, index, false, x == 64);
                        hasRoad = true;
                    }
                    if (((roadDataPt & NW) > 0 && ((_x == y + 1 && x < 64) || (_x + 1 == y && y > 63))) || ((roadDataPt & SE) > 0 && ((_x == y + 1 && x >= 63) || (_x + 1 == y && y <= 64))))
                    {
                        PaintHalfRoad(x, y, index, true, (_x != y + 1));
                        hasRoad = true;
                    }
                }

                return hasRoad;
            }

            private void PaintHalfRoad(int x, int y, int index, bool rotate, bool flip)
            {
                int tileMap = tilemapData[index] & 0x3F;
                if (tileMap == road || tileMap == road_grass || tileMap == road_dirt)
                    return;

                byte tile = tileData[JobA.Idx(x, y, tdDim)];
                if (tile == grass)
                    tilemapData[index] = road_grass;
                else if (tile == dirt)
                    tilemapData[index] = road_dirt;
                else if (tile == stone)
                    tilemapData[index] = road_grass;

                if (rotate)
                    tilemapData[index] += 64;
                if (flip)
                    tilemapData[index] += 128;
            }

            private bool IsTileNotSetOrValid(byte tile)
            {
                int record = tile & 0x4F;
                return tile == 0 || record == road || record == road_grass || record == road_dirt;
            }
        }

        // Smoothes road terrain by averaging corner heights of road tiles
        struct SmoothRoadTerrainJob : IJob
        {
            [ReadOnly]
            public NativeArray<byte> tilemapData;

            public NativeArray<float> heightmapData;

            public int hDim;
            public int tDim;
            public Rect locationRect;

            public void Execute()
            {
                for (int y = 1; y < hDim-2; y++)
                {
                    for (int x = 1; x < hDim-2; x++)
                    {
                        if (!locationRect.Contains(new Vector2(x, y)) && true)
                        {
                            int idx = JobA.Idx(y, x, hDim);
                            int tIdx = JobA.Idx(x, y, tDim);

                            if (tIdx < tilemapData.Length && tilemapData[tIdx] == road)
                            {
                                SmoothRoad(idx);
                                SmoothRoad(idx + 1);
                                SmoothRoad(idx + hDim);
                                SmoothRoad(idx + hDim + 1);
                            }
                        }
                    }
                }
            }

            void SmoothRoad(int idx)
            {
                float height = heightmapData[idx];
                float h1 = heightmapData[idx + hDim];
                float h2 = heightmapData[idx + 1];
                float h3 = heightmapData[idx - hDim];
                float h4 = heightmapData[idx - 1];
                heightmapData[idx] = (height + h1 + h2 + h3 + h4) / 5;
            }
        }
    }
}