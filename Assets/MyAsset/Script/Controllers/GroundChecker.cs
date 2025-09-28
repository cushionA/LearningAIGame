using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    public bool IsGround { get; private set; }

    private const string k_GroundTag = "Ground";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(k_GroundTag))
        {
            IsGround = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(k_GroundTag))
        {
            IsGround = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(k_GroundTag))
        {
            IsGround = false;
        }
    }
}
