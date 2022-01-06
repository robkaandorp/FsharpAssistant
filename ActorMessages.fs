module ActorMessages

open Model

type StateActorMessages =
    | Start
    | Stop
    | State of EventMessage