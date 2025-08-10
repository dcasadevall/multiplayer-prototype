using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Adapters.UI
{
    public class NetworkErrorModal : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _errorText;

        [SerializeField] 
        private Button _okButton;

        private void Awake()
        {
            if (_errorText == null)
            {
                Debug.LogError("Error Text not assigned in the NetworkErrorModal script.");
                return;
            }
            
            if (_okButton == null)
            {
                Debug.LogError("OK Button not assigned in the NetworkErrorModal script.");
            }
        }

        public void Show(string text)
        {
            _errorText.text = text;
            _okButton.onClick.AddListener(Hide);
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            _okButton.onClick.RemoveListener(Hide);
            gameObject.SetActive(false);
        }
    }
}