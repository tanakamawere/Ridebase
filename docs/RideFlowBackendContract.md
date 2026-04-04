# Ride Flow Backend Contract

## Summary
This document captures the placeholder REST and realtime contracts the mobile app now expects for the rider and driver ride-hailing flow.

The flow is:
`home search -> quote -> rider request -> driver accept/counter -> rider selects driver -> driver en route -> driver arrived -> trip started -> trip completed -> rider rating`

## REST endpoints

### `POST /api/rides/request`
Create a rider request and start marketplace matching.

Request body:
```json
{
  "rideGuid": "string-guid",
  "riderId": "string",
  "riderName": "string",
  "riderPhoneNumber": "string",
  "startLocation": { "latitude": 0, "longitude": 0 },
  "startAddress": "string",
  "destinationLocation": { "latitude": 0, "longitude": 0 },
  "destinationAddress": "string",
  "offerAmount": 0,
  "recommendedAmount": 0,
  "estimatedDistanceKm": 0,
  "estimatedMinutes": 0,
  "requestedAtUtc": "2026-04-04T12:00:00Z",
  "comments": "string"
}
```

Response body:
```json
{
  "rideRequestId": "string",
  "rideStatus": "Requested",
  "rideDistance": 0,
  "estimatedWaitTime": 0
}
```

### `POST /api/rides/select-offer`
Persist the rider's selected driver offer.

### `POST /api/rides/rating`
Persist the rider's post-trip rating.

### `POST /api/rides/{rideId}/cancel`
Cancel a pending or active ride.

### `GET /api/rides/{rideId}`
Return the current `RideSessionModel`.

### `GET /api/rides/{rideId}/status`
Return the current `RideStatus`.

### `GET /api/rides/{rideId}/track`
Return the latest driver location.

## Realtime events

### Rider-facing
- `RiderOfferReceived`
  Used for both direct accepts and counteroffers from drivers.
- `RideStatusUpdated`
  Used for `DriverEnRoute`, `DriverArrived`, `TripStarted`, `TripCompleted`, and `Cancelled`.
- `DriverLocationUpdated`
  Used while the driver is moving toward pickup and optionally during the trip.

### Driver-facing
- `DriverRideRequestReceived`
  Sent when a rider request is broadcast to online drivers.

## Realtime client actions
- `StartRiderMatching`
- `SubmitDriverOffer`
- `AcceptOffer`
- `StartDriverRequestStream`
- `UpdateRideStatus`
- `PublishDriverLocation`
- `CompleteRide`
- `Stop`

## Shared payloads

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
  "driver": {
    "driverId": "guid",
    "name": "string",
    "phoneNumber": "string",
    "rating": 4.8,
    "ridesCompleted": 487,
    "vehicle": "Toyota Aqua"
  }
}
```

### `RideStatusUpdateEvent`
```json
{
  "rideId": "string",
  "status": "DriverEnRoute",
  "statusMessage": "Driver selected. Heading to pickup now.",
  "etaMinutes": 6,
  "updatedAt": "2026-04-04T12:00:00Z"
}
```

### `DriverLocationUpdate`
```json
{
  "rideId": "string",
  "driverId": "guid",
  "currentLocation": { "latitude": 0, "longitude": 0 },
  "etaMinutes": 4,
  "distanceToPickupKm": 1.2,
  "updatedAtUtc": "2026-04-04T12:00:00Z"
}
```

## State transitions
- `Requested -> SearchingDrivers`
- `SearchingDrivers -> OfferAccepted`
- `SearchingDrivers -> OfferCountered`
- `OfferAccepted -> DriverEnRoute`
- `DriverEnRoute -> DriverArrived`
- `DriverArrived -> TripStarted`
- `TripStarted -> TripCompleted`
- Any pre-completion state can move to `Cancelled`

## Notes
- The app now assumes pickup and destination display strings are first-class API fields, not derived client-only values.
- Counteroffers and direct accepts intentionally share one rider-offer list.
- Rider rating happens after `TripCompleted` and is posted separately from ride completion.
