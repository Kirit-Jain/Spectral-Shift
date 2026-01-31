using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements;
using System.Drawing;
using System.Collections;

public class WeaponController : NetworkBehaviour
{
    [Header("Gun Settings")]
    [SerializeField] float damage = 10f;
    [SerializeField] float range = 100f;
    [SerializeField] float fireRate = 0.2f;

    [Header("References")]
    [SerializeField] Transform firePoint;
    [SerializeField] Camera playerCamera;
    [SerializeField] LineRenderer bulletTrail;

    float nextFireTime = 0f;

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    } 

    void Shoot()
    {
        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range))
        {
            targetPoint = hit.point;
            Debug.Log("Hit: " + hit.collider.name);
        }
        else
        {
            targetPoint = playerCamera.transform.position + playerCamera.transform.forward * range;
        }

        StartCoroutine(ShowBulletTrail(firePoint.position, targetPoint));

        ShootServerRpc(firePoint.position, targetPoint);
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 start, Vector3 end)
    {
        ShootClientRpc(start, end);
    }

    [ClientRpc]
    void ShootClientRpc(Vector3 start, Vector3 end)
    {
        if (!IsOwner)
        {
            StartCoroutine(ShowBulletTrail(start, end));
        }
    }

    IEnumerator ShowBulletTrail(Vector3 start, Vector3 end)
    {
        bulletTrail.enabled = true;
        bulletTrail.SetPosition(0, start);
        bulletTrail.SetPosition(1, end);

        yield return new WaitForSeconds(0.05f);

        bulletTrail.enabled = false;
    }
}