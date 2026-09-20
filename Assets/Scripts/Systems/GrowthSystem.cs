using System;
using UnityEngine;
using GigaGrub.Player;

namespace GigaGrub.Systems
{
    public class GrowthSystem : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the player body component")]
        [SerializeField] private PlayerBody playerBody;

        [Header("Growth Animation & Rate")]
        [Tooltip("Rate at which pending growth is converted to body segments per second (0 = instant)")]
        [SerializeField] private float growthRate = 25f;

        [Tooltip("Minimum time between adding new physical segments during rapid consumption")]
        [SerializeField] private float minIntervalBetweenSegments = 0.02f;

        private int pendingGrowth = 0;
        private float growthTimer = 0f;

        public int CurrentLength => playerBody != null ? playerBody.CurrentLength : 0;
        public int MaxLength => playerBody != null ? playerBody.MaxLength : 600;
        public int PendingGrowth => pendingGrowth;
        public int TargetLength => Mathf.Min(CurrentLength + pendingGrowth, MaxLength);

        public event Action<int, int, int> OnLengthChanged;
        public event Action OnMaxGrowthReached;

        private void Awake()
        {
            if (playerBody == null)
            {
                playerBody = GetComponent<PlayerBody>();
                if (playerBody == null)
                {
                    playerBody = GetComponentInParent<PlayerBody>();
                }
            }
        }

        private void Update()
        {
            if (pendingGrowth <= 0 || playerBody == null) return;

            growthTimer += Time.deltaTime;

            // When large amounts of food are eaten rapidly (e.g. 100 or 500 food), dynamically scale consumption rate
            float effectiveRate = growthRate;
            if (pendingGrowth > 50)
            {
                effectiveRate = Mathf.Max(growthRate, pendingGrowth * 4f);
            }
            else if (pendingGrowth > 10)
            {
                effectiveRate = Mathf.Max(growthRate, pendingGrowth * 2f);
            }

            float interval = 1f / Mathf.Max(1f, effectiveRate);
            interval = Mathf.Max(interval, minIntervalBetweenSegments);

            if (growthTimer >= interval)
            {
                growthTimer = 0f;
                int segmentsToSpawn = 1;

                // For extreme batch growth (> 50 queued), add in small batches per tick
                if (pendingGrowth > 100)
                {
                    segmentsToSpawn = Mathf.Min(5, pendingGrowth);
                }
                else if (pendingGrowth > 30)
                {
                    segmentsToSpawn = Mathf.Min(2, pendingGrowth);
                }

                for (int i = 0; i < segmentsToSpawn && pendingGrowth > 0; i++)
                {
                    if (playerBody.CurrentLength < playerBody.MaxLength)
                    {
                        playerBody.AddSegment();
                        pendingGrowth--;
                        OnLengthChanged?.Invoke(playerBody.CurrentLength, TargetLength, 1);
                    }
                    else
                    {
                        pendingGrowth = 0;
                        OnMaxGrowthReached?.Invoke();
                        break;
                    }
                }
            }
        }

        public void BindPlayerBody(PlayerBody body)
        {
            playerBody = body;
        }

        public void Grow(int growthAmount)
        {
            if (growthAmount <= 0) return;

            int spaceRemaining = MaxLength - (CurrentLength + pendingGrowth);
            if (spaceRemaining <= 0)
            {
                OnMaxGrowthReached?.Invoke();
                return;
            }

            int allowedGrowth = Mathf.Min(growthAmount, spaceRemaining);
            pendingGrowth += allowedGrowth;

            OnLengthChanged?.Invoke(CurrentLength, TargetLength, allowedGrowth);
        }

        public void GrowInstant(int growthAmount)
        {
            if (growthAmount <= 0 || playerBody == null) return;

            int spaceRemaining = MaxLength - CurrentLength;
            int toAdd = Mathf.Min(growthAmount, spaceRemaining);

            for (int i = 0; i < toAdd; i++)
            {
                playerBody.AddSegment();
            }

            OnLengthChanged?.Invoke(playerBody.CurrentLength, playerBody.CurrentLength, toAdd);
        }

        public void ClearPendingGrowth()
        {
            pendingGrowth = 0;
            if (playerBody != null)
            {
                OnLengthChanged?.Invoke(playerBody.CurrentLength, playerBody.CurrentLength, 0);
            }
        }

        public void ResetGrowth(int currentLength = 10)
        {
            pendingGrowth = 0;
            growthTimer = 0f;
            OnLengthChanged?.Invoke(currentLength, currentLength, 0);
        }
    }
}
