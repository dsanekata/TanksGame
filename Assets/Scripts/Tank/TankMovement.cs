using UnityEngine;
using System.Collections.Generic;

namespace Complete
{
    public class TankMovement : MonoBehaviour
    {
        public int m_PlayerNumber = 1;              // Used to identify which tank belongs to which player.  This is set by this tank's manager.
        public float m_Speed = 12f;                 // How fast the tank moves forward and back.
        public float m_TurnSpeed = 180f;            // How fast the tank turns in degrees per second.
        public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
        public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
        public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
        public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.
        public GameObject m_Turret;
        [HideInInspector]
        public List<GameObject> enemyList;
        public Transform spawnPoint;

        private string m_MovementAxisName;          // The name of the input axis for moving forward and back.
        private string m_TurnAxisName;              // The name of the input axis for turning.
        private string m_TurretTurnAxisName;
        private string m_AimButtonName;
        private Rigidbody m_Rigidbody;              // Reference used to move the tank.
        private float m_MovementInputValue;         // The current value of the movement input.
        private float m_TurnInputValue;             // The current value of the turn input.
        private float m_TurretTurnInputValue;
        private float m_AimInputValue;
        private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.
        private ParticleSystem[] m_particleSystems; // References to all the particles systems used by the Tanks
        private GameObject minDistanceEnemy;
        private float angle;
        private float m_searchRadius = 30f;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }


        private void OnEnable()
        {
            // When the tank is turned on, make sure it's not kinematic.
            m_Rigidbody.isKinematic = false;

            // Also reset the input values.
            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;
            m_TurretTurnInputValue = 0f;
            m_AimInputValue = 0f;

            angle = 0f;

            // We grab all the Particle systems child of that Tank to be able to Stop/Play them on Deactivate/Activate
            // It is needed because we move the Tank when spawning it, and if the Particle System is playing while we do that
            // it "think" it move from (0,0,0) to the spawn point, creating a huge trail of smoke
            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Play();
            }
        }


        private void OnDisable()
        {
            // When the tank is turned off, set it to kinematic so it stops moving.
            m_Rigidbody.isKinematic = true;

            // Stop all particle system so it "reset" it's position to the actual one instead of thinking we moved when spawning
            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Stop();
            }

            m_Turret.transform.rotation = transform.rotation;
        }


        private void Start()
        {
            // The axes names are based on player number.
            m_MovementAxisName = "Vertical" + m_PlayerNumber;
            m_TurnAxisName = "Horizontal" + m_PlayerNumber;
            m_TurretTurnAxisName = "TurretHorizontal" + m_PlayerNumber;
            m_AimButtonName = "Aim" + m_PlayerNumber;
            

            // Store the original pitch of the audio source.
            m_OriginalPitch = m_MovementAudio.pitch;
        }


        private void Update()
        {
            // Store the value of both input axes.
            m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
            m_TurnInputValue = Input.GetAxis(m_TurnAxisName);
            m_TurretTurnInputValue = Input.GetAxis(m_TurretTurnAxisName);
            m_AimInputValue = Input.GetAxis(m_AimButtonName);

            for (int i = 0; i < enemyList.Count; i++)
            {
                if (Vector3.Distance(transform.position, enemyList[i].transform.position) < m_searchRadius)
                {
                    if (minDistanceEnemy == null || Vector3.Distance(transform.position, minDistanceEnemy.transform.position) > Vector3.Distance(transform.position, enemyList[i].transform.position))
                    {
                        minDistanceEnemy = enemyList[i];
                    }
                }
                else
                {
                    minDistanceEnemy = null;
                }
            }
;
            if (minDistanceEnemy != null)
            {
                var axis = Vector3.Cross(m_Turret.transform.forward, (minDistanceEnemy.transform.position - m_Turret.transform.position));
                angle = Vector3.Angle(m_Turret.transform.forward, (minDistanceEnemy.transform.position - m_Turret.transform.position)) * (axis.y < 0 ? -1 : 1);
            }

            if (angle < 0)
            {
                m_AimInputValue = -m_AimInputValue;
            }
            EngineAudio();
        }


        private void EngineAudio()
        {
            // If there is no input (the tank is stationary)...
            if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f && Mathf.Abs(m_TurretTurnInputValue) < 0.1f)
            {
                // ... and if the audio source is currently playing the driving clip...
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    // ... change the clip to idling and play it.
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else
            {
                // Otherwise if the tank is moving and if the idling clip is currently playing...
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    // ... change the clip to driving and play.
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }


        private void FixedUpdate()
        {
            // Adjust the rigidbodies position and orientation in FixedUpdate.
            Move();
            Turn();
            TurretTurn();
            Aim();
        }


        private void Move()
        {
            float translation = m_MovementInputValue * m_Speed * Time.deltaTime;
            transform.Translate(0, 0, translation);
        }


        private void Turn()
        {
            float rotation = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;
            transform.Rotate(0, rotation, 0);
        }

        private void TurretTurn()
        {
            float rotation = m_TurretTurnInputValue * m_TurnSpeed * Time.deltaTime;
            m_Turret.transform.Rotate(0, rotation, 0);
        }

        private void Aim()
        {
            if (angle != 0)
            {
                float rotation = m_AimInputValue * m_TurnSpeed * Time.deltaTime;
                m_Turret.transform.Rotate(0, rotation, 0);
            }
        }
    }
}