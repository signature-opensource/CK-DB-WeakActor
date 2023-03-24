namespace CK.DB.Zone.WeakActor
{
    /// <summary>
    /// Defines the behavior for existing groups whenever a WeakActor is moved into an other Zone.
    /// </summary>
    public enum WeakActorZoneMoveOption
    {
        /// <summary>
        /// Throws whenever any Group, that WeakActor is registered into, is found.
        /// In case of HZone, if at least 1 Group is out of the Hierarchy, this will throw.
        /// This is the safest option.
        /// </summary>
        None = 0,

        /// <summary>
        /// WeakActor will be automatically removed from the all its (non compatible) Groups.
        /// Without HZone, all Groups are then removed.
        /// With HZone, all Groups out of the Hierarchy will be removed.
        /// </summary>
        Intersect = 1,
    }
}
