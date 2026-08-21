namespace RealtimePatient
{
    /// <summary>
    /// Which side of the conversation a Realtime client is playing. A scene
    /// uses this to pick the matching prompt and voice out of the active
    /// <see cref="ChapterDefinition"/>.
    /// </summary>
    public enum ChapterRole
    {
        Instructor = 0,
        Patient = 1,
    }
}
