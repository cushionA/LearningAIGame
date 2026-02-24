using UnityEngine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LearningAIGame.CombatSystem.Core
{
public class UIStatusSystem : MonoBehaviour
{
    [SerializeField]
    int BaseHealth;
    [SerializeField]
    int BaseEnergy;
    [SerializeField]
    Image HealthBar;
    [SerializeField]
    Image EnergyBar;
    [SerializeField]
    float UISmoothSpeed = 0.25f;

    [SerializeField]
    Animator animator; 

    [SerializeField]
    bool isDamage = false;


    [SerializeField]    
    StateSystem stateSystem;
    void Start()
    {
        animator = GetComponent<Animator>();

        BaseHealth = stateSystem.Hp;
        BaseEnergy = stateSystem.Energy;
    }

    // Update is called once per frame
    void Update()
    {
        if (Camera.main != null )
        {
            transform.LookAt(Camera.main.transform);
        }
        

        float targetHealthFill = stateSystem.Hp / BaseHealth;
        float targetEnergyFill = stateSystem.Energy/ BaseHealth;

        if (HealthBar != null )
        {
            if(HealthBar.fillAmount!=targetHealthFill)
            isDamage = true;

            if(isDamage)
            {
                animator.Play("DamageAni");
            }

            HealthBar.fillAmount = Mathf.MoveTowards(
                HealthBar.fillAmount,
                targetHealthFill,
                UISmoothSpeed * Time.deltaTime
            );
        }

        if (EnergyBar != null)
        {
            EnergyBar.fillAmount = Mathf.MoveTowards(
                EnergyBar.fillAmount,
                targetEnergyFill,
                UISmoothSpeed * Time.deltaTime
            );
            if(EnergyBar.fillAmount == 0 && animator.GetCurrentAnimatorStateInfo(0).IsName("New State"))
            {
                animator.Play("NoEnergyAni");
            }
        }
    }

    public void getDamage()
    {
        isDamage = false;
    }

}
}
