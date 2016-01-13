namespace Utils.NordPoolSpot
{
    public enum Currency { DKK, SEK, NOK, EUR}
    public enum DataItem {
        Production, Production_Prognosis,
        Consumption, Consumption_Prognosis,
        Wind_Power, Wind_Power_Prognosis,
        Elspot_Prices, Elspot_Volumes, Elspot_Flows,
        Hydro_Reservoir
    }
}