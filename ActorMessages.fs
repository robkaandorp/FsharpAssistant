﻿module ActorMessages

open Model

type StateActorMessages =
    | Start
    | Stop
    | State of EventMessage

type ServiceActorMessages =
    | Start
    | Stop
    | GetServiceResponse of Map<string, obj>

type WsActorMessages =
    | Start
    | Send of obj
    | Receive of Message