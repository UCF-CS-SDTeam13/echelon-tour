﻿using System.Collections;
using UnityEngine;

public class BezierTracker : MonoBehaviour
{
    // Variables to adjust the target position, rotation, and look ahead
    [SerializeField] private float targetOffset = 2;
    [SerializeField] private float targetFactor = 0.5f;
    [SerializeField] private float speedOffset = 3;
    [SerializeField] private float speedFactor = 0.5f;
    [SerializeField] private float lookAheadOffset = 15;

    [SerializeField] private Transform target;

    private float progressDistance;
    private BezierCircuit.TrackPoint progressPoint;
    private Vector3 lastPosition;

    public BezierCircuit circuit;
    public Vector3 targetLookAhead;
    public float speed = 0;

    public bool isMultiplayer = false;
    public int peerId = 0;
    public Vector3 serverPlayerPosition = Vector3.zero;
    public float serverProgressDistance = 0;
    public bool startTrack = false;

    public BezierFollowV2 followScript;

    public void StartTracking()
    {
        followScript = GetComponent<BezierFollowV2>();
        if (isMultiplayer == true)
        {
            peerId = GetComponent<PlayerStats>().peerId;
            RealTimeClient.Instance.StatsUpdate += GetPositions;
            if (peerId == RealTimeClient.Instance.peerId)
            {
                StartCoroutine("UpdateServerStats");
            }

        }

        if (circuit == null)
        {
            Debug.Log("Failed to find circuit gameobject, possible tag missing.");
        }

        startTrack = true;

        // Makes sure everything is inital
        Reset();
    }

    // Resets the progressDistnace to 0
    public void Reset()
    {
        progressDistance = 0;
    }

    private void FixedUpdate()
    {
        if (isMultiplayer == true && startTrack == false)
        {
            return;
        }

        // Get a speed based off how much is traveled between frames
        if (Time.fixedDeltaTime > 0)
        {
            speed = Mathf.Lerp(speed, (lastPosition - transform.position).magnitude / Time.fixedDeltaTime,
                               Time.fixedDeltaTime);
        }

        // Change the target position
        target.position =
            circuit.GetTrackPoint(progressDistance + targetOffset + targetFactor * speed).position;

        // Change the target rotation
        target.rotation =
            Quaternion.LookRotation(
                circuit.GetTrackPoint(progressDistance + speedOffset + speedFactor * speed).direction);

        // Save the look ahead for animation purposes
        targetLookAhead =
            circuit.GetTrackPoint(progressDistance + targetOffset + lookAheadOffset + targetFactor * speed).position;

        // Get the current point on the track and the amount changed
        progressPoint = circuit.GetTrackPoint(progressDistance);
        Vector3 progressDelta = progressPoint.position - transform.position;

        // Compare progressDelta with server progressDelta
        if (isMultiplayer == true && peerId != RealTimeClient.Instance.peerId && serverProgressDistance != 0 && serverPlayerPosition != Vector3.zero)
        {
            BezierCircuit.TrackPoint serverProgressPoint = circuit.GetTrackPoint(serverProgressDistance);
            float distanceDelta = Mathf.Abs(serverProgressDistance - progressDistance);

            // Debug.Log(serverProgressDistance + " " + progressDistance);
            if (distanceDelta > 30) //Need a new value
            {
                transform.position = serverPlayerPosition;
                progressDistance = serverProgressDistance;
                progressPoint = serverProgressPoint;
                progressDelta = serverProgressPoint.position - serverPlayerPosition;
                followScript.speedOffset = 0;
            }
            else if (distanceDelta > 5)
            {
                followScript.speedOffset = 10;
            }
            else
            {
                followScript.speedOffset = 0;
            }
        }

        // Checks if less than 0 and increment the progressDistance
        if (Vector3.Dot(progressDelta, progressPoint.direction) < 0)
        {
            progressDistance += progressDelta.magnitude * 0.5f;
        }

        // Mod the float as it will keep increase if not
        progressDistance = Mathf.Repeat(progressDistance, circuit.lengths[circuit.bezierCurves.Length - 1]);

        // Save the last position
        lastPosition = transform.position;
    }

    private void GetPositions(object sender, StatsUpdateEventArgs e)
    {
        if (e.peerId != RealTimeClient.Instance.peerId)
        {
            Vector3 newPlayerPos = new Vector3();
            float newProgressDis = e.progressDistance;

            for (int i = 0; i < 3; i++)
            {
                newPlayerPos[i] = e.playerPosition[i];
            }

            serverPlayerPosition = newPlayerPos;
            serverProgressDistance = newProgressDis;
        }
    }

    IEnumerator UpdateServerStats()
    {
        while (true)
        {
            float[] playerPos = { transform.position.x, transform.position.y, transform.position.z };
            float progressDis = progressDistance;

            RealTimeClient.Instance.UpdateStats(Bike.Instance.Count, Bike.Instance.RPM, playerPos, progressDis);
            yield return new WaitForSeconds(1.0f);
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // Draw a line to the target position
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);

            // Draw a line to the target lookAhead
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(target.position, targetLookAhead);
        }
    }
}
