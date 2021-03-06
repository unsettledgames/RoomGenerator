﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace RoomGenerator
{
    class Level
    {
        // Data about room generation
        private int minRoomWidth;
        private int maxRoomWidth;
        private int minRoomHeight;
        private int maxRoomHeight;

        // Data about corridor generation
        private int minCorridorWidth;
        private int maxCorridorWidth;
        private int minCorridorHeight;
        private int maxCorridorHeight;

        // Maximum number of corridors per room side
        private int maxCorridorsPerSide;
        // Probabiloty that a corridor is attached to a side of a room
        private int corridorProbability;
        // Probability that, during the generation of a room, the point in which a corridor is attached changes
        private int roomChangeProbability;
        // Total number of rooms in the level
        private int nRooms;

        // Matrix in which the level will be stored
        private int[][] level;
        // Output bitmap
        private Bitmap bitmap;
        // Background color
        private Color backgroundColor;
        // Room color
        private Color foregroundColor;

        // Lists where rooms and corridors are saved
        private List<Room> rooms;
        private List<Corridor> corridors;

        public Level(int minRoomWidth, int maxRoomWidth, int minRoomHeight, int maxRoomHeight, int minCorridorWidth,
                     int maxCorridorWidth, int minCorridorHeight, int maxCorridorHeight, int maxCorridorsPerSide, 
                     int corridorProbability, int nRooms, Color backgroundColor, Color foregroundColor, int roomChangeProbability)
        {
            // Setting room data
            this.minRoomWidth = minRoomWidth;
            this.minRoomHeight = minRoomHeight;
            this.maxRoomWidth = maxRoomWidth;
            this.maxRoomHeight = maxRoomHeight;

            // Setting corridor data
            this.maxCorridorHeight = maxCorridorHeight;
            this.minCorridorHeight = minCorridorHeight;
            this.maxCorridorWidth = maxCorridorWidth;
            this.minCorridorWidth = minCorridorWidth;

            // Setting generation data
            this.maxCorridorsPerSide = maxCorridorsPerSide;
            this.corridorProbability = corridorProbability;
            this.nRooms = nRooms;
            this.backgroundColor = backgroundColor;
            this.foregroundColor = foregroundColor;
            this.roomChangeProbability = roomChangeProbability;

            // Initializing lists
            this.rooms = new List<Room>();
            this.corridors = new List<Corridor>();

            // Initializing bitmap
            this.bitmap = new Bitmap(Consts.MAX_LEVEL_WIDTH, Consts.MAX_LEVEL_HEIGHT);

            InitializeMatrix();
        }

        private void InitializeMatrix()
        {
            this.level = new int[Consts.MAX_LEVEL_WIDTH][];

            for (int i=0; i<Consts.MAX_LEVEL_WIDTH; i++)
            {
                this.level[i] = new int[Consts.MAX_LEVEL_HEIGHT];
            }

            for (int i=0; i<Consts.MAX_LEVEL_WIDTH; i++)
            {
                for (int j=0; j<Consts.MAX_LEVEL_HEIGHT; j++)
                {
                    this.level[i][j] = -1;
                }
            }
        }

        public void GenerateMap()
        {
            // Utility random number generator
            Random random = new Random();

            // Generating first room
            Room start = new Room(random.Next(minRoomWidth, maxRoomWidth), random.Next(minRoomHeight, maxRoomHeight), new Corner(0, 0));
            // Adding it to the list
            rooms.Add(start);

            // Repeating until I haven't generated all the necessary rooms
            while (rooms.Count < nRooms)
            {
                // Taking the reference room (I'll attach corridors to it)
                Room toAddTo = GetRandomExpandableRoom();

                // Repeating until I can add corridors or I've added enough rooms
                while ((rooms.Count < nRooms) && (toAddTo.GetNCorridors() < (maxCorridorsPerSide * Consts.SIDE_COUNT)) 
                    && (toAddTo.GetNCorridors() < toAddTo.GetMaxCorridors()))
                {
                    // Adding corridors
                    // Trying to change reference room
                    if (random.Next(0, 101) < roomChangeProbability)
                    {
                        // This gives more randomness to the level
                        toAddTo = GetRandomExpandableRoom();
                    }

                    // Cycling through all the sides of the room
                    for (int sideIndex=Consts.NORTH; sideIndex <= Consts.WEST; sideIndex++)
                    {
                        // Trying to add corridors
                        for (int corridorIndex=0; corridorIndex < maxCorridorsPerSide; corridorIndex++)
                        {
                            // Getting corners of the reference room
                            List<Corner> currentRoomCorners = toAddTo.GetCorners();
                            // Corridor to add to the list
                            Corridor toAddCorridor = null;
                            // Room to add to the list
                            Room toAddRoom = null;
                            // Generating width and height of the corridor
                            /* WARNING! The term "height" is referred to the shortest, vertical side. In case of vertical
                             * corridors (north or south), the height is inverted with the width so that the shortest
                             * side is always attached to the room.
                             */
                            int corridorWidth = random.Next(minCorridorWidth, maxCorridorWidth);
                            int corridorHeight = random.Next(minCorridorHeight, maxCorridorHeight);
                            // Generating width and height of the room 
                            int roomWidth = random.Next(minRoomWidth, maxRoomWidth);
                            int roomHeight = random.Next(minRoomHeight, maxRoomHeight);

                            // If I can add a corridor
                            if (random.Next(0, 100) >= corridorProbability)
                            {
                                // I generate a corridor
                                toAddCorridor = GenerateCorridor(sideIndex, toAddTo, corridorHeight, corridorWidth);

                                if (toAddCorridor != null)
                                {
                                    toAddRoom = GenerateRoom(sideIndex, toAddCorridor, roomWidth, roomHeight);
                                }

                                if (toAddRoom != null && toAddCorridor != null && !CollidesWithAnything(toAddRoom))
                                {
                                    // Adding corridor to the reference room
                                    toAddRoom.AddCorridor(toAddCorridor);
                                    // Adding new room at the end of the corridor
                                    this.corridors.Add(toAddCorridor);
                                    this.rooms.Add(toAddRoom);
                                }
                            }
                        }
                    }
                }
            }

            // Filling the matrix (depending on the values contained in the corridor and room lists)
            FillMatrix();
            // Exporting the bitmap obtained from the matrix
            ExportBitmap();
        }

        /** Checks if the room collides with something else
         * 
         * @return: true if the room collides, false if doesn't
         */ 
        public bool CollidesWithAnything(Room toCheck)
        {
            for (int i=0; i<rooms.Count; i++)
            {
                if (rooms[i].CollidesWith(toCheck))
                {
                    return true;
                }
            }

            return false;
        }

        /** Generates a corridor
         * 
         * @param sideIndex:        side of the room on which the corridor should be added
         * @param toAddTo:          room to which the corridor should be added
         * @param corridorHeight:   height of the corridor
         * @param corridorWidth:    width of the corridor
         * 
         * @return: the generated corridor
         */
        private Corridor GenerateCorridor(int sideIndex, Room toAddTo, int corridorHeight, int corridorWidth)
        {
            // Getting corners of the reference room
            List<Corner> currentRoomCorners = toAddTo.GetCorners();
            // Top left corner of the corridor
            Corner corridorTopLeftCorner;
            // Corridor that will be added
            Corridor toAddCorridor = null;
            // Utility random number generator
            Random random = new Random();
            // Room corner list
            List<Corner> roomReference = toAddTo.GetCornersForCorridor[sideIndex](corridorWidth, corridorHeight);

            if (!toAddTo.corridorAdjaciencies[sideIndex])
            {
                corridorTopLeftCorner = new Corner(
                        random.Next(
                            roomReference[0].GetX(),
                            roomReference[1].GetX()
                        ),
                        random.Next(
                            roomReference[0].GetY(),
                            roomReference[1].GetY()
                        )
                    );

                // Instantiating corridor
                if (sideIndex == Consts.EAST || sideIndex == Consts.WEST)
                {
                    int tmp = corridorWidth;
                    corridorWidth = corridorHeight;
                    corridorHeight = tmp;
                }

                toAddCorridor = new Corridor(corridorHeight, corridorWidth, corridorTopLeftCorner);

                // Notifying the room that now it has a corridor on top of it
                toAddTo.corridorAdjaciencies[sideIndex] = true;
            }

            return toAddCorridor;
        }

        /** Generates a room
         * 
         * @param sideIndex:        side to which the corridor is attached
         * @param toAddCorridor:    corridor to which the room is attached
         * @roomWidth:              width of the room
         * @roomHeight:             height of the room
         * 
         * @return: the generated room
         * 
         */
        private Room GenerateRoom(int sideIndex, Corridor toAddCorridor, int roomWidth, int roomHeight)
        {
            Room toAddRoom;
            List<Corner> corridorCorners = toAddCorridor.GetCorners();
            Corner roomTopLeftCorner;
            int cornerX;
            int cornerY;

            corridorCorners = toAddCorridor.GetCorners();

            switch (sideIndex)
            {
                case Consts.NORTH:
                    cornerX = (corridorCorners[Consts.TOP_LEFT].GetX() + corridorCorners[Consts.TOP_RIGHT].GetX()) / 2 - roomWidth / 2;
                    cornerY = corridorCorners[Consts.TOP_LEFT].GetY() + roomHeight;

                    break;
                case Consts.EAST:
                    cornerX = corridorCorners[Consts.TOP_RIGHT].GetX();
                    cornerY = (corridorCorners[Consts.TOP_RIGHT].GetY() + corridorCorners[Consts.BOTTOM_RIGHT].GetY()) / 2 + roomHeight / 2;

                    break;
                case Consts.SOUTH:
                    cornerX = (corridorCorners[Consts.TOP_LEFT].GetX() + corridorCorners[Consts.TOP_RIGHT].GetX()) / 2 - roomWidth / 2;
                    cornerY = corridorCorners[Consts.BOTTOM_RIGHT].GetY();

                    break;
                case Consts.WEST:
                    cornerX = corridorCorners[Consts.TOP_LEFT].GetX() - roomWidth;
                    cornerY = (corridorCorners[Consts.TOP_RIGHT].GetY() + corridorCorners[Consts.BOTTOM_RIGHT].GetY()) / 2 + roomHeight / 2;

                    break;
                default:
                    toAddRoom = new Room(-1, -1, new Corner(-1, -1));

                    cornerX = -1;
                    cornerY = -1;

                    break;
            }

            roomTopLeftCorner = new Corner(cornerX, cornerY);

            /* Now that I have the corner, I can generate the room */
            toAddRoom = new Room(roomWidth, roomHeight, roomTopLeftCorner);

            toAddRoom.corridorAdjaciencies[Consts.SOUTH] = true;

            return toAddRoom;
        }

        /** Fills the matrix with the ids of the rooms 
         */ 
        private void FillMatrix()
        {
            Random random = new Random();
            PerlinNoise noise = new PerlinNoise(random.Next(0, 0));

            // Adding corridors
            for (int i = 0; i < corridors.Count; i++)
            {
                corridors[i].AddToMatrix(level, noise);
            }

            // Adding rooms
            for (int i=0; i<rooms.Count; i++)
            {
                rooms[i].AddToMatrix(level, noise);
            }
        }

        /** Exports the previously generated matrix in bitmap format
         * 
         */ 
        private void ExportBitmap()
        {
            for (int i=0; i<Consts.MAX_LEVEL_WIDTH; i++)
            {
                for (int j=0; j<Consts.MAX_LEVEL_HEIGHT; j++)
                {
                    if (level[i][j] == -1)
                    {
                        bitmap.SetPixel(i, j, backgroundColor);
                    }
                    else
                    {
                        bitmap.SetPixel(i, j, foregroundColor);//Color.FromArgb(level[i][j], level[i][j] * 2, level[i][j] * 3));
                    }

                    
                }
            }

            Console.WriteLine("Finito, salvo su file...");
            bitmap.Save("generated.bmp");
        }
        
        /** Returns the first expandable room from the list
         * 
         */ 
        public Room GetFirstExpandableRoom()
        {
            for (int i=0; i<rooms.Count; i++)
            {
                if (rooms[i].GetNCorridors() < maxCorridorsPerSide * Consts.SIDE_COUNT && rooms[i].GetNCorridors() < rooms[i].GetMaxCorridors())
                {
                    return rooms[i];
                }
            }

            return null;
        }
        

        /** Returns a random expandable room
         * 
         */
        public Room GetRandomExpandableRoom()
        {
            List<Room> rooms = GetExpandableRooms();
            return rooms[new Random().Next(0, rooms.Count)];
        }

        /* Returns the entire list of the expandable rooms
         * 
         */ 
        public List<Room> GetExpandableRooms()
        {
            List<Room> ret = new List<Room>();

            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].GetNCorridors() < maxCorridorsPerSide * Consts.SIDE_COUNT)
                {
                    ret.Add(rooms[i]);
                }
            }

            return ret;
        }

        public void Initialize(int minBlocks, int maxBlocks, int precision)
        {
            int blockDiff = maxBlocks - minBlocks;
            int blockIncrease = blockDiff / precision;
            int minArea = minRoomHeight * minRoomWidth;
            int maxArea = maxRoomWidth * maxRoomHeight;
            int areaDiff = maxArea - minArea;
            int areaIncrease = areaDiff / precision;

            int currentArea = minArea;
            int currentBlocks = minBlocks;

            Utility.blocksPerArea = new Dictionary<int, int>();

            for (int i=minBlocks; i<maxBlocks; i++)
            {
                Utility.blocksPerArea[currentArea] = currentBlocks;

                currentArea += areaIncrease;
                currentBlocks += blockIncrease;
            }
        }
    }
}