using UnityEngine;
using TMPro;
using System;

public class ShowCameraTransform : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private Camera mainCamera;

    private Vector3[] posizioni = new Vector3[4];
    private Quaternion[] rotazioni = new Quaternion[4];


    private int curr_positions = 0;

    private void Start()
    {
        //if (mainCamera == null)
            //mainCamera = Camera.main;

        InvokeRepeating(nameof(UpdateCameraTransform), 0f, 2f);

        posizioni[0] = new Vector3(-8.79f, 7.33f, 0.08f);
        posizioni[1] = new Vector3(8.40f, 5.47f, 0.00f);
        posizioni[2] = new Vector3(-7.50f, 4.71f, 4.35f);
        posizioni[3] = new Vector3(7.78f, 7.46f, 2.44f);

        rotazioni[0] = Quaternion.Euler(21.00f, 83.80f, 0.00f);
        rotazioni[1] = Quaternion.Euler(11.90f, 266.50f, 0.00f);
        rotazioni[2] = Quaternion.Euler(15.50f, 109.80f, 0.00f);
        rotazioni[3] = Quaternion.Euler(23.40f, 246.60f, 0.00f);

        curr_positions = 0;


    }

    private void UpdateCameraTransform()
    {
        if (outputText == null || mainCamera == null)
            return;

        Transform camTransform = mainCamera.transform;

        outputText.text =
            $"Position:\n" +
            $"X: {camTransform.position.x:F2}\n" +
            $"Y: {camTransform.position.y:F2}\n" +
            $"Z: {camTransform.position.z:F2}\n\n" +
            $"Rotation:\n" +
            $"X: {camTransform.eulerAngles.x:F2}\n" +
            $"Y: {camTransform.eulerAngles.y:F2}\n" +
            $"Z: {camTransform.eulerAngles.z:F2}";
    }

    public void switchView()
    {
        Debug.Log("BOTTONE PREMUTO");
        if (curr_positions == posizioni.Length)
        {
            curr_positions = 0;
        }

        mainCamera.transform.SetPositionAndRotation(
            posizioni[curr_positions],
            rotazioni[curr_positions]
        );
        curr_positions++;
        UpdateCameraTransform();
        Debug.Log("BOTTONE PREMUTO -- OK");
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(UpdateCameraTransform));
    }
}
