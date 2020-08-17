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

        public const byte grass_dirt = 12;

        public const byte dirt_path = 11;

        public const byte dirt_grass = 10;
        public const byte dirt_stone = 25;

        public const byte road = 46;
        public const byte road_grass = 55;
        public const byte road_dirt = 47;

        public const byte temp_water = byte.MaxValue;

        const int CardInn = 0;
        const int CardOut = 1;
        const int DiagInn = 2;
        const int DiagOut = 3;
        const int DiagGap = 4;

        // Tile replacement arrays:
        //          water, dirt, grass, stone
        static readonly byte[][] roadTiles = {
            new byte[] { 46, 46, 46, 46 },  // Cardinal - Inner
            null,                           // Cardinal - Outer 
            new byte[] { 46, 46, 46, 46 },  // Diagonal - Inner
            new byte[] { 47, 47, 55, 55 },  // Diagonal - Outer 
            null,                           // Diagonal - Gaps (Outer)
        };
        static readonly byte[] trackTiles = {
            01, 11, 26,  // Cardinal - Inner
            99, 99, 99,  // Cardinal - Outer 
            01, 51, 52,  // Diagonal - Inner
            01, 12, 27,  // Diagonal - Outer1 
            99, 99, 99,  // Diagonal - Outer2
        };
        static readonly byte[] streamTiles = {
            01, 11, 26,  // Cardinal - Inner
            99, 99, 99,  // Cardinal - Outer 
            01, 51, 52,  // Diagonal - Inner
            01, 12, 27,  // Diagonal - Outer1 
            99, 99, 99,  // Diagonal - Outer2
        };
        static readonly byte[][] riverTiles = {
            new byte[] { 00, 00, 00, 00 },  // Cardinal - Inner
            new byte[] { 00, 06, 21, 31 },  // Cardinal - Outer 
            new byte[] { 00, 01, 51, 52 },  // Diagonal - Inner
            new byte[] { 00, 05, 20, 30 },  // Diagonal - Outer1 
            new byte[] { 00, 07, 22, 32 },  // Diagonal - Outer2
        };

        public const byte N  = 128;//0b_1000_0000;
        public const byte NE = 64; //0b_0100_0000;
        public const byte E  = 32; //0b_0010_0000;
        public const byte SE = 16; //0b_0001_0000;
        public const byte S  = 8;  //0b_0000_1000;
        public const byte SW = 4;  //0b_0000_0100;
        public const byte W  = 2;  //0b_0000_0010;
        public const byte NW = 1;  //0b_0000_0001;

        public const string RoadDataFilename = "roadData.bytes";

        static byte[] roadData;

        bool smoothRoads;
        bool editorEnabled;

        public BasicRoadsTexturing(bool smooth, bool editor)
        {
            // Read in road data.
            TextAsset dataAsset;
            if (ModManager.Instance.TryGetAsset(RoadDataFilename, false, out dataAsset))
            {
                roadData = dataAsset.bytes;
            }
            if (roadData == null || roadData.Length != MapsFile.MaxMapPixelX * MapsFile.MaxMapPixelY)
            {
                Debug.LogWarning("BasicRoads: Unable to load road data, starting with blank path data.");
                roadData = new byte[MapsFile.MaxMapPixelX * MapsFile.MaxMapPixelY];
            }
            smoothRoads = smooth;
            editorEnabled = editor;
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
            int roadIndex = mapData.mapPixelX + (mapData.mapPixelY * MapsFile.MaxMapPixelX);
            byte roadDataPt = roadData[roadIndex];
            byte roadCorners = (byte)((roadData[roadIndex + 1] & 0x5) | (roadData[roadIndex - 1] & 0x50));
            if (editorEnabled)
            {
                roadDataPt = BasicRoadsPathEditor.roadData[roadIndex];
                roadCorners = (byte)((BasicRoadsPathEditor.roadData[roadIndex + 1] & 0x5) | (BasicRoadsPathEditor.roadData[roadIndex - 1] & 0x50));
            }

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
                midLo = (assignTilesDim / 2) - 1,
                midHi = assignTilesDim / 2,
                roadDataPt = roadDataPt,
                roadCorners = roadCorners,
            };
            JobHandle assignTilesHandle = assignTilesJob.Schedule(assignTilesDim * assignTilesDim, 64, tileDataHandle);

            JobHandle returnHandle = assignTilesHandle;
            if (smoothRoads)
            {
                SmoothRoadTerrainJob smoothRoadTerrainJob = new SmoothRoadTerrainJob()
                {
                    heightmapData = mapData.heightmapData,
                    tilemapData = mapData.tilemapData,
                    hDim = DaggerfallUnity.Instance.TerrainSampler.HeightmapDimension,
                    tDim = assignTilesDim,
                    locationRect = mapData.locationRect,
                };
                JobHandle smoothRoadHandle = smoothRoadTerrainJob.Schedule(assignTilesHandle);
                returnHandle = smoothRoadHandle;
            }

            // Add both working native arrays to disposal list.
            mapData.nativeArrayList.Add(tileData);
            mapData.nativeArrayList.Add(lookupData);

            return returnHandle;
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
            public int midLo;   // 63
            public int midHi;   // 64
            public byte roadDataPt;
            public byte roadCorners;

            public void Execute(int index)
            {
                int x = JobA.Row(index, tDim);
                int y = JobA.Col(index, tDim);

                // Paint tracks
//                if (ValidTrackLocationTile(tilemapData[index]) && PaintPath(x, y, index, trackTiles, roadDataPt, roadCorners))
//                    return;

                // Do nothing if in location rect as texture already set, to 0xFF if zero
                if (tilemapData[index] != 0)
                    return;

                if (PaintPath(x, y, index, riverTiles, roadDataPt, roadCorners))
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

            private bool ValidTrackLocationTile(byte tileData)
            {
                int tile = tileData & 0x3F;
                return tile == 0 || tile == grass || (tile >= 9 && tile <= 13);
            }

            private void PaintPathTile(int x, int y, int index, byte[] pathTile, bool rotate, bool flip, bool overwrite = true)
            {
                if (overwrite || tilemapData[index] == 0)
                {
                    byte tile = tileData[JobA.Idx(x, y, tdDim)];
                    tilemapData[index] = pathTile[tile];
                    RotateFlipTile(index, rotate, flip);
                    if (tilemapData[index] == 0)
                        tilemapData[index] = temp_water;
                }
            }

            private bool PaintPath(int x, int y, int index, byte[][] pathTiles, byte pathDataPt, byte pathCorners)
            {
                bool hasPath = false;

                pathDataPt = E|N|S;

                // Paint path sections if path data is present:
                if (pathDataPt != 0)
                {
                    // N-S
                    if ((((pathDataPt & N) != 0 && (x == midLo || x == midHi) && y > midLo) || ((pathDataPt & S) != 0 && (x == midLo || x == midHi) && y < midHi)) && pathTiles[CardInn] != null)
                    {   // Cardinal - Inner
                        PaintPathTile(x, y, index, pathTiles[CardInn], false, x == midHi);
                        hasPath = true;
                    }
                    if ((((pathDataPt & N) != 0 && (x == midLo || x == midHi) && y == midLo) || ((pathDataPt & S) != 0 && (x == midLo || x == midHi) && y == midHi)) && pathTiles[DiagOut] != null)
                    {   // Cardinal end /\
                        PaintPathTile(x, y, index, pathTiles[DiagOut], x == y, x == midHi, false);
                        hasPath = true;
                    }
                    if ((((pathDataPt & N) != 0 && (x == midLo-1 || x == midHi+1) && y > midLo) || ((pathDataPt & S) != 0 && (x == midLo-1 || x == midHi+1) && y < midHi)) && pathTiles[CardOut] != null && !hasPath)
                    {   // Cardinal - Outer
                        PaintPathTile(x, y, index, pathTiles[CardOut], false, x == midHi+1);
                        hasPath = true;
                    }

                    // E-W
                    if ((((pathDataPt & E) != 0 && (y == midLo || y == midHi) && x > midLo) || ((pathDataPt & W) != 0 && (y == midLo || y == midHi) && x < midHi)) && pathTiles[CardInn] != null)
                    {   // Cardinal - Inner
                        PaintPathTile(x, y, index, pathTiles[CardInn], true, x == midHi);
                        hasPath = true;
                    }
                    if ((((pathDataPt & E) != 0 && (y == midLo || y == midHi) && x == midLo) || ((pathDataPt & W) != 0 && (y == midLo || y == midHi) && x == midHi)) && pathTiles[DiagOut] != null)
                    {   // Cardinal end /\
                        PaintPathTile(x, y, index, pathTiles[DiagOut], x == y, x == midHi, false);
                        hasPath = true;
                    }
                    if ((((pathDataPt & E) != 0 && (y == midLo-1 || y == midHi+1) && x > midLo) || ((pathDataPt & W) != 0 && (y == midLo-1 || y == midHi+1) && x < midHi)) && pathTiles[CardOut] != null && !hasPath)
                    {   // Cardinal - Outer
                        PaintPathTile(x, y, index, pathTiles[CardOut], true, y == midHi+1);
                        hasPath = true;
                    }

/*
                    // NE-SW
                    if (((pathDataPt & NE) != 0 && x == y && x > midLo) || ((pathDataPt & SW) != 0 && x == y && x < midHi))
                    {
                        tilemapData[index] = pathTile;
                        hasPath = true;
                    }
                    if (((pathDataPt & NE) != 0 && x == y && x == midLo) || ((pathDataPt & SW) != 0 && x == y && x == midHi))
                    {
                        PaintHalfPath(x, y, index, pathTile, true, x == midHi);
                        hasPath = true;
                    }
                    if (((pathDataPt & NE) != 0 && ((x == y + 1 && x > midLo) || (x + 1 == y && y > midLo))) || ((pathDataPt & SW) != 0 && ((x == y + 1 && x <= midHi) || (x + 1 == y && y <= midHi))))
                    {
                        PaintHalfPath(x, y, index, pathTile, false, (x == y + 1));
                        hasPath = true;
                    }
                    // NW-SE
                    int _x = 127 - x;
                    if (((pathDataPt & NW) != 0 && _x == y && x < midHi) || ((pathDataPt & SE) != 0 && _x == y && x > midLo))
                    {
                        tilemapData[index] = pathTile;
                        hasPath = true;
                    }
                    if (((pathDataPt & NW) != 0 && _x == y && x == midHi) || ((pathDataPt & SE) != 0 && _x == y && x == midLo))
                    {
                        PaintHalfPath(x, y, index, pathTile, false, x == midHi);
                        hasPath = true;
                    }
                    if (((pathDataPt & NW) != 0 && ((_x == y + 1 && x < midHi) || (_x + 1 == y && y > midLo))) || ((pathDataPt & SE) != 0 && ((_x == y + 1 && x >= midLo) || (_x + 1 == y && y <= midHi))))
                    {
                        PaintHalfPath(x, y, index, pathTile, true, (_x != y + 1));
                        hasPath = true;
                    }
*/
                    // Paint roads around locations
                    if (x > locationRect.xMin && x < locationRect.xMax && y > locationRect.yMin && y < locationRect.yMax && pathTiles == roadTiles)
                    {
                        tilemapData[index] = road;
                        return true;
                    }
                }
/*
                // Paint corners
                if (pathCorners != 0)
                {
                    if ((pathCorners & NW) != 0 && x == tDim-1 && y == tDim-1)
                    {   // NE
                        PaintHalfPath(x, y, index, pathTile, true, false);
                        hasPath = true;
                    }
                    if ((pathCorners & SW) != 0 && x == tDim-1 && y == 0)
                    {   // SE
                        PaintHalfPath(x, y, index, pathTile, false, false);
                        hasPath = true;
                    }
                    if ((pathCorners & SE) != 0 && x == 0 && y == 0)
                    {   // SW
                        PaintHalfPath(x, y, index, pathTile, true, true);
                        hasPath = true;
                    }
                    if ((pathCorners & NE) != 0 && x == 0 && y == tDim-1)
                    {   // NW
                        PaintHalfPath(x, y, index, pathTile, false, true);
                        hasPath = true;
                    }
                }
*/
                return hasPath;
            }



            private void PaintHalfPath(int x, int y, int index, byte pathType, bool rotate, bool flip)
            {
                int tileMap = tilemapData[index] & 0x3F;
                if (tileMap == road || tileMap == road_grass || tileMap == road_dirt)
                    return;
                if (pathType == dirt && (tileMap == dirt || tileMap == dirt_grass || tileMap == dirt_stone))
                    return;

                byte tile = tileData[JobA.Idx(x, y, tdDim)];
                if (pathType == road)
                {
                    if (tile == grass)
                        tilemapData[index] = road_grass;
                    else if (tile == dirt)
                        tilemapData[index] = road_dirt;
                    else if (tile == stone)
                        tilemapData[index] = road_grass;
                }
                else if (pathType == dirt)
                {
                    if (tile == grass)
                        tilemapData[index] = dirt_grass;
                    else if (tile == dirt)
                        tilemapData[index] = dirt;
                    else if (tile == stone)
                        tilemapData[index] = dirt_stone;
                }
                RotateFlipTile(index, rotate, flip);
            }

            private void RotateFlipTile(int index, bool rotate, bool flip)
            {
                if (rotate)
                    tilemapData[index] += 64;
                if (flip)
                    tilemapData[index] += 128;
            }
        }

        static readonly byte[] smoothTiles = { road, temp_water }; // Use a list for check below to expand smoothing?

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
                        if (!locationRect.Contains(new Vector2(x, y)) && true)  // TODO fix this conditional!!
                        {
                            int idx = JobA.Idx(y, x, hDim);
                            int tIdx = JobA.Idx(x, y, tDim);

                            if (tIdx < tilemapData.Length && tilemapData[tIdx] == road && tilemapData[tIdx] == temp_water)
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