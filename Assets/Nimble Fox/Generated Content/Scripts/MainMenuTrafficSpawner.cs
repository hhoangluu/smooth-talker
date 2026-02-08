using System.Collections.Generic;
using UnityEngine;

public class MainMenuTrafficSpawner : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private Transform[] pathPoints;

    [Header("Traffic Settings")]
    [SerializeField] private int carCount = 3;
    [SerializeField] private float carSpeed = 10f;
    [SerializeField] private float spawnSpacing = 20f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool alignToPath = true;
    [SerializeField] private float yOffset = 0f;

    private class CarState
    {
        public Transform Transform;
        public float Distance;
    }

    private readonly List<CarState> _cars = new List<CarState>();
    private float _pathLength;

    private void Start()
    {
        _pathLength = GetPathLength();
        SpawnCars();
    }

    private void Update()
    {
        if (_cars.Count == 0) return;

        float dt = Time.deltaTime;
        TickCars(dt);
    }

    private void SpawnCars()
    {
        _cars.Clear();

        if (carPrefab == null)
        {
            Debug.LogWarning($"{nameof(MainMenuTrafficSpawner)} on {name}: carPrefab is not assigned.");
            return;
        }

        if (pathPoints == null || pathPoints.Length < 2)
        {
            Debug.LogWarning($"{nameof(MainMenuTrafficSpawner)} on {name}: Need at least 2 path points to spawn cars.");
            return;
        }

        if (_pathLength <= 0f)
        {
            Debug.LogWarning($"{nameof(MainMenuTrafficSpawner)} on {name}: Path length is zero or negative.");
            return;
        }

        for (int i = 0; i < carCount; i++)
        {
            float distance = i * spawnSpacing;

            if (loop)
            {
                distance = Mathf.Repeat(distance, _pathLength);
            }
            else
            {
                distance = Mathf.Clamp(distance, 0f, _pathLength);
            }

            Vector3 pos = EvaluatePathPosition(distance);
            pos.y += yOffset;

            GameObject car = Instantiate(carPrefab, pos, Quaternion.identity, transform);

            Vector3 forward = EvaluatePathForward(distance);
            if (alignToPath && forward.sqrMagnitude > 0.0001f)
            {
                car.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }

            _cars.Add(new CarState
            {
                Transform = car.transform,
                Distance = distance
            });
        }
    }

    private void TickCars(float dt)
    {
        if (_pathLength <= 0f) return;

        for (int i = 0; i < _cars.Count; i++)
        {
            CarState car = _cars[i];
            if (car.Transform == null) continue;

            car.Distance += carSpeed * dt;

            if (loop)
            {
                car.Distance = Mathf.Repeat(car.Distance, _pathLength);
            }
            else
            {
                if (car.Distance > _pathLength)
                    car.Distance = _pathLength;
            }

            Vector3 pos = EvaluatePathPosition(car.Distance);
            pos.y += yOffset;
            car.Transform.position = pos;

            if (alignToPath)
            {
                Vector3 forward = EvaluatePathForward(car.Distance);
                if (forward.sqrMagnitude > 0.0001f)
                {
                    car.Transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                }
            }
        }
    }

    private Vector3 EvaluatePathPosition(float distance)
    {
        if (pathPoints == null || pathPoints.Length == 0)
            return transform.position;

        if (pathPoints.Length == 1)
            return pathPoints[0].position;

        if (_pathLength <= 0f)
            _pathLength = GetPathLength();

        distance = Mathf.Clamp(distance, 0f, _pathLength);

        float accumulated = 0f;

        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            Vector3 a = pathPoints[i].position;
            Vector3 b = pathPoints[i + 1].position;

            float segmentLength = Vector3.Distance(a, b);
            if (segmentLength <= 0f) continue;

            if (accumulated + segmentLength >= distance)
            {
                float t = (distance - accumulated) / segmentLength;
                return Vector3.Lerp(a, b, t);
            }

            accumulated += segmentLength;
        }

        // Fallback to last point
        return pathPoints[pathPoints.Length - 1].position;
    }

    private Vector3 EvaluatePathForward(float distance)
    {
        if (pathPoints == null || pathPoints.Length < 2)
            return Vector3.forward;

        if (_pathLength <= 0f)
            _pathLength = GetPathLength();

        // Small offset to derive forward direction along the path
        const float sampleOffset = 0.5f;

        float d0 = Mathf.Clamp(distance, 0f, _pathLength);
        float d1 = Mathf.Clamp(distance + sampleOffset, 0f, _pathLength);

        Vector3 p0 = EvaluatePathPosition(d0);
        Vector3 p1 = EvaluatePathPosition(d1);

        Vector3 dir = p1 - p0;
        if (dir.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        return dir.normalized;
    }

    private float GetPathLength()
    {
        if (pathPoints == null || pathPoints.Length < 2)
            return 0f;

        float length = 0f;

        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            Vector3 a = pathPoints[i].position;
            Vector3 b = pathPoints[i + 1].position;
            length += Vector3.Distance(a, b);
        }

        return length;
    }
}