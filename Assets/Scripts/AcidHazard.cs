using UnityEngine;

/// <summary>
/// Zona de peligro tóxica/radiactiva en el fondo del cañón del mapa postapocalíptico.
/// Daña al Mecha si cae en el río de ácido y reproduce efectos cómicos de advertencia.
/// </summary>
public class AcidHazard : MonoBehaviour
{
    [Header("Configuración de Daño")]
    [SerializeField] private int damagePerSecond = 18;
    [SerializeField] private float damageInterval = 0.5f;

    private float timer = 0f;

    private void OnTriggerStay(Collider other)
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = damageInterval;

            var mecha = other.GetComponentInParent<MechaController>();
            if (mecha != null && !mecha.IsGameOver)
            {
                int dmg = Mathf.CeilToInt(damagePerSecond * damageInterval);
                Debug.Log($"[GAME] ¡Contacto con ácido tóxico! Aplicando {dmg} daño corrosivo");
                mecha.TakeDamage(dmg);

                ProceduralAudio.Instance?.PlayAcidSizzle();

                Vector3 textPos = mecha.transform.position + Vector3.up * 16f;
                FloatingText.Spawn(textPos, "¡PELIGRO: RÍO ÁCIDO CORROSIVO!", new Color(0.2f, 1f, 0.4f), 0.38f);
            }
        }
    }
}
