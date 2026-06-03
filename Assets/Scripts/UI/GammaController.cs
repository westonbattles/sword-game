using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GammaController : MonoBehaviour
{
    private Volume volume;
    private LiftGammaGain liftGammaGain;
    

    void Start()
    {
        volume = GetComponent<Volume>();

        // Try to get the Lift Gamma Gain component from the volume profile
        if (volume.profile.TryGet(out liftGammaGain))
        {
            // Set an initial gamma value (Vector4: R, G, B, Gamma)
            // Gamma values can range from -1 to 1
            float initialGammaValue = 0.2f;
            liftGammaGain.gamma.Override(new Vector4(1f, 1f, 1f, initialGammaValue));
        }

    }

    public void AdjustSceneGamma(float gammaValue)
    {
        if (liftGammaGain != null)
        {
            // Override just the gamma property while keeping RGB values intact
            liftGammaGain.gamma.Override(new Vector4(1f, 1f, 1f, gammaValue));
        }
    }
}
