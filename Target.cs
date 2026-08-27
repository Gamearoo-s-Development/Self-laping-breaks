using DV.ThingTypes;

namespace SelfLappingBrakes
{
    static class Target
    {
        public static bool Applies(TrainCar? car)
        {
            if (!Main.Enabled || car == null || !car.IsLoco)
            {
                return false;
            }

            switch (car.carType)
            {
                case TrainCarType.LocoDM3:
                    return Main.Settings.Dm3;
                case TrainCarType.LocoS060:
                    return Main.Settings.S060;
                case TrainCarType.LocoSteamHeavy:
                    return Main.Settings.S282;
                default:
                    return false;
            }
        }

        public static TrainCar? CarFrom(UnityEngine.Component? component)
        {
            if (component == null)
            {
                return null;
            }

            return component.GetComponent<TrainCar>()
                   ?? component.GetComponentInParent<TrainCar>()
                   ?? TrainCar.Resolve(component.gameObject);
        }
    }
}
