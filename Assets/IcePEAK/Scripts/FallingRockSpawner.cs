using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Periodically spawns rock prefabs inside a box volume above the cliff.
/// Each spawned rock automatically gets a RockHitDetector that sends the
/// player back to the last Checkpoint on impact.
/// </summary>
public class FallingRockSpawner : MonoBehaviour
{
    [Header("Rock Prefabs")]
    [Tooltip("One is picked at random each spawn.")]
    [SerializeField] private GameObject[] rockPrefabs;

    [Header("Spawn Volume (local space, centered on this transform)")]
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 1f, 10f);

    [Header("Timing")]
    [SerializeField] private float minInterval = 1.5f;
    [SerializeField] private float maxInterval = 4f;
    [SerializeField] private float startDelay = 2f;

    [Header("Rock Physics")]
    [SerializeField] private Vector2 scaleRange = new Vector2(0.3f, 0.8f);
    [SerializeField] private float initialDownwardSpeed = 0f;
    [SerializeField] private float horizontalJitter = 0.5f;
    [SerializeField] private float defaultMass = 5f;

    [Header("Cleanup")]
    [SerializeField] private float rockLifetime = 10f;

    [Header("Hit Settings")]
    [Tooltip("Minimum impact speed (m/s) to trigger a respawn.")]
    [SerializeField] private float minImpactSpeed = 1f;

    [Header("Haptics")]
    [Range(0f, 1f)][SerializeField] private float hapticAmplitude = 0.8f;
    [SerializeField] private float hapticDuration = 0.4f;

    [Header("On Hit")]
    [Tooltip("Optional VFX/SFX prefab spawned at the contact point.")]
    [SerializeField] private GameObject hitEffectPrefab;

    [Header("Gizmo")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.85f, 0.1f, 0.5f);

    private float _nextSpawnTime;

    private void OnEnable()
    {
        _nextSpawnTime = Time.time + startDelay;
    }

    private void Update()
    {
        if (Time.time < _nextSpawnTime) return;
        if (rockPrefabs == null || rockPrefabs.Length == 0) return;

        SpawnRock();
        _nextSpawnTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    private void SpawnRock()
    {
        GameObject prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
        if (prefab == null) return;

        Vector3 localPos = new Vector3(
            Random.Range(-spawnAreaSize.x, spawnAreaSize.x) * 0.5f,
            Random.Range(-spawnAreaSize.y, spawnAreaSize.y) * 0.5f,
            Random.Range(-spawnAreaSize.z, spawnAreaSize.z) * 0.5f);

        GameObject rock = Instantiate(prefab, transform.TransformPoint(localPos), Random.rotation);

        rock.transform.localScale *= Random.Range(scaleRange.x, scaleRange.y);

        if (!rock.TryGetComponent<Rigidbody>(out var rb))
        {
            rb = rock.AddComponent<Rigidbody>();
            rb.mass = defaultMass;
        }
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.down * initialDownwardSpeed + new Vector3(
            Random.Range(-horizontalJitter, horizontalJitter), 0f,
            Random.Range(-horizontalJitter, horizontalJitter));
        rb.angularVelocity = Random.insideUnitSphere * 2f;

        // Attach hit detector, passing spawner settings by value
        var detector = rock.AddComponent<RockHitDetector>();
        detector.Init(minImpactSpeed, hapticAmplitude, hapticDuration, hitEffectPrefab);

        Destroy(rock, rockLifetime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, spawnAreaSize);
        Gizmos.DrawLine(Vector3.zero, Vector3.down * 5f);
    }

    // -------------------------------------------------------------------------
    // Inner component — added at runtime to each rock, never shown in Inspector
    // -------------------------------------------------------------------------
    private class RockHitDetector : MonoBehaviour
    {
        private float _minSpeed;
        private float _hapticAmp;
        private float _hapticDur;
        private GameObject _hitEffect;
        private bool _hasHit;

        public void Init(float minSpeed, float amp, float dur, GameObject effect)
        {
            _minSpeed   = minSpeed;
            _hapticAmp  = amp;
            _hapticDur  = dur;
            _hitEffect  = effect;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasHit) return;
            if (!collision.gameObject.CompareTag("Player")) return;
            if (collision.relativeVelocity.magnitude < _minSpeed) return;

            _hasHit = true;

            if (_hitEffect != null)
            {
                var c = collision.GetContact(0);
                Instantiate(_hitEffect, c.point, Quaternion.LookRotation(c.normal));
            }

            SendHaptics();
            RespawnPlayer(collision.transform);
            Destroy(gameObject);
        }

        private void RespawnPlayer(Transform player)
        {
            if (!Checkpoint.HasCheckpoint)
            {
                Debug.LogWarning("[RockHitDetector] No checkpoint reached yet.");
                return;
            }

            foreach (var pick in FindObjectsByType<IcePickController>(FindObjectsSortMode.None))
                if (pick.IsEmbedded) pick.Release();

            player.position = Checkpoint.LastPosition;
        }

        private void SendHaptics()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, devices);
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, devices);

            foreach (var d in devices)
                d.SendHapticImpulse(0, _hapticAmp, _hapticDur);
        }
    }
}
