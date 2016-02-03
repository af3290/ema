namespace MarketModels

module MathFunctions =
    open System
    open Operations
    open MathNet.Numerics.LinearAlgebra
    open MathNet.Numerics.Statistics
    open MathNet.Numerics.Distributions
    
    ///Shorthands for those long Array methods...
    
    ///Old boring arrays sum
    let sum (vec : float[]) : float =
        vec |> Array.sum

    ///Retrieves a new array formed by all the values at the specified indices
    let sub (vec : float[]) (indices : int[]) : float[] =
        Array.init indices.Length (fun i -> vec.[indices.[i]])

    ///Old boring mean...
    let mean (vec : float[]) : float =
        Statistics.Mean vec

    ///Sample standard deviation
    let stdev (vec : float[]) : float =
        Statistics.StandardDeviation vec

    ///Euclidean norm
    let L2Norm (vec : float[]) : float = 
        sqrt ( vec |> Array.map (fun x -> x * x) |> Array.sum )

    ///Takes first n elements, if n > len returns maximum, no error
    let takeFirstN (arr : float[]) (n : int) : float[] = 
        arr |> Seq.take (min arr.Length n) |> Seq.toArray

    ///takes last n elements before index m, if n > len returns maximum... so no error
    let takeLastNBeforeIndexM (arr : float[]) (n : int) (mIndex : int): float[] =
        if mIndex <= 0 || n <= 0 then 
            Array.empty<float>
        else
            let lastN = max 0 (mIndex - n)      // start at 1 to include mIndex as last first
            let toSkip = min arr.Length lastN   // no overflow
            let toTake = min mIndex n           // to take
            arr |> Seq.skip toSkip |> Seq. take toTake |> Seq.toArray

    //Serious methods follow:

    ///Corresponding to 1D digital filter from matlab... similar to moving average
    ///Follows this formula: 
    ///y(n) = ( b(0)*x(n) + b(1)*x(n-1) + ... + b(nb-1)*x(n-nb-1) - a(1)*y(n-1) - ... - a(na)*y(n-na-2) ) / a(0)
    let Filter1D (b : float[]) (a : float[]) (series : float[]) =
        if b.Length = 0 || a.Length = 0 || series.Length = 0 then
            failwith "Can't do"

        let nb = b.Length
        let na = a.Length

        let filteredSeries = Array.init series.Length (fun i -> 0.0)
        
        //maybe too slow...???
        for i in 0..filteredSeries.Length - 1 do
            //take last x values, x(i)...x(i-nb-1), reverse..., and first b coefficients
            let firstBs = takeFirstN b (i + 1)
            let lastXs = takeLastNBeforeIndexM series nb (i + 1) |> Array.rev
                        
            //take last y values, but before current, y(n-1)...y(n-na), and first a coefficients, skip a.[0] 
            let newa = a |> Seq.skip 1 |> Seq.toArray            
            let firstAs = takeFirstN newa i
            let lastYs = takeLastNBeforeIndexM filteredSeries (na - 1) i  |> Array.rev

            //compute the result
            filteredSeries.[i] <- (Array.sum (lastXs .* firstBs) - Array.sum (lastYs .* firstAs) ) / a.[0]                    

        filteredSeries

    ///Teoplitz matrix initialization, like in matlab
    let TeoplitzInit (column : float[]) (row : float[]) : float[,] =
        if column.Length <> row.Length then 
            failwith "Lengths disagree"

        Array2D.init column.Length row.Length (fun i j -> if i >= j then column.[i-j] else row.[-i+j])

    let SeriesAutocovariance (series : float[]) (lags : int) : float =
        Statistics.Covariance(series, series)

    ///Autocorrelation from matlab, faster than pearson, based on Fast Fourier Transform
    ///Includes autocorrelation with itself (i.e. 1)
    let SeriesAutocorrelationFFT (series : float[]) (lags : int) : float[] =
        if lags > series.Length then
            failwith "Can't..."

        let mean = series |> Array.average
        
        //transform to complex series
        let mutable cseries = series |> Array.map (fun x -> alglib.complex(x - mean))

        //Find first next power of 2 number
        let nextpowerof2 = (int)(2.0 ** ceil(log((float)series.Length)/log(2.0) - 1.0))
        
        alglib.fftc1d(&cseries, cseries.Length)
        
        //Multiply with conjugate
        let cF = cseries |> Array.map (fun x -> alglib.math.conj(x))
        let mutable acf = cseries |> Array.mapi(fun i x -> x * cF.[i])
        
        //Do the inverse
        alglib.fftc1dinv(&acf, cseries.Length)

        //Scale back and transform to real numbers, take lags+1 since it's 0 index based...
        let racf  = acf |> Seq.take (lags + 1) |> Seq.map (fun x -> x / acf.[0])  |> Seq.map (fun x -> x.x) |> Seq.toArray
        
        racf
        
    ///Basic autocorrelation method using Pearson's method, slightly slower
    ///Excludes autocorrelation with itself (i.e. 1)
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

