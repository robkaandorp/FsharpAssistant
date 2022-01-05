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

type AuthenticationRequestMessage =
    { ``type``: string
      access_token: string }

type ErrorResponse =
    { code: string
      message: string }

type CommandResponseMessage =
    { id: int
      ``type``: string
      success: bool
      error: ErrorResponse }

type SubscribeEvents =
    { id: int
      ``type``: string
      event_type: string }

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