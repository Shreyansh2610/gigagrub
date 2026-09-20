using UnityEngine;

namespace GigaGrub.Player
{
    public class CreatureCollision : MonoBehaviour
    {
        [Header("Collision Rules")]
        [Tooltip("Rule for resolving head-on collisions between two creatures")]
        [SerializeField] private HeadToHeadRule headToHeadRule = HeadToHeadRule.LongerSurvives;

        [Header("References")]
        [SerializeField] private PlayerBody ownerBody;
        [SerializeField] private CreatureDeath creatureDeath;
        [SerializeField] private Collider2D headCollider;

        public HeadToHeadRule HeadToHeadRule => headToHeadRule;
        public PlayerBody OwnerBody => ownerBody;
        public CreatureDeath CreatureDeath => creatureDeath;

        private void Awake()
        {
            if (ownerBody == null)
            {
                ownerBody = GetComponent<PlayerBody>();
            }

            if (creatureDeath == null)
            {
                creatureDeath = GetComponent<CreatureDeath>();
                if (creatureDeath == null)
                {
                    creatureDeath = gameObject.AddComponent<CreatureDeath>();
                }
            }

            if (headCollider == null)
            {
                headCollider = GetComponent<Collider2D>();
            }
        }

        public void SetHeadToHeadRule(HeadToHeadRule rule)
        {
            headToHeadRule = rule;
        }

        public void BindComponents(PlayerBody body, CreatureDeath death)
        {
            ownerBody = body;
            creatureDeath = death;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (creatureDeath != null && creatureDeath.IsDead) return;

            // 1. Check if colliding with another creature's body segment
            PlayerSegment segment = other.GetComponent<PlayerSegment>();
            if (segment == null)
            {
                segment = other.GetComponentInParent<PlayerSegment>();
            }

            if (segment != null)
            {
                // Ignore collisions with own body segments
                if (segment.Owner == ownerBody || segment.transform.IsChildOf(transform))
                {
                    return;
                }

                // Touching another creature's body kills this creature
                if (creatureDeath != null)
                {
                    creatureDeath.Die(DeathReason.HitCreatureBody);
                }
                return;
            }

            // 2. Check if colliding head-to-head with another creature
            CreatureCollision otherHead = other.GetComponent<CreatureCollision>();
            if (otherHead == null)
            {
                otherHead = other.GetComponentInParent<CreatureCollision>();
            }

            if (otherHead != null && otherHead != this)
            {
                ResolveHeadToHeadCollision(otherHead);
            }
        }

        public void ResolveHeadToHeadCollision(CreatureCollision otherHead)
        {
            if (creatureDeath != null && creatureDeath.IsDead) return;
            if (otherHead.CreatureDeath != null && otherHead.CreatureDeath.IsDead) return;

            switch (headToHeadRule)
            {
                case HeadToHeadRule.BothDie:
                    if (creatureDeath != null) creatureDeath.Die(DeathReason.HeadToHeadDraw);
                    if (otherHead.CreatureDeath != null) otherHead.CreatureDeath.Die(DeathReason.HeadToHeadDraw);
                    break;

                case HeadToHeadRule.LongerSurvives:
                    int myLength = ownerBody != null ? ownerBody.CurrentLength : 10;
                    int otherLength = otherHead.OwnerBody != null ? otherHead.OwnerBody.CurrentLength : 10;

                    if (myLength < otherLength)
                    {
                        if (creatureDeath != null) creatureDeath.Die(DeathReason.HitLargerHead);
                    }
                    else if (otherLength < myLength)
                    {
                        if (otherHead.CreatureDeath != null) otherHead.CreatureDeath.Die(DeathReason.HitLargerHead);
                    }
                    else
                    {
                        // Equal lengths result in mutual elimination
                        if (creatureDeath != null) creatureDeath.Die(DeathReason.HeadToHeadDraw);
                        if (otherHead.CreatureDeath != null) otherHead.CreatureDeath.Die(DeathReason.HeadToHeadDraw);
                    }
                    break;

                case HeadToHeadRule.ShorterSurvives:
                    int myLen = ownerBody != null ? ownerBody.CurrentLength : 10;
                    int otherLen = otherHead.OwnerBody != null ? otherHead.OwnerBody.CurrentLength : 10;

                    if (myLen > otherLen)
                    {
                        if (creatureDeath != null) creatureDeath.Die(DeathReason.HitLargerHead);
                    }
                    else if (otherLen > myLen)
                    {
                        if (otherHead.CreatureDeath != null) otherHead.CreatureDeath.Die(DeathReason.HitLargerHead);
                    }
                    else
                    {
                        if (creatureDeath != null) creatureDeath.Die(DeathReason.HeadToHeadDraw);
                        if (otherHead.CreatureDeath != null) otherHead.CreatureDeath.Die(DeathReason.HeadToHeadDraw);
                    }
                    break;
            }
        }
    }
}
