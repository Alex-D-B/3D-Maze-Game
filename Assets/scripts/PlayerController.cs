using UnityEngine;

public class PlayerController : MonoBehaviour {

    [SerializeField] private float groundSpeed;
    [SerializeField] private float downSpeed;
    [SerializeField] private float upSpeed;

    [SerializeField] private GameObject mainCamera;
    [SerializeField] private RoomGenerator mazeGenerator;
    [SerializeField] private GameObject baseRoom;

    Rigidbody rb;

    // Start is called before the first frame update
    void Start() {

        rb = GetComponent<Rigidbody>();

        float roomLength = baseRoom.transform.localScale.x;
        int midpoint = mazeGenerator.dimensions / 2;

        transform.position = new Vector3(0, 15 + (midpoint - 1) * roomLength, 0);

    }

    float ToRadians(float degrees) {
        return degrees * 3.1415926535f / 180.0f;
    }

    // Update is called once per frame
    void FixedUpdate() {

        transform.rotation = mainCamera.transform.rotation;

        float xVelocity = 0;
        float yVelocity = rb.velocity.y;
        float zVelocity = 0;
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

        rb.velocity = new Vector3(xVelocity, yVelocity, zVelocity);
        
    }

}
