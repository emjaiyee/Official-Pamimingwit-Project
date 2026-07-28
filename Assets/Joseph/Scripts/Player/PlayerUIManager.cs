using UnityEngine;
using TMPro;

public class PlayerUIManager : MonoBehaviour
{

    [Header("Currency")]
    public TextMeshProUGUI selyoText;

    void Start()
    {
        PlayerWallet.Instance.OnCoinsChanged.AddListener(UpdateCoins);

        UpdateCoins(PlayerWallet.Instance.coins);
    }

    void UpdateCoins(int coins)
    {
        selyoText.text = "Selyo: " + coins;
    }
}
