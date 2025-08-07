using UnityEngine;
using UnityEngine.UI;

namespace Adapters.Health
{
    public class HealthBarView : MonoBehaviour
    {
        [Tooltip("Image component to use as the health bar fill.")]
        [SerializeField] private Image _healthBarFill;
        
        [SerializeField] private Text _healthText;
        
        private Transform _target;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (maxHealth <= 0)
            {
                return;
            }
            
            var fillAmount = currentHealth / maxHealth;
            _healthBarFill.fillAmount = Mathf.Clamp01(fillAmount);
            _healthText.text = $"{currentHealth}/{maxHealth}";
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            
            // Position the health bar above the target
            transform.position = _target.position + Vector3.up * 2.0f;
                
            // Make the health bar face the camera
            // In a real game, we may want to inject the main camera
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }
    }
}
