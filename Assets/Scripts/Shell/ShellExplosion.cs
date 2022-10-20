using UnityEngine;
using UnityEngine.Pool;
using System;
using System.Collections;

namespace Complete
{
    public class ShellExplosion : MonoBehaviour
    {
        public LayerMask m_TankMask;                        // Used to filter what the explosion affects, this should be set to "Players".
        public ParticleSystem m_ExplosionParticles;         // Reference to the particles that will play on explosion.
        public AudioSource m_ExplosionAudio;                // Reference to the audio that will play on explosion.
        public float m_MaxDamage = 100f;                    // The amount of damage done if the explosion is centred on a tank.
        public float m_ExplosionForce = 1000f;              // The amount of force added to a tank at the centre of the explosion.
        public float m_MaxLifeTime = 2f;                    // The time in seconds before the shell is removed.
        public float m_ExplosionRadius = 5f;                // The maximum distance away from the explosion tanks can be and are still affected.
        public Action<Rigidbody> tankShootingFunc;


        private Rigidbody shell;


        private void OnEnable()
        {
            StartCoroutine(Cor());
        }


        private void Start()
        {
            // If it isn't destroyed by then, destroy the shell after it's lifetime.
            //Destroy(gameObject, m_MaxLifeTime);

            shell = this.gameObject.GetComponent<Rigidbody>();
        }


        private IEnumerator Cor()
        {
            yield return new WaitForSeconds(m_MaxLifeTime);

            tankShootingFunc(shell);
        }


        private void OnTriggerEnter(Collider other)
        {
            m_ExplosionParticles.transform.position = transform.position;

            // Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
            Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

            // Go through all the colliders...
            for (int i = 0; i < colliders.Length; i++)
            {
                // ... and find their rigidbody.
                Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();
                TankHealth targetHealth = colliders[i].GetComponent<TankHealth>();

                // If they don't have a rigidbody, go on to the next collider.
                if (!targetRigidbody)
                    continue;

                // Add an explosion force.
                targetRigidbody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);
                targetHealth.TakeDamage(CalculateDamage(targetRigidbody.transform.position));
            }

            // Unparent the particles from the shell.
            m_ExplosionParticles.transform.parent = null;

            m_ExplosionParticles.gameObject.SetActive(true);

            // Play the particle system.
            m_ExplosionParticles.Play();

            // Play the explosion sound effect.
            m_ExplosionAudio.Play();

            // Once the particles have finished, destroy the gameobject they are on.
            ParticleSystem.MainModule mainModule = m_ExplosionParticles.main;
            //Destroy(m_ExplosionParticles.gameObject, mainModule.duration);
            Invoke(nameof(DisableParticle), mainModule.duration);

            // Destroy the shell.
            //Destroy(gameObject);

            tankShootingFunc(shell);
            StopCoroutine(Cor());
        }


        private void DisableParticle()
        {
            m_ExplosionParticles.gameObject.SetActive(false);
            m_ExplosionParticles.transform.parent = shell.transform;
        }


        private float CalculateDamage(Vector3 targetPosition)
        {
            // Create a vector from the shell to the target.
            Vector3 explosionToTarget = targetPosition - transform.position;

            // Calculate the distance from the shell to the target.
            float explosionDistance = explosionToTarget.magnitude;

            // Calculate the proportion of the maximum distance (the explosionRadius) the target is away.
            float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;

            // Calculate damage as this proportion of the maximum possible damage.
            float damage = relativeDistance * m_MaxDamage;

            // Make sure that the minimum damage is always 0.
            damage = Mathf.Max(0f, damage);

            return damage;
        }
    }
}