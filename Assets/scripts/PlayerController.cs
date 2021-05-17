using UnityEngine;

/*
 * class to control player movement
 */
public class PlayerController : MonoBehaviour {

    // store speeds for various movements
    [SerializeField] private float groundSpeed;
    [SerializeField] private float downSpeed;
    [SerializeField] private float upSpeed;

    // the main camera tells us the horizontal direction we are facing so we can properly move forward using trig
    [SerializeField] private GameObject mainCamera;
    // mazeGenerator and baseRoom contain the dimensions of the maze which are used to determine the starting player position
    [SerializeField] private RoomGenerator mazeGenerator;
    [SerializeField] private GameObject baseRoom;

    // rigid body to control the player velocity
    Rigidbody rb;

    // Start is called before the first frame update
    void Start() {

        // set the rigid body variable to the player's rigid body
        rb = GetComponent<Rigidbody>();

        // get / find maze size measurments
        float roomLength = baseRoom.transform.localScale.x;
        int midpoint = mazeGenerator.dimensions / 2;

        // set the player position to start in the center of the maze
        transform.position = new Vector3(0, 15 + (midpoint - 1) * roomLength, 0);

    }

    // method to convert degrees to radians
    private float ToRadians(float degrees) {
        return degrees * 3.1415926535f / 180.0f;
    }

    // FixedUpdate controls player movement
    void FixedUpdate() {

        // get the viewing angle to determine which way is forward
        transform.rotation = mainCamera.transform.rotation;

        // set default velocities
        float xVelocity = 0;
        // use the current velocity determined by gravity by default
        float yVelocity = rb.velocity.y;
        float zVelocity = 0;

        // update velocities for key presses
        if (Input.GetKey(KeyCode.W)) {
            xVelocity += Mathf.Sin(ToRadians(mainCamera.transform.rotation.eulerAngles.y)) * groundSpeed;
            zVelocity += Mathf.Cos(ToRadians(mainCamera.transform.rotation.eulerAngles.y)) * groundSpeed;
        }
        if (Input.GetKey(KeyCode.S)) {
            xVelocity -= Mathf.Sin(ToRadians(mainCamera.transform.rotation.eulerAngles.y)) * groundSpeed;
            zVelocity -= Mathf.Cos(ToRadians(mainCamera.transform.rotation.eulerAngles.y)) * groundSpeed;
        }
        if (Input.GetKey(KeyCode.D)) {
            xVelocity += Mathf.Cos(ToRadians(mainCamera.transform.rotation.eulerAngles.y)) * groundSpeed;
            zVelocity -= Mathf.Sin(ToRadians(mainCamera.transform.rotation.eulerAngles.y)) * groundSpeed;
        }
        if (Input.GetKey(KeyCode.A)) {
            xVelocity -= Mathf.Cos(ToRadians(mainCamera.transform.rotation.eulerAngles.y)) * groundSpeed;
            zVelocity += Mathf.Sin(ToRadians(mainCamera.transform.rotation.eulerAngles.y)) * groundSpeed;
        }
        if (Input.GetKey(KeyCode.Space)) {
            yVelocity = upSpeed;
        }

        // move the player via the rigid body by giving it the determined velocities
        rb.velocity = new Vector3(xVelocity, yVelocity, zVelocity);
        
    }

}
