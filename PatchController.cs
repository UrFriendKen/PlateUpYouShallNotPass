using Kitchen;
using KitchenData;
using KitchenMods;
using System.Collections.Generic;
using Unity.Entities;

namespace KitchenYouShallNotPass
{
    public class PatchController : GenericSystemBase, IModSystem
    {
        static PatchController instance;

        protected override void Initialise()
        {
            base.Initialise();
            instance = this;
        }

        protected override void OnUpdate()
        {
        }

        static HashSet<int> allowedFloorOccupants = new HashSet<int>()
        {
            377065033, // MopWater
            -1424385600, // MopWaterLong,
            1630557157, // BuffedFloor
            -1324288299, // MessCustomer1
            -374077567, // MessCustomer2
            147181555, // MessCustomer3
            31731938, // MessKitchen1,
            1419995156, // MessKitchen2,
            34773971, // MessKitchen3
        };

        internal static Entity GetOccupantWithFallbackAndExceptions(IntVector3 position, OccupancyLayer layer = OccupancyLayer.Default)
        {
            if (Session.CurrentGameNetworkMode != GameNetworkMode.Host)
                return default;
            
            Entity occupant = instance.TileManager.GetOccupant(position, layer);
            if (occupant == default)
            {
                occupant = instance.TileManager.GetOccupant(position, OccupancyLayer.Floor);
                if (occupant != default &&
                    instance.Require(occupant, out CAppliance appliance) &&
                    allowedFloorOccupants.Contains(appliance.ID))
                {
                    occupant = default;
                }
            }
            return occupant;
        }
    }
}
