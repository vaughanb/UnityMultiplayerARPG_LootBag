namespace MultiplayerARPG
{
    public abstract partial class BaseMonsterCharacterEntity : BaseCharacterEntity
    {
        /// <summary>
        /// Sets the monster character entity's destroy delay to the specified value.
        /// </summary>
        /// <param name="delay">seconds until destroy after death</param>
        public void SetDestroyDelay(float delay)
        {
            destroyDelay = delay;
        }
    }
}