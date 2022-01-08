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
    { ``type``: string
      ha_version: string
      message: string option }

type ErrorResponse =
    { code: string
      message: string }

type CommandResponseMessage =
    { id: int
      ``type``: string
      success: bool
      result: Map<string, obj> option
      error: ErrorResponse option }

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

type RequestMessage(``type``: string) =
    member val ``type`` = ``type`` with get

type RequestMessageWithId(``type``: string) =
    inherit RequestMessage(``type``)
    member val id = -1 with get, set

type AuthenticationRequestMessage(access_token: string) =
    inherit RequestMessage("auth")
    member val access_token = access_token with get

type SubscribeEvents(event_type: string) =
    inherit RequestMessageWithId("subscribe_events")
    member val event_type = event_type with get

type GetServices() =
    inherit RequestMessageWithId("get_services")

type Target = { entity_id: string }

type CallService(domain: string, service: string, entityId: string) =
    inherit RequestMessageWithId("call_service")
    member val domain = domain with get
    member val service = service with get
    member val service_data = Map<string, string> with get, set
    member val target = { entity_id = entityId } with get
