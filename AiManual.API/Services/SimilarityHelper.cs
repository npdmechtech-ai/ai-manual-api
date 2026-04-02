namespace AiManual.API.Services
{
    public static class SimilarityHelper
    {
        public static double CosineSimilarity(float[] v1, float[] v2)
        {
            if (v1 == null || v2 == null || v1.Length != v2.Length)
                return 0;

            double dot = 0;
            double mag1 = 0;
            double mag2 = 0;

            for (int i = 0; i < v1.Length; i++)
            {
                dot += v1[i] * v2[i];
                mag1 += v1[i] * v1[i];
                mag2 += v2[i] * v2[i];
            }

            if (mag1 == 0 || mag2 == 0)
                return 0;

            return dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
        }
    }
}