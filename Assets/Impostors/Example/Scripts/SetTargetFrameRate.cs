using UnityEngine;

namespace Impostors.Example
{
    [AddComponentMenu("")]
    internal class SetTargetFrameRate : MonoBehaviour
    {
        [SerializeField] private int _targetFrameRate = 1000;

        [Range(0f,2f)]
        [SerializeField]
        private float _timeScale = 1f;
        
        private void Update()
        {
            Application.targetFrameRate = _targetFrameRate;
            Time.timeScale = _timeScale;
        }
    }
}