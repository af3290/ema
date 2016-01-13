namespace MarketModels

module MathFunctions =
    open System
    open MathNet.Numerics.Statistics
    open MathNet.Numerics.Distributions

    let SeriesAutocorrelation (series : float[]) (lags : int) : float[] =
        let len = series.Length

        //shift the series array by a place backwards for each lag, like
        //1..n, 
        //2..n, 1
        //3..n, 1, 2
        let seriesWithLags =  Array.init lags (fun lag -> 
            Array.concat([|Array.sub series lag (len-lag); Array.sub series 0 lag|])
        )

        let res = Correlation.PearsonMatrix(seriesWithLags)

        let autocorr = Array.init (lags - 1) (fun lag -> res.[lag + 1, 0])

        autocorr

    let TakeShortPeriods (data : float[]) (shortPeriodLength : int) (shortPeriodIndex : int) (longPeriodLength : int) : float[] =
        let nLongPeriods = data.Length/longPeriodLength
        
        //starting offest of a short period
        let shortPeriodOffset = shortPeriodIndex * shortPeriodLength

        //convert to a table of containing each short period at the provided index for all contained long periods
        let res = Array2D.init nLongPeriods shortPeriodLength (fun i j -> data.[i * longPeriodLength + shortPeriodOffset + j])

        let resLength = nLongPeriods * shortPeriodLength

        //flatten the result by concatenating each short period
        let flatRes = Array.init resLength (fun i -> res.[i / shortPeriodLength, i % shortPeriodLength])
        
        flatRes
    
    type HistogramFit = None | Normal | LogNormal

    //returns 3D rolling histogram from 1 to period...
    let SeasonalProbabilityDensities (data : float[]) (period : int)  (bins : int) (fit : HistogramFit): float[,] =
        let nPeriods = data.Length/period

        //transform the series to a matrix nPeriod * period
        let res = Array2D.init nPeriods period (fun i j -> data.[i * period + j])
        
        //calculat eeach period's mean                
        let periodMeans = Array.init nPeriods (fun i -> res.[i, *] |> Array.average)

        //take the means out, make it "stationary"
        let flattenedData = Array2D.init nPeriods period (fun i j -> res.[i, j] - periodMeans.[i])

        //find each period's histogram
        let hists = Array.init period (fun i -> new Histogram(flattenedData.[*, i], bins))
        
        let resHits = Array2D.init period bins (fun i j -> Convert.ToInt32(hists.[i].[j].Count))

        if fit = None then
            let histogramCountSums = Array.init period (fun i -> res.[i, *] |> Array.sum)
            let densities = resHits |> Array2D.mapi (fun i j x -> (float)x / (float)histogramCountSums.[i])
            densities
        elif fit = LogNormal then
            let densitieEstimates = Array.init period (fun i -> 
                let estimationData = res.[*, i]
                LogNormal.Estimate(estimationData)   
            )    
            
            let densities = resHits |> Array2D.mapi (fun i j x ->                
                let avg = [| hists.[i].[j].LowerBound; hists.[i].[j].UpperBound |] |> Array.average
                densitieEstimates.[i].Density(avg)
            )

            densities
        
        else
            let densitieEstimates = Array.init period (fun i -> 
                let estimationData = flattenedData.[*, i]
                new Normal(Statistics.Mean(estimationData), Statistics.StandardDeviation(estimationData))    
            )    
            
            let densities = resHits |> Array2D.mapi (fun i j x ->                
                let avg = [| hists.[i].[j].LowerBound; hists.[i].[j].UpperBound |] |> Array.average
                densitieEstimates.[i].Density(avg)
            )

            densities

    ///return a N-1 length front diffed series
    let DiffSeries (series : float[]) : float[] = 
        series |> Seq.skip 1 |> Seq.mapi(fun i x -> x / series.[i]) |> Seq.toArray

