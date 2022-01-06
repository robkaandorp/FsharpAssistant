module Model

open System

type ApiMessage = { message: string }

type State = 
    { entity_id: string
      last_changed: DateTime
      last_updated: DateTime
      state: string
      attributes: Map<string, Object> }

type ResponseMessage =
    { ``type``: string }

type AuthenticationResponseMessage =
    { ha_version: string
      access_token: string
      message: string }

type ErrorResponse =
    { code: string
      message: string }

type CommandResponseMessage =
    { id: int
      ``type``: string
      success: bool
      error: ErrorResponse }

type EventData =
    { entity_id: string
      new_state: State
      old_state: State }

type Event =
    { data: EventData
      event_type: string
      time_fired: DateTime
      origin: string }

type EventMessage =
    { id: int
      event: Event }

type Message =
    | AuthRequired of AuthenticationResponseMessage
    | AuthOk of AuthenticationResponseMessage
    | AuthInvalid of AuthenticationResponseMessage
    | Result of CommandResponseMessage
    | Event of EventMessage
    | Other of string
    | Closed of string
    | Fail of Runtime.ExceptionServices.ExceptionDispatchInfo

type RequestMessage() = class end

type RequestMessageWithId() =
    inherit RequestMessage()
    member val id = -1 with get, set

type AuthenticationRequestMessage(access_token: string) =
    inherit RequestMessage()
    member val ``type`` = "auth" with get
    member val access_token = access_token with get

type SubscribeEvents(event_type: string) =
    inherit RequestMessageWithId()
    member val ``type`` = "subscribe_events" with get
    member val event_type = event_type with get