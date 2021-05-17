// #define DEBUG_MAZE
using System.Collections.Generic;
using UnityEngine;

/*
 * class to generate the maze
 *
 * if DEBUG_MAZE is defined, two copies of the maze will be created, one regular maze, and one with each z layer spread out
 */
public class RoomGenerator : MonoBehaviour {

    /*
     * helper struct to hold positional data
     * struct and not a class to create a copy by default when adding a Point to data structures
     */
    private struct Point {

        public int x;
        public int y;
        public int z;

        public Point(int xVal, int yVal, int zVal) {
            x = xVal;
            y = yVal;
            z = zVal;
        }

    }

    // base room to use for all the generated rooms
    [SerializeField] private GameObject baseRoom;

    // variables to determine maze size
    public int dimensions;
    private int midpoint;

    // variables to control path generation
    [SerializeField] private int minFirstStepsLow;
    [SerializeField] private int minFirstStepsHigh;
    [SerializeField] private int minFinalPathLengthLow;
    [SerializeField] private int minFinalPathLengthHigh;
    private int minFinalPathLength;
    private Queue<Point> finalPath;
    [SerializeField] private int failsToExitSecondaryPathGen;
    [SerializeField] private int maxPathsToCenterLow;
    [SerializeField] private int maxPathsToCenterHigh;
    private int maxPathsToCenter;
    private int currentPathsToCenter = 1;

    // the rooms of the maze
    private GameObject[,,] rooms;

    // random number generator for random path generation
    private System.Random rand = new System.Random();

    // Start creates the maze
    void Start() {

        // start a timer for benchmarking info
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();

        // set length measurments for proper room placement
        float roomLength = baseRoom.transform.localScale.x;
        midpoint = dimensions / 2;

        // randomly set variables to be within a given range
        maxPathsToCenter = rand.Next(maxPathsToCenterHigh - maxPathsToCenterLow) + maxPathsToCenterLow;
        minFinalPathLength = rand.Next(minFinalPathLengthHigh - minFinalPathLengthLow) + minFinalPathLengthLow;

        // initialize rooms
        rooms = new GameObject[dimensions, dimensions, dimensions];
        for (int x = 0; x < dimensions; ++x) {
            for (int y = 0; y < dimensions; ++y) {
                for (int z = 0; z < dimensions; ++z) {
                    
                    // create rooms and set their position
                    rooms[x, y, z] = GameObject.Instantiate(baseRoom);
                    rooms[x, y, z].transform.position = new Vector3(
                        x - dimensions / 2.0f + 0.5f,
                        y + 15 / roomLength,
                        z
#if DEBUG_MAZE
                        * 5
#endif
                        - dimensions / 2.0f + 0.5f
                    ) * roomLength;

                    // hollow out center
                    if (x >= midpoint - 1 && x <= midpoint
                        && y >= midpoint - 1 && y <= midpoint
                        && z >= midpoint - 1 && z <= midpoint
                    ) {
                        rooms[x, y, z].gameObject.SetActive(false);
                    }

                }
            }
        }
        // hide baseRoom, as it merely serves as a template
        baseRoom.gameObject.SetActive(false);

        // initialize a Queue to keep track of the final path
        finalPath = new Queue<Point>(minFinalPathLength);
        // continuously attempt to generate a main path until successful
        bool successfulGeneration;
        do {
            successfulGeneration = GenMainPath();
        } while (!successfulGeneration);

        // generate secondary paths until the amount of failed generations has exceeded the alloted amount
        int fails = 0;
        while (fails < failsToExitSecondaryPathGen) {
            if (!GenSecondaryPath()) {
                ++fails;
            }
        }
#if DEBUG_MAZE
        DuplicateMaze();
#endif
        timer.Stop();

        // print benchmark information to the console
        Debug.Log("Took " + string.Format(
            "{0}.{1}", timer.Elapsed.Seconds, timer.Elapsed.Milliseconds
        ) + " seconds");
        
    }

    // attempt to generate a main path, returns true if successful, false otherwise
    // if path generation fails, this method will reset the maze to its default state
    private bool GenMainPath() {

        // initialize starting location relative to the center
        // the starting location can not be along the ground of the center space
        int startingWall = rand.Next(5);
        int startingSquare = rand.Next(4);

        // Points to store data for the start of the path with prevPos being the first opening and pos being the subsequent opening
        Point prevPos;
        Point pos;
        
        // update prevPos and pos in relationship to prevPos based on the randomly generated starting data
        switch (startingWall) {

            case 0:

                prevPos = new Point(
                    midpoint - startingSquare % 2,
                    midpoint - startingSquare / 2,
                    midpoint + 1
                );
                pos = prevPos;
                ++pos.z;

                break;
            case 1:

                prevPos = new Point(
                    midpoint - startingSquare % 2,
                    midpoint - startingSquare / 2,
                    midpoint - 2
                );
                pos = prevPos;
                --pos.z;

                break;
            case 2:

                prevPos = new Point(
                    midpoint + 1,
                    midpoint - startingSquare % 2,
                    midpoint - startingSquare / 2
                );
                pos = prevPos;
                ++pos.x;

                break;
            case 3:

                prevPos = new Point(
                    midpoint - 2,
                    midpoint - startingSquare % 2,
                    midpoint - startingSquare / 2
                );
                pos = prevPos;
                --pos.x;

                break;
            default:

                prevPos = new Point(
                    midpoint - startingSquare % 2,
                    midpoint + 1,
                    midpoint - startingSquare / 2
                );
                pos = prevPos;
                ++pos.y;

                break;

        }

        // open up the start of the path
        rooms[prevPos.x, prevPos.y, prevPos.z].gameObject.SetActive(false);
        rooms[pos.x, pos.y, pos.z].gameObject.SetActive(false);

        // variable passed as a reference to determine if there is no possible next position / path generation has failed
        bool stopGen = false;
        // generate the start of the main path
        int minFirstSteps = rand.Next(minFirstStepsHigh - minFirstStepsLow) + minFirstStepsLow;
        for (int i = 0; i < minFirstSteps; ++i) {
            // false signifies that the next position can not be an exit
            FindNextPosMain(ref pos, ref stopGen, false);
            // add potential members of the final path strech to the finalPath Queue
            if (minFirstSteps - i <= minFinalPathLength) {
                finalPath.Enqueue(pos);
            }
        }

        // continue generating the main path until path generation fails or creates an exit to the maze
        while (!stopGen) {
            // true signifies that the next position can be an exit
            FindNextPosMain(ref pos, ref stopGen, true);
            if (
                pos.x <= 0 || pos.x >= dimensions - 1
                || pos.y <= 0 || pos.y >= dimensions - 1
                || pos.z <= 0 || pos.z >= dimensions - 1
            ) {
                // end generation if an exit was created
                break;
            }
            // update members of the final path
            finalPath.Dequeue();
            finalPath.Enqueue(pos);
        }

        // if generation failed, reset the maze and return false, indicating that generation failed
        if (stopGen) {
            ResetRooms();
            return false;
        }

        // return true, indicating that generation was successful
        return true;

    }

    // attempt to generate a secondary path, returns true if successful, false otherwise
    // if path generation fails, this method will reset the maze to its prior state
    private bool GenSecondaryPath() {

        // find a random origin for the new secondary branch
        // if this step takes too long (possibly indicates no remaining valid spaces to start), return false, indicating generation failure
        int x, y, z;
        int fails = 0;
        do {
            x = rand.Next(dimensions - 2) + 1;
            y = rand.Next(dimensions - 2) + 1;
            z = rand.Next(dimensions - 2) + 1;
            ++fails;
        } while (!IsValidSpace(x, y, z) && fails < 2 * failsToExitSecondaryPathGen);
        if (fails >= 2 * failsToExitSecondaryPathGen) {
            return false;
        }
        // open up the origin of the branch
        rooms[x, y, z].gameObject.SetActive(false);

        // set a Point to the branch origin, and add it to the List of points in the secondary path
        Point pos = new Point(x, y, z);
        List<Point> pointsInPath = new List<Point>(10);
        pointsInPath.Add(pos);

        // store the number of center paths beforehand so if path gen fails, the number is properly restored
        int centerPaths = currentPathsToCenter;
        // Generate half of the branch until finding a next position fails (and so does the path generation),
        // or this half of the branch connects to a different branch
        bool stopGen = false;
        bool finishedBranch = false;
        while (!stopGen && !finishedBranch) {
            FindNextPosSecondary(ref pos, pointsInPath, ref stopGen, ref finishedBranch);
        }

        // if generation failed, revert opened rooms to closed (undoing the path), and return false, indicating that generation failed
        if (stopGen) {
            for (int i = 0; i < pointsInPath.Count; ++i) {
                rooms[pointsInPath[i].x, pointsInPath[i].y, pointsInPath[i].z].gameObject.SetActive(true);
            }
            return false;
        }

        // second half of branch, reset the position to the origin of the branch
        pos.x = x;
        pos.y = y;
        pos.z = z;

        // Generate the second half of the branch until finding a next position fails (and so does the path generation),
        // or this half of the branch connects to a different branch
        finishedBranch = false;
        while (!stopGen && !finishedBranch) {
            FindNextPosSecondary(ref pos, pointsInPath, ref stopGen, ref finishedBranch);
        }

        // if generation failed, revert opened rooms to closed (undoing the path), and return false, indicating that generation failed
        // update currentPathsToCenter (will only need updating if the second half of the branch generation failed)
        if (stopGen) {
            for (int i = 0; i < pointsInPath.Count; ++i) {
                rooms[pointsInPath[i].x, pointsInPath[i].y, pointsInPath[i].z].gameObject.SetActive(true);
            }
            if (centerPaths != currentPathsToCenter) {
                currentPathsToCenter = centerPaths;
            }
            return false;
        }

        // return true, indicating that generation was successful
        return true;
        
    }

    // opens up a random next position for the main path
    // if solveOk is true, the next position can be an exit
    // if no positions are valid next positions, stopGen will be set to true
    private void FindNextPosMain(ref Point pos, ref bool stopGen, bool solveOk) {

        // store whether the next position has been found and which spaces can still be checked
        bool found = false;
        List<int> sides = new List<int> {0, 1, 2, 3, 4, 5};

        // search for a next position while there are still sides to check
        while (sides.Count > 0) {

            // pick a random side that has not been checked
            int nextPos = rand.Next(sides.Count);

            // check if the selected side is a valid spot, and update pos and found accordingly
            switch (sides[nextPos]) {

                case 0:
                    if (IsValidSpace(ref pos, 1, 0, 0, solveOk)) {
                        found = true;
                        ++pos.x;
                    }
                    break;
                case 1:
                    if (IsValidSpace(ref pos, -1, 0, 0, solveOk)) {
                        found = true;
                        --pos.x;
                    }
                    break;
                case 2:
                    if (IsValidSpace(ref pos, 0, 1, 0, solveOk)) {
                        found = true;
                        ++pos.y;
                    }
                    break;
                case 3:
                    if (IsValidSpace(ref pos, 0, -1, 0, solveOk)) {
                        found = true;
                        --pos.y;
                    }
                    break;
                case 4:
                    if (IsValidSpace(ref pos, 0, 0, 1, solveOk)) {
                        found = true;
                        ++pos.z;
                    }
                    break;
                default:
                    if (IsValidSpace(ref pos, 0, 0, -1, solveOk)) {
                        found = true;
                        --pos.z;
                    }
                    break;

            }

            // a valid position was found, so stop the search
            if (found) {
                break;
            }

            // the side was not a valid position, so remove it from the list
            sides.RemoveAt(nextPos);

        }

        // if no valid position was found, indicate that generation should stop, otherwise open up the next room
        if (!found) {
            stopGen = true;
        } else {
            rooms[pos.x, pos.y, pos.z].gameObject.SetActive(false);
        }

    }

    // opens up a random next position for a secondary path
    // if the next position completes a half of the branch, finishedBranch will be set to true
    // if no positions are valid next positions, stopGen will be set to true
    private void FindNextPosSecondary(ref Point pos, List<Point> prevPoints, ref bool stopGen, ref bool finishedBranch) {

        // store whether the next position has been found and which spaces can still be checked
        bool found = false;
        List<int> sides = new List<int> {0, 1, 2, 3, 4, 5};

        // search for a next position while there are still sides to check
        while (sides.Count > 0) {

            // pick a random side that has not been checked
            int nextPos = rand.Next(sides.Count);

            // check if the selected side is a valid spot, and update pos and found accordingly
            switch (sides[nextPos]) {

                case 0:
                    if (IsValidSpaceSecondary(ref pos, prevPoints, 1, 0, 0, ref finishedBranch)) {
                        found = true;
                        ++pos.x;
                    }
                    break;
                case 1:
                    if (IsValidSpaceSecondary(ref pos, prevPoints, -1, 0, 0, ref finishedBranch)) {
                        found = true;
                        --pos.x;
                    }
                    break;
                case 2:
                    if (IsValidSpaceSecondary(ref pos, prevPoints, 0, 1, 0, ref finishedBranch)) {
                        found = true;
                        ++pos.y;
                    }
                    break;
                case 3:
                    if (IsValidSpaceSecondary(ref pos, prevPoints, 0, -1, 0, ref finishedBranch)) {
                        found = true;
                        --pos.y;
                    }
                    break;
                case 4:
                    if (IsValidSpaceSecondary(ref pos, prevPoints, 0, 0, 1, ref finishedBranch)) {
                        found = true;
                        ++pos.z;
                    }
                    break;
                default:
                    if (IsValidSpaceSecondary(ref pos, prevPoints, 0, 0, -1, ref finishedBranch)) {
                        found = true;
                        --pos.z;
                    }
                    break;

            }

            // a valid position was found, so stop the search
            if (found) {
                break;
            }

            // the side was not a valid position, so remove it from the list
            sides.RemoveAt(nextPos);

        }

        // If no valid position was found, indicate that generation should stop, otherwise open up the next room
        // and add it to the list of previous points in the current path being generated
        if (!found) {
            stopGen = true;
        } else {
            prevPoints.Add(pos);
            rooms[pos.x, pos.y, pos.z].gameObject.SetActive(false);
        }

    }
    
    // returns true if the room determined by point and the respective shifts is a valid next position for the main path
    // if solveOk is true, any position which creates an exit is a valid position
    private bool IsValidSpace(ref Point point, int xShift, int yShift, int zShift, bool solveOk) {

        // set positional data for the room in question
        int x = point.x + xShift;
        int y = point.y + yShift;
        int z = point.z + zShift;

        // if the next position is not allowed to create an exit, return false if it would
        if (!solveOk && (x <= 0 || x >= dimensions - 1 || y <= 0 || y >= dimensions - 1 || z <= 0 || z >= dimensions - 1)) {
            return false;
        }

        // return true if the room in question does not border any previously opened rooms
        return !BordersOpenSpace(ref point, xShift, yShift, zShift);

    }

    // returns true if the room determined by x, y, and z does not border any previously opened rooms
    private bool IsValidSpace(int x, int y, int z) {
        return (
            GetActive(x - 1, y, z)
            &&
            GetActive(x + 1, y, z)
            &&
            GetActive(x, y - 1, z)
            &&
            GetActive(x, y + 1, z)
            &&
            GetActive(x, y, z - 1)
            &&
            GetActive(x, y, z + 1)
        );
    }

    // returns true if the room determined by point and the respective shifts is a valid next position for a secondary path
    // If the next position joins the branch to a previously generated branch at exactly one side of the new position,
    // finishedBranch will be set to true
    private bool IsValidSpaceSecondary(ref Point point, List<Point> prevPoints, int xShift, int yShift, int zShift, ref bool finishedBranch) {

        // set positional data for the room in question
        int x = point.x + xShift;
        int y = point.y + yShift;
        int z = point.z + zShift;

        bool possiblePathToCenter = false;

        // return false if the current position is already opened
        // Necessary for the situation in which after the first move from the origin, the first half of a secondary
        // branch moves back into the origin location, where no adjacent (this is key, as BordersPrevPoints does not
        // check the current room) rooms are stored in prevPoints
        if (!GetActive(x, y, z)) {
            return false;
        }

        // return false if the next position would open up a path through the ground of the center room
        if (
            x >= midpoint - 1 && x <= midpoint
            && y == midpoint - 2
            && z >= midpoint - 1 && z <= midpoint
        ) {
            return false;
        }

        // If the next position creates a path to center and there can be more paths to center, update possiblePathToCenter,
        // otherwise return false
        if (

            // check corners / edges (2 = edge, 3 = corner)
            !(
                ((x == midpoint - 2 || x == midpoint + 1) ? 1 : 0)
                + ((y == midpoint - 2 || y == midpoint + 1) ? 1 : 0)
                + ((z == midpoint - 2 || z == midpoint + 1) ? 1 : 0)
                >= 2
            )
            && x >= midpoint - 2 && x <= midpoint + 1
            && y >= midpoint - 2 && y <= midpoint + 1
            && z >= midpoint - 2 && z <= midpoint + 1

        ) {
            if (currentPathsToCenter < maxPathsToCenter) {
                possiblePathToCenter = true;
            } else {
                return false;
            }
        }

        // return false if the next position would create an exit
        if (x <= 0 || x >= dimensions - 1 || y <= 0 || y >= dimensions - 1 || z <= 0 || z >= dimensions - 1) {
            return false;
        }

        // returns false if the next position would border part of the path being generated
        if (BordersPrevPoints(ref point, prevPoints, xShift, yShift, zShift)) {
            return false;
        }

        // Check how many open rooms the next position borders, and return false if more than one room is open (only
        // join the branch to a previously generated branch at exactly one side of the new position)
        int numBorders = BorderNum(ref point, xShift, yShift, zShift);
        if (numBorders > 1) {
            return false;
        }

        // if the next position joing the path with the center, update currentPathsToCenter to reflect this
        if (possiblePathToCenter) {
            ++currentPathsToCenter;
        }

        // update finshedBranch to reflect if the branch has been joined with a previously generated path
        finishedBranch = numBorders == 1;
        // the next position has satisfied all requirements and is valid, so return true
        return true;
        
    }

    // returns true if the room determined by point and the respective shifts borders a previously opened room
    // ignores the space the point is coming from
    private bool BordersOpenSpace(ref Point point, int xShift, int yShift, int zShift) {

        // set positional data for the room in question
        int x = point.x + xShift;
        int y = point.y + yShift;
        int z = point.z + zShift;

        // return true if any of the adjacent rooms that is not point is open
        return (

            (xShift != 1 && !GetActive(x - 1, y, z))
            ||
            (xShift != -1 && !GetActive(x + 1, y, z))
            ||
            (yShift != 1 && !GetActive(x, y - 1, z))
            ||
            (yShift != -1 && !GetActive(x, y + 1, z))
            ||
            (zShift != 1 && !GetActive(x, y, z - 1))
            ||
            (zShift != -1 && !GetActive(x, y, z + 1))

        );

    }

    // returns true if the room determined by point and the respective shifts borders a previous part of the path
    // ingnores the space the point is coming from
    private bool BordersPrevPoints(ref Point point, List<Point> prevPoints, int xShift, int yShift, int zShift) {

        // set positional data for the room in question
        int x = point.x + xShift;
        int y = point.y + yShift;
        int z = point.z + zShift;

        Point pos;

        // return true if an adjacent room (ignoring point) is part of the path
        if (xShift != 1) {
            pos = new Point(x - 1, y, z);
            if (prevPoints.Contains(pos) || finalPath.Contains(pos)) {
                return true;
            }
        }
        if (xShift != -1) {
            pos = new Point(x + 1, y, z);
            if (prevPoints.Contains(pos) || finalPath.Contains(pos)) {
                return true;
            }
        }
        if (yShift != 1) {
            pos = new Point(x, y - 1, z);
            if (prevPoints.Contains(pos) || finalPath.Contains(pos)) {
                return true;
            }
        }
        if (yShift != -1) {
            pos = new Point(x, y + 1, z);
            if (prevPoints.Contains(pos) || finalPath.Contains(pos)) {
                return true;
            }
        }
        if (zShift != 1) {
            pos = new Point(x, y, z - 1);
            if (prevPoints.Contains(pos) || finalPath.Contains(pos)) {
                return true;
            }
        }
        if (zShift != -1) {
            pos = new Point(x, y, z + 1);
            if (prevPoints.Contains(pos) || finalPath.Contains(pos)) {
                return true;
            }
        }

        // no adjacent rooms (ignoring point) were part of the path, so return false
        return false;

    }

    // returns the number of open rooms surrounding the room determined by point and the respective shifts
    private int BorderNum(ref Point point, int xShift, int yShift, int zShift) {

        // set positional data for the room in question
        int x = point.x + xShift;
        int y = point.y + yShift;
        int z = point.z + zShift;
        
        // add to num for each open room adjecent to the room in question
        int num = 0;
        if (xShift != 1 && !GetActive(x - 1, y, z)) {
            ++num;
        }
        if (xShift != -1 && !GetActive(x + 1, y, z)) {
            ++num;
        }
        if (yShift != 1 && !GetActive(x, y - 1, z)) {
            ++num;
        }
        if (yShift != -1 && !GetActive(x, y + 1, z)) {
            ++num;
        }
        if (zShift != 1 && !GetActive(x, y, z - 1)) {
            ++num;
        }
        if (zShift != -1 && !GetActive(x, y, z + 1)) {
            ++num;
        }

        // return the number of open rooms bordering the room in question
        return num;

    }

    // returns true if the current room is closed or if the room is out of bounds, false otherwise
    private bool GetActive(int x, int y, int z) {
        // return true if the room is out of bounds
        if (
            x < 0 || x >= dimensions
            || y < 0 || y >= dimensions
            || z <=0 || z >= dimensions
        ) {
            return true;
        }
        // return whether the room is currently active
        return rooms[x, y, z].gameObject.activeSelf;
    }
#if DEBUG_MAZE
    // create a regular maze using the debug maze as a template
    private void DuplicateMaze() {

        // set length measurments for proper room placement
        float roomLength = baseRoom.transform.localScale.x;

        for (int x = 0; x < dimensions; ++x) {
            for (int y = 0; y < dimensions; ++y) {
                for (int z = 0; z < dimensions; ++z) {
                    // Copy the room (and if it is open or closed)
                    // and move it away from the debug maze by changing the z transform component
                    GameObject newRoom = GameObject.Instantiate(rooms[x, y, z]);
                    newRoom.transform.position = new Vector3(
                        newRoom.transform.position.x,
                        newRoom.transform.position.y,
                        (z - dimensions) * roomLength - 35
                    );
                }
            }
        }

    }
#endif
    // reset the maze to its default state, keeping the center open (these never should have been closed)
    private void ResetRooms() {

        for (int x = 0; x < dimensions; ++x) {
            for (int y = 0; y < dimensions; ++y) {
                for (int z = 0; z < dimensions; ++z) {
                    // if a room does not make up the center, close it
                    if (
                        x < midpoint - 1 || x > midpoint
                        || y < midpoint - 1 || y > midpoint
                        || z < midpoint - 1 || z > midpoint
                    ) {
                        rooms[x, y, z].gameObject.SetActive(true);
                    }
                }
            }
        }

    }

}
