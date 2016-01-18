namespace MarketModels

module Forecast =
    open Operations
    open MathFunctions
    open MathNet.Numerics.Distributions
    open MathNet.Numerics.Statistics

    type SpikePreprocess = None | SimilarDay | Limited

    type ForecastResult = {
        Forecast : float[]
        Confidence : float[,] //because the inners have the same lengths! not array of arrays!
    }

    ///Averages over all seasonalities provided and returns the average as the next period's forecast
    ///while the min and max represent confidence bands
    let Naive (data : float[]) (seasonalities : int[]) (forecastSteps : int) (alpha : float) : ForecastResult =
        //data provided at starting of first largest season...???
        //average from all seasonalities.. make some histogram... return 95%..
        
        let seasonalPeriods = 
            Array2D.init seasonalities.Length forecastSteps (fun i j -> data.[data.Length - seasonalities.[i] + j])

        let avg = Array.init forecastSteps (fun i -> seasonalPeriods.[*,i] |> Array.average)

        let res = {
                Forecast = avg;
                Confidence = Array2D.init 2 forecastSteps (fun i j -> 
                    if i = 0 
                    then seasonalPeriods.[*,j] |> Array.max 
                    else seasonalPeriods.[*,j] |> Array.min)
            }

        res

    let NaiveMultivariate (data : float[]) (predictors : float[,])  (forecastSteps : int) (alpha : float) : ForecastResult =
        
        let res = {
                Forecast = [| 0.0 |];
                Confidence = Array2D.init 2 forecastSteps (fun i j -> 0.0)
            }

        res

    ///Shorthand version from given
    let RMSE (forecasted : float[]) (realized : float[]) : float = 
        let residuals = realized -- forecasted
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
        let residuals = realized -- forecasted
        let ressd = Statistics.StandardDeviation residuals
        let resm = Statistics.Mean residuals

        let prcErrs = residuals./realized |> Array.map abs
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
            Bias = forecasted -- realized |> Array.average;
            RSquared = MathNet.Numerics.GoodnessOfFit.RSquared(realized, forecasted)
            IndependencePValue = autocorr |> Array.max;
            NormalityPValue = normalityPValue;
            APE = [| prcErrs|>Array.min; prcErrs|>Array.average; prcErrs|>Array.max |];
            RSE = [| sqrt(sqErrs|>Array.min); sqrt(sqErrs|>Array.average); sqrt(sqErrs|>Array.max) |];
        }

        res