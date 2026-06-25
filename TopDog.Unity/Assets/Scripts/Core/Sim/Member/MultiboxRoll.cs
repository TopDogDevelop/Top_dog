namespace TopDog.Sim.Member;

public static class MultiboxRoll
{
    private const double Mu = 2.0;
    private const double Sigma = 1.2;
    private const double P99 = 0.001;

    public static int Roll(Random rng)
    {
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var g = rng.NextDouble() * Sigma + Mu;
            var v = (int)Math.Round(g);
            if (v < 1)
            {
                v = 1;
            }
            if (v > 99)
            {
                if (rng.NextDouble() < P99)
                {
                    return 99;
                }
                continue;
            }
            if (v == 99 && rng.NextDouble() >= P99)
            {
                continue;
            }
            return v;
        }
        return 2;
    }
}
