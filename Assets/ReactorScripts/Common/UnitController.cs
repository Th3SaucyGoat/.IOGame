using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor;

public class Axes
{
    public const uint X = 0;
    public const uint Y = 1;
}

public class Buttons
{
    public const uint SpawnCollector = 0;
}

public class UnitController : ksPlayerController
{
    private float SPEED = 2f;

    private float ACCELERATION = 7f;

    private ksVector2 velocity;

    // Unique non-zero identifier for this player controller class.
    public override uint Type
    {
        get { return 1; }
    }

    public UnitController(float speed = 2f, float acc = 7f)
    {
        this.SPEED = speed;
        this.ACCELERATION = acc;
    }

    // Register all buttons and axes you will be using here.
    public override void RegisterInputs(ksInputRegistrar registrar)
    {
        registrar.RegisterAxes(Axes.X, Axes.Y);
        registrar.RegisterButtons(Buttons.SpawnCollector);
    }

    // Called after properties are initialized.
    public override void Initialize()
    {

    }

    // Called during the update cycle.
    public override void Update()
    {
        ksVector2 input_vector = new ksVector2(Input.GetAxis(Axes.X), Input.GetAxis(Axes.Y));
        velocity = ksVector2.MoveTowards(velocity, input_vector.Normalized() * SPEED, Time.Delta * ACCELERATION);


        RigidBody2D.Velocity = velocity;


    }
}