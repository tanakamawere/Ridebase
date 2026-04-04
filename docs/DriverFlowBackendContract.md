# Driver Ride Flow Backend Contract

## Purpose
This document is the backend handoff for the driver-side ride-hailing flow in the mobile app.

It covers the full driver journey:
`go online -> receive rider request -> accept or counteroffer -> navigate to pickup -> mark arrived -> start trip -> update trip progress -> trigger SOS if needed -> end trip`

This contract is designed to align with the current app behavior and the new driver-side pages:
- incoming request dashboard
- dedicated counteroffer page
- active trip progress page
- dedicated SOS page

## Driver Flow Summary
### 1. Driver goes online
The driver app opens a realtime request stream after the driver toggles online status.

The backend should only begin sending ride requests to drivers who:
- are online
- are authenticated
- are subscription-eligible / marketplace-eligible
- are not already in an active trip

### 2. Driver receives a ride request
Each request pushed to the driver should include:
- rider identity summary
- rider offer amount
- recommended fare
- pickup and destination text
- pickup and destination coordinates
- ETA to pickup
- distance to pickup

### 3. Driver chooses one of two actions
- accept the rider's offer immediately
- open the counteroffer flow and send a custom amount

### 4. Rider selects the winning driver
After the rider accepts a specific offer, the backend should mark that ride request as assigned and stop showing it to other drivers.

### 5. Driver progresses the ride
The driver app emits:
- `DriverEnRoute`
- `DriverArrived`
- `TripStarted`
- `TripCompleted`

### 6. Driver can trigger SOS
During an active trip, the driver can submit a safety/emergency incident with:
- reason code
- optional notes
- current trip/ride id
- current driver location

## Lifecycle States
Canonical shared ride states:

```text
Requested
SearchingDrivers
OfferCountered
OfferAccepted
DriverEnRoute
DriverArrived
TripStarted
TripCompleted
Cancelled
OfferRejected
```

Recommended progression from the driver perspective:

```text
Requested -> OfferAccepted
Requested -> OfferCountered
OfferAccepted -> DriverEnRoute
DriverEnRoute -> DriverArrived
DriverArrived -> TripStarted
TripStarted -> TripCompleted
Any open state -> Cancelled
Any unselected offer -> OfferRejected or Expired
```

## Authentication Assumption
All driver REST and realtime operations should be authenticated with the driver's normal access token.

Suggested headers:

```http
Authorization: Bearer <jwt>
X-Client-Platform: android|ios
X-Client-Version: <app-version>
```

## REST API

### `POST /api/drivers/availability`
Optional REST endpoint to explicitly mark the driver online or offline in addition to websocket presence.

Request:
```json
{
  "driverId": "driver_123",
  "isOnline": true,
  "updatedAtUtc": "2026-04-04T14:00:00Z"
}
```

Response:
```json
{
  "driverId": "driver_123",
  "isOnline": true
}
```

### `POST /api/rides/driver/accept`
Optional REST confirmation when the driver accepts a rider offer directly.

If the system uses realtime only for offer submission, this can be omitted. If used, it should be idempotent.

Request:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
  "offerAmount": 6.5,
  "acceptedAtUtc": "2026-04-04T14:00:35Z"
}
```

Response:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "status": "OfferAccepted"
}
```

### `POST /api/rides/driver-counter-offer`
Optional REST persistence for the driver's counteroffer if the backend wants offers stored outside websocket transport.

Request:
```json
{
  "rideOfferId": "5cbd40cc-ccde-41db-b62f-a6ed95de7cb2",
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
  "offerAmount": 7.0,
  "riderOfferAmount": 6.5,
  "recommendedAmount": 7.25,
  "pickupAddress": "Avondale, Harare",
  "destinationAddress": "Joina City Mall, Harare",
  "offerTimeUtc": "2026-04-04T14:00:45Z"
}
```

Response:
```json
{
  "rideOfferId": "5cbd40cc-ccde-41db-b62f-a6ed95de7cb2",
  "status": "OfferCountered"
}
```

### `POST /api/rides/{rideId}/status`
Persist a driver-originated ride state transition.

Request:
```json
{
  "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
  "status": "DriverArrived",
  "statusMessage": "Driver has arrived at pickup.",
  "etaMinutes": 0,
  "updatedAtUtc": "2026-04-04T14:08:12Z"
}
```

Response:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "status": "DriverArrived"
}
```

### `POST /api/rides/{rideId}/driver-location`
Persist latest driver location.

Request:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
  "currentLocation": {
    "latitude": -17.8284,
    "longitude": 31.0490
  },
  "etaMinutes": 3,
  "distanceToPickupKm": 1.1,
  "updatedAtUtc": "2026-04-04T14:06:59Z"
}
```

Response:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "accepted": true
}
```

### `POST /api/rides/{rideId}/complete`
Mark ride completed from the driver side.

Request:
```json
{
  "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
  "completedAtUtc": "2026-04-04T14:22:00Z"
}
```

Response:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "status": "TripCompleted"
}
```

### `POST /api/rides/driver-sos`
Create a driver-side emergency / safety incident.

Request:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
  "driverName": "John Dube",
  "riderId": "user_123",
  "riderName": "Tanaka Mawere",
  "tripStatus": "TripStarted",
  "reasonCode": "unsafe_situation",
  "message": "Driver triggered SOS from the active trip screen.",
  "currentLocation": {
    "latitude": -17.8302,
    "longitude": 31.0474
  },
  "triggeredAtUtc": "2026-04-04T14:12:30Z"
}
```

Response:
```json
{
  "incidentId": "driver_sos_17dc8b92",
  "status": "Received",
  "receivedAtUtc": "2026-04-04T14:12:31Z"
}
```

Suggested backend behavior:
- persist the SOS incident
- notify support/ops immediately
- attach latest ride and driver metadata
- optionally open a live intervention workflow
- optionally emit a realtime acknowledgement to the driver

### `GET /api/drivers/{driverId}/open-requests`
Optional fallback polling endpoint if realtime request delivery fails.

Response:
```json
[
  {
    "rideId": "0f18c8fe-1d1d-4585-95c2-1c2e05e39f9f",
    "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
    "riderId": "4f44a546-a0bc-4fc2-a3d2-d530a72f8a9d",
    "riderName": "Tanaka Mawere",
    "riderPhoneNumber": "+263771234567",
    "offerAmount": 6.5,
    "recommendedAmount": 7.25,
    "pickupAddress": "Avondale, Harare",
    "destinationAddress": "Joina City Mall, Harare",
    "etaToPickupMinutes": 6,
    "distanceToPickupKm": 2.3,
    "status": "Requested",
    "startLocation": {
      "latitude": -17.8292,
      "longitude": 31.0522
    },
    "destinationLocation": {
      "latitude": -17.8311,
      "longitude": 31.0456
    }
  }
]
```

## WebSocket / Realtime Contract

## Connection
Suggested websocket endpoint:

```text
wss://api.example.com/ws/rides
```

The driver app connects after authentication and starts a request stream once the driver goes online.

## Driver-initiated realtime actions

### `StartDriverRequestStream`
Sent when the driver goes online and is ready to receive requests.

Payload:
```json
{
  "type": "StartDriverRequestStream",
  "driverId": "driver_123"
}
```

### `SubmitDriverOffer`
Sent when the driver counters a rider offer.

Payload:
```json
{
  "type": "SubmitDriverOffer",
  "data": {
    "rideOfferId": "5cbd40cc-ccde-41db-b62f-a6ed95de7cb2",
    "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
    "offerAmount": 7.0,
    "riderOfferAmount": 6.5,
    "recommendedAmount": 7.25,
    "isCounterOffer": true,
    "etaToPickupMinutes": 5,
    "distance": 2.3,
    "pickupAddress": "Avondale, Harare",
    "destinationAddress": "Joina City Mall, Harare",
    "pickupLocation": {
      "latitude": -17.8292,
      "longitude": 31.0522
    },
    "destinationLocation": {
      "latitude": -17.8311,
      "longitude": 31.0456
    },
    "offerTime": "2026-04-04T14:00:45Z",
    "driver": {
      "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
      "name": "John Dube",
      "phoneNumber": "+263772112233",
      "rating": 4.8,
      "ridesCompleted": 487,
      "vehicle": "Toyota Aqua | ABC 1234"
    }
  }
}
```

### `UpdateRideStatus`
Sent by the driver app when marking arrived or starting a journey.

Payload:
```json
{
  "type": "UpdateRideStatus",
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "status": "TripStarted"
}
```

### `PublishDriverLocation`
Sent while approaching pickup and optionally during the trip.

Payload:
```json
{
  "type": "PublishDriverLocation",
  "data": {
    "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
    "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
    "currentLocation": {
      "latitude": -17.8284,
      "longitude": 31.0490
    },
    "etaMinutes": 3,
    "distanceToPickupKm": 1.1,
    "updatedAtUtc": "2026-04-04T14:06:59Z"
  }
}
```

### `CompleteRide`
Sent when the driver ends the trip.

Payload:
```json
{
  "type": "CompleteRide",
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f"
}
```

### `Stop`
Sent when the driver goes offline or leaves the marketplace.

Payload:
```json
{
  "type": "Stop",
  "driverId": "driver_123"
}
```

## Driver-facing server events

### `DriverRideRequestReceived`
Sent to drivers when a nearby rider request matches their availability.

Payload:
```json
{
  "type": "DriverRideRequestReceived",
  "data": {
    "rideId": "0f18c8fe-1d1d-4585-95c2-1c2e05e39f9f",
    "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
    "riderId": "4f44a546-a0bc-4fc2-a3d2-d530a72f8a9d",
    "riderName": "Tanaka Mawere",
    "riderPhoneNumber": "+263771234567",
    "offerAmount": 6.5,
    "recommendedAmount": 7.25,
    "pickupAddress": "Avondale, Harare",
    "destinationAddress": "Joina City Mall, Harare",
    "etaToPickupMinutes": 6,
    "distanceToPickupKm": 2.3,
    "status": "Requested",
    "startLocation": {
      "latitude": -17.8292,
      "longitude": 31.0522
    },
    "destinationLocation": {
      "latitude": -17.8311,
      "longitude": 31.0456
    }
  }
}
```

### `RideAssignedToDriver`
Recommended acknowledgement sent when the rider chooses a specific driver offer.

Payload:
```json
{
  "type": "RideAssignedToDriver",
  "data": {
    "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
    "selectedOfferId": "5cbd40cc-ccde-41db-b62f-a6ed95de7cb2",
    "acceptedAmount": 7.0,
    "status": "DriverEnRoute",
    "acceptedAtUtc": "2026-04-04T14:01:11Z"
  }
}
```

### `RideCancelled`
Recommended event when the rider or backend cancels an open or active request.

Payload:
```json
{
  "type": "RideCancelled",
  "data": {
    "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
    "status": "Cancelled",
    "cancelledBy": "Rider",
    "reasonCode": "changed_mind",
    "updatedAtUtc": "2026-04-04T14:02:00Z"
  }
}
```

### `SosAcknowledged`
Recommended event sent after backend receives a driver SOS.

Payload:
```json
{
  "type": "SosAcknowledged",
  "data": {
    "incidentId": "driver_sos_17dc8b92",
    "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
    "status": "Received",
    "message": "Emergency alert received. Support is being notified.",
    "updatedAtUtc": "2026-04-04T14:12:31Z"
  }
}
```

## Shared Payload Shapes

### `DriverRideRequest`
```json
{
  "rideId": "guid",
  "driverId": "guid",
  "riderId": "guid",
  "riderName": "string",
  "riderPhoneNumber": "string",
  "offerAmount": 0,
  "recommendedAmount": 0,
  "pickupAddress": "string",
  "destinationAddress": "string",
  "etaToPickupMinutes": 0,
  "distanceToPickupKm": 0,
  "status": "Requested",
  "startLocation": {
    "latitude": 0,
    "longitude": 0
  },
  "destinationLocation": {
    "latitude": 0,
    "longitude": 0
  }
}
```

### `DriverOfferSelectionModel`
```json
{
  "rideOfferId": "guid",
  "rideId": "string",
  "offerAmount": 0,
  "riderOfferAmount": 0,
  "recommendedAmount": 0,
  "isCounterOffer": true,
  "etaToPickupMinutes": 0,
  "distance": 0,
  "pickupAddress": "string",
  "destinationAddress": "string",
  "pickupLocation": {
    "latitude": 0,
    "longitude": 0
  },
  "destinationLocation": {
    "latitude": 0,
    "longitude": 0
  },
  "offerTime": "2026-04-04T14:00:45Z",
  "driver": {
    "driverId": "guid",
    "name": "string",
    "phoneNumber": "string",
    "rating": 4.8,
    "ridesCompleted": 487,
    "vehicle": "string"
  }
}
```

### `DriverLocationUpdate`
```json
{
  "rideId": "string",
  "driverId": "guid",
  "currentLocation": {
    "latitude": 0,
    "longitude": 0
  },
  "etaMinutes": 0,
  "distanceToPickupKm": 0,
  "updatedAtUtc": "2026-04-04T14:06:59Z"
}
```

### `DriverSosRequest`
```json
{
  "rideId": "string",
  "driverId": "guid",
  "driverName": "string",
  "riderId": "string",
  "riderName": "string",
  "tripStatus": "TripStarted",
  "reasonCode": "unsafe_situation",
  "message": "string",
  "currentLocation": {
    "latitude": 0,
    "longitude": 0
  },
  "triggeredAtUtc": "2026-04-04T14:12:30Z"
}
```

## Edge Cases

### Driver goes online without required eligibility
Backend should reject or ignore request-stream activation when:
- subscription is inactive
- driver account is suspended
- vehicle/account verification is incomplete
- driver is already in an active ride

### Request expires before driver acts
If the request is no longer valid:
- reject accept/counter attempts
- return `offer_expired` or equivalent
- remove it from the driver's open request list

### Rider accepts another driver's offer first
If this driver tries to act on a request already assigned elsewhere:
- return `ride_already_accepted`
- stop sending that request to other drivers

### Duplicate status updates
`DriverArrived`, `TripStarted`, and `TripCompleted` should be idempotent.

If the client retries:
- backend should not create duplicate transitions
- latest ride state should remain consistent

### Driver loses connectivity mid-trip
Backend should tolerate delayed location/status events and allow recovery via:
- `GET /api/rides/{rideId}`
- `GET /api/rides/{rideId}/status`

### Rider cancels after driver accepted
Backend should:
- notify the selected driver in realtime
- mark the ride cancelled
- release the driver back into available state if applicable

### Trip completion race
If both client and backend retry completion:
- only one completed state should be persisted
- rating should still remain available to the rider

### SOS during cancelled or completed ride
Backend should still accept the SOS if it is close enough in time to the trip, or at minimum preserve the incident attempt for audit/support follow-up.

### Counteroffer amount validation
Backend should reject:
- zero or negative amounts
- malformed decimals
- unreasonable amounts outside marketplace rules

### Driver assigned while offline
If a driver drops offline between request receipt and assignment:
- backend should either reject rider acceptance or force a driver availability recheck before confirming assignment

## Driver UI Expectations
The driver app currently expects:
- incoming ride requests to appear in realtime
- one-tap direct acceptance from dashboard
- a dedicated counteroffer page for custom pricing
- an active trip page with `Mark Arrived`, `Start Journey`, and `End Journey`
- a dedicated SOS page for emergency escalation

## Error Handling
Recommended REST error shape:
```json
{
  "code": "offer_expired",
  "message": "This ride request is no longer available.",
  "details": null
}
```

Suggested codes:
- `driver_not_eligible`
- `driver_offline`
- `offer_expired`
- `ride_not_found`
- `ride_already_accepted`
- `ride_already_cancelled`
- `ride_already_completed`
- `invalid_status_transition`
- `invalid_counter_offer`
- `sos_unavailable`
- `unauthorized`
- `forbidden`

## Current App Interfaces
This document aligns with the current app contracts in:
- [IRideApiClient.cs](/c:/Users/tanak/source/repos/Ridebase/Services/Interfaces/IRideApiClient.cs)
- [IRideRealtimeService.cs](/c:/Users/tanak/source/repos/Ridebase/Services/Interfaces/IRideRealtimeService.cs)
- [DriverRideRequest.cs](/c:/Users/tanak/source/repos/Ridebase/Models/Ride/DriverRideRequest.cs)
- [DriverOfferSelectionModel.cs](/c:/Users/tanak/source/repos/Ridebase/Models/Ride/DriverOfferSelectionModel.cs)
- [DriverLocationUpdate.cs](/c:/Users/tanak/source/repos/Ridebase/Models/Ride/DriverLocationUpdate.cs)
- [DriverSosRequest.cs](/c:/Users/tanak/source/repos/Ridebase/Models/Ride/DriverSosRequest.cs)
- [DriverDashboardViewModel.cs](/c:/Users/tanak/source/repos/Ridebase/ViewModels/Driver/DriverDashboardViewModel.cs)
- [DriverRideProgressViewModel.cs](/c:/Users/tanak/source/repos/Ridebase/ViewModels/Driver/DriverRideProgressViewModel.cs)

