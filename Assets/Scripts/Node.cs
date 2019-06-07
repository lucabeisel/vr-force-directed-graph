using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Node : GraphObject
{
    public int id;
    public Text namefield;

    public bool infected = false;
    public bool endangered = false;

    public float timeOfInfection;

    public bool changingColor;

    public bool hideconnections;
    public bool dontRepel;

    public Vector3 savedPosition;

    public Vector3 forceVelocity;
    public Vector3 masterVelocity = Vector3.zero;
    public Vector3 throwVelocity;

    public List<Node> repulsionlist = new List<Node>();
    public List<Edge> attractionlist = new List<Edge>();
    public List<Node> connectedNodes = new List<Node>();

    public Material materialStandard;
    public Material materialHighlighted;
    public Material materialInfected;
    public Material materialEndangered;
    public Material materialImmune;
    public Material materialEvil;
    public Material materialEvilMaster;

    #region showandhide

    public override void Hide()
    {
        base.Hide();
        GetComponent<Renderer>().enabled = false;
        GetComponent<SphereCollider>().enabled = false;

        CanvasGroup cv = GetComponent<CanvasGroup>();
        cv.alpha = 0;
        cv.blocksRaycasts = false;
        cv.interactable = false;
    }

    public override void Show()
    {
        base.Show();
        GetComponent<Renderer>().enabled = true;
        GetComponent<SphereCollider>().enabled = true;

        CanvasGroup cv = GetComponent<CanvasGroup>();
        cv.alpha = 1;
        cv.blocksRaycasts = true;
        cv.interactable = true;
        namefield.transform.localPosition = Vector3.zero;
    }

    public GameObject GrabbedBy()
    {
        return null;
        // if (graph.leftController.GetComponent<ViveGrab>().grabbedObj == gameObject) { // GRABBED BY LEFT
        // 	grabbedBy = graph.leftController.gameObject;
        // } else if (graph.rightController.GetComponent<ViveGrab>().grabbedObj == gameObject) { // GRABBED BY RIGHT
        // 	grabbedBy = graph.rightController.gameObject;
        // } else // NOT GRABBED
        // 	grabbedBy = null;
    }

    public void HideConnections()
    {
        hideconnections = true;
    }

    public void ShowConnections()
    {
        hideconnections = false;
    }

    public void Remove()
    {
        dontRepel = true;
        HideConnections();
        Invoke("Destroy", 1.5f);
    }

    protected virtual void Destroy() // IS INVOKED
    {
        Destroy(gameObject);
    }

    #endregion


    public void InfectOthers()
    {
        if (Time.time - timeOfInfection > graph.infectionManager.incubation)
        {
            foreach (Edge e in attractionlist)
            {
                e.endangered = true;
                e.Other(this).endangered = true;
                e.Other(this).CheckForInfection();
            }
        }
    }

    public void CheckForInfection()
    {
        if (!infected && Random.value > graph.infectionManager.infectionChance)
        {
            BecomeInfected();
        }
    }

    public void BecomeInfected()
    {
        Debug.Log("Infected " + name + " at " + Time.time);
        graph.infectionManager.infectedNodes.Add(this);
        infected = true;
        timeOfInfection = Time.time;
    }


    public void Heal()
    {
        Debug.Log("Healed " + name);
        infected = false;
        SetMaterial(materialImmune);
    }


    public void CheckForBreak()
    {
        if (GrabbedBy() != null)
        {
            Node otherNode = null;
            if (graph.grabLeft == this && graph.grabRight != null)
                otherNode = graph.grabRight;
            else if (graph.grabRight == this && graph.grabLeft != null)
                otherNode = graph.grabLeft;

            if (connectedNodes.Contains(otherNode))
            {
                float currentDistance = Vector3.Distance(gameObject.transform.position, otherNode.gameObject.transform.position);

                Edge findEdge = attractionlist.Find(x => x.Other(this) == otherNode);

                float strain = (float)(currentDistance / (graph.initialGrabDistance * graph.ripFactor));
                findEdge.Strain(strain);

                //Debug.Log("Check for break " + name + otherNode.name + strain.ToString());

                if (currentDistance > graph.initialGrabDistance * graph.ripFactor)
                {
                    Debug.Log("BREAK!");
                    BreakConnection(otherNode, findEdge);

                    if (attractionlist.Count == 0)
                        Separate();

                }
            }
        }
    }

    public void Separate()
    {
        graph.nodes.Remove(this);
        graph.infectionManager.infectedNodes.Remove(this);

        graph.infectionManager.AddToEvilNodes(this);
        
        //gameObject.AddComponent<Rigidbody>();
    }


    public void BreakConnection(Node otherNode, Edge connectingEdge)
    {
        otherNode.attractionlist.Remove(connectingEdge);
        attractionlist.Remove(connectingEdge);

        otherNode.connectedNodes.Remove(this);
        connectedNodes.Remove(otherNode);

        if (otherNode.endangered)
            otherNode.endangered = false;

        //Debug.Log("Destroyed " + connectingEdge.name);
        Destroy(connectingEdge.gameObject);
    }





    protected IEnumerator ChangeNodeColor(Material startMat, Material endMat)
    {
        changingColor = true;
        Debug.Log("Lerping between " + startMat.name + " and " + endMat.name);
        for (float i = 0; i < 1; i = i + 0.01f)
        {
            GetComponent<Renderer>().material.Lerp(startMat, endMat, i);
            GetComponent<Light>().color = Color.Lerp(startMat.GetColor("_RimColor"), endMat.GetColor("_RimColor"), i);

            foreach (Edge e in attractionlist)
            {
                e.GetComponent<Renderer>().material.Lerp(startMat, endMat, i);
            }

            yield return new WaitForEndOfFrame();
        }
        changingColor = false;
    }


    public void RefreshRepulsionList()
    {
        repulsionlist.Clear();
        GameObject[] allnodes = GameObject.FindGameObjectsWithTag("Node");
        foreach (GameObject go in allnodes)
        {
            if (go != gameObject)
                repulsionlist.Add(go.GetComponent<Node>());
        }
    }

    public void Start()
    {
        namefield.text = name;
        Reset();
        foreach (Edge e in attractionlist)
        {
            connectedNodes.Add(e.Other(this));
        }
    }

    public void Reset()
    {
        if (graph)
            transform.localPosition = graph.center.transform.position + new Vector3(Random.value - 0.5f, Random.value - 0.5f, Random.value - 0.5f);
    }

    public void Accelerate(Vector3 force)
    {
        throwVelocity = force;
    }

    public override void Update()
    {
        base.Update();
        UpdateInfection();
    }

    public void UpdateInfection()
    {
        if (infected)
        {
            InfectOthers();
            CheckForBreak();
        }
    }

    public override void UpdatePosition()
    {
        base.UpdatePosition();

        if (GrabbedBy() != null)
        {
            //Debug.Log (name + " grabbed by " + grabbedBy);
            transform.position = GrabbedBy().transform.position;
        }
        else
        {
            CalculateForces();

            if (graph.infectionManager.evilMaster == this)
            {
                CalculateEvilDrag();
            }

            if (!graph.gameOver)
            {
                ApplyForces();
            }
        }
    }

    #region physics
    public void CalculateEvilDrag()
    {
        masterVelocity = Vector3.zero;

        if (graph.infectionManager.infectedNodes.Count > 0)
        {
            Vector3 attractPosition = graph.infectionManager.infectedNodes.First().transform.position;
            masterVelocity += (attractPosition - transform.position).normalized * graph.masterSpeed;
            // Debug.Log("ATTRACT " + name + " to " + graph.infectedNodes.First().name + " with " + masterVelocity.ToString());
        }
    }


    protected void CalculateForces()
    {
        forceVelocity = Vector3.zero;

        // REPULSION
        foreach (Node rn in repulsionlist)
            forceVelocity += CalcRepulsion(rn);

        //ATTRACTION
        foreach (Edge e in attractionlist)
            forceVelocity += CalcAttraction(e.Other(this), e.weight);

        //ATTRACTION TO CENTER
        forceVelocity += CalcAttractionToCenter();
    }


    public void ApplyForces()
    {
        if (!float.IsNaN(forceVelocity.x) && !float.IsNaN(forceVelocity.y) && !float.IsNaN(forceVelocity.z))
        {

            transform.localPosition += forceVelocity * graph.damping * Time.deltaTime;

            transform.localPosition += throwVelocity * Time.deltaTime;
            transform.localPosition += masterVelocity * Time.deltaTime;

            savedPosition = transform.localPosition;

            throwVelocity = new Vector3(throwVelocity.x * 0.8f, throwVelocity.y * 0.8f, throwVelocity.z * 0.8f);

        }
        else
            Debug.LogError(name + " " + forceVelocity.ToString());
    }
    #endregion physics

    public override void UpdateAppearance()
    {
        base.UpdateAppearance();
        if (graph.infectionManager.evilMaster == this) // evilMaster
        {
            SetMaterial(materialEvilMaster);
            SetLight(Color.red);
        }
        else if (graph.infectionManager.evilNodes.Contains(this) && !graph.infectionManager.evilMaster == this) // evil
        {
            SetMaterial(materialEvil);
            SetLight(Color.black);
        }
        else if (infected) // infected
        {
            SetMaterial(materialInfected);
            SetLight(Color.red);
        }
        else if (endangered) // endangered
        {
            SetMaterial(materialEndangered);//Material.Lerp(materialStandard, materialEndangered, Mathf.FloorToInt(Time.time) % 2));
            SetLight(Color.Lerp(Color.yellow, Color.red, Mathf.FloorToInt(Time.time) % 2));
        }
        else if (GrabbedBy() != null) // highLighted
        {
            SetMaterial(materialHighlighted);
            SetLight(Color.white);
        }
        else // standard
        {
            SetMaterial(materialStandard);
            SetLight(Color.cyan);
        }
    }

    public void SetMaterial(Material material)
    {
        GetComponent<Renderer>().material = material;
    }

    public void SetLight(Color color)
    {
        GetComponent<Light>().color = color;
    }


    #region physics

    protected Vector3 CalcAttractionToCenter()
    {
        Vector3 a = transform.position;
        Vector3 b = graph.center.transform.position;
        return (b - a).normalized * graph.attraction * (Vector3.Distance(a, b) / graph.springLength);
    }

    protected Vector3 CalcAttraction(Node otherNode, float weight)
    {
        if (otherNode)
        {

            Vector3 a = transform.localPosition;
            Vector3 b = otherNode.transform.localPosition;

            float springLength = graph.springLength;
            float springAttraction = graph.attraction;

            if (otherNode.infected)
            {
                springLength = graph.springLength / 3f;
                springAttraction = graph.attraction * 2f;
            }

            return (b - a).normalized * (graph.attraction + weight) * (Vector3.Distance(a, b) / springLength);

        }
        else
            return Vector3.zero;
    }

    protected Vector3 CalcRepulsion(Node otherNode)
    {
        if (!dontRepel)
        {
            // Coulomb's Law: F = k(Qq/r^2)
            float distance = Vector3.Distance(transform.localPosition, otherNode.transform.localPosition);
            Vector3 returnvector = ((transform.localPosition - otherNode.transform.localPosition).normalized * graph.repulsion) / (distance * distance);

            if (!float.IsNaN(returnvector.x) && !float.IsNaN(returnvector.y) && !float.IsNaN(returnvector.z))
                return returnvector;
            else
                return Vector3.zero;
        }
        else
            return Vector3.zero;
    }
    #endregion physics
}
