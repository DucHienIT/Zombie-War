namespace ZombieWar.Utils
{
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
