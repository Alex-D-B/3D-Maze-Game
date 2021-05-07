// #define DEBUG_MAZE
using System.Collections.Generic;
using UnityEngine;

public class RoomGenerator : MonoBehaviour {

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

    [SerializeField] private GameObject baseRoom;
    public int dimensions;
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
    private GameObject[,,] rooms;

    private System.Random rand = new System.Random();

    // Start is called before the first frame update
    void Start() {

        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();

        float roomLength = baseRoom.transform.localScale.x;
        int midpoint = dimensions / 2;

        maxPathsToCenter = rand.Next(maxPathsToCenterHigh - maxPathsToCenterLow) + maxPathsToCenterLow;
        minFinalPathLength = rand.Next(minFinalPathLengthHigh - minFinalPathLengthLow) + minFinalPathLengthLow;

        // initialize rooms
        rooms = new GameObject[dimensions, dimensions, dimensions];
        for (int x = 0; x < dimensions; ++x) {
            for (int y = 0; y < dimensions; ++y) {
                for (int z = 0; z < dimensions; ++z) {
                    
                    // fill up maze
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

        finalPath = new Queue<Point>(minFinalPathLength);
        bool successfulGeneration;
        do {
            successfulGeneration = GenMainPath();
        } while (!successfulGeneration);

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

        Debug.Log("Took " + string.Format(
            "{0}.{1}", timer.Elapsed.Seconds, timer.Elapsed.Milliseconds
        ) + " seconds");
        
    }

    // returns true if successful, false otherwise
    private bool GenMainPath() {

        int midpoint = dimensions / 2;

        // initialize paths
        int startingWall = rand.Next(5);
        int startingSquare = rand.Next(4);

        Point prevPos;
        Point pos;
        
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

        rooms[prevPos.x, prevPos.y, prevPos.z].gameObject.SetActive(false);
        rooms[pos.x, pos.y, pos.z].gameObject.SetActive(false);

        bool stopGen = false;
        int minFirstSteps = rand.Next(minFirstStepsHigh - minFirstStepsLow) + minFirstStepsLow;
        for (int i = 0; i < minFirstSteps; ++i) {
            FindNextPosMain(ref pos, ref stopGen, false);
            if (minFirstSteps - i <= minFinalPathLength) {
                finalPath.Enqueue(pos);
            }
        }

        while (!stopGen) {
            FindNextPosMain(ref pos, ref stopGen, true);
            if (
                pos.x <= 0 || pos.x >= dimensions - 1
                || pos.y <= 0 || pos.y >= dimensions - 1
                || pos.z <= 0 || pos.z >= dimensions - 1
            ) {
                break;
            }
            finalPath.Dequeue();
            finalPath.Enqueue(pos);
        }

        if (stopGen) {
            ResetRooms();
            return false;
        }

        return true;

    }

    // single attempt
    private bool GenSecondaryPath() {

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
        rooms[x, y, z].gameObject.SetActive(false);

        Point pos = new Point(x, y, z);
        List<Point> pointsInPath = new List<Point>(10);
        pointsInPath.Add(pos);

        // store the number of center paths beforehand so if path gen fails, the number is properly updated
        int centerPaths = currentPathsToCenter;
        bool stopGen = false;
        bool finishedBranch = false;
        while (!stopGen && !finishedBranch) {
            FindNextPosSecondary(ref pos, pointsInPath, ref stopGen, ref finishedBranch);
        }

        if (stopGen) {
            for (int i = 0; i < pointsInPath.Count; ++i) {
                rooms[pointsInPath[i].x, pointsInPath[i].y, pointsInPath[i].z].gameObject.SetActive(true);
            }
            return false;
        }

        // second half of branch
        pos.x = x;
        pos.y = y;
        pos.z = z;

        finishedBranch = false;
        while (!stopGen && !finishedBranch) {
            FindNextPosSecondary(ref pos, pointsInPath, ref stopGen, ref finishedBranch);
        }

        if (stopGen) {
            for (int i = 0; i < pointsInPath.Count; ++i) {
                rooms[pointsInPath[i].x, pointsInPath[i].y, pointsInPath[i].z].gameObject.SetActive(true);
            }
            if (centerPaths != currentPathsToCenter) {
                currentPathsToCenter = centerPaths;
            }
            return false;
        }

        return true;
        
    }

    private void FindNextPosMain(ref Point pos, ref bool stopGen, bool solveOk) {

        bool found = false;
        List<int> sides = new List<int> {0, 1, 2, 3, 4, 5};

        while (sides.Count > 0) {

            int nextPos = rand.Next(sides.Count);

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

            if (found) {
                break;
            }

            sides.RemoveAt(nextPos);

        }

        if (!found) {
            stopGen = true;
        } else {
            rooms[pos.x, pos.y, pos.z].gameObject.SetActive(false);
        }

    }

    private void FindNextPosSecondary(ref Point pos, List<Point> prevPoints, ref bool stopGen, ref bool finishedBranch) {

        bool found = false;
        List<int> sides = new List<int> {0, 1, 2, 3, 4, 5};

        while (sides.Count > 0) {

            int nextPos = rand.Next(sides.Count);

            switch (sides[nextPos]) {

                case 0: // 1, 0, 0
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

            if (found) {
                break;
            }

            sides.RemoveAt(nextPos);

        }

        if (!found) {
            stopGen = true;
        } else {
            prevPoints.Add(pos);
            rooms[pos.x, pos.y, pos.z].gameObject.SetActive(false);
        }

    }
    
    private bool IsValidSpace(ref Point point, int xShift, int yShift, int zShift, bool solveOk) {

        int x = point.x + xShift;
        int y = point.y + yShift;
        int z = point.z + zShift;

        int midpoint = dimensions / 2;
        
        if (
            x >= midpoint - 1 && x <= midpoint
            && y == midpoint - 2
            && z >= midpoint - 1 && z <= midpoint
        ) {
            return false;
        }

        if (!solveOk && (x <= 0 || x >= dimensions - 1 || y <= 0 || y >= dimensions - 1 || z <= 0 || z >= dimensions - 1)) {
            return false;
        }

        return !BordersOpenSpace(ref point, xShift, yShift, zShift);

    }

    private bool IsValidSpace(int x, int y, int z) {

        int midpoint = dimensions / 2;

        if (!GetActive(x, y, z)) {
            return false;
        }

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

    private bool IsValidSpaceSecondary(ref Point point, List<Point> prevPoints, int xShift, int yShift, int zShift, ref bool finishedBranch) {

        int x = point.x + xShift;
        int y = point.y + yShift;
        int z = point.z + zShift;

        int midpoint = dimensions / 2;

        bool possiblePathToCenter = false;

        if (!GetActive(x, y, z)) {
            return false;
        }

        if (
            x >= midpoint - 1 && x <= midpoint
            && y == midpoint - 2
            && z >= midpoint - 1 && z <= midpoint
        ) {
            return false;
        }

        if (

            // check corners / edges (2 = corner, 3 = edge)
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

        if (x <= 0 || x >= dimensions - 1 || y <= 0 || y >= dimensions - 1 || z <= 0 || z >= dimensions - 1) {
            return false;
        }

        if (BordersPrevPoints(ref point, prevPoints, xShift, yShift, zShift)) {
            return false;
        }

        int numBorders = BorderNum(ref point, xShift, yShift, zShift);
        if (numBorders > 1) {
            return false;
        }

        if (possiblePathToCenter) {
            ++currentPathsToCenter;
        }

        finishedBranch = numBorders == 1;
        return true;
        
    }

    // ignores the space the point is coming from
    private bool BordersOpenSpace(ref Point point, int xShift, int yShift, int zShift) {

        int x = point.x + xShift;
        int y = point.y + yShift;
        int z = point.z + zShift;

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

    // ingnores the space the point is coming from
    private bool BordersPrevPoints(ref Point point, List<Point> prevPoints, int xShift, int yShift, int zShift) {

        int x = point.x + xShift;
        int y = point.y + yShift;
        int z = point.z + zShift;

        Point pos;

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

        return false;

    }

    private int BorderNum(ref Point point, int xShift, int yShift, int zShift) {

        int x = point.x + xShift;
        int y = point.y + yShift;
        int z = point.z + zShift;
        
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

        return num;

    }

    private bool GetActive(int x, int y, int z) {
        if (
            x < 0 || x >= dimensions
            || y < 0 || y >= dimensions
            || z <=0 || z >= dimensions
        ) {
            return true;
        }
        return rooms[x, y, z].gameObject.activeSelf;
    }
#if DEBUG_MAZE
    private void DuplicateMaze() {

        float roomLength = baseRoom.transform.localScale.x;

        for (int x = 0; x < dimensions; ++x) {
            for (int y = 0; y < dimensions; ++y) {
                for (int z = 0; z < dimensions; ++z) {
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
    // keeps the center open
    private void ResetRooms() {

        int midpoint = dimensions / 2;

        for (int x = 0; x < dimensions; ++x) {
            for (int y = 0; y < dimensions; ++y) {
                for (int z = 0; z < dimensions; ++z) {
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
