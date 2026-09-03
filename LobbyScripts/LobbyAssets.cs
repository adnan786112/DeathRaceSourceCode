using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities;
using UnityEngine;
using UnityEngine.UI;

public class LobbyAssets : MonoBehaviour {



    public static LobbyAssets Instance { get; private set; }


    [SerializeField] private Sprite hitmanSprite;
    [SerializeField] private Sprite ninjaSprite;
    [SerializeField] private Sprite dodgeSprite;
    [SerializeField] private Sprite assasinSprite;
    [SerializeField] private Sprite shooterSprite;
    [SerializeField] private Sprite jamesBondSprite;
    [SerializeField] private Sprite skullfaceSprite;
    [SerializeField] private Sprite frankensteinSprite;

    public static List<Sprite> markerSprites  = new();

    private void Awake()
    {
        Instance = this;
       
        for (int i = 0; i < 7; i++)
        {
            markerSprites.AddRange(GetSprites());
        }
    }

    private IEnumerable<Sprite> GetSprites()
    {
        yield return hitmanSprite;
        yield return ninjaSprite;
        yield return dodgeSprite;
        yield return assasinSprite; 
        yield return shooterSprite;
        yield return jamesBondSprite;
        yield return skullfaceSprite;
        yield return frankensteinSprite;
    }

    public Sprite GetSprite(LobbyManager.PlayerCharacter playerCharacter) {
        switch (playerCharacter) {
            default:
            case LobbyManager.PlayerCharacter.Assasin: return assasinSprite;
            case LobbyManager.PlayerCharacter.Ninja: return ninjaSprite;
            case LobbyManager.PlayerCharacter.Hitman:   return hitmanSprite;    
            case LobbyManager.PlayerCharacter.Dodge:   return dodgeSprite;
            case LobbyManager.PlayerCharacter.Shooter: return shooterSprite;
            case LobbyManager.PlayerCharacter.JamesBond: return jamesBondSprite;
            case LobbyManager.PlayerCharacter.Skullface: return skullfaceSprite;
            case LobbyManager.PlayerCharacter.Frankenstein: return frankensteinSprite;
        }
    }

}