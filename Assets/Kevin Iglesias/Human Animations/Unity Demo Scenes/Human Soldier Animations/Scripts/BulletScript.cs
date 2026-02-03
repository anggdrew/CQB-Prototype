using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float life = 3f;
    public float dmg = 20f;
    public GameObject explosionPrefab;

    void Awake()
    {
        Destroy(gameObject, life);
    }

    void OnCollisionEnter(Collision collision)
    {
        /* if (collision.gameObject.CompareTag("Destructable")) //destructable terrain/objects
        {
            //Health target = collision.gameObject.transform.GetComponent<Health>();
            if (target != null)
            {
                target.TakeDmg(dmg);
                Destroy(gameObject);
                //GameObject explosion = Instantiate(explosionPrefab, collision.gameObject.transform.position, collision.gameObject.transform.rotation);
                //Destroy(explosion, 0.1f);
            }
        }*/

        if (collision.gameObject.CompareTag("Indestructable Env")) //environment
        {
            Destroy(gameObject);
            GameObject explosion = Instantiate(explosionPrefab, collision.gameObject.transform.position, collision.gameObject.transform.rotation);
            Destroy(explosion, 0.1f);       
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            Physics.IgnoreCollision(collision.collider, gameObject.GetComponent<Collider>());
    }

}

