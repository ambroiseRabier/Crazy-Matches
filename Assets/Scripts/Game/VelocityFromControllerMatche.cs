﻿using UnityEngine;
using GAF.Core;
using Events;
using Assets.Scripts.Game.Actors;

namespace Assets.Scripts.Game {

    /// <summary>
    /// SPEED variable IS NOT TO BE SETTED on the matches, (change burn speed on matches scripts)
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VelocityFromControllerMatche : VelocityFromController {

        /*[SerializeField]*/ private float m_turnSpeed = 0.1f; // 0 to 1;
        /*[SerializeField] */private float m_maxTurnAnglePerFrame = 15f;
        /*[SerializeField] */private float m_maxTurnAnglePerFrameCar = 5f;
        //private Vector3 previousPosition = Vector3.zero; // don't use transform.rotation dummy, get the direction from last position
        private float previousInputAngle; // don't use transform.rotation dummy, get the direction from last position
        private Vector3 previousDirection = Vector3.zero;
        private bool firstInputSet = false;

        [SerializeField] private Color m_ImLostColor = Color.blue;
        [SerializeField] private GAFBakedMovieClip m_gafHead;
        [SerializeField] private VibrationSettings vibrationOnMatcheBurnMatche;
        [SerializeField] private VibrationSettings vibrationOnPlayerMatcheChangeBody;
        [Tooltip("The vibration get highter whit the distance, ignore vibrationOnPlayerMatcheChangeBody left and right settings if checked. Duration is not ignored.")]
        [SerializeField] private bool vibrationOnPlayerChangeMatcheAuto;
        [Tooltip("Distance at wich the vibration is maximise to 1. Should be assigned depending on the stage width. (roughtly 1800+ px)")]
        [SerializeField] private float maxVibrationAtDistance = 700;
        [Tooltip("Give the force of vibration depending on the current distance / maxVibrationDistance. (you can set minVibration and maxVibration)")]
        [SerializeField] private AnimationCurve vibrationForceCurve;

        protected void Start () {
            GlobalEventBus.onLightningMatcheByPlayer.AddListener(OnLightningMatcheByPlayer);
            GlobalEventBus.onPlayerMatcheChangeBody.AddListener(OnPlayerMatcheChangeBody);
        }

        protected void Update () {

            if (m_Controller) {
                //print(m_gafHead.settings.animationColorMultiplier);
                //print(m_gafHead.settings.animationColorOffset);

                m_gafHead.setColorAndOffset(
                    m_Controller.Fire ? m_ImLostColor : Color.white, // bad for performance ? should i check the color before applying filter ?
                    new Vector4(0, 0, 0, 0)
                );

            }
        }

        override protected void FixedUpdate () {
            if (m_Controller && m_Controller.Joystick.normalized != Vector3.zero) {
                //SixthTest();
                if (firstInputSet) {


                    //ThirdTest();
                    //FourthTest(); // choose one
                    SeventhController(); // choosen one whit "speed growing whit death" (speed increase is managed in Matches.cs, so it change pnj speed too)
                    //FirstTest();
                    //FifthTest();
                } else {
                    firstInputSet = true;
                    //previousInputAngle = Mathf.Atan2(m_Controller.Joystick.normalized.y, m_Controller.Joystick.normalized.x);
                    previousDirection = m_Controller.Joystick.normalized;
                    m_currentDirection = m_Controller.Joystick.normalized;
                }

            }
        }

        protected void OnLightningMatcheByPlayer() {
            if (m_Controller != null)
                m_Controller.rewiredController.SetVibration(
                    vibrationOnMatcheBurnMatche.left,
                    vibrationOnMatcheBurnMatche.right,
                    vibrationOnMatcheBurnMatche.duration
                );
        }

        protected void OnPlayerMatcheChangeBody(Vector2 pDistance) {
            if (m_Controller != null) {
                if (vibrationOnPlayerChangeMatcheAuto) {
                    float lRight = 0;
                    float lLeft = 0;

                    if (pDistance.x < 0) {
                        lLeft = Mathf.Max(pDistance.x, -maxVibrationAtDistance) / -maxVibrationAtDistance; //max(-400, -700) / -700
                        m_Controller.rewiredController.SetVibration(
                            Mathf.Clamp01(vibrationForceCurve.Evaluate(lLeft)),
                            0,
                            vibrationOnPlayerMatcheChangeBody.duration
                        );
                    } else {
                        lRight = Mathf.Min(pDistance.x, maxVibrationAtDistance) / maxVibrationAtDistance; //min(400, 700) / 700
                        m_Controller.rewiredController.SetVibration(
                            0,
                            Mathf.Clamp01(vibrationForceCurve.Evaluate(lRight)),
                            vibrationOnPlayerMatcheChangeBody.duration
                        );
                    }
                        

                    /*print("lLeft " + lLeft);
                    print("lLeftC " + Mathf.Clamp01(vibrationForceCurve.Evaluate(lLeft)));
                    print("lRight " + lRight);
                    print("lRightC " + Mathf.Clamp01(vibrationForceCurve.Evaluate(lRight)));*/

                    
                    
                }
                else
                    m_Controller.rewiredController.SetVibration(
                        vibrationOnPlayerMatcheChangeBody.left,
                        vibrationOnPlayerMatcheChangeBody.right,
                        vibrationOnPlayerMatcheChangeBody.duration
                    );
            }
                
        }

        void SeventhController() {
            m_Rigidbody.velocity = m_Controller.Joystick.normalized * m_Speed;
        }

        //physic control, whit max speed
        void SixthTest () {
            m_Rigidbody.AddForce(previousDirection * m_Speed);
            m_Rigidbody.velocity = Vector3.Lerp(m_Rigidbody.velocity, previousDirection * m_Speed, m_turnSpeed);
            if (m_Controller.Joystick.normalized != Vector3.zero)
                previousDirection = m_Controller.Joystick.normalized;
        }

        //car control
        void FifthTest () {
            float angleDiff = m_Controller.Joystick.normalized.x * m_maxTurnAnglePerFrameCar; // avec vitesse 200, mais collision galère
            Vector3 newDirection = Quaternion.Euler(0, 0, angleDiff) * previousDirection;
            float newAngleDiff = Vector3.Angle(newDirection, m_Controller.Joystick.normalized);

            m_Rigidbody.velocity = newDirection.normalized * m_Speed;
            previousDirection = newDirection.normalized;
        }

        //normal control
        void FourthTest () {
            m_Rigidbody.velocity = m_Controller.Joystick.normalized * m_Speed;
        }

        // physic control, no alway max speed
        void ThirdTest() {
            m_Rigidbody.AddForce(m_Controller.Joystick.normalized * m_Speed);
            m_Rigidbody.velocity = Vector3.Lerp(m_Rigidbody.velocity, m_Controller.Joystick.normalized * m_Speed, m_turnSpeed);
        }

        // limited angle turn
        void FirstTest () {
            //Vector3 newDirection = Vector3.zero;
            //float maxTurnPerFrameRad = m_maxTurnAnglePerFrame * Mathf.PI / 180;
            //float inputDirectionAngle = Mathf.Atan2(m_Controller.Joystick.normalized.y, m_Controller.Joystick.normalized.x);
            //float limitedDirectionAngle = Mathf.Lerp(previousInputAngle, inputDirectionAngle, 0.5f);//Mathf.Clamp(previousInputAngle - inputDirectionAngle, -maxTurnPerFrameRad, maxTurnPerFrameRad);
            float angleDiff = Vector3.Angle(previousDirection, m_Controller.Joystick.normalized); // can be a mess if z is different
            Debug.Log(angleDiff);
            angleDiff = Mathf.Clamp(angleDiff, -m_maxTurnAnglePerFrame, m_maxTurnAnglePerFrame);
            Debug.Log("after " + angleDiff);
            Vector3 newDirection = Quaternion.Euler(0, 0, -angleDiff) * previousDirection;
            float newAngleDiff = Vector3.Angle(newDirection, m_Controller.Joystick.normalized);
            Debug.Log("new " + newAngleDiff);

            /*newDirection = new Vector3(
                Mathf.Cos(limitedDirectionAngle),
                Mathf.Sin(limitedDirectionAngle),
                0
            );*/

            m_Rigidbody.velocity = newDirection.normalized * m_Speed;
            //previousInputAngle = limitedDirectionAngle;
            previousDirection = newDirection.normalized;
        }

        float m_maxAngleRotation = 15f;   // Défini comme membre sérializé
        Vector3 m_currentDirection; // Actuelle direction, défini comme membre

        // not working
        void SecondTest () {

            float currentRadian = Mathf.Atan2(m_currentDirection.y, m_currentDirection.x);
            float wantedRadian = Mathf.Atan2(m_Controller.Joystick.y, m_Controller.Joystick.x);
            float currentToWantedRadian = wantedRadian - currentRadian;
            float maxRadianRotation = m_maxAngleRotation * Mathf.Deg2Rad;
            float newRadian = currentRadian + Mathf.Clamp(currentToWantedRadian, maxRadianRotation, -maxRadianRotation);

            Vector3 newDirection = new Vector3(
              Mathf.Cos(newRadian),
              Mathf.Sin(newRadian)
            );

            m_Rigidbody.velocity = newDirection * m_Speed;
        }

        Vector2 rotateVector (Vector2 pVec, float pRad) {
            Vector2 copyTransform = pVec;//transform.rotation.eulerAngles;
            //Vector2 unitVector = Vector3.Normalize(copyTransform);
           /* Debug.Log("rotation: ");
            Debug.Log(Math.Atan2(unitVector.y, unitVector.x) / Mathf.PI * 180);*/
            Vector2 unitVectorRotated = Quaternion.Euler(0, 0, pRad *180/Mathf.PI) * copyTransform;
            return unitVectorRotated;
            /*Debug.Log("rotation -45: ");
            Debug.Log(Math.Atan2(unitVectorRotated.y, unitVectorRotated.x) / Mathf.PI * 180);*/
        }

        protected void OnDestroy () {
            GlobalEventBus.onLightningMatcheByPlayer.RemoveListener(OnLightningMatcheByPlayer);
            GlobalEventBus.onPlayerMatcheChangeBody.RemoveListener(OnPlayerMatcheChangeBody);
        }
    }
}