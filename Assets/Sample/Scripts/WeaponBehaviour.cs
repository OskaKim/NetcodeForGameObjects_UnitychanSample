using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBehaviour : MonoBehaviour {
    [SerializeField] private GameObject BulletPrefab;

    private List<GameObject> bullets = new List<GameObject>();

    public void Fire() {
        bullets.Add(GameObject.Instantiate(BulletPrefab, transform));
    }

    private void Update() {
        var deltaPos = Time.deltaTime * 10.0f;

        bullets.ForEach(bullet => {
            var pos = bullet.transform.position;
            bullet.transform.position = new Vector3(
                pos.x += deltaPos,
                pos.y,
                pos.z
                );
        });
    }
}
