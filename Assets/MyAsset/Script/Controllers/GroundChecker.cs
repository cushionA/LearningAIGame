using R3;
using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    public ReactiveProperty<bool> IsGround = new ReactiveProperty<bool>(false);

    private const string k_GroundTag = "Ground";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(k_GroundTag))
        {
            this.IsGround.Value = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(k_GroundTag))
        {
            this.IsGround.Value = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(k_GroundTag))
        {
            this.IsGround.Value = false;
        }
    }

    private void OnDestroy()
    {
        this.IsGround?.Dispose();
    }
}
