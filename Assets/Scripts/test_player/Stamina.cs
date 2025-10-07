using UnityEngine;
using UnityEngine.UI; // если используете UI для отображения стамины

public class Stamina : MonoBehaviour
{
    public float maxStamina = 100f;
    public float currentStamina;

    public float staminaDrainRate = 10f;  // скорость уменьшения стамины в секунду
    public float staminaRecoveryRate = 5f; // скорость восстановления стамины в секунду

    public bool isDraining = false;  // флаг для уменьшения стамины (например, во время лазанья)
    public bool canRecover = true;   // можно ли восстанавливать стамину

    // UI
    public Slider staminaSlider;

    void Start()
    {
        currentStamina = maxStamina;
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    void Update()
    {
        if (isDraining)
        {
            DrainStamina();
        }
        else if (canRecover)
        {
            RecoverStamina();
        }

        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }
    }

    void DrainStamina()
    {
        currentStamina -= staminaDrainRate * Time.deltaTime;
        if (currentStamina < 0f)
        {
            currentStamina = 0f;
            // Можно добавить логику для отключения лазанья или вызова события усталости
        }
    }

    void RecoverStamina()
    {
        currentStamina += staminaRecoveryRate * Time.deltaTime;
        if (currentStamina > maxStamina)
        {
            currentStamina = maxStamina;
        }
    }

    public bool HasStamina()
    {
        return currentStamina > 0;
    }
}
