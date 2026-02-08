using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    private Coroutine activeTransition;

    [Header("Settings")]
    [SerializeField] private float duration = 2.0f;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        // Khởi đầu ở vị trí ban đầu
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }
    Vector3 initialPosition = new Vector3(82.32f, 2.26f, 8.28f);    
    Quaternion initialRotation = Quaternion.Euler(-5.736f, 90f, 0f);
    Vector3 targetPosition = new Vector3(78.604f, 3.132f, 9.233f);
    Quaternion targetRotation = Quaternion.Euler(7.083f, 90f, 0f);
    public void CastReviewCamera()
    {
        // Nếu đang di chuyển dở mà bấm lần nữa thì dừng cái cũ để chạy cái mới
        if (activeTransition != null) StopCoroutine(activeTransition);
        
      //  activeTransition = StartCoroutine(MoveToTarget(targetPosition, targetRotation));
    }

    private IEnumerator MoveToTarget(Vector3 destination, Quaternion destRotation)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Sử dụng SmoothStep để chuyển động mượt hơn (nhanh ở giữa, chậm ở hai đầu)
            t = Mathf.SmoothStep(0, 1, t);

            transform.position = Vector3.Lerp(startPos, destination, t);
            transform.rotation = Quaternion.Slerp(startRot, destRotation, t);
            
            yield return null; // Chờ đến khung hình tiếp theo
        }

        // Đảm bảo khớp chính xác vị trí cuối cùng
        transform.position = destination;
        transform.rotation = destRotation;
        activeTransition = null;
    }
}
