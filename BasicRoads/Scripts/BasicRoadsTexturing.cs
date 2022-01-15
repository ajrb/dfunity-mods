// Project:         BasicRoads mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut
// Contributors:    Interkarma

using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility.ModSupport;

namespace BasicRoads
{
    /// <summary>
    /// Paints roads onto terrain and uses default DFU jobs for generating
    /// texture tiles for terrains.
    /// </summary>
    public class BasicRoadsTexturing : DefaultTerrainTexturing
    {
        public const int MP_ARRAY_SIZE = MapsFile.MaxMapPixelX * MapsFile.MaxMapPixelY;

        public const byte water = 0;
        public const byte dirt = 1;
        public const byte grass = 2;
        public const byte stone = 3;

        public const byte road = 46;
        public const byte road_dirt = 47;
        public const byte road_grass = 55;
        public const byte water_temp = byte.MaxValue;
        public const byte no_change = 99;

        public const byte N  = 128;//0b_1000_0000;
        public const byte NE = 64; //0b_0100_0000;
        public const byte E  = 32; //0b_0010_0000;
        public const byte SE = 16; //0b_0001_0000;
        public const byte S  = 8;  //0b_0000_1000;
        public const byte SW = 4;  //0b_0000_0100;
        public const byte W  = 2;  //0b_0000_0010;
        public const byte NW = 1;  //0b_0000_0001;

        // Tile replacement arrays:
        const int CardInn = 0;
        const int CardOut = 1;
        const int DiagInn = 2;
        const int DiagOut = 3;
        const int DiagGap = 4;
        const int ICorner = 5;
        //          water, dirt, grass, stone
        static readonly byte[][] roadTiles = {
            new byte[] { 46, 46, 46, 46 },  // Cardinal - Inner
            null,                           // Cardinal - Outer 
            new byte[] { 46, 46, 46, 46 },  // Diagonal - Inner
            new byte[] { 47, 47, 55, 55 },  // Diagonal - Outer 
            null,                           // Diagonal - Gaps
            null                            // Cardinal - Inside 90o Corners
        };
        static readonly byte[][] trackTilesNorm = {
            new byte[] { 00, 99, 11, 26 },  // Cardinal - Inner
            null,                           // Cardinal - Outer 
            new byte[] { 00, 99, 51, 52 },  // Diagonal - Inner
            new byte[] { 00, 99, 12, 27 },  // Diagonal - Outer 
            null,                           // Diagonal - Gaps
            new byte[] { 00, 99, 10, 25 },  // Cardinal - Inside 90o Corners
        };
        static readonly byte[][] trackTilesSplat = {
            new byte[] { 00, 99, 01, 01 },  // Cardinal - Inner
            null,                           // Cardinal - Outer 
            new byte[] { 00, 99, 01, 01 },  // Diagonal - Inner
            new byte[] { 00, 99, 10, 25 },  // Diagonal - Outer 
            new byte[] { 00, 99, 12, 27 },  // Diagonal - Gaps
            new byte[] { 00, 99, 10, 25 },  // Cardinal - Inside 90o Corners
        };
        static byte[][] trackTiles = trackTilesNorm;
        static readonly byte[][] streamTiles = {
            new byte[] { 00, 06, 21, 31 },  // Cardinal - Inner
            null,                           // Cardinal - Outer 
            new byte[] { 00, 48, 49, 50 },  // Diagonal - Inner
            new byte[] { 00, 07, 22, 32 },  // Diagonal - Outer 
            null,                           // Diagonal - Gaps
            new byte[] { 00, 05, 20, 30 },  // Cardinal - Inside 90o Corners
        };
        static readonly byte[][] riverTiles = {
            new byte[] { 00, 00, 00, 00 },  // Cardinal - Inner
            new byte[] { 00, 06, 21, 31 },  // Cardinal - Outer 
            new byte[] { 00, 00, 00, 00 },  // Diagonal - Inner
            new byte[] { 00, 05, 20, 30 },  // Diagonal - Outer
            new byte[] { 00, 07, 22, 32 },  // Diagonal - Gaps
            new byte[] { 00, 05, 20, 30 },  // Cardinal - Inside 90o Corners
        };

        public const string RoadDataFilename = "roadData.bytes";
        public const string TrackDataFilename = "trackData.bytes";
        public const string RiverDataFilename = "riverData.bytes";
        public const string StreamDataFilename = "streamData.bytes";

        // Path data array and path type access constants
        public const int roads = 0;
        public const int tracks = 1;
        public const int rivers = 2;
        public const int streams = 3;
        static byte[][] pathsData = new byte[4][];

        readonly bool smoothPaths;
        readonly bool renderWater;
        readonly string editing;

        public BasicRoadsTexturing(bool smooth, bool riversStreams, string editingEnabled, bool splatModEnabled)
        {
            // Read in path data.
            pathsData[roads] = ReadPathData(RoadDataFilename);
            pathsData[tracks] = ReadPathData(TrackDataFilename);
            pathsData[rivers] = ReadPathData(RiverDataFilename);
            pathsData[streams] = ReadPathData(StreamDataFilename);

            smoothPaths = smooth;
            renderWater = riversStreams;
            editing = editingEnabled;

            if (splatModEnabled)
                trackTiles = trackTilesSplat;
        }

        private static byte[] ReadPathData(string filename)
        {
            byte[] pathData = null;
            TextAsset dataAsset;
            if (ModManager.Instance.TryGetAsset(filename, false, out dataAsset))
            {
                pathData = dataAsset.bytes;
            }
            if (pathData == null || pathData.Length != MP_ARRAY_SIZE)
            {
                Debug.LogWarningFormat("BasicRoads: Unable to load path data from {0}, starting with blank path data.", filename);
                pathData = new byte[MP_ARRAY_SIZE];
            }
            return pathData;
        }

        internal byte[] GetPathData(int pathType)
        {
            byte[] data = new byte[MP_ARRAY_SIZE];

            if (pathType >= roads && pathType <= streams)
                pathsData[pathType].CopyTo(data, 0);

            return data;
        }

        internal byte GetPathDataPoint(int pathType, int x, int y)
        {
            int pathsIndex = x + (y * MapsFile.MaxMapPixelX);
            return pathsData[pathType][pathsIndex];
        }

        public bool InRange(int pathsIndex)
        {
            return pathsIndex > 0 && pathsIndex < MP_ARRAY_SIZE;
        }

        public override JobHandle ScheduleAssignTilesJob(ITerrainSampler terrainSampler, ref MapPixelData mapData, JobHandle dependencies, bool march = true)
        {
            // Cache tile data to minimise noise sampling during march (using default job)
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

            // Schedule painting of roads, including smoothing if enabled
            JobHandle paintRoadsHandle = SchedulePaintRoadsJob(ref mapData, ref tileData, tileDataHandle);

            // Assign tile data to terrain (using default job)
            NativeArray<byte> lookupData = new NativeArray<byte>(lookupTable, Allocator.TempJob);
            AssignTilesJob assignTilesJob = new AssignTilesJob
            {
                lookupTable = lookupData,
                tileData = tileData,
                tilemapData = mapData.tilemapData,
                tdDim = tileDataDim,
                tDim = assignTilesDim,
                march = march,
                locationRect = mapData.locationRect,
            };
            JobHandle assignTilesHandle = assignTilesJob.Schedule(assignTilesDim * assignTilesDim, 64, paintRoadsHandle);

            // Add both working native arrays to disposal list.
            mapData.nativeArrayList.Add(tileData);
            mapData.nativeArrayList.Add(lookupData);

            return assignTilesHandle;
        }

        public JobHandle SchedulePaintRoadsJob(ref MapPixelData mapData, ref NativeArray<byte> tileData, JobHandle dependencies)
        {
            // Assign tile data to terrain, painting paths in the process
            int pathsIndex = mapData.mapPixelX + (mapData.mapPixelY * MapsFile.MaxMapPixelX);

            byte roadDataPt = pathsData[roads][pathsIndex];
            byte roadCorners = (byte)(InRange(pathsIndex) ? (pathsData[roads][pathsIndex + 1] & 0x5) | (pathsData[roads][pathsIndex - 1] & 0x50) : 0);
            byte trackDataPt = pathsData[tracks][pathsIndex];
            byte trackCorners = (byte)(InRange(pathsIndex) ? (pathsData[tracks][pathsIndex + 1] & 0x5) | (pathsData[tracks][pathsIndex - 1] & 0x50) : 0);
            if (editing == "path")
            {
                roadDataPt = BasicRoadsPathEditor.pathsData[roads][pathsIndex];
                roadCorners = (byte)(InRange(pathsIndex) ? (BasicRoadsPathEditor.pathsData[roads][pathsIndex + 1] & 0x5) | (BasicRoadsPathEditor.pathsData[roads][pathsIndex - 1] & 0x50) : 0);
                trackDataPt = BasicRoadsPathEditor.pathsData[tracks][pathsIndex];
                trackCorners = (byte)(InRange(pathsIndex) ? (BasicRoadsPathEditor.pathsData[tracks][pathsIndex + 1] & 0x5) | (BasicRoadsPathEditor.pathsData[tracks][pathsIndex - 1] & 0x50) : 0);
            }

            byte riverDataPt = pathsData[rivers][pathsIndex];
            byte riverCorners = (byte)(InRange(pathsIndex) ? (pathsData[rivers][pathsIndex + 1] & 0x5) | (pathsData[rivers][pathsIndex - 1] & 0x50) : 0);
            byte streamDataPt = pathsData[streams][pathsIndex];
            byte streamCorners = (byte)(InRange(pathsIndex) ? (pathsData[streams][pathsIndex + 1] & 0x5) | (pathsData[streams][pathsIndex - 1] & 0x50) : 0);
            if (editing == "water")
            {
                riverDataPt = BasicRoadsPathEditor.pathsData[rivers][pathsIndex];
                riverCorners = (byte)(InRange(pathsIndex) ? (BasicRoadsPathEditor.pathsData[rivers][pathsIndex + 1] & 0x5) | (BasicRoadsPathEditor.pathsData[rivers][pathsIndex - 1] & 0x50) : 0);
                streamDataPt = BasicRoadsPathEditor.pathsData[streams][pathsIndex];
                streamCorners = (byte)(InRange(pathsIndex) ? (BasicRoadsPathEditor.pathsData[streams][pathsIndex + 1] & 0x5) | (BasicRoadsPathEditor.pathsData[streams][pathsIndex - 1] & 0x50) : 0);
            }

            PaintRoadsJob paintRoadsJob = new PaintRoadsJob
            {
                tileData = tileData,
                tilemapData = mapData.tilemapData,
                tdDim = tileDataDim,
                tDim = assignTilesDim,
                locationRect = mapData.locationRect,
                midLo = (assignTilesDim / 2) - 1,
                midHi = assignTilesDim / 2,
                roadDataPt = roadDataPt,
                roadCorners = roadCorners,
                trackDataPt = trackDataPt,
                trackCorners = trackCorners,
                renderWater = renderWater,
                riverDataPt = riverDataPt,
                riverCorners = riverCorners,
                streamDataPt = streamDataPt,
                streamCorners = streamCorners,
            };
            JobHandle paintRoadsHandle = paintRoadsJob.Schedule(assignTilesDim * assignTilesDim, 64, dependencies);

            JobHandle returnHandle = paintRoadsHandle;
            if (smoothPaths)
            {
                SmoothRoadsTerrainJob smoothRoadTerrainJob = new SmoothRoadsTerrainJob()
                {
                    heightmapData = mapData.heightmapData,
                    tilemapData = mapData.tilemapData,
                    hDim = DaggerfallUnity.Instance.TerrainSampler.HeightmapDimension,
                    tDim = assignTilesDim,
                    locationRect = mapData.locationRect,
                };
                JobHandle smoothRoadHandle = smoothRoadTerrainJob.Schedule(paintRoadsHandle);
                returnHandle = smoothRoadHandle;
            }

            return returnHandle;
        }

        // Road painting job.
        struct PaintRoadsJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<byte> tileData;

            public NativeArray<byte> tilemapData;

            public int tdDim;
            public int tDim;
            public Rect locationRect;
            public int midLo;   // 63
            public int midHi;   // 64
            public byte roadDataPt;
            public byte roadCorners;
            public byte trackDataPt;
            public byte trackCorners;
            public bool renderWater;
            public byte riverDataPt;
            public byte riverCorners;
            public byte streamDataPt;
            public byte streamCorners;

            public void Execute(int index)
            {
                int x = JobA.Row(index, tDim);
                int y = JobA.Col(index, tDim);

                // Do nothing if in location rect as texture already set, to 0xFF if zero
                if (tilemapData[index] != 0)
                    return;

                // Paint roads, rivers, dirt tracks, then streams
                if (PaintPath(x, y, index, roadTiles, roadDataPt, roadCorners) ||
                    (renderWater && PaintPathWithSubPathJoins(x, y, index, riverTiles, riverDataPt, riverCorners, streamDataPt)) ||
                    (renderWater && PaintPath(x, y, index, streamTiles, streamDataPt, streamCorners)) ||
                    PaintPath(x, y, index, trackTiles, trackDataPt, trackCorners))
                    return;
            }

            private bool PaintPathTile(int x, int y, int index, byte[] pathTile, bool rotate, bool flip, bool overwrite = true)
            {
                if (overwrite || tilemapData[index] == 0)
                {
                    byte tile = tileData[JobA.Idx(x, y, tdDim)];
                    if (tile > stone)
                        tile = grass;
                    if (pathTile[tile] != no_change)// && pathTile[tile] != tile)
                    {
                        tilemapData[index] = pathTile[tile];
                        RotateFlipTile(index, rotate, flip);
                        if (tilemapData[index] == 0)
                            tilemapData[index] = water_temp;
                        return true;
                    }
                }
                return false;
            }

            private void SetPathTile(int index, byte pathTile, bool rotate, bool flip)
            {
                tilemapData[index] = pathTile;
                RotateFlipTile(index, rotate, flip);
                if (tilemapData[index] == 0)
                    tilemapData[index] = water_temp;
            }

            private void RotateFlipTile(int index, bool rotate, bool flip)
            {
                if (rotate)
                    tilemapData[index] += 64;
                if (flip)
                    tilemapData[index] += 128;
            }

            private bool PaintPath(int x, int y, int index, byte[][] pathTiles, byte pathDataPt, byte pathCorners)
            {
                // Paint path sections if path data is present:
                bool hasPath = false;
                if (pathDataPt != 0)
                {
                    // N-S
                    if ((((pathDataPt & N) != 0 && (x == midLo || x == midHi) && y > midLo) || ((pathDataPt & S) != 0 && (x == midLo || x == midHi) && y < midHi)) && pathTiles[CardInn] != null)
                    {   // Cardinal - Inner
                        hasPath |= PaintPathTile(x, y, index, pathTiles[CardInn], false, x == midHi);
                    }
                    if ((((pathDataPt & N) != 0 && (x == midLo || x == midHi) && y == midLo) || ((pathDataPt & S) != 0 && (x == midLo || x == midHi) && y == midHi)) && pathTiles[DiagOut] != null)
                    {   // Cardinal end /\
                        hasPath |= PaintPathTile(x, y, index, pathTiles[DiagOut], x == y, x == midHi, false);
                    }

                    // E-W
                    if ((((pathDataPt & E) != 0 && (y == midLo || y == midHi) && x > midLo) || ((pathDataPt & W) != 0 && (y == midLo || y == midHi) && x < midHi)) && pathTiles[CardInn] != null)
                    {   // Cardinal - Inner
                        hasPath |= PaintPathTile(x, y, index, pathTiles[CardInn], true, y == midHi);
                    }
                    if ((((pathDataPt & E) != 0 && (y == midLo || y == midHi) && x == midLo) || ((pathDataPt & W) != 0 && (y == midLo || y == midHi) && x == midHi)) && pathTiles[DiagOut] != null)
                    {   // Cardinal end /\
                        hasPath |= PaintPathTile(x, y, index, pathTiles[DiagOut], x == y, x == midHi, false);
                    }

                    // NE-SW
                    if ((((pathDataPt & NE) != 0 && x == y && x > midLo) || ((pathDataPt & SW) != 0 && x == y && x < midHi)) && pathTiles[DiagInn] != null)
                    {   // Diagonal - Inner
                        hasPath |= PaintPathTile(x, y, index, pathTiles[DiagInn], false, false);
                    }
                    if ((((pathDataPt & NE) != 0 && ((x == y + 1 && x > midLo) || (x + 1 == y && y > midLo))) || ((pathDataPt & SW) != 0 && ((x == y + 1 && x <= midHi) || (x + 1 == y && y <= midHi)))) && pathTiles[DiagOut] != null && !hasPath)
                    {   // Diagonal - Outer
                        hasPath |= PaintPathTile(x, y, index, pathTiles[DiagOut], false, (x == y + 1));
                    }

                    // NW-SE
                    int _x = 127 - x;
                    if ((((pathDataPt & NW) != 0 && _x == y && x < midHi) || ((pathDataPt & SE) != 0 && _x == y && x > midLo)) && pathTiles[DiagInn] != null)
                    {   // Diagonal - Inner
                        hasPath |= PaintPathTile(x, y, index, pathTiles[DiagInn], true, false);
                    }
                    if ((((pathDataPt & NW) != 0 && ((_x == y + 1 && x < midHi) || (_x + 1 == y && y > midLo))) || ((pathDataPt & SE) != 0 && ((_x == y + 1 && x >= midLo) || (_x + 1 == y && y <= midHi)))) && pathTiles[DiagOut] != null && !hasPath)
                    {   // Diagonal - Outer
                        hasPath |= PaintPathTile(x, y, index, pathTiles[DiagOut], true, (_x != y + 1));
                    }

                    // Cardinal - Outer
                    if (pathTiles[CardOut] != null && !hasPath)
                    {
                        if (((pathDataPt & N) != 0 && (x == midLo - 1 || x == midHi + 1) && y > midLo) || ((pathDataPt & S) != 0 && (x == midLo - 1 || x == midHi + 1) && y < midHi))
                        {   // Cardinal - Outer
                            hasPath |= PaintPathTile(x, y, index, pathTiles[CardOut], false, x == midHi + 1, false);
                        }
                        if (((pathDataPt & E) != 0 && (y == midLo - 1 || y == midHi + 1) && x > midLo) || ((pathDataPt & W) != 0 && (y == midLo - 1 || y == midHi + 1) && x < midHi))
                        {   // Cardinal - Outer
                            hasPath |= PaintPathTile(x, y, index, pathTiles[CardOut], true, y == midHi + 1);
                        }
                    }

                    // Diagonal - Gaps
                    if (pathTiles[DiagGap] != null && !hasPath)
                    {
                        if (((pathDataPt & NE) != 0 && ((x - 1 == y + 1 && x > midLo) || (x + 1 == y - 1 && y > midLo))) || ((pathDataPt & SW) != 0 && ((x - 1 == y + 1 && x <= midHi) || (x + 1 == y - 1 && y <= midHi))))
                        {   // NE-NW
                            hasPath |= PaintPathTile(x, y, index, pathTiles[DiagGap], false, (x - 1 == y + 1));
                        }
                        if (((pathDataPt & NW) != 0 && ((_x - 1 == y + 1 && x < midHi) || (_x + 1 == y - 1 && y > midLo))) || ((pathDataPt & SE) != 0 && ((_x - 1 == y + 1 && x >= midLo) || (_x + 1 == y - 1 && y <= midHi))))
                        {   // NW-SE
                            hasPath |= PaintPathTile(x, y, index, pathTiles[DiagGap], true, (_x - 1 != y + 1));
                        }
                    }

                    // Special handling for inside of 90deg cardinal corners
                    if (pathTiles[ICorner] != null)
                    {
                        int offset = pathTiles[CardOut] == null ? 0 : 1;
                        if ((pathDataPt & N) != 0 && (pathDataPt & W) != 0 && x == midLo - offset && y == midHi + offset)
                            PaintPathTile(x, y, index, pathTiles[ICorner], false, false);
                        if ((pathDataPt & N) != 0 && (pathDataPt & E) != 0 && x == midHi + offset && y == midHi + offset)
                            PaintPathTile(x, y, index, pathTiles[ICorner], true, true);
                        if ((pathDataPt & S) != 0 && (pathDataPt & W) != 0 && x == midLo - offset && y == midLo - offset)
                            PaintPathTile(x, y, index, pathTiles[ICorner], true, false);
                        if ((pathDataPt & S) != 0 && (pathDataPt & E) != 0 && x == midHi + offset && y == midLo - offset)
                            PaintPathTile(x, y, index, pathTiles[ICorner], false, true);
                    }

                    // Paint roads around locations
                    if (x > locationRect.xMin && x < locationRect.xMax && y > locationRect.yMin && y < locationRect.yMax && pathTiles == roadTiles)
                    {
                        tilemapData[index] = road;
                        return true;
                    }
                }

                // Paint map pixel corners in adjacent pixels
                if (pathCorners != 0)
                {
                    if ((pathCorners & NW) != 0 && x == tDim-1 && y == tDim-1)
                    {   // NE
                        hasPath |= PaintPathTile(x, y, index, pathTiles[DiagOut], true, false);
                    }
                    if ((pathCorners & SW) != 0 && x == tDim-1 && y == 0)
                    {   // SE
                        hasPath |= PaintPathTile(x, y, index, pathTiles[DiagOut], false, false);
                    }
                    if ((pathCorners & SE) != 0 && x == 0 && y == 0)
                    {   // SW
                        hasPath |= PaintPathTile(x, y, index, pathTiles[DiagOut], true, true);
                    }
                    if ((pathCorners & NE) != 0 && x == 0 && y == tDim-1)
                    {   // NW
                        hasPath |= PaintPathTile(x, y, index, pathTiles[DiagOut], false, true);
                    }
                }

                return hasPath;
            }

            // Special handling for sub path joins if required (rivers only presently)
            private bool PaintPathWithSubPathJoins(int x, int y, int index, byte[][] pathTiles, byte pathDataPt, byte pathCorners, byte subPathDataPt)
            {
                bool hasPath = PaintPath(x, y, index, pathTiles, pathDataPt, pathCorners);

                // Paint map pixel corner joins
                if (pathCorners != 0)
                {
                    if (((pathCorners & NW) != 0 && (subPathDataPt & NE) != 0 && x == tDim - 1 && y == tDim - 1) ||
                        ((pathCorners & SW) != 0 && (subPathDataPt & SE) != 0 && x == tDim - 1 && y == 0) ||
                        ((pathCorners & SE) != 0 && (subPathDataPt & SW) != 0 && x == 0 && y == 0) ||
                        ((pathCorners & NE) != 0 && (subPathDataPt & NW) != 0 && x == 0 && y == tDim - 1))
                    {
                        SetPathTile(index, water, false, false);
                    }
                }
                // Paint map pixel centre joins
                if (subPathDataPt != 0 && pathTiles[ICorner] != null)
                {
                    int offset = pathTiles[CardOut] == null ? 0 : 1;
                    // N-S path
                    if ((pathDataPt & N) != 0 && (subPathDataPt & W) != 0 && x == midLo - offset && y == midHi && (pathDataPt & W) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], false, false);
                    if ((pathDataPt & N) != 0 && (subPathDataPt & E) != 0 && x == midHi + offset && y == midHi && (pathDataPt & E) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], true, true);
                    if ((pathDataPt & S) != 0 && (subPathDataPt & W) != 0 && x == midLo - offset && y == midLo && (pathDataPt & W) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], true, false);
                    if ((pathDataPt & S) != 0 && (subPathDataPt & E) != 0 && x == midHi + offset && y == midLo && (pathDataPt & E) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], false, true);

                    if ((pathDataPt & N) != 0 && (subPathDataPt & NW) != 0 && x == midLo - offset && y == midHi + 1 && (pathDataPt & W) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], true, false);
                    if ((pathDataPt & N) != 0 && (subPathDataPt & NW) != 0 && x == midLo - offset && y == midHi + 2)
                        PaintPathTile(x, y, index, pathTiles[ICorner], false, false);
                    if ((pathDataPt & N) != 0 && (subPathDataPt & NE) != 0 && x == midHi + offset && y == midHi + 1 && (pathDataPt & E) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], false, true);
                    if ((pathDataPt & N) != 0 && (subPathDataPt & NE) != 0 && x == midHi + offset && y == midHi + 2)
                        PaintPathTile(x, y, index, pathTiles[ICorner], true, true);

                    if ((pathDataPt & S) != 0 && (subPathDataPt & SW) != 0 && x == midLo - offset && y == midLo - 1 && (pathDataPt & W) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], false, false);
                    if ((pathDataPt & S) != 0 && (subPathDataPt & SW) != 0 && x == midLo - offset && y == midLo - 2)
                        PaintPathTile(x, y, index, pathTiles[ICorner], true, false);
                    if ((pathDataPt & S) != 0 && (subPathDataPt & SE) != 0 && x == midHi + offset && y == midLo - 1 && (pathDataPt & E) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], true, true);
                    if ((pathDataPt & S) != 0 && (subPathDataPt & SE) != 0 && x == midHi + offset && y == midLo - 2)
                        PaintPathTile(x, y, index, pathTiles[ICorner], false, true);

                    // E-W path
                    if ((pathDataPt & W) != 0 && (subPathDataPt & N) != 0 && x == midLo && y == midHi + offset && (pathDataPt & N) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], false, false);
                    if ((pathDataPt & E) != 0 && (subPathDataPt & N) != 0 && x == midHi && y == midHi + offset && (pathDataPt & N) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], true, true);
                    if ((pathDataPt & W) != 0 && (subPathDataPt & S) != 0 && x == midLo && y == midLo - offset && (pathDataPt & S) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], true, false);
                    if ((pathDataPt & E) != 0 && (subPathDataPt & S) != 0 && x == midHi && y == midLo - offset && (pathDataPt & S) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], false, true);

                    if ((pathDataPt & E) != 0 && (subPathDataPt & NE) != 0 && x == midHi + 1 && y == midHi + offset && (pathDataPt & N) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], false, false);
                    if ((pathDataPt & E) != 0 && (subPathDataPt & NE) != 0 && x == midHi + 2 && y == midHi + offset)
                        PaintPathTile(x, y, index, pathTiles[ICorner], true, true);
                    if ((pathDataPt & E) != 0 && (subPathDataPt & SE) != 0 && x == midHi + 1 && y == midLo - offset && (pathDataPt & S) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], true, false);
                    if ((pathDataPt & E) != 0 && (subPathDataPt & SE) != 0 && x == midHi + 2 && y == midLo - offset)
                        PaintPathTile(x, y, index, pathTiles[ICorner], false, true);

                    if ((pathDataPt & W) != 0 && (subPathDataPt & NW) != 0 && x == midLo - 1 && y == midHi + offset && (pathDataPt & N) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], true, true);
                    if ((pathDataPt & W) != 0 && (subPathDataPt & NW) != 0 && x == midLo - 2 && y == midHi + offset)
                        PaintPathTile(x, y, index, pathTiles[ICorner], false, false);
                    if ((pathDataPt & W) != 0 && (subPathDataPt & SW) != 0 && x == midLo - 1 && y == midLo - offset && (pathDataPt & S) == 0)
                        PaintPathTile(x, y, index, pathTiles[ICorner], false, true);
                    if ((pathDataPt & W) != 0 && (subPathDataPt & SW) != 0 && x == midLo - 2 && y == midLo - offset)
                        PaintPathTile(x, y, index, pathTiles[ICorner], true, false);

                    if (pathTiles[CardOut] != null)
                    {
                        // NE-SW path
                        if ((pathDataPt & NE) != 0 && (subPathDataPt & SE) != 0 && x == midHi && y == midLo && (pathDataPt & SE) == 0)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & NE) != 0 && (subPathDataPt & SE) != 0 && x == midHi + offset && y == midLo && (pathDataPt & SE) == 0)
                            if ((pathDataPt & E) == 0 && (subPathDataPt & E) != 0)
                                PaintPathTile(x, y, index, pathTiles[ICorner], false, true);
                            else
                                PaintPathTile(x, y, index, pathTiles[CardOut], false, true);
                        else if ((pathDataPt & NE) != 0 && (subPathDataPt & E) != 0 && x == midHi + 1 && y == midLo)
                            PaintPathTile(x, y, index, pathTiles[CardOut], true, false);

                        if ((pathDataPt & NE) != 0 && (subPathDataPt & E) != 0 && x == midHi + 1 && y == midHi)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & NE) != 0 && (subPathDataPt & E) != 0 && x == midHi + 2 && y == midHi)
                            PaintPathTile(x, y, index, pathTiles[ICorner], true, true);

                        if ((pathDataPt & SW) != 0 && (subPathDataPt & SE) != 0 && x == midHi && y == midLo && (pathDataPt & SE) == 0)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & SW) != 0 && (subPathDataPt & SE) != 0 && x == midHi && y == midLo - offset && (pathDataPt & SE) == 0)
                            if ((pathDataPt & S) == 0 && (subPathDataPt & S) != 0)
                                PaintPathTile(x, y, index, pathTiles[ICorner], false, true);
                            else
                                PaintPathTile(x, y, index, pathTiles[CardOut], true, false);
                        else if ((pathDataPt & SW) != 0 && (subPathDataPt & S) != 0 && x == midHi && y == midLo - 1)
                            PaintPathTile(x, y, index, pathTiles[CardOut], false, true);

                        if ((pathDataPt & SW) != 0 && (subPathDataPt & S) != 0 && x == midLo && y == midLo - 1)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & SW) != 0 && (subPathDataPt & S) != 0 && x == midLo && y == midLo - 2)
                            PaintPathTile(x, y, index, pathTiles[ICorner], true, false);


                        if ((pathDataPt & NE) != 0 && (subPathDataPt & NW) != 0 && x == midLo && y == midHi && (pathDataPt & NW) == 0)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & NE) != 0 && (subPathDataPt & NW) != 0 && x == midLo && y == midHi + offset && (pathDataPt & NW) == 0)
                            if ((pathDataPt & N) == 0 && (subPathDataPt & N) != 0)
                                PaintPathTile(x, y, index, pathTiles[ICorner], false, false);
                            else
                                PaintPathTile(x, y, index, pathTiles[CardOut], true, true);
                        else if ((pathDataPt & NE) != 0 && (subPathDataPt & N) != 0 && x == midLo && y == midHi + 1)
                            PaintPathTile(x, y, index, pathTiles[CardOut], false, false);

                        if ((pathDataPt & NE) != 0 && (subPathDataPt & N) != 0 && x == midHi && y == midHi + 1)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & NE) != 0 && (subPathDataPt & N) != 0 && x == midHi && y == midHi + 2)
                            PaintPathTile(x, y, index, pathTiles[ICorner], true, true);

                        if ((pathDataPt & SW) != 0 && (subPathDataPt & NW) != 0 && x == midLo && y == midHi && (pathDataPt & SE) == 0)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & SW) != 0 && (subPathDataPt & NW) != 0 && x == midLo - offset && y == midHi && (pathDataPt & SE) == 0)
                            if ((pathDataPt & W) == 0 && (subPathDataPt & W) != 0)
                                PaintPathTile(x, y, index, pathTiles[ICorner], false, false);
                            else
                                PaintPathTile(x, y, index, pathTiles[CardOut], false, false);
                        else if ((pathDataPt & SW) != 0 && (subPathDataPt & W) != 0 && x == midLo - 1 && y == midHi)
                            PaintPathTile(x, y, index, pathTiles[CardOut], true, true);

                        if ((pathDataPt & SW) != 0 && (subPathDataPt & W) != 0 && x == midLo - 1 && y == midLo)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & SW) != 0 && (subPathDataPt & W) != 0 && x == midLo - 2 && y == midLo)
                            PaintPathTile(x, y, index, pathTiles[ICorner], true, false);


                        // NW-SE path
                        if ((pathDataPt & NW) != 0 && (subPathDataPt & SW) != 0 && x == midLo && y == midLo && (pathDataPt & SW) == 0)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & NW) != 0 && (subPathDataPt & SW) != 0 && x == midLo - offset && y == midLo && (pathDataPt & SW) == 0)
                            if ((pathDataPt & E) == 0 && (subPathDataPt & W) != 0)
                                PaintPathTile(x, y, index, pathTiles[ICorner], true, false);
                            else
                                PaintPathTile(x, y, index, pathTiles[CardOut], false, false);
                        else if ((pathDataPt & NW) != 0 && (subPathDataPt & W) != 0 && x == midLo - 1 && y == midLo)
                            PaintPathTile(x, y, index, pathTiles[CardOut], true, false);

                        if ((pathDataPt & NW) != 0 && (subPathDataPt & W) != 0 && x == midLo - 1 && y == midHi)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & NW) != 0 && (subPathDataPt & W) != 0 && x == midLo - 2 && y == midHi)
                            PaintPathTile(x, y, index, pathTiles[ICorner], false, false);

                        if ((pathDataPt & SE) != 0 && (subPathDataPt & SW) != 0 && x == midLo && y == midLo && (pathDataPt & SW) == 0)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & SE) != 0 && (subPathDataPt & SW) != 0 && x == midLo && y == midLo - offset && (pathDataPt & SW) == 0)
                            if ((pathDataPt & S) == 0 && (subPathDataPt & S) != 0)
                                PaintPathTile(x, y, index, pathTiles[ICorner], true, false);
                            else
                                PaintPathTile(x, y, index, pathTiles[CardOut], true, false);
                        else if ((pathDataPt & SE) != 0 && (subPathDataPt & S) != 0 && x == midLo && y == midLo - 1)
                            PaintPathTile(x, y, index, pathTiles[CardOut], false, false);

                        if ((pathDataPt & SE) != 0 && (subPathDataPt & S) != 0 && x == midHi && y == midLo - 1)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & SE) != 0 && (subPathDataPt & S) != 0 && x == midHi && y == midLo - 2)
                            PaintPathTile(x, y, index, pathTiles[ICorner], false, true);


                        if ((pathDataPt & NW) != 0 && (subPathDataPt & NE) != 0 && x == midHi && y == midHi && (pathDataPt & NE) == 0)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & NW) != 0 && (subPathDataPt & NE) != 0 && x == midHi && y == midHi + offset && (pathDataPt & NE) == 0)
                            if ((pathDataPt & N) == 0 && (subPathDataPt & N) != 0)
                                PaintPathTile(x, y, index, pathTiles[ICorner], true, true);
                            else
                                PaintPathTile(x, y, index, pathTiles[CardOut], true, true);
                        else if ((pathDataPt & NW) != 0 && (subPathDataPt & N) != 0 && x == midHi && y == midHi + 1)
                            PaintPathTile(x, y, index, pathTiles[CardOut], false, true);

                        if ((pathDataPt & NW) != 0 && (subPathDataPt & N) != 0 && x == midLo && y == midHi + 1)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & NW) != 0 && (subPathDataPt & N) != 0 && x == midLo && y == midHi + 2)
                            PaintPathTile(x, y, index, pathTiles[ICorner], false, false);

                        if ((pathDataPt & SE) != 0 && (subPathDataPt & NE) != 0 && x == midHi && y == midHi && (pathDataPt & NE) == 0)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & SE) != 0 && (subPathDataPt & NE) != 0 && x == midHi + offset && y == midHi && (pathDataPt & NE) == 0)
                            if ((pathDataPt & E) == 0 && (subPathDataPt & E) != 0)
                                PaintPathTile(x, y, index, pathTiles[ICorner], true, true);
                            else
                                PaintPathTile(x, y, index, pathTiles[CardOut], false, true);
                        else if ((pathDataPt & SE) != 0 && (subPathDataPt & E) != 0 && x == midHi + 1 && y == midHi)
                            PaintPathTile(x, y, index, pathTiles[CardOut], true, true);

                        if ((pathDataPt & SE) != 0 && (subPathDataPt & E) != 0 && x == midHi + 1 && y == midLo)
                            SetPathTile(index, water, false, false);
                        if ((pathDataPt & SE) != 0 && (subPathDataPt & E) != 0 && x == midHi + 2 && y == midLo)
                            PaintPathTile(x, y, index, pathTiles[ICorner], false, true);
                    }
                }
                return hasPath;
            }
        }


        // Smoothes terrain for roads and rivers by averaging corner heights of matching tiles
        struct SmoothRoadsTerrainJob : IJob
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
                        if (!locationRect.Contains(new Vector2(x, y)))
                        {
                            int idx = JobA.Idx(y, x, hDim);
                            int tIdx = JobA.Idx(x, y, tDim);

                            byte tile = tilemapData[tIdx];
                            if (tIdx < tilemapData.Length && (tile == road || tile == water_temp)) // || (tile >= 5 && tile <= 7) || (tile >= 20 && tile <= 22) || (tile >= 30 && tile <= 32)))
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