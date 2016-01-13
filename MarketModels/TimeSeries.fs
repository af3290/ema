namespace MarketModels

module TimeSeries =
    //TODO: port from matlab... yes, awesome...
    open System
    open System.Collections.Generic    

    //AR estimation function based on specific lags
    let AR (series : float[]) (lags : int[]) : float[] =
        series
    //todo... etc...

    //holt winter's method
    let HWT (series : float[]) : float[] = 
        series