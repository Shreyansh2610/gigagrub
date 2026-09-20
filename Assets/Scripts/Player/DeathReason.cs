namespace GigaGrub.Player
{
    public enum DeathReason
    {
        HitCreatureBody,
        HitLargerHead,
        HeadToHeadDraw,
        HitBoundary,
        Suicide
    }

    public enum HeadToHeadRule
    {
        LongerSurvives,
        BothDie,
        ShorterSurvives
    }
}
