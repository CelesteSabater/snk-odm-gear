using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CheckWalls : MonoBehaviour
{
    public static CheckWalls instance { get; private set; }
    private Renderer[] renderers;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        renderers = new Renderer[walls.Count()];
        for(int i = 0; i < walls.Count(); i++)
        {
            renderers[i] = walls[i].GetComponentInChildren<Renderer>();
        }     
    }

    public List<GameObject> OutputVisibleRenderers()
    {
        List<GameObject> lista = new List<GameObject>();

        foreach (Renderer renderer in renderers)
        {
            if (IsVisible(renderer))
            {
                lista.Add(renderer.GameObject());
            }
        }

        return lista;
    }

    private bool IsVisible(Renderer renderer)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
            return true;
        else
            return false;
    }
}
