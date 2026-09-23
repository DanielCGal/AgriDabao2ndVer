using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AgriDabao3D
{
    public class HoldButton : MonoBehaviour, IPointerDownHandler
    {
        public Action onPressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            onPressed?.Invoke();
        }
    }
}
