namespace MarketModels

module TimeSeries =
    //TODO: port from matlab... yes, awesome...
    open System
    open System.Collections.Generic    
    open MathFunctions

    //AR estimation function based on specific lags
    let AR2 (series : float[]) (nbForecastSteps :int) : float[] =
        let autocor = SeriesAutocorrelation series 2
        //etc...
        series

    let AR2Simulate (series : float[]) (nbForecastSteps :int) : float[] =
        series
    //todo... etc...
