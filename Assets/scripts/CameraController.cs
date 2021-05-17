using UnityEngine;

/*
 * class to control the game camera in relationship to a player
 */
public class CameraController : MonoBehaviour {

    // player object the camera is following (first person)
    [SerializeField] private GameObject player;
    // the maximum viewing angle in degrees
    [SerializeField] private float maxAngle;
    // the minimum viewing angle in degrees
    [SerializeField] private float minAngle;
    // the sensitivity for verticle camera rotations
    [SerializeField] private float verticleMovementSensitivity;
    // the sensitivity for horizontal camera rotations
    [SerializeField] private float horizontalMovementSensitivity;

    // data values to store the camera rotations, initialized to default values
    private float verticleAngle = 0;
    private float horizontalAngle = 0;

    // LateUpdate is used so the camera moves after the player position has been determined
    void LateUpdate() {

        // get the player position, add a y offset (for servicable viewing height), and set the camera position to equal the result
        Vector3 newPosition = player.transform.position;
        newPosition.y += 0.5f;
        transform.position = newPosition;

        // update verticleAngle based off of mouse movement, and clamp it to be within the allowed viewing angles
        verticleAngle += Input.GetAxis("Mouse Y") * verticleMovementSensitivity;
        verticleAngle = Mathf.Clamp(verticleAngle, minAngle, maxAngle);

        // update horizontalAngle based off of mouse movement
        horizontalAngle += Input.GetAxis("Mouse X") * horizontalMovementSensitivity;

        // set the rotation to the determined angles (x and y values change, z should always be 0)
        transform.eulerAngles = new Vector3(-verticleAngle, horizontalAngle, 0);

    }

}
