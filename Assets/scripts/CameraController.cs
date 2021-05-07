using UnityEngine;

public class CameraController : MonoBehaviour {

    [SerializeField] private GameObject player;
    [SerializeField] private float maxAngle;
    [SerializeField] private float minAngle;
    [SerializeField] private float verticleMovementSensitivity;
    [SerializeField] private float horizontalMovementSensitivity;
    private float verticleAngle;
    private float horizontalAngle;

    // Start is called before the first frame update
    void Start() {
        verticleAngle = 0;
        horizontalAngle = 0;
    }

    // Update is called once per frame
    void LateUpdate() {

        Vector3 newPosition = player.transform.position;
        newPosition.y += 0.5f;
        transform.position = newPosition;

        verticleAngle += Input.GetAxis("Mouse Y") * verticleMovementSensitivity;
        verticleAngle = Mathf.Clamp(verticleAngle, minAngle, maxAngle);

        horizontalAngle += Input.GetAxis("Mouse X") * horizontalMovementSensitivity;

        transform.eulerAngles = new Vector3(-verticleAngle, horizontalAngle, 0);

    }

}
