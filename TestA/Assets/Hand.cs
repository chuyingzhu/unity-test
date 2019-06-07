﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class Hand : MonoBehaviour {
    // For pickup/drop objects
    public SteamVR_Action_Boolean m_GrabAction = null;
    public SteamVR_Action_Boolean m_UseAction = null;

    // Added to L/R controller game objects
    private SteamVR_Behaviour_Pose m_Pose = null;
    private FixedJoint m_Joint = null;

    // Current object that the controller is holding
    private Interactable m_CurrentInteractable = null;
    // List of stuff that the controller is touching
    public List<Interactable> m_ContactInteractables = new List<Interactable>();

    public GameObject otherController = null;
    public FixedJoint m_otherJoint = null;
    public bool isGrabDown = false;

    private void Awake() {
        m_Pose = GetComponent<SteamVR_Behaviour_Pose>();
        m_Joint = GetComponent<FixedJoint>();
        m_otherJoint = otherController.GetComponent<FixedJoint>();
    }

    // Update is called once per frame
    private void Update() {
        // If grab button is pressed
        if (m_GrabAction.GetStateDown(m_Pose.inputSource)) {
            isGrabDown = true;

            if (m_CurrentInteractable != null) {
                Drop();
                return;
            }

            Pickup();
        }
        else {
            isGrabDown = false;
        }

        /*
        if (m_GrabAction.GetStateUp(m_Pose.inputSource)) {
            print(m_Pose.inputSource + " Grab Up");
            Drop();
        }*/
        if (m_UseAction.GetStateDown(m_Pose.inputSource)) {
            print(m_Pose.inputSource + " Use Down");
            if (m_CurrentInteractable != null) {
                m_CurrentInteractable.Action();
            }
        }
    }

    // Called when controller collides with an object
    private void OnTriggerEnter(Collider other) {
        // If object is neither type "Interactable" or "Heavy", simply ignore it
        if (!other.gameObject.CompareTag("Interactable") && !other.gameObject.CompareTag("Heavy")) {
            return;
        }
        m_ContactInteractables.Add(other.gameObject.GetComponent<Interactable>());
        // If this controller is not holding anything
        if (m_CurrentInteractable == null) {
            // One addition check if obj is heavy
            if (other.gameObject.CompareTag("Heavy")) {
                // If the other controller is hovering over the same target
                if (otherController.GetComponent<Hand>().m_ContactInteractables.IndexOf(other.gameObject.GetComponent<Interactable>()) >= 0) {
                    other.gameObject.GetComponent<ColorManager>().changeToGreen();
                }
            }
            else if (other.gameObject.CompareTag("Interactable")) {
                other.gameObject.GetComponent<ColorManager>().changeToGreen();
            }
        }
    }

    // Called when controller no longer collides with an object
    private void OnTriggerExit(Collider other) {
        // If object is neither type "Interactable" or "Heavy", simply ignore it
        if (!other.gameObject.CompareTag("Interactable") && !other.gameObject.CompareTag("Heavy")) {
            return;
        }
        m_ContactInteractables.Remove(other.gameObject.GetComponent<Interactable>());
        if (other.gameObject.CompareTag("Interactable")) {
            other.gameObject.GetComponent<ColorManager>().changeToRed();
        }
        else if (other.gameObject.CompareTag("Heavy")) {
            other.gameObject.GetComponent<ColorManager>().changeToBlack();
        }
    }

    public void Pickup() {
        // Get nearest interactable
        m_CurrentInteractable = GetNearestInteractable();
        // Null check
        if (!m_CurrentInteractable) {
            return;
        }
        // Heavy obj check
        if (m_CurrentInteractable.gameObject.CompareTag("Heavy")) {
            // If the other hand is not hovering over the same obj
            if (otherController.GetComponent<Hand>().m_ContactInteractables.IndexOf(m_CurrentInteractable) < 0) {
                return;
            }
        }
        // Already held, check
        if (m_CurrentInteractable.m_ActiveHand) {
            m_CurrentInteractable.m_ActiveHand.Drop();
            m_CurrentInteractable.GetComponent<ColorManager>().changeToBlue();
        }   
        // Position
        // m_CurrentInteractable.transform.position = transform.position;
        m_CurrentInteractable.ApplyOffset(transform);
        // Attach
        Rigidbody targetBody = m_CurrentInteractable.GetComponent<Rigidbody>();
        m_Joint.connectedBody = targetBody;
        if (m_CurrentInteractable.gameObject.CompareTag("Heavy")) {
            m_otherJoint.connectedBody = targetBody;
        }
        // Set active hand
        m_CurrentInteractable.m_ActiveHand = this;
        // Change color
        m_CurrentInteractable.GetComponent<ColorManager>().changeToBlue();
    }

    public void Drop() {
        // Null check
        if (!m_CurrentInteractable) {
            return;
        }
        // Heavy obj check
        if (m_CurrentInteractable.gameObject.CompareTag("Heavy")) {
            // If the other hand is not holding grip
            if (!otherController.GetComponent<Hand>().isGrabDown) {
                return;
            }
        }
        // Apply velocity
        Rigidbody targetBody = m_CurrentInteractable.GetComponent<Rigidbody>();
        targetBody.velocity = m_Pose.GetVelocity();
        targetBody.angularVelocity = m_Pose.GetAngularVelocity();
        // Detach
        m_Joint.connectedBody = null;
        if (m_CurrentInteractable.gameObject.CompareTag("Heavy")) {
            m_otherJoint.connectedBody = null;
        }
        // Change color
        m_CurrentInteractable.GetComponent<ColorManager>().changeToRed();
        // Clear
        m_CurrentInteractable.m_ActiveHand = null;
        m_CurrentInteractable = null;
    }

    private Interactable GetNearestInteractable() {
        Interactable nearest = null;
        float minDistance = float.MaxValue;
        float distance = 0.0f;

        foreach(Interactable interactable in m_ContactInteractables) {
            distance = (interactable.transform.position - transform.position).sqrMagnitude;
            if (distance < minDistance) {
                minDistance = distance;
                nearest = interactable;
            }
        }

        return nearest;
    }
}