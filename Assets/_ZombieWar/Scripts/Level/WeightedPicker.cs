using ZombieWar.Data;

namespace ZombieWar.Level
{
    public static class WeightedPicker
    {
        public static ZombieDefinitionSO Pick(SpawnWeight[] weights, float random01)
        {
            if (weights == null || weights.Length == 0)
            {
                return null;
            }

            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                total += weights[i].Weight;
            }

            if (total <= 0f)
            {
                return null;
            }

            float roll = random01 * total;
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= weights[i].Weight;
                if (roll <= 0f)
                {
                    return weights[i].Definition;
                }
            }

            return weights[weights.Length - 1].Definition;
        }
    }
}
