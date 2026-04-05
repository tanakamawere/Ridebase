using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ridebase.Models.Ride;

public class RideRequestResponseModel
{
    public string RideRequestId { get; set; }
    public RideStatus RideStatus { get; set; }
    public double RideDistance { get; set; }
    public int EstimatedWaitTime { get; set; }
}
