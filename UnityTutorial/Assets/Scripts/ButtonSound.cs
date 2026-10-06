using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSound : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    public void OnPointerClick(PointerEventData eventData) => UIAudio.Click();
    public void OnSubmit(BaseEventData eventData) => UIAudio.Click();
}
