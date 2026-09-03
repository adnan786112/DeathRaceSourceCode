using UnityEngine;

public enum CarType
{
    Car1,
    Car2,
    Car3,
    Car4,
  
}

public class CarSelectionManager : MonoBehaviour
{
    public static CarSelectionManager instance;
    public CarType SelectedCar = CarType.Car1; // default

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}