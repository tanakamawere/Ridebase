# Rider Ride Flow Backend Contract

## Purpose
This document is the backend handoff for the rider-side ride-hailing flow in the mobile app.

It covers the full rider journey:
`resolve locations -> request ride -> receive accepts/counteroffers -> accept a driver -> track driver to pickup -> receive ride status updates -> ride in progress -> SOS -> trip completion -> rating`

The app already resolves rider pickup and destination text into coordinates before calling the backend. Backend services should treat the mobile app as the source of truth for:
- `startLocation`
- `startAddress`
- `destinationLocation`
- `destinationAddress`

The backend should not geocode raw rider input.

## Rider Flow Summary
### 1. Rider resolves trip locally
The app uses Google Places, Geocoding, and Directions to determine:
- pickup coordinates
- pickup display address
- destination coordinates
- destination display address
- route distance
- route ETA
- recommended fare

### 2. Rider taps `Find Offers`
The app creates a ride request over REST, then opens a realtime matching session so nearby drivers can:
- accept the rider's amount
- counter with a different amount

### 3. Rider receives offers in realtime
The rider sees one combined list of:
- accepted offers
- counteroffers

### 4. Rider selects a driver
The app confirms the chosen offer via REST and realtime, then moves into the active trip session.

### 5. Rider tracks driver to pickup
The app expects realtime status and location updates for:
- `DriverEnRoute`
- `DriverArrived`

### 6. Ride starts
When the driver starts the trip, the rider moves into the in-progress experience.

### 7. SOS is available during active trip
The rider must be able to trigger an emergency/safety event while waiting for pickup or while riding.

### 8. Ride completes and rider rates driver
After `TripCompleted`, the rider submits a rating and optional feedback.

## Lifecycle States
These should remain the canonical ride states across REST, WebSocket, and persistence:

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

Recommended progression:

```text
Requested -> SearchingDrivers
SearchingDrivers -> OfferCountered
SearchingDrivers -> OfferAccepted
OfferAccepted -> DriverEnRoute
DriverEnRoute -> DriverArrived
DriverArrived -> TripStarted
TripStarted -> TripCompleted
Any non-completed state -> Cancelled
```

## Authentication Assumption
All REST and WebSocket traffic should be authenticated with the rider's normal app token.

Suggested headers:

```http
Authorization: Bearer <jwt>
X-Client-Platform: android|ios
X-Client-Version: <app-version>
```

## REST API

### `POST /api/rides/request`
Create a rider request and return the server ride identifier.

Request:
```json
{
  "rideGuid": "0f18c8fe-1d1d-4585-95c2-1c2e05e39f9f",
  "riderId": "user_123",
  "riderName": "Tanaka Mawere",
  "riderPhoneNumber": "+263771234567",
  "startLocation": {
    "latitude": -17.8292,
    "longitude": 31.0522
  },
  "startAddress": "Avondale, Harare",
  "destinationLocation": {
    "latitude": -17.8311,
    "longitude": 31.0456
  },
  "destinationAddress": "Joina City Mall, Harare",
  "offerAmount": 6.5,
  "recommendedAmount": 7.25,
  "estimatedDistanceKm": 4.8,
  "estimatedMinutes": 11,
  "isOrderingForSomeoneElse": false,
  "requestedForName": "",
  "requestedAtUtc": "2026-04-04T14:00:00Z",
  "comments": "Please call on arrival."
}
```

Response:
```json
{
  "rideRequestId": "ride_9f2d1c74",
  "rideStatus": "Requested",
  "rideDistance": 4.8,
  "estimatedWaitTime": 5
}
```

Notes:
- `rideGuid` is the client-generated correlation id currently used by the app.
- Backend may return its own `rideRequestId`, but it should preserve the original guid for tracing and websocket routing.

### `POST /api/rides/select-offer`
Persist the rider's selected driver offer and create the active ride session.

Request:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "rideOfferId": "5cbd40cc-ccde-41db-b62f-a6ed95de7cb2",
  "riderId": "user_123",
  "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
  "offerAmount": 7.0,
  "recommendedAmount": 7.25,
  "status": "OfferAccepted",
  "pickupAddress": "Avondale, Harare",
  "destinationAddress": "Joina City Mall, Harare",
  "startLocation": {
    "latitude": -17.8292,
    "longitude": 31.0522
  },
  "destinationLocation": {
    "latitude": -17.8311,
    "longitude": 31.0456
  }
}
```

Response:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "status": "DriverEnRoute",
  "selectedOfferId": "5cbd40cc-ccde-41db-b62f-a6ed95de7cb2",
  "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
  "acceptedAmount": 7.0,
  "acceptedAtUtc": "2026-04-04T14:01:11Z"
}
```

### `POST /api/rides/{rideId}/cancel`
Cancel a ride request or active ride from the rider side.

Request:
```json
{
  "cancelledBy": "Rider",
  "reasonCode": "changed_mind",
  "reasonText": "Rider no longer needs the trip.",
  "cancelledAtUtc": "2026-04-04T14:02:00Z"
}
```

Response:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "status": "Cancelled"
}
```

### `GET /api/rides/{rideId}`
Return the current active ride session for rider tracking / resume.

Response:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "riderId": "user_123",
  "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
  "riderName": "Tanaka Mawere",
  "riderPhoneNumber": "+263771234567",
  "driverName": "John Dube",
  "driverPhoneNumber": "+263772112233",
  "vehicleInfo": "Toyota Aqua | ABC 1234",
  "startLocation": {
    "latitude": -17.8292,
    "longitude": 31.0522
  },
  "startAddress": "Avondale, Harare",
  "destinationLocation": {
    "latitude": -17.8311,
    "longitude": 31.0456
  },
  "destinationAddress": "Joina City Mall, Harare",
  "riderOfferAmount": 6.5,
  "recommendedAmount": 7.25,
  "acceptedAmount": 7.0,
  "selectedOfferId": "5cbd40cc-ccde-41db-b62f-a6ed95de7cb2",
  "distanceKm": 4.8,
  "estimatedMinutes": 11,
  "driverEtaMinutes": 3,
  "driverCurrentLocation": {
    "latitude": -17.8284,
    "longitude": 31.0490
  },
  "driverStatusNote": "Driver is heading to pickup",
  "riderRating": null,
  "riderFeedback": "",
  "requestedAtUtc": "2026-04-04T14:00:00Z",
  "acceptedAtUtc": "2026-04-04T14:01:11Z",
  "completedAtUtc": null,
  "status": "DriverEnRoute"
}
```

### `GET /api/rides/{rideId}/status`
Return the current ride state only.

Response:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "status": "DriverArrived",
  "updatedAtUtc": "2026-04-04T14:08:12Z"
}
```

### `GET /api/rides/{rideId}/track`
Return the driver's latest known location for fallback polling.

Response:
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

### `POST /api/rides/{rideId}/sos`
Create an emergency / safety incident from the rider side.

This endpoint is required for the rider flow even if the UI is still being finalized.

Request:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "triggeredBy": "Rider",
  "riderId": "user_123",
  "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
  "tripStatus": "TripStarted",
  "currentLocation": {
    "latitude": -17.8302,
    "longitude": 31.0474
  },
  "timestampUtc": "2026-04-04T14:12:30Z",
  "message": "Rider pressed SOS in app."
}
```

Response:
```json
{
  "incidentId": "sos_17dc8b92",
  "status": "Received",
  "receivedAtUtc": "2026-04-04T14:12:31Z"
}
```

Suggested backend behavior:
- persist the SOS event
- alert internal operations tooling
- optionally dispatch emergency contacts / support workflows
- emit a rider-facing realtime acknowledgement

### `POST /api/rides/rating`
Persist rider rating after trip completion.

Request:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "riderId": "user_123",
  "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
  "rating": 5,
  "feedback": "Driver was polite and arrived quickly.",
  "submittedAtUtc": "2026-04-04T14:25:00Z"
}
```

Response:
```json
{
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "ratingSaved": true
}
```

## WebSocket / Realtime Contract

## Connection
Suggested websocket endpoint:

```text
wss://api.example.com/ws/rides
```

The app should connect after authentication and then subscribe to rider ride updates.

Suggested auth options:
- bearer token in websocket handshake
- or short-lived websocket token created from the normal API token

## Rider-initiated realtime actions

### `StartRiderMatching`
Sent after the ride request is accepted over REST so the backend can begin pushing offers.

Payload:
```json
{
  "type": "StartRiderMatching",
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "riderId": "user_123"
}
```

### `AcceptOffer`
Sent when the rider accepts a driver offer.

Payload:
```json
{
  "type": "AcceptOffer",
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "rideOfferId": "5cbd40cc-ccde-41db-b62f-a6ed95de7cb2",
  "driverId": "6f7ca88a-53d0-4f5f-b878-a98c5355e1a0",
  "riderId": "user_123",
  "acceptedAmount": 7.0
}
```

### `Stop`
Sent when the rider stops listening for offers or leaves the flow.

Payload:
```json
{
  "type": "Stop",
  "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
  "riderId": "user_123"
}
```

## Rider-facing server events

### `RiderOfferReceived`
Sent whenever a driver accepts the rider's amount or sends a counteroffer.

Payload:
```json
{
  "type": "RiderOfferReceived",
  "data": {
    "rideOfferId": "5cbd40cc-ccde-41db-b62f-a6ed95de7cb2",
    "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
    "offerAmount": 7.0,
    "riderOfferAmount": 6.5,
    "recommendedAmount": 7.25,
    "isCounterOffer": true,
    "etaToPickupMinutes": 4,
    "distance": 1.4,
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

### `RideStatusUpdated`
Sent whenever the ride session status changes.

Payload:
```json
{
  "type": "RideStatusUpdated",
  "data": {
    "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
    "status": "DriverArrived",
    "statusMessage": "Your driver has arrived at pickup.",
    "etaMinutes": 0,
    "updatedAt": "2026-04-04T14:08:12Z"
  }
}
```

Statuses the rider app expects here:
- `DriverEnRoute`
- `DriverArrived`
- `TripStarted`
- `TripCompleted`
- `Cancelled`

### `DriverLocationUpdated`
Sent while the driver is approaching pickup and optionally during the trip.

Payload:
```json
{
  "type": "DriverLocationUpdated",
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

### `SosAcknowledged`
Recommended realtime acknowledgement after the rider triggers SOS.

Payload:
```json
{
  "type": "SosAcknowledged",
  "data": {
    "incidentId": "sos_17dc8b92",
    "rideId": "0f18c8fe1d1d458595c21c2e05e39f9f",
    "status": "Received",
    "message": "Emergency alert received. Support is being notified.",
    "updatedAtUtc": "2026-04-04T14:12:31Z"
  }
}
```

## Shared Payload Shapes

### `RideRequestModel`
```json
{
  "rideGuid": "guid",
  "riderId": "string",
  "riderName": "string",
  "riderPhoneNumber": "string",
  "startLocation": {
    "latitude": 0,
    "longitude": 0
  },
  "startAddress": "string",
  "destinationLocation": {
    "latitude": 0,
    "longitude": 0
  },
  "destinationAddress": "string",
  "offerAmount": 0,
  "recommendedAmount": 0,
  "estimatedDistanceKm": 0,
  "estimatedMinutes": 0,
  "isOrderingForSomeoneElse": false,
  "requestedForName": "string",
  "requestedAtUtc": "2026-04-04T14:00:00Z",
  "status": "Requested",
  "comments": "string"
}
```

### `DriverOfferSelectionModel`
```json
{
  "rideOfferId": "guid",
  "rideId": "string",
  "driver": {
    "driverId": "guid",
    "name": "string",
    "phoneNumber": "string",
    "rating": 4.8,
    "ridesCompleted": 487,
    "vehicle": "Toyota Aqua | ABC 1234"
  },
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
  "offerTime": "2026-04-04T14:00:45Z"
}
```

### `RideSessionModel`
```json
{
  "rideId": "string",
  "riderId": "string",
  "driverId": "guid",
  "riderName": "string",
  "riderPhoneNumber": "string",
  "driverName": "string",
  "driverPhoneNumber": "string",
  "vehicleInfo": "string",
  "startLocation": {
    "latitude": 0,
    "longitude": 0
  },
  "startAddress": "string",
  "destinationLocation": {
    "latitude": 0,
    "longitude": 0
  },
  "destinationAddress": "string",
  "riderOfferAmount": 0,
  "recommendedAmount": 0,
  "acceptedAmount": 0,
  "selectedOfferId": "guid",
  "distanceKm": 0,
  "estimatedMinutes": 0,
  "driverEtaMinutes": 0,
  "driverCurrentLocation": {
    "latitude": 0,
    "longitude": 0
  },
  "driverStatusNote": "string",
  "riderRating": null,
  "riderFeedback": "",
  "requestedAtUtc": "2026-04-04T14:00:00Z",
  "acceptedAtUtc": "2026-04-04T14:01:11Z",
  "completedAtUtc": null,
  "status": "DriverEnRoute"
}
```

### `RideStatusUpdateEvent`
```json
{
  "rideId": "string",
  "status": "DriverEnRoute",
  "statusMessage": "Driver selected. Heading to pickup now.",
  "etaMinutes": 6,
  "updatedAt": "2026-04-04T14:03:00Z"
}
```

### `RideRatingRequest`
```json
{
  "rideId": "string",
  "riderId": "string",
  "driverId": "guid",
  "rating": 5,
  "feedback": "string",
  "submittedAtUtc": "2026-04-04T14:25:00Z"
}
```

## Rider UI Expectations
The rider app currently expects these backend-visible milestones:

- once ride request succeeds, offers will begin arriving quickly over realtime
- accepted offers and counteroffers appear in one list
- once an offer is accepted, the rider can immediately load a full ride session
- the rider progress page updates from realtime events without reloading the page
- driver ETA should be included in both location and status updates whenever possible
- trip completion should be explicit and final so the app can route to rating
- SOS should be acknowledged immediately, even if follow-up handling is asynchronous

## Error Handling
Backend responses should be consistent across REST and realtime.

Recommended REST error shape:
```json
{
  "code": "ride_not_found",
  "message": "Ride could not be found.",
  "details": null
}
```

Suggested codes:
- `invalid_request`
- `ride_not_found`
- `offer_not_found`
- `offer_expired`
- `ride_already_accepted`
- `ride_already_completed`
- `ride_already_cancelled`
- `unauthorized`
- `forbidden`
- `driver_unavailable`
- `sos_unavailable`

## Open Items For Backend + Mobile Alignment
- Whether backend ride id should exactly match the client `rideGuid` or whether both ids should be stored
- Whether websocket messages are plain JSON envelopes or SignalR events
- Whether driver location should continue during `TripStarted` or only while approaching pickup
- Whether SOS should notify driver, only support staff, or both
- Whether cancellation rules differ before and after driver acceptance

## Current App Interfaces
This document aligns with the current app contracts in:
- [IRideApiClient.cs](/c:/Users/tanak/source/repos/Ridebase/Services/Interfaces/IRideApiClient.cs)
- [IRideRealtimeService.cs](/c:/Users/tanak/source/repos/Ridebase/Services/Interfaces/IRideRealtimeService.cs)
- [RideRequestModel.cs](/c:/Users/tanak/source/repos/Ridebase/Models/Ride/RideRequestModel.cs)
- [DriverOfferSelectionModel.cs](/c:/Users/tanak/source/repos/Ridebase/Models/Ride/DriverOfferSelectionModel.cs)
- [RideSessionModel.cs](/c:/Users/tanak/source/repos/Ridebase/Models/Ride/RideSessionModel.cs)
- [RideStatusUpdateEvent.cs](/c:/Users/tanak/source/repos/Ridebase/Models/Ride/RideStatusUpdateEvent.cs)
- [RideRatingRequest.cs](/c:/Users/tanak/source/repos/Ridebase/Models/Ride/RideRatingRequest.cs)

