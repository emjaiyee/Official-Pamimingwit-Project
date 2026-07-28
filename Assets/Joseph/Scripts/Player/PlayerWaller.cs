using UnityEngine;
using UnityEngine.Events;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance;

    public int coins;

    public UnityEvent<int> OnCoinsChanged;

    void Awake()
    {
        Instance = this;
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        OnCoinsChanged?.Invoke(coins);
    }

    public bool SpendCoins(int amount)
    {
        if (coins < amount) return false;

        coins -= amount;
        OnCoinsChanged?.Invoke(coins);
        return true;
    }
}