using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public Image HP_bar;
    public Image HP_back;
    public Text HP_text;
    public Image EXP_bar;
    public Text EXP_text;
    public Player player;

    public Canvas inGameCanvas;

    void Update()
    {
        float width = player.HP * 5;
        HP_bar.rectTransform.sizeDelta = new Vector2(width, 50);
        HP_text.text = player.HP.ToString() + " / " + player.MaxHP.ToString();
        HP_back.rectTransform.sizeDelta = new Vector2(player.MaxHP * 5, 50);

        float width_exp = player.Exp / player.MaxExp;
        EXP_bar.rectTransform.sizeDelta = new Vector2(width_exp, 50);
        EXP_text.text = "Level: " + player.Level.ToString() + "( " + player.Exp + " / " + player.MaxExp + " )";
    }
}
