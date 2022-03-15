using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public enum PlayerStates
    {
        grounded,
        inair,
        onwall,
        ledgegrab,
    }

    public PlayerStates CurrentState; //controlla in quale stato si trova il giocatore

    [Header("Physics")]
    public float MaxSpeed;
    public float BackwardsMovementSpeed; //movimento sinistra destra oppure indietro
    [Range(0, 1)]
    public float InAirControl; //quanto controllo si ha in aria

    private float ActSpeed; //la velocità del giocatore

    public float Acceleration;
    public float Deccelleration;
    public float DirectionalControl; //quanto si può cambiare velocemente la direzione del movimento
    private float InAirTimer;
    private float GroundedTimer;
    private float AdjustmentAmt; //quanto il giocatore può aggiustare il movimento durante uno stato (per esempio è 0 mentre scivoli perchè perdi controllo della velocità)

    [Header("Jumping")]
    public float JumpAmt;

    [Header("Turning")]
    public float TurnSpeed;
    public float TurnSpeedInAir;
    public float TurnSpeedOnWalls;

    public float LookUpSpeed; //quanto velocemente il giocatore guarda verso l'alto
    public Camera Head; //reference per la nostra testa per muoversi

    private float YTurn;
    private float XTurn;
    public float MaxLookAngle;
    public float MinLookAngle;

    [Header("WallRuns")]
    public float WallRunTime = 1; //per quanto tempo si può correre su un muro
    private float ActWallTime = 0; //il timer per la corsa sul muro
    public float WallRunUpwardsMovement = 4; //quanto si corre in metri su un muro
    public float WallRunSpeedAcceleration = 2f; //quanto velocemente ottieni velocità sul muro


    [Header("Sliding")]
    public float PlayerCtrl; //quanto controllo il giocatore ha in diverse azioni (per esempio scivolata)
    public float SlideSpeedLimit; //quanto si deve essere veloci per scivolare
    public float SlideAmt; //quanto si viene spinti mentre si effettua la scivolata

    [Header("Ledge Grab")]
    public float PullUpTime = 0.5f; //quanto ci si mette ad arrampicarsi su un ledge
    private Vector3 OriginPos;
    private Vector3 LedgePos;
    private float ActPullUpTime;

    private PlayerCollision Coli;
    private Rigidbody Rigid;
    private Animator Anim;

    [Header("Crouching")]
    private bool Crouch;
    private CapsuleCollider Cap;
    public float CrouchHeight;
    private float StandingHeight;
    public float CrouchSpeed;
    private void Start()
    {
        Coli = GetComponent<PlayerCollision>();
        Rigid = GetComponent<Rigidbody>();
        Anim = GetComponent<Animator>();
        Cap = GetComponent<CapsuleCollider>();
        StandingHeight = Cap.height;

        AdjustmentAmt = 1; //reset della quantità di Tuning fatti
    }

    private void Update()
    {
        float XMOV = Input.GetAxis("Horizontal");
        float YMOV = Input.GetAxis("Vertical");

        if (CurrentState == PlayerStates.grounded)
        {
            //check per il salto
            if (Input.GetButtonDown("Jump"))
                JumpUp();

            //check per crouching
            if(Input.GetButton("Crouching"))
            {
                if(!Crouch)
                {
                    StartCrouching();
                }
            }
            else //stand up
            {
                bool Check = Coli.CheckRoof(transform.up);

                if (!Check)
                    StopCrouching();
            }

            //check per il pavimento
            bool checkG = Coli.CheckFloor(-transform.up);
            if(!checkG)
            {
                InAir();
            }
        }
        else if (CurrentState == PlayerStates.inair)
        {
            //check per aggrapparsi
            if (Input.GetButtonDown("Grab"))
            {
                Vector3 Ledge = Coli.CheckLedges();
                if (Ledge != Vector3.zero)
                {
                    LedgeGrab(Ledge);
                    return;
                }
            }
            //check per muri su cui correre
            bool wall = CheckWall(XMOV, YMOV);

            if(wall)
            {
                WallRun();
                return;
            }

            //check per il pavimento
            bool checkG = Coli.CheckFloor(-transform.up);
            if (checkG && InAirTimer > 0.2f)
            {
                OnGround();
            }
        }
        else if (CurrentState == PlayerStates.ledgegrab)
        {
            //rimuovere ogni velocità al giocatore
            Rigid.velocity = Vector3.zero;
        }
        else if (CurrentState == PlayerStates.onwall)
        {
            //check per muri su cui correre
            bool wall = CheckWall(XMOV, YMOV);

            if (!wall)
            {
                InAir();
                return;
            }

            bool onGround = Coli.CheckFloor(-transform.up);
            if (onGround)
            {
                OnGround();
            }
        }
    }

    private void FixedUpdate()
    {
        float Del = Time.deltaTime;
        
        float XMOV = Input.GetAxis("Horizontal");
        float YMOV = Input.GetAxis("Vertical");

        float CamX = Input.GetAxis("Mouse X");
        float CamY = Input.GetAxis("Mouse Y");

        LookUpDown(CamY, Del);

        if (CurrentState == PlayerStates.grounded)
        {
            //aumenta il tempo a terra
            if (GroundedTimer < 10)
                GroundedTimer += Del;

            //get the magnitude of our inputs
            float inputmag = new Vector2(XMOV, YMOV).normalized.magnitude;
            //get wich speed to apply to player (forwards or backwards)
            float targetSpd = Mathf.Lerp(BackwardsMovementSpeed, MaxSpeed, YMOV);
            //check for crouching (se è così applica la velocità di crouch)
            if (Crouch)
                targetSpd = CrouchSpeed;

            lerpSpeed(inputmag, Del, targetSpd);

            MovePlayer(XMOV, YMOV, Del, 1);
            TurnPlayer(CamX, Del, TurnSpeed);

            if (AdjustmentAmt < 1) //riottenere il controllo del giocatore
                AdjustmentAmt += Del * PlayerCtrl;
            else
                AdjustmentAmt = 1;
        }
        else if (CurrentState == PlayerStates.inair)
        {
            if (InAirTimer < 10)
                InAirTimer += Del;

            //movimento del giocatore in aria, con meno controllo
            MovePlayer(XMOV, YMOV, Del, InAirControl);

            TurnPlayer(CamX, Del, TurnSpeedInAir);
        }
        else if (CurrentState == PlayerStates.ledgegrab)
        {
            //aumento del pullup time
            ActPullUpTime += Del;

            float pullUpLerp = ActPullUpTime / PullUpTime;

            if(pullUpLerp < 0.5)
            {
                //pull up verticalmente
                float lamt = pullUpLerp * 2;
                Vector3 LPos = new Vector3(OriginPos.x, LedgePos.y, OriginPos.z);
                transform.position = Vector3.Lerp(OriginPos, LPos, lamt);
            }
            else if(pullUpLerp <= 1)
            {
                if (OriginPos.y != LedgePos.y) //stabilisce la nuova posizione di origine rispetto alla posizione corrente
                    OriginPos = new Vector3(transform.position.x, LedgePos.y, transform.position.z);

                //now lerp to ledge position
                float lamt = (pullUpLerp - 0.5f) * 2f;
                transform.position = Vector3.Lerp(OriginPos, LedgePos, pullUpLerp);

            }
            else
            {
                //il giocatore si è arrampicato sul ledge!
                OnGround();
            }
        }
        else if (CurrentState == PlayerStates.onwall)
        {
            //timer per stare sul muro
            ActWallTime += Del;

            TurnPlayer(CamX, Del, TurnSpeedOnWalls);

            WallRunMovement(YMOV, Del);
        }
    }

    void JumpUp()
    {
        Vector3 Vel = Rigid.velocity;
        Vel.y = 0;

        Rigid.velocity = Vel;

        Rigid.AddForce(transform.up * JumpAmt, ForceMode.Impulse);

        InAir();
    }

    void lerpSpeed(float Mag, float d, float spd)
    {
        //l'attuale velocità legata ai nostri input
        float LaMT = spd * Mag;

        //se ci stiamo muovendo o fermando
        float Accel = Acceleration;
        if (Mag == 0)
            Accel = Deccelleration;

        //lerp our actual speed
        ActSpeed = Mathf.Lerp(ActSpeed, LaMT, d * Accel);
    }
       
    void MovePlayer(float hor, float ver, float d, float Control)
    {
        //trova la direzione per il movimento
        Vector3 MovDir = (transform.forward * ver) + (transform.right * hor);
        MovDir = MovDir.normalized;

        //se non si preme nessun input, segue per inerzia la direzione della velocità
        if (hor == 0 && ver == 0)
            MovDir = Rigid.velocity.normalized;

        //moltiplica la nostra direzione per la nostra velocità
        MovDir = MovDir * ActSpeed;

        MovDir.y = Rigid.velocity.y;

        //apply acceleration
        float Acel = (DirectionalControl * AdjustmentAmt) * Control; //quanto controllo abbiamo sul nostro movimento
        Vector3 LerpVel = Vector3.Lerp(Rigid.velocity, MovDir, Acel * d);
        Rigid.velocity = LerpVel;
    }

    void TurnPlayer(float XAmt, float D, float Spd)
    {
        YTurn += (XAmt * D) * Spd;

        transform.rotation = Quaternion.Euler(0, YTurn, 0);
    }

    void LookUpDown(float YAmt, float d)
    {
      XTurn -= (YAmt * d) *LookUpSpeed;
      XTurn = Mathf.Clamp(XTurn, MinLookAngle, MaxLookAngle);

      Head.transform.localRotation = Quaternion.Euler(XTurn, 0, 0);

    }
    
    void InAir()
    {
        if (Crouch)
            StopCrouching();
        
        InAirTimer = 0;
        CurrentState = PlayerStates.inair;
    }

    void OnGround()
    {
        GroundedTimer = 0;
        ActWallTime = 0; //reset della possibilità di fare una corsa sul muro quando si tocca il pavimento
        CurrentState = PlayerStates.grounded;
    }

    void LedgeGrab(Vector3 LPos)
    {
        //reset di tutte le informazioni riguardo ad un ledge
        LedgePos = LPos;
        OriginPos = transform.position;
        ActPullUpTime = 0;
        CurrentState = PlayerStates.ledgegrab;
    }

    bool CheckWall(float XM, float YM)
    {
        if (XM == 0 && YM ==0)
        return false;

        if (ActWallTime > WallRunTime)
            return false;

        Vector3 WallDirection = transform.forward * YM + transform.right * XM;
        WallDirection = WallDirection.normalized;
        bool WallCol = Coli.CheckWalls(WallDirection);

        return WallCol;
    }

    void WallRun()
    {
        CurrentState = PlayerStates.onwall;
    }

    void WallRunMovement(float verticalMov, float D)
    {
        //la direzione della corsa sul muro
        Vector3 MovDir = transform.up * verticalMov;
        MovDir = MovDir * WallRunUpwardsMovement;

        //la velocità e il momentum del giocatore vengono applicati al muro
        MovDir += transform.forward * ActSpeed;

        Vector3 lerpAmt = Vector3.Lerp(Rigid.velocity, MovDir, WallRunSpeedAcceleration * D);
        Rigid.velocity = lerpAmt;
    }

    void StartCrouching()
    {
        Crouch = true;
        Cap.height = CrouchHeight;

        if (ActSpeed > SlideSpeedLimit)
            SlideForwards();
    }

    void StopCrouching()
    {
        Crouch = false;
        Cap.height = StandingHeight;
    }

    void SlideForwards()
    {
        ActSpeed = SlideSpeedLimit;

        AdjustmentAmt = 0;

        Vector3 Dir = Rigid.velocity.normalized;

        Dir.y = 0;

        Rigid.AddForce(Dir * SlideAmt, ForceMode.Impulse);
    }
}
