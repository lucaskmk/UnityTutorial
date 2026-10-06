using UnityEngine;
using UnityEngine.EventSystems;

// Garante que sempre tem um botão selecionado, para dar para navegar no menu com o controle
// mesmo depois de clicar fora com o mouse.
public class SelectionKeeper : MonoBehaviour
{
    static GameObject defaultSelection;

    public static void SetDefault(GameObject go)
    {
        defaultSelection = go;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
    }

    void Update()
    {
        var es = EventSystem.current;
        if (es == null) return;
        var selected = es.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy)
        {
            // No pause, o botão padrão é o do painel de pausa
            if (defaultSelection != null && defaultSelection.activeInHierarchy)
                es.SetSelectedGameObject(defaultSelection);
        }
    }
}
