using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Grapple : MonoBehaviour
{
    private Camera _camera;

    [Header("Grapple Settings")]
    [Tooltip("Velocity while using the grapple.")]
    [SerializeField] private float _grappleSpeed;
    [Tooltip("Strength the hook has to push you to the gripping point.")]
    [SerializeField] private float _grappleStrength;
    [Tooltip("Strength you have to move freely through the air.")]
    [SerializeField] private float _grappleAgility;
    [Tooltip("Grappling distance. Range[1-200].")]
    [Range(1f, 200f)]
    [SerializeField] private float _grappleDistance;
    [SerializeField] private LayerMask _collMask;

    private Vector3 _grapplePos;
    private bool _grappling;
    private Vector2 _hitPointSP;
    [SerializeField] private List<Vector3> _ropePositions { get; set; } = new List<Vector3>();

    private Rigidbody _rg;
    private LineRenderer _ropeRenderer;

    [Header("UI Settings")]
    [SerializeField] private Image _indicator;
    [Tooltip("Speed at which the indicator moves.")]
    [SerializeField] private float _indicatorSpeed;

    void Start()
    {
        _rg = GetComponent<Rigidbody>();
        _camera = Camera.main;
        _ropeRenderer = GetComponent<LineRenderer>();
        _ropeRenderer.enabled = false;
    } 

    void Update()
    {
        //Controles
        GrappleControls();

        //Fisicas
        GrappleMovement();

        //Visuales
        UpdateIndicator();
        RenderRope();
    }

    private void GrappleControls()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position, _camera.transform.forward, out hit, _grappleDistance);

        if (Input.GetMouseButtonDown(0))
        {
            if (hit.transform != null) StartGrapple(hit.point);
            else if (GrappableWall()) StartGrapple(_grapplePos);
        }

        if (Input.GetMouseButtonUp(0))
        {
            StopGrapple();
        }
    }

    private void GrappleMovement()
    {
        if (_grappling)
        {
            Vector3 moveVec = (_grapplePos - transform.position).normalized * _grappleStrength;
            moveVec += _camera.transform.forward.normalized * _grappleAgility;
            _rg.AddForce(moveVec * _grappleSpeed  * Time.deltaTime, ForceMode.VelocityChange);

            if (GetRopeLenght() > _grappleDistance) StopGrapple();
        }
    }

    private bool GrappableWall()
    {
        List<GameObject> walls = CheckWalls.instance.OutputVisibleRenderers();
        GameObject nearestWall = null;
        float distance = _grappleDistance;

        foreach (GameObject wall in walls)
        {
            Collider collider = wall.GetComponent<Collider>();
            if (collider != null)
            {
                Vector3 closestPoint = collider.ClosestPoint(new Vector3(transform.position.x, transform.position.y, transform.position.z) + _camera.transform.forward * 3);
                float _currentWallDistance = Vector3.Distance(closestPoint, transform.position);
                if (_currentWallDistance <= distance)
                {
                    nearestWall = wall;
                    distance = _currentWallDistance;
                    _grapplePos = closestPoint;
                }
            }
        }

        if (nearestWall == null) return false;
        return true;
    }

    private void UpdateIndicator()
    {
        _indicator.color = Color.red;
        _hitPointSP = new Vector2(Screen.width / 2, Screen.height / 2);

        if (_grappling)
        {
            _indicator.color = Color.yellow;
            _hitPointSP = _camera.WorldToScreenPoint(_grapplePos);
        }
        else
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, _camera.transform.forward, out hit, _grappleDistance))
            {
                _hitPointSP = _camera.WorldToScreenPoint(hit.point);
                _indicator.color = Color.green;
            }
            else if (GrappableWall())
            {
                _hitPointSP = _camera.WorldToScreenPoint(_grapplePos);
                _indicator.color = Color.green;
            }
        }

        _indicator.rectTransform.position = Vector2.Lerp(_indicator.rectTransform.position, _hitPointSP, Time.deltaTime * _indicatorSpeed);
    }

    private void StartGrapple(Vector3 point)
    {
        _grapplePos = point;
        _grappling = true;
        _rg.useGravity = false;
        _ropeRenderer.enabled = true;
        _ropePositions = new List<Vector3>();
        AddPosToRope(_grapplePos);
    }

    private void StopGrapple()
    {
        _grapplePos = Vector3.zero;
        _grappling = false;
        _rg.useGravity = true;
        _ropePositions = new List<Vector3>();
        _ropeRenderer.enabled = false;
    }

    private void RenderRope()
    {
        if (_grappling)
        {
            Update_ropePositions();
            LastSegmentGoTotransformPos();

            DetectCollisionEnter();
            if (_ropePositions.Count > 2) DetectCollisionExits();
        }
    }

    private void AddPosToRope(Vector3 _pos)
    {
        if (_ropePositions.Count() == 0)
        {
            _ropePositions.Add(_pos);
            _ropePositions.Add(transform.position);
        }
        else if (_ropePositions[_ropePositions.Count() - 2] != _pos)
        {
            _ropePositions.RemoveAt(_ropePositions.Count - 1);
            _ropePositions.Add(_pos);
            _ropePositions.Add(transform.position);
        }
    }

    private void Update_ropePositions()
    {
        _ropeRenderer.positionCount = _ropePositions.Count;
        _ropeRenderer.SetPositions(_ropePositions.ToArray());
    }

    private void LastSegmentGoTotransformPos() => _ropeRenderer.SetPosition(_ropeRenderer.positionCount - 1, transform.position);

    private void DetectCollisionEnter()
    {
        RaycastHit hit;
        if (Physics.Linecast(transform.position, _ropePositions[_ropePositions.Count() - 2], out hit, _collMask))
        {
            AddPosToRope(hit.point);
            _grapplePos = hit.point;
        }
    }

    private void DetectCollisionExits()
    {
        RaycastHit hit;
        if (!Physics.Linecast(transform.position, _ropePositions[_ropePositions.Count() - 3], out hit, _collMask))
        {
            _ropePositions.RemoveAt(_ropePositions.Count() - 2);
            _grapplePos = _ropePositions[_ropePositions.Count() - 2];
        }
    }

    private float GetRopeLenght()
    {
        float distance = 0;

        for (int i = 1; i < _ropePositions.Count(); i++)
        {
            distance += Vector3.Distance(_ropePositions[i], _ropePositions[i-1]);
        }

        return distance;
    }
}
