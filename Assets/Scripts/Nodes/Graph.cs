using UnityEngine;
using System.Collections.Generic;

public class Graph : MonoBehaviour
{
	public static Graph instance;
	public SteamVR_TrackedObject leftController;
	public SteamVR_TrackedObject rightController;
	public GameObject center;
	public float repulsion;
	public float attraction;
	public float springLength;
	public float damping;
    float initialRepulsion;

	public List<Node> nodes = new List<Node> ();
	public List<Edge> edges = new List<Edge> ();

    public float initialDistance;
    public Vector3 initialCenter;
    Vector3 initialGraphCenter;
    Vector3 initialPoition;

    Quaternion initialGraphRotation;
    float initialControllerAxis;

    GameObject checkRotationGo;
    Quaternion initalCheckRotation;

    public void Awake ()
	{
		instance = this;


    }

	public void Update ()
	{


      

       var deviceLeft = SteamVR_Controller.Input((int)leftController.index);
       var deviceRight = SteamVR_Controller.Input((int)rightController.index);



       if (deviceLeft.GetTouchDown(SteamVR_Controller.ButtonMask.Trigger) || deviceRight.GetTouchDown(SteamVR_Controller.ButtonMask.Trigger))
       {
            initialDistance = Vector3.Distance(leftController.transform.position, rightController.transform.position);
            initialCenter = Vector3.Lerp(leftController.transform.position, rightController.transform.position, 0.5f);
            initialGraphCenter = center.transform.position;
            initialRepulsion = repulsion;
            initialPoition = transform.position;

            initialGraphRotation = transform.rotation;
            //initialControllerAxis  = Vector3.Angle(rightController.transform.position, leftController.transform.position);

            checkRotationGo = new GameObject();

            checkRotationGo.transform.position = rightController.transform.position;
            checkRotationGo.transform.LookAt(leftController.transform.position);

            initalCheckRotation = checkRotationGo.transform.rotation;
        }

       if (deviceLeft.GetTouch(SteamVR_Controller.ButtonMask.Trigger) && deviceRight.GetTouch(SteamVR_Controller.ButtonMask.Trigger))
        {
        Zoom();
        DragCenter();
       // RotateGraph();
        }
    

    }

    public void Zoom()
    {
        float currentDistance = Vector3.Distance(leftController.transform.position, rightController.transform.position);
        repulsion = initialRepulsion * (currentDistance / initialDistance);   
    }

    public void DragCenter()
    {
        Vector3 currentCenter = Vector3.Lerp(leftController.transform.position, rightController.transform.position, 0.5f);
        center.transform.position = initialGraphCenter + (currentCenter - initialCenter);
        transform.position = initialPoition + (currentCenter - initialCenter);
    }

    public void RotateGraph()
    {
        //float currentControllerAxis = Vector3.Angle(rightController.transform.position, leftController.transform.position);
        //transform.RotateAround(center.transform.position, Vector3.up, currentControllerAxis);

        checkRotationGo.transform.LookAt(leftController.transform.position);

        transform.rotation = checkRotationGo.transform.rotation * initalCheckRotation ;
        

    }



}