namespace MarketModels

module Forecast =
    open Operations
    open MathFunctions
    open MathNet.Numerics.Distributions
    open MathNet.Numerics.Statistics

    type SpikePreprocess = None | SimilarDay | Limited

    type ForecastResult = {
        //the resulting data from estimated parameters for the provided in-sample data, kinda "backcasting"
        Backcast : float[];
        //the n steps ahead prediced values from the estimated parameters
        Forecast : float[];
        //each pairs level, f.ex. 95%
        ConfidenceLevels: float[];
        //from highest to lowest prediction intervals
        Confidence : float[,] //because the inners have the same lengths! not array of arrays!
    }

    (*
        We have a nice scheme for confidence intervals... whenever an alpha is passed, do a logarithmic
        lower and upper alphas to generate 3 intervals, without letting the user provide them individually.

        alpha... alpha+/-25% from 1..100%

		for example:
			95% => 80%, 96%
			90% => 60%, 92%...
			and so on...
    *)
    let ConfidenceAlphaBounds (alpha : float) : float*float =
        (alpha - 0.25 * alpha, alpha + 0.25 * (1.0 - alpha))

    ///Increasing order of alpha...
    let NormalPredictionIntervals (avg : float[]) (stdevs : float[]) (alphas : float[]) : float[,] =
        if avg.Length <> stdevs.Length then
            failwith "Can't do it"

        let stdNorm = new Normal()

        Array2D.init (alphas.Length * 2) avg.Length (fun i j -> 
            if i < alphas.Length
            //go in inverse order through upper intervals, from highest to lowest
            then exp (avg.[j] + stdevs.[j] * stdNorm.InverseCumulativeDistribution(alphas.[-i + alphas.Length - 1])) 
            //go in direct order now
            else exp (avg.[j] - stdevs.[j] * stdNorm.InverseCumulativeDistribution(alphas.[i % alphas.Length]))
        )       

    ///Passes both the in and out of sample series and for last period builds the
    ///prediction intervals. Passes just the in-sample residuals
    let NormalPredictionIntervalsFromSeries (series : float[]) (residuals : float[]) (period : int) (cis : float[]) : float[,] = 
        if residuals.Length + period <> series.Length then
            failwith "Can't do it, must pass in-sample residuals and whole (in+out-sample) data"

        let nbPeriods = residuals.Length/period

        //kinda crappy... but OK for demo...
        let seasonalPeriods =  Array2D.init nbPeriods period (fun i j -> residuals.[i*period+j])

        let seasonalPeriodsAvgs = Array.init period (fun i -> seasonalPeriods.[*,i] |> mean)

        let logTrans = true
        let logFunc = if logTrans then (fun x -> log x) else (fun x -> x)
        let deLogFunc = if logTrans then (fun x -> x) else (fun x -> log x)

        //average those periods, log as well... out-of-sample part follows here...
        let avg = series.[series.Length - period..series.Length-1] |> Array.map (fun x -> logFunc x)
        
        //TODO: revise here...

        //find variations of residuals
        let stdevs = Array.init period (fun i -> seasonalPeriods.[*,i] |> Array.map (fun x -> logFunc(abs(x))) |> stdev)

        //convert the resulting intervals...
        NormalPredictionIntervals avg stdevs cis |> Array2D.map (fun x -> deLogFunc x)

    ///Averages over all seasonalities provided and returns the average as the next period's forecast
    ///while the min and max represent confidence bands
    let Naive (data : float[]) (seasonalities : int[]) (forecastSteps : int) (alpha : float) : ForecastResult =
        //assumes LogNormal distribution, so log all...
        let logData = data |> Array.map (fun x -> log x)
        
        //stack all previous periods (of forecastSteps length) for each seasonality provided together
        //e.g. if day ahead, then each day starting with previous and ending to this day from last year
        let seasonalPeriods =  Array2D.init seasonalities.Length forecastSteps (fun i j -> logData.[logData.Length - seasonalities.[i] + j])

        //average those periods
        let avg = Array.init forecastSteps (fun i -> seasonalPeriods.[*,i] |> mean)
        
        //find variations
        let stdevs = Array.init forecastSteps (fun i -> seasonalPeriods.[*,i] |> stdev)
        
        let alphaBounds = ConfidenceAlphaBounds alpha
        let cis = [|fst alphaBounds; alpha; snd alphaBounds|]

        let res = {
                Backcast = [||];
                Forecast = avg |> Array.map (fun x -> exp x);
                ConfidenceLevels = cis;
                Confidence = NormalPredictionIntervals avg stdevs cis
            }
        res

    ///Shorthand version from given
    let RMSE (forecasted : float[]) (realized : float[]) : float = 
        let residuals = realized .- forecasted
        let sqErrs = residuals ^^ 2.0

        sqrt(sqErrs|>Array.average)

    type FitStatistics = {
        Bias : float  //mean error
        RSquared : float
        IndependencePValue: float
        NormalityPValue: float
        //min, avg, max
        //avg values are RMSQ and MAPE...
        RSE : float[]
        APE : float[]
    }

    ///Calculates are relevant forecast fitting statistics: absolute errors, % errors, residual IID tests
    ///and R square...
    let ForecastFit (forecasted : float[]) (realized : float[]) : FitStatistics =
        let residuals = realized .- forecasted
        let ressd = Statistics.StandardDeviation residuals
        let resm = Statistics.Mean residuals

        //return % value
        let prcErrs = residuals./realized |> Array.map (fun x -> abs(100.0*x))
        let sqErrs = residuals ^^ 2.0
        
        let mutable normalityPValue = 0.0
        
        //determine the pValue for residuals' normality
        if forecasted.Length < 30 then
            //use T-Test
            let tval = resm / (ressd * sqrt((float)residuals.Length))
            let studt = new StudentT(0.0, ressd, (float)residuals.Length)
            //in case mean is negative, use abs and then negative
            let pVal = studt.CumulativeDistribution(-abs tval)
            normalityPValue <- pVal
        else
            //use Z-Test
            let zval = resm / (ressd * sqrt((float)residuals.Length))
            let norm = new Normal(resm, ressd)    
            let pVal = norm.CumulativeDistribution(-abs zval)
            normalityPValue <- pVal

        let autocorrLen = min 23 (residuals.Length - 1)
        let autocorr = SeriesAutocorrelation residuals autocorrLen
        
        let res = {
            Bias = forecasted .- realized |> Array.average;
            RSquared = MathNet.Numerics.GoodnessOfFit.RSquared(realized, forecasted)
            IndependencePValue = autocorr |> Array.max;
            NormalityPValue = normalityPValue;
            APE = [| prcErrs|>Array.min; prcErrs|>Array.average; prcErrs|>Array.max |];
            RSE = [| sqrt(sqErrs|>Array.min); sqrt(sqErrs|>Array.average); sqrt(sqErrs|>Array.max) |];
        }

        res