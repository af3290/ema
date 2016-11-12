namespace MarketModels

module MathFunctions =
    open System
    
    open MathNet    
    open MathNet.Numerics
    open MathNet.Numerics.LinearAlgebra
    open MathNet.Numerics.Statistics
    open MathNet.Numerics.Distributions
    
    open Operations

    ///Creates a matrix of length N filled with indicator values of shortPeriod length at each longPeriod 
    let IndicatorVariablesMatrix (N : int) (longPeriod : int) (shortPeriod : int) (periodIndices : int[]) : float[,] =
        let X = Array2D.zeroCreate N periodIndices.Length

        //indicator values, 1
        let ones = (Array.zeroCreate shortPeriod) .+. 1.0 

        //function to provide de index of the ith short period at the jth long period
        let idxStart i j = j*longPeriod + periodIndices.[i]*shortPeriod

        //the amount of periods included in the total array
        let nLen = N / longPeriod
         
        for j in 0..nLen-1 do
            for i in 0..periodIndices.Length-1 do
                let fromIdx = idxStart i j
                let toIdx = idxStart i j + (shortPeriod-1)
                X.[fromIdx .. toIdx, i] <- ones;

        X
    
    let sin (x : float) = Trig.Sin x
    let cos (x : float) = Trig.Cos x
    
    let zeros (rows : int) (columns : int) : float[,] =
        Array2D.init rows columns (fun i j -> 0.0)

    let ones (rows : int) (columns : int) : float[,] =
        Array2D.init rows columns (fun i j -> 1.0)

    ///Old boring arrays sum
    let sum (vec : float[]) : float =
        vec |> Array.sum

    ///Old boring arrays sum
    let sum2D (mat : float[,]) : float =
        Array.init (mat.GetLength(0)) (fun i -> mat.[i, *] |> sum) |> sum

    ///Reverses the matrix on rows or columns direction
    let rev2D (mat : float[,]) (onRows : bool) : float[,] = 
        let N = mat.GetLength(0)
        let M = mat.GetLength(1)
        let iIdx i = if onRows then N - i - 1 else i
        let jIdx j = if onRows then j else M - j - 1
        Array2D.init N M (fun i j -> mat.[iIdx i, jIdx j])

    ///Retrieves a new array formed by all the values after and including the specified index row.
    let afterRows2D (vec : float[,]) (index : int) : float[,] =
        vec.[index..vec.GetLength(0) - 1, *]

    let firstRows2D (mat : float[,]) (n : int) : float[,] =
        mat.[0..n - 1, *]

    ///Returns the last n rows
    let lastRows2D (mat : float[,]) (n : int) : float[,] =
        mat.[mat.GetLength(0) - n..mat.GetLength(0) - 1, *]

    ///Concatenates matrices as per rows or per columns
    let concat2D (mat1 : float[,]) (mat2 : float[,]) (toRows : bool) : float[,] = 
        if toRows then
            if mat1.GetLength(1) <> mat2.GetLength(1) then
                failwith "Column counts must be equal"

            Array2D.init (mat1.GetLength(0) + mat2.GetLength(0)) (mat1.GetLength(1)) 
                (fun i j -> if i < mat1.GetLength(0) then mat1.[i, j] else mat2.[i - mat1.GetLength(0), j])
        else
            if mat1.GetLength(0) <> mat2.GetLength(0) then
                failwith "Row counts must be equal"

            Array2D.init (mat1.GetLength(0)) (mat1.GetLength(1) + mat2.GetLength(1)) 
                (fun i j -> if j < mat1.GetLength(1) then mat1.[i, j] else mat2.[i , j - mat1.GetLength(1)])

    ///Retrieves a new array formed by all the values after and including the specified index.
    let after (vec : float[]) (index : int) : float[] =
        vec.[index..vec.Length - 1]

    ///Retrieves a new array formed by all the values strictly before the specified index, thereby excluding it.
    let before (vec : float[]) (index : int) : float[] =
        vec.[0..index - 1]

    //Initializes an new array of n length with the specified value, aking to zeroCreate
    let valueCreate (n : int) (value : float) : float[] =
        Array.init n (fun i -> value)

    ///Retrieves a new array formed by all the values at the specified indices
    let sub (vec : float[]) (indices : int[]) : float[] =
        Array.init indices.Length (fun i -> vec.[indices.[i]])

    ///Old boring mean...
    let mean (vec : float[]) : float =
        Statistics.Mean vec

    ///Old boring variance...
    let var (vec : float[]) : float =
        Statistics.Variance vec

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

    //TODO: test this filter...
    ///Simpler version of above, assume a = [|1.0|], therefore it has only the x component:
    ///y(n) = b(1)*x(n) + b(2)*x(n-1) + ... + b(nb+1)*x(n-nb) which can be written
    ///... b(nb+1)...b(1)... etc...
    let Filter1D1 (b : float[]) (series : float[]) =
        if b.Length = 0 || series.Length = 0 then
            failwith "Can't do"

        let nb = b.Length

        //pre revert to match, so we don't revert Xs... faster
        let revb = Array.rev b

        //include current index or not?? not for starters...
        let filteredSeries = Array.init series.Length (fun index ->
            match index with
            //3 cases
            |i when i >= nb -> 
                revb .* Array.sub series (i - nb) nb |> sum
            //intermediary, can't get the full b coeffs, revert and match to last most recent b coeffs
            |i when i > 0 -> 
                let lastBs = after b (nb-i)
                let lastXs = Array.sub series 0 i
                (Array.rev lastBs) .* lastXs |> sum
            |0 -> series.[0]
        )
        
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

    ///Returns a matrix where each column is the series lagged by each given lag value
    ///Initial values are NaN, the rest of series is truncated
    let LaggedMatrix (series : float[]) (lags : int[]) : float[,] =
        Array2D.init series.Length lags.Length (fun i j -> if i < lags.[j] then nan else series.[i-lags.[j]]) 

    type MovingAverageType = Simple | Exponential
    
    ///Simple alternative to Filter1D... MAt = Average(St-1..St-lag)
    let MovAvg (series : float[]) (lag : int) : float[] =
        Array.init series.Length (fun i-> if i = 0 then series.[0] else series.[max 0 (i-lag)..i-1] |> mean)

     ///Returns a moving standard deviation with nans for lag
    let MovingStandardDeviationInner (series : float[]) (maType : MovingAverageType) (lag : int) (dim : int) : float[] =
        let nansIndex = 2*lag - dim 

        if series.Length < nansIndex then
            failwith "Not enough observations, the result will be filled with nans"
                
        let filteredSeries = MovAvg series lag  
        
        //squared residuals of the first moving average
        let residuals = (series .- filteredSeries) ^^ 2.0
        
        //filter residuals again
        let result = MovAvg residuals lag
        
        //square results and preappend nans...
        Array.concat [|valueCreate nansIndex nan ; after result nansIndex|]  ^^ 0.5

    ///Returns a moving standard deviation with no nans, by using backward estimation
    let MovingStandardDeviation (series : float[]) (maType : MovingAverageType) (lag : int) (dim : int) : float[] =
        let nansIndex = 2*lag - dim 

        //calculate backwards then revert the result
        let preRes = Array.rev (MovingStandardDeviationInner (Array.rev series) maType lag dim)
        //calculate forward the rolling SD
        let res = MovingStandardDeviationInner series maType lag dim

        Array.concat [|before preRes nansIndex ; after res nansIndex |] 