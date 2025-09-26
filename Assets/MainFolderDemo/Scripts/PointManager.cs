using UnityEngine;
using System;  // for Action

//using UnityEngine.UI;

public class PointManager : MonoBehaviour
{
    public static PointManager Instance;

    public int points = 500;            //starting point for points
    public static float GlobalPointsMult = 1f;

    public event Action<int, int> OnPointsChanged;  // (delta, newTotal)   this is for the animation flashy stuff we add


    void Awake()  //Runs before Start(). Used to initialize things early. 
    {
        if (Instance == null)
            Instance = this;  //	Makes this the global PointManager.
        else
            Destroy(gameObject);
    }

    public void AddPoints(int ZombPoints)      // CHANGE THIS
    {
        //points += Mathf.RoundToInt(ZombPoints * GlobalPointsMult); //global multiplier is for the drop upgrades

        int delta = Mathf.RoundToInt(ZombPoints * GlobalPointsMult);
        points += delta;
        OnPointsChanged?.Invoke(delta, points);
    }
    public void SubtractPoints(int cost)
    {
        points -= cost;
        OnPointsChanged?.Invoke(-cost, points);

    }

    public int GetPoints()    //ui
    {
        return points;
    }

    public bool CanAfford(int cost)
    {
        return points >= cost;
    }

    public bool TrySpend(int cost)
    {
        if (points < cost)
        {
            return false;
        }
        OnPointsChanged?.Invoke(-cost, points);
        points -= cost;
        return true;
    }

}
